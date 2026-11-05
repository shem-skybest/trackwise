import { HttpClient, HttpErrorResponse } from "@angular/common/http";
import { Component, computed, effect, inject, input, model, output, signal } from "@angular/core";
import type { GeoPoint } from "@trackwise/shared/api";
import type { AppError } from "@trackwise/shared/errors";
import { Icon } from "@trackwise/shared/ui";
import type { LineString } from "geojson";
import type { LngLatLike, Map, MapMouseEvent } from "mapbox-gl";
import {
  GeoJSONSourceComponent,
  LayerComponent,
  MapComponent,
  PopupComponent,
} from "ngx-mapbox-gl";
import { firstValueFrom } from "rxjs";
import { MapStyleService } from "@/core/services";
import { TenantService } from "@/core/services/tenant.service";
import { environment } from "@/env";
import { MapResizeDirective } from "@/shared/directives";
import type { MapboxDirectionsResponse } from "@/shared/types/mapbox";
import { MapContainer } from "../map-container/map-container";
import { MapControls } from "../map-controls/map-controls";
import { DEFAULT_MAP_CHROME, formatDistanceMiles, formatDuration, type MapChrome } from "../types";
import type {
  MapClickEvent,
  RouteChangeEvent,
  RouteLegMode,
  RouteSegmentClickEvent,
  RouteSegmentInfo,
  SegmentFeature,
  SegmentLayer,
  Waypoint,
  WaypointClickEvent,
  WaypointFeature,
} from "./types";

/**
 * Wire shape of `GET routes/segments` (`RouteController`/`FerryAwareRouteDto` on the API). A plain
 * driving route comes back as a single `driving` leg; a PH inter-island pair comes back as
 * drive/ferry/drive - see `IFerryRouteResolver`. Hand-typed rather than generated: this repo's
 * generated client is regenerated from a checked-in `openapi.json` snapshot, same as
 * `shared/types/mapbox.ts` hand-types the external Mapbox response shapes.
 */
interface RouteSegmentsResponse {
  legs: {
    mode: RouteLegMode;
    from: GeoPoint;
    to: GeoPoint;
    distanceMeters: number;
    durationSeconds: number;
    label?: string;
  }[];
}

@Component({
  selector: "app-direction-map",
  templateUrl: "./direction-map.html",
  imports: [
    GeoJSONSourceComponent,
    Icon,
    LayerComponent,
    MapComponent,
    MapContainer,
    MapControls,
    MapResizeDirective,
    PopupComponent,
  ],
})
export class DirectionMap {
  private readonly http = inject(HttpClient);
  private readonly mapStyleService = inject(MapStyleService);
  private readonly tenantService = inject(TenantService);

  protected readonly accessToken = environment.mapboxToken;

  /** Region-aware default center: US → continental US; EU → central Europe; PH → Philippines. */
  private readonly defaultCenter = computed<LngLatLike>(() => {
    switch (this.tenantService.tenantData()?.settings?.region) {
      case "eu":
        return [10, 50];
      case "ph":
        return [122, 13];
      default:
        return [-95, 35];
    }
  });
  private readonly defaultZoom = computed<number>(() => {
    switch (this.tenantService.tenantData()?.settings?.region) {
      case "eu":
        return 4;
      case "ph":
        return 4;
      default:
        return 3;
    }
  });
  protected readonly segmentColors = [
    "#28a745", // green
    "#0074D9", // blue
    "#FF4136", // red
    "#FF851B", // orange
    "#B10DC9", // purple
  ];

  /** Mapbox style URL based on current layer and theme */
  protected readonly mapStyle = this.mapStyleService.currentStyle;

  // States
  protected readonly loading = signal(false);
  protected readonly error = signal<AppError | null>(null);
  protected readonly mapInstance = signal<Map | null>(null);
  protected readonly segments = signal<SegmentLayer[]>([]);
  protected readonly waypointsData = signal<WaypointFeature | null>(null);
  protected readonly waypointHighlight = signal<WaypointFeature | null>(null);
  protected readonly segmentHighlight = signal<SegmentFeature | null>(null);
  protected readonly bounds = signal<[LngLatLike, LngLatLike] | null>(null);
  protected readonly hoveredSegment = signal<SegmentLayer | null>(null);

  /** Whether the map has no valid waypoints */
  protected readonly isEmpty = computed(() => {
    const pts = this.waypoints();
    return !pts || pts.length < 2 || !this.coordsAreValid(pts);
  });

