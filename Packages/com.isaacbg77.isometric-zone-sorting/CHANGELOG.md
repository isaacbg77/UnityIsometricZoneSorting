# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-04-17

### Added

- Initial release.
- `ZoneSortingService` — registers sortables and assigns their `sortingOrder` each `LateUpdate` based on a zone graph. Call `RebuildZones()` from your scene/room loader when the set of sorting lines changes; the service does not auto-build on `Awake`. The demo sample ships with a `RebuildZonesOnAwake` helper for convenience.
- `ZoneSortingLine` — authoring component that partitions the scene into front/back sides.
- `ZoneSortable` + `IZoneSortable` — marks objects whose depth should be driven by the zone graph.
- `ZoneGraph` — computes zones from sorting lines and runs a topological sort (Kahn's algorithm) over the resulting DAG.
- `[SortingLayer]` attribute + editor PropertyDrawer — renders a string field as a dropdown populated from the project's sorting layers.
- Gizmos: `ZoneSortingLineGizmos` (per-line) and `ZoneSortingGizmos` (zone overlay).
- Demo scene (importable via Package Manager → Samples).
