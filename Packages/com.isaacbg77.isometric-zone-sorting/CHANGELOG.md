# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `ZoneSortingPivot` — authoring component that defines an isometric plane origin: a single point with a configurable isometric angle and per-arm "V" vector lengths. Replaces the dual-point `ZoneSortingLine` system; boundary lines are now implied by the pivot's V-shaped axes. Draws its own scene gizmos (V arms, tip extents, front-facing normals).
- `BoundaryZoneSortable` — `IStaticZoneSortable` for geometry that sits on a sorting boundary (walls, fences, doors, railings). Derives its `SortPosition` and sorting axes from a referenced `ZoneSortingPivot` (a child pivot is auto-created when the component is added in the editor). The set of `BoundaryZoneSortable`s in the scene is what the service builds the zone graph from.
- `StaticZoneSortable` — `IStaticZoneSortable` for stationary objects that have no boundary of their own. Its order is resolved once at registration and again on every `RebuildZones()`, skipping the per-frame loop. Tilemap-aware: when placed on a `Tilemap`, `SortPosition` is the average position of all painted tiles (visualized with a gizmo when selected).
- `IDynamicZoneSortable`, `IStaticZoneSortable`, and `IBoundaryZoneSortable` — marker interfaces extending `IZoneSortable` that select the registration track. Dynamic sortables are re-resolved every `LateUpdate`; static and boundary sortables are stamped once, so rooms full of stationary geometry pay O(moving sortables) per frame instead of O(all sortables).
- `IZoneSortable.Renderers` — a `SortingGroup` is no longer required (`SortingGroup` is now nullable); when absent, sorting orders are written directly to the sortable's renderers.
- `IZoneSortingService.RebuildZones(Transform? root = null)` — rebuild is now part of the service interface. The optional root scopes discovery of boundary and static sortables to a hierarchy, useful for additively loaded content.
- `IZoneSortingService.AddSortableToRenderers(Transform? root = null)` — bulk-adds a `StaticZoneSortable` to every `SpriteRenderer`, `TilemapRenderer`, and `SortingGroup` on the zone sorting layer that isn't already covered by a sortable.
- `ZoneSortingService.prefab` — preconfigured drop-in service.

### Changed

- **Zone computation rewritten** to fix the O(2^N) complexity across N boundary lines. `ZoneGraph` no longer discovers zones by combining boundary-line signatures and topologically sorting a DAG. Instead, pivots are sorted by Y into back-to-front strips; a position is resolved by binary search over pivot Y plus a per-pivot V-shape front/back test. Graph build is O(N log N) and each lookup O(log N); dynamic sortables additionally cache their last pivot index (`IDynamicZoneSortable.CachedPivotIndex`), making per-frame resolution effectively O(1) for movers.
- **Renamed** `ZoneSortable` → `DynamicZoneSortable`. The file's `.meta` GUID is preserved, so existing scene references continue to resolve. If you reference the class by name in code, rename accordingly.
- `IZoneSortingService.Register` / `Unregister` now take either `IDynamicZoneSortable` or `IStaticZoneSortable` — the unqualified `IZoneSortable` overload is gone. Built-in sortables (`DynamicZoneSortable`, `StaticZoneSortable`, `BoundaryZoneSortable`) already pick the right one; custom `IZoneSortable` implementations need to pick which marker to implement.
- `RebuildZones()` now prunes destroyed sortables, rebuilds the graph from the scene's `BoundaryZoneSortable`s, re-registers static sortables found under the root, resets dynamic sortables' cached pivot indices, and re-applies orders to everything.
- `ZoneSortingService` only writes `sortingLayerID` / `sortingOrder` when the value actually changed, avoiding dirtying renderers every frame.
- Package assemblies renamed to `IsometricZoneSorting.Runtime` and `IsometricZoneSorting.Editor`.

### Removed

- `ZoneSortingLine` and its prefab — replaced by `ZoneSortingPivot` referenced from a `BoundaryZoneSortable`.
- `ZoneSortingGizmos` and `ZoneSortingLineGizmos` — `ZoneSortingPivot` draws its own gizmos.
- `ZoneSignature`, `ZoneDefinition`, and `SortingPoint` — internals of the old signature-based zone discovery.

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