  /** The center coordinates of the map. Region-aware default ([-95, 35] for US, [10, 50] for EU, [122, 13] for PH). */
  public readonly center = model<LngLatLike>(this.defaultCenter());

  /** The zoom level of the map. Region-aware default (3 for US, 4 for EU/PH). */
  public readonly zoom = model<number>(this.defaultZoom());

  /** Array of stop coordinates in order. */
  public readonly waypoints = input<Waypoint[]>([]);

  /** Selected waypoint for highlighting. */
  public readonly selectedWaypoint = input<Waypoint | null>(null);

  /** Color used for highlights. */
  public readonly highlightColor = input<string>("#111");

  /** The height of the map container. Default is 100%. */
  public readonly height = input<string>("100%");

  /** Chrome (width/controls/layer toggle) options. */
  public readonly chrome = input<MapChrome>({});

  protected readonly chromeCfg = computed(() => ({ ...DEFAULT_MAP_CHROME, ...this.chrome() }));

  /** Emitted when the route changes. */
  public readonly routeChange = output<RouteChangeEvent>();

  public readonly routeSegmentClick = output<RouteSegmentClickEvent>();
  public readonly waypointClick = output<WaypointClickEvent>();
  /** Emitted on a click in empty map space (not on a waypoint or segment layer). */
  public readonly mapClick = output<MapClickEvent>();

  constructor() {
    effect(() => this.draw());

    // respond to selectedWaypoint changes
    effect(() => {
      const wp = this.selectedWaypoint();
      if (!wp) {
        return this.clearHighlight();
      }
      this.selectSegmentByWaypoint(wp.id);
    });
  }

  protected onMapLoad(map: Map): void {
    this.mapInstance.set(map);
  }

  /**
   * Fires for any click in empty map space. Waypoint and segment clicks are handled by
   * `onWaypointClick` / `onSegmentClick` and never reach the underlying `(mapClick)` because
   * those layers absorb the event first.
   */
  protected onMapClick(evt: MapMouseEvent): void {
    const { lng, lat } = evt.lngLat;
    this.mapClick.emit({ point: { longitude: lng, latitude: lat } });
  }

  /**
   * Smoothly animates the map to the given coordinate. Call this from the parent form when
   * the user picks an address from autocomplete so the map is already centred on the area
   * before they click to set the pin.
   */
  public flyTo(point: GeoPoint): void {
    const map = this.mapInstance();
    if (!map) return;

    const center: LngLatLike = [point.longitude ?? 0, point.latitude ?? 0];
    map.flyTo({
      center,
      zoom: 13,
      duration: 1500,
      essential: true,
    });
  }

  /** Public method to fit map bounds to all waypoints */
  public fitToBounds(): void {
    const pts = this.waypoints();
    if (pts && pts.length >= 2) {
      this.fitAndCenter(pts);
    }
  }

  /** Retry loading after an error */
  protected handleRetry(): void {
    this.error.set(null);
    this.draw();
  }

  protected setCursor(cursor: string, evt: MapMouseEvent): void {
    evt.target.getCanvas().style.cursor = cursor;
  }

  protected onSegmentClick(_evt: MapMouseEvent, seg: SegmentLayer): void {
    this.applySegmentHighlight(seg);

    this.routeSegmentClick.emit({
      fromWaypoint: seg.fromWaypoint,
      toWaypoint: seg.toWaypoint,
    });
  }

  protected onSegmentMouseEnter(evt: MapMouseEvent, seg: SegmentLayer): void {
    this.setCursor("pointer", evt);
    this.hoveredSegment.set(seg);
  }

  protected onSegmentMouseLeave(evt: MapMouseEvent): void {
    this.setCursor("", evt);
    this.hoveredSegment.set(null);
  }

  protected onWaypointClick(evt: MapMouseEvent): void {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const waypointId = (evt.features?.[0] as any)?.properties?.id as string | undefined;
    const waypoint = this.waypoints().find((w) => w.id === waypointId);

    if (!waypoint) {
      return;
    }
    this.waypointClick.emit({ waypoint });
    this.selectSegmentByWaypoint(waypoint.id);
  }

  /** Format segment info for tooltip display */
  protected formatSegmentTooltip(seg: SegmentLayer): string {
    const distance = seg.data.properties?.distance ?? 0;
    const duration = seg.data.properties?.duration ?? 0;
    const miles = formatDistanceMiles(distance);
    const time = formatDuration(duration);

    if (seg.data.properties?.mode === "ferry") {
      return `Ferry crossing (est.) · ${miles} mi · ${time}`;
    }

    return `${miles} mi · ${time}`;
  }

