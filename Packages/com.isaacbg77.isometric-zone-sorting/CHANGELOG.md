# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `IZoneSortable.SortOrderBias` — integer offset added on top of the zone's order, so sortables sitting on a zone boundary (walls, fences, doors) can render strictly between two zones and never tie with movers.
- `BoundaryZoneSortable` — `IZoneSortable` that derives its `SortPosition` from a serialized `ZoneSortingLine` (midpoint, nudged onto the back side) and defaults `SortOrderBias` to `1`. Drop this on a wall sprite to handle the boundary recipe automatically.
- `ZoneSortingService.ZoneOrderStride` — inspector-configurable gap between adjacent zones' sorting orders. Defaults to `10`; passed into `ZoneGraph` at build time.
- `ZoneGraph` exposes `ZoneOrderStride` as an instance property and accepts it as a constructor argument.

### Changed

- **Renamed** `ZoneSortable` → `DynamicZoneSortable`. The file's `.meta` GUID is preserved, so existing scene references continue to resolve. If you reference the class by name in code, rename accordingly.
- Zones are assigned sorting orders `0, stride, 2·stride, …` instead of `0, 1, 2, …`. If your project reads `SortingOrderInLayer` directly, divide by the graph's `ZoneOrderStride` to recover the old value.

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
