# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `IZoneSortable.SortOrderBias` — integer offset added on top of the zone's first sorting layer, so sortables inside a zone can pick a slot and sortables on a boundary (walls, fences, doors) can land exactly on the boundary order.
- `BoundaryZoneSortable` — `IStaticZoneSortable` that derives its `SortPosition` from a serialized `ZoneSortingLine` (midpoint, nudged onto the back side). Auto-sets `SortOrderBias` to `stride - 1` so it lands on the zone boundary. Drop this on a wall sprite to handle the boundary recipe automatically.
- `ZoneSortingService.ZoneOrderStride` / `IZoneSortingService.ZoneOrderStride` — inspector-configurable distance between adjacent zone boundaries. Defaults to `10`; passed into `ZoneGraph` at build time and exposed on the service interface for sortables that need it.
- `ZoneGraph` exposes `ZoneOrderStride` as an instance property and accepts it as a constructor argument.
- `IDynamicZoneSortable` and `IStaticZoneSortable` — marker interfaces extending `IZoneSortable`. Dynamic sortables are re-resolved every `LateUpdate`; static sortables are stamped once on registration (if the graph exists) and again on every `RebuildZones()`, skipping the per-frame loop entirely. Rooms full of stationary boundary geometry now pay O(moving sortables) per frame instead of O(all sortables).
- `ZoneSortingService.RebuildAllZones()` — static convenience wrapper that locates the active service and calls `RebuildZones()`, so callers don't need to hold a reference for the common single-service case.

### Changed

- **Renamed** `ZoneSortable` → `DynamicZoneSortable`. The file's `.meta` GUID is preserved, so existing scene references continue to resolve. If you reference the class by name in code, rename accordingly.
- Zones are assigned sorting orders `depth · stride + 1` (so with the default stride of 10: `1, 11, 21, …`) instead of `0, 1, 2, …`. The stride multiples (`0, stride, 2·stride, …`) are reserved for zone boundaries; each zone occupies the `stride - 1` integers between adjacent boundaries. If your project reads `SortingOrderInLayer` directly, use `(order - 1) / ZoneOrderStride` to recover the old depth value.
- `IZoneSortingService.Register` / `Unregister` now take either `IDynamicZoneSortable` or `IStaticZoneSortable` — the unqualified `IZoneSortable` overload is gone. Built-in sortables (`DynamicZoneSortable`, `BoundaryZoneSortable`) already pick the right one; custom `IZoneSortable` implementations need to pick which marker to implement.

## [0.1.0] - 2026-04-17

### Added

- Initial release.
- `ZoneSortingService` — registers sortables and assigns their `sortingOrder` each `LateUpdate` based on a zone graph. Builds the graph on `Awake` by default; toggle `Rebuild Zones On Awake` off and call `RebuildZones()` manually if you load content additively.
- `ZoneSortingLine` — authoring component that partitions the scene into front/back sides.
- `ZoneSortable` + `IZoneSortable` — marks objects whose depth should be driven by the zone graph.
- `ZoneGraph` — computes zones from sorting lines and runs a topological sort (Kahn's algorithm) over the resulting DAG.
- `[SortingLayer]` attribute + editor PropertyDrawer — renders a string field as a dropdown populated from the project's sorting layers.
- Gizmos: `ZoneSortingLineGizmos` (per-line) and `ZoneSortingGizmos` (zone overlay).
- Demo scene (importable via Package Manager → Samples).