  /**
   * Midpoint of the segment's own geometry, for hover-tooltip placement. Uses the drawn line
   * itself rather than the outer waypoints - an inter-island leg can render as multiple
   * drive/ferry/drive sub-segments per waypoint pair, so the outer waypoints' midpoint would
   * mis-place the tooltip for any sub-segment that isn't the whole leg.
   */
  protected segmentMidpoint(seg: SegmentLayer): [number, number] {
    const coords = seg.data.geometry.coordinates;
    const mid = coords[Math.floor(coords.length / 2)];
    return [mid[0], mid[1]];
  }

  /**
   * Draws the direction map.
   */
  private async draw(): Promise<void> {
    const pts = this.waypoints();

    if (!pts || pts.length < 2 || !this.coordsAreValid(pts)) {
      return this.clear();
    }

    this.loading.set(true);
    this.error.set(null);

    try {
      // build waypoint FeatureCollection with labels
      this.waypointsData.set(this.toWaypointFC(pts));

      // build one or more LineStrings per leg (a PH inter-island leg splits into drive/ferry/drive)
      const segs: SegmentLayer[] = [];
      for (let i = 0; i < pts.length - 1; i++) {
        const from = pts[i];
        const to = pts[i + 1];
        const features = await this.buildSegments(from.location, to.location);

        features.forEach((feature, subIndex) => {
          segs.push({
            layerId: `seg-${from.id}-${to.id}-${subIndex}`,
            data: feature,
            fromWaypoint: from,
            toWaypoint: to,
            legIndex: i,
          });
        });
      }
      this.segments.set(segs);

      // Fit bounds & center
      this.fitAndCenter(pts);

      // Emit route info (sum of legs, including any ferry sub-legs)
      const distance = segs.reduce((sum, s) => sum + (s.data.properties?.distance ?? 0), 0);
      const duration = segs.reduce((sum, s) => sum + (s.data.properties?.duration ?? 0), 0);
      const segmentInfos: RouteSegmentInfo[] = segs.map((s, i) => ({
        index: i,
        distance: s.data.properties?.distance ?? 0,
        duration: s.data.properties?.duration ?? 0,
        fromWaypointId: s.fromWaypoint.id,
        toWaypointId: s.toWaypoint.id,
        mode: s.data.properties?.mode ?? "driving",
      }));

      this.routeChange.emit({
        origin: pts[0].location,
        destination: pts.at(-1)!.location,
        distance,
        duration,
        segments: segmentInfos,
      });

      // Clear any previous selection
      this.clearHighlight();
    } catch (err) {
      const message =
        err instanceof HttpErrorResponse
          ? "Failed to load route directions. Please check your internet connection."
          : "An unexpected error occurred while loading the route.";

      this.error.set({
        category: "network",
        message,
        retryable: true,
        originalError: err,
      });
    } finally {
      this.loading.set(false);
    }
  }

  /**
   * Builds the LineString feature(s) for one leg between two waypoints. Mapbox's Directions API is
   * driving-only and has no ferry profile - for a PH inter-island pair (e.g. Manila -> Davao) it
   * still returns a fabricated road route across open ocean rather than an error. `routes/segments`
   * is the source of truth for whether this leg needs a ferry: a plain leg comes back as one
   * `driving` sub-leg (unchanged behavior, one Mapbox Directions call); an inter-island leg comes
   * back as multiple sub-legs and each `driving` sub-leg still gets its real road geometry from
   * Mapbox, while each `ferry` sub-leg is drawn as a straight crossing using the backend's real
   * distance/duration (see `IFerryRouteResolver` / `PhilippineFerryNetwork`) - no Mapbox call, since
   * there's no road to snap to.
   */
  private async buildSegments(a: GeoPoint, b: GeoPoint): Promise<SegmentFeature[]> {
    const url = `${environment.apiUrl}/routes/segments`;
    const params = {
      originLng: String(a.longitude),
      originLat: String(a.latitude),
      destinationLng: String(b.longitude),
      destinationLat: String(b.latitude),
    };
    const plan = await firstValueFrom(this.http.get<RouteSegmentsResponse>(url, { params }));

    return Promise.all(
      plan.legs.map((leg) =>
        leg.mode === "ferry" ? this.buildFerrySegment(leg) : this.buildDrivingSegment(leg),
      ),
    );
  }

