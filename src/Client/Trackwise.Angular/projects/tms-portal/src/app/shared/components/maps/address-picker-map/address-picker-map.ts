import {
  booleanAttribute,
  Component,
  computed,
  inject,
  input,
  output,
  signal,
} from "@angular/core";
import type { GeoPoint } from "@trackwise/shared/api";
import { Icon } from "@trackwise/shared/ui";
import type { LngLatLike, Map, MapMouseEvent } from "mapbox-gl";
import { MapComponent, MarkerComponent } from "ngx-mapbox-gl";
import { MapStyleService } from "@/core/services";
import { environment } from "@/env";
import { MapResizeDirective } from "@/shared/directives";

/**
 * Bare single-pin map used by the address picker dialog. Renders one draggable marker
 * (or none, if `marker` is `null`) on a tenant-aware default center. The parent
 * controls the marker position and the dialog's `flyTo()` previews geocoded addresses.
 */
@Component({
  selector: "app-address-picker-map",
  templateUrl: "./address-picker-map.html",
  imports: [Icon, MapComponent, MapResizeDirective, MarkerComponent],
})
export class AddressPickerMap {
  private readonly mapStyleService = inject(MapStyleService);

  protected readonly accessToken = environment.mapboxToken;
  protected readonly mapStyle = this.mapStyleService.currentStyle;

  /** Initial map center. */
  public readonly center = input.required<GeoPoint>();

  /** Current pin position; `null` means no pin yet. */
  public readonly marker = input.required<GeoPoint | null>();

  /** Whether the marker is draggable. */
  public readonly draggable = input(true, { transform: booleanAttribute });

  /** Map container height. */
  public readonly height = input<string>("100%");

  /** Emitted when the user clicks empty map space. */
  public readonly mapClick = output<GeoPoint>();

  /** Emitted when the user drags the marker. */
  public readonly markerDrag = output<GeoPoint>();

  protected readonly mapInstance = signal<Map | null>(null);

  /** Mapbox-compatible center: GeoPoint → [lng, lat] */
  protected readonly mapCenter = computed<LngLatLike>(() => {
    const c = this.center();
    return [c.longitude ?? 121, c.latitude ?? 14];
  });

  /** Mapbox-compatible marker lngLat: GeoPoint → [lng, lat] */
  protected readonly markerLngLat = computed<LngLatLike | null>(() => {
    const pin = this.marker();
    if (!pin) return null;
    return [pin.longitude ?? 0, pin.latitude ?? 0];
  });

  /**
   * Smoothly animates the map to a given coordinate. Called by the parent dialog when
   * forward geocoding previews the user's typed address.
   */
  public flyTo(point: GeoPoint): void {
    const map = this.mapInstance();
    if (!map) return;

    map.flyTo({
      center: [point.longitude ?? 0, point.latitude ?? 0],
      zoom: 14,
      duration: 1200,
      essential: true,
    });
  }

  protected onMapLoad(map: Map): void {
    this.mapInstance.set(map);
  }

  /**
   * Clicks on waypoints/segments in the map's other layers would never reach here;
   * this fires for clicks on empty space only.
   */
  protected onMapClick(evt: MapMouseEvent): void {
    const { lng, lat } = evt.lngLat;
    this.mapClick.emit({ longitude: lng, latitude: lat });
  }
}
