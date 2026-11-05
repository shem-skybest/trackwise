import type { GeoPoint } from "@trackwise/shared/api";
import type { Feature, FeatureCollection, LineString, Point } from "geojson";

/**
 * Fired when the map is clicked at an empty point (not on a waypoint or segment).
 */
export interface MapClickEvent {
  point: GeoPoint;
}

/**
 * Fired when a route segment is clicked.
 */
export interface RouteSegmentClickEvent {
  fromWaypoint: Waypoint;
  toWaypoint: Waypoint;
}

/**
 * Represents a waypoint click event.
 */
export interface WaypointClickEvent {
  waypoint: Waypoint;
}

/**
 * Represents a change in the route.
 */
export interface RouteChangeEvent {
  origin: GeoPoint;
  destination: GeoPoint;
  /** Total distance in meters */
  distance: number;
  /** Total duration in seconds */
  duration: number;
  /** Per-segment breakdown */
  segments: RouteSegmentInfo[];
}

/** How a segment is traversed - "ferry" legs are drawn/labeled distinctly from driving. */
export type RouteLegMode = "driving" | "ferry";

/**
 * Information about a single route segment.
 */
export interface RouteSegmentInfo {
  index: number;
  /** Distance in meters */
  distance: number;
  /** Duration in seconds */
  duration: number;
  fromWaypointId: string;
  toWaypointId: string;
  mode: RouteLegMode;
}

/**
 * Represents a waypoint on the map.
 */
export interface Waypoint {
  id: string;
  location: GeoPoint;
}

interface WaypointProps {
  id: string;
  label: string;
}

export type SegmentFeature = Feature<
  LineString,
  { distance: number; duration: number; mode: RouteLegMode; label?: string }
>;
export type WaypointFeature = FeatureCollection<Point, WaypointProps>;

/**
 * Represents a segment of the route in the directions map.
 */
export interface SegmentLayer {
  layerId: string; // seg-{from}-{to}-{subIndex}
  data: SegmentFeature; // GeoJSON line
  fromWaypoint: Waypoint;
  toWaypoint: Waypoint;
  /** Index of the waypoint-to-waypoint leg this belongs to - a leg can render as more than one
   *  `SegmentLayer` (e.g. drive/ferry/drive), and all of them share this so they render in the
   *  same color instead of cycling the palette per sub-segment. */
  legIndex: number;
}