  /** A real Mapbox-snapped driving route for one drive-only sub-leg. */
  private async buildDrivingSegment(
    leg: RouteSegmentsResponse["legs"][number],
  ): Promise<SegmentFeature> {
    const coords = `${leg.from.longitude},${leg.from.latitude};${leg.to.longitude},${leg.to.latitude}`;
    const url = `https://api.mapbox.com/directions/v5/mapbox/driving/${coords}?geometries=geojson&access_token=${this.accessToken}`;
    const res = await firstValueFrom(this.http.get<MapboxDirectionsResponse>(url));

    return {
      type: "Feature",
      geometry: res.routes?.[0]?.geometry as LineString,
      properties: {
        distance: res.routes?.[0]?.distance ?? leg.distanceMeters,
        duration: res.routes?.[0]?.duration ?? leg.durationSeconds,
        mode: "driving",
      },
    };
  }

  /** A straight crossing between two ports - there's no road to snap to, so no Mapbox call. */
  private buildFerrySegment(leg: RouteSegmentsResponse["legs"][number]): SegmentFeature {
    return {
      type: "Feature",
      geometry: {
        type: "LineString",
        coordinates: [
          [leg.from.longitude ?? 0, leg.from.latitude ?? 0],
          [leg.to.longitude ?? 0, leg.to.latitude ?? 0],
        ],
      },
      properties: {
        distance: leg.distanceMeters,
        duration: leg.durationSeconds,
        mode: "ferry",
        label: leg.label,
      },
    };
  }

  /**
   * Fits the map to the given waypoints.
   */
  private fitAndCenter(waypoints: Waypoint[]): void {
    const xs = waypoints.map((p) => p.location.longitude ?? 0);
    const ys = waypoints.map((p) => p.location.latitude ?? 0);
    const minX = Math.min(...xs);
    const minY = Math.min(...ys);
    const maxX = Math.max(...xs);
    const maxY = Math.max(...ys);

    this.bounds.set([
      [minX, minY],
      [maxX, maxY],
    ]);

    this.center.set({
      lng: (minX + maxX) / 2,
      lat: (minY + maxY) / 2,
    });
  }

  /** Prefer segment that STARTS at id; if none, take the one that ENDS at id (for last waypoint). */
  private selectSegmentByWaypoint(waypointId: string): void {
    const segs = this.segments();
    const byFrom = segs.find((s) => s.fromWaypoint.id === waypointId);
    const seg = byFrom ?? segs.find((s) => s.toWaypoint.id === waypointId);
    if (!seg) {
      return;
    }
    this.applySegmentHighlight(seg);
  }

  private applySegmentHighlight(seg: SegmentLayer): void {
    // highlight segment
    this.segmentHighlight.set(seg.data);

    // highlight endpoints
    const all = this.waypointsData()?.features ?? [];
    const selected = all.filter(
      (f) => f.properties?.id === seg.fromWaypoint.id || f.properties?.id === seg.toWaypoint.id,
    );

    this.waypointHighlight.set({
      type: "FeatureCollection",
      features: selected,
    });
  }

  private clearHighlight(): void {
    this.segmentHighlight.set(null);
    this.waypointHighlight.set(null);
  }

  /**
   * Clears the map and resets all state.
   */
  private clear(): void {
    this.segments.set([]);
    this.waypointsData.set(null);
    this.waypointHighlight.set(null);
    this.segmentHighlight.set(null);
    this.bounds.set(null);
    this.center.set(this.defaultCenter());
    this.zoom.set(this.defaultZoom());
  }

  /**
   * Converts an array of waypoints to a FeatureCollection.
   */
  private toWaypointFC(pts: Waypoint[]): WaypointFeature {
    return {
      type: "FeatureCollection",
      features: pts.map((p, i) => ({
        type: "Feature",
        geometry: {
          type: "Point",
          coordinates: [p.location.longitude ?? 0, p.location.latitude ?? 0],
        },
        properties: { id: p.id, label: String(i + 1) },
      })),
    };
  }

  /**
   * Validates if the given point is a valid GeoPoint.
   */
  private isValidPoint(point: Waypoint): boolean {
    const longitude = point.location.longitude ?? 0;
    const latitude = point.location.latitude ?? 0;

    // Ignore zero coordinates
    if (longitude === 0 && latitude === 0) {
      return false;
    }

    return longitude >= -180 && longitude <= 180 && latitude >= -90 && latitude <= 90;
  }

  /** Ensures every waypoint is within valid longitude/latitude bounds. */
  private coordsAreValid(pts: Waypoint[]): boolean {
    return pts.every((pt) => this.isValidPoint(pt));
  }
}
