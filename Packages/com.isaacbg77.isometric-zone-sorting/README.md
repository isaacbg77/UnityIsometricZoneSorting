# Isometric Zone Sorting

A general-purpose depth sorting solution for 2D isometric Unity games.

Drop **sorting lines** into your scene to hand-author which side of each line renders in front of the other. At runtime, any object tagged with `ZoneSortable` gets a correct integer `sortingOrder` every frame — no per-object tweaking, no "sort by Y" hacks, no fighting with `SortingGroup` priorities.

## Install

In Unity's Package Manager → **+** → **Install package from git URL…**:

```
https://github.com/isaacbg77/UnityIsometricZoneSorting.git?path=/Packages/com.isaacbg77.isometric-zone-sorting
```

Or add to `Packages/manifest.json`:

```json
"com.isaacbg77.isometric-zone-sorting": "https://github.com/isaacbg77/UnityIsometricZoneSorting.git?path=/Packages/com.isaacbg77.isometric-zone-sorting"
```

**Requires Unity 6.0 or newer.**

## How it works

- A **`ZoneSortingLine`** is a line segment (two `SortingPoint` endpoints) plus a `FrontNormal` indicating which side renders on top.
- `N` lines partition the scene into up to `2^N` **zones**, one per front/back combination.
- A **`ZoneGraph`** builds a directed acyclic graph from these zones (zones differing by one line have an edge: back → front) and runs Kahn's topological sort to assign each zone an integer depth.
- A **`ZoneSortingService`** registers all `ZoneSortable` components in the scene. In `LateUpdate` it looks up each sortable's current zone and writes the resulting order into its `SortingGroup.sortingOrder`.

Cycles (contradictory line orientations) are detected and the affected zones fall back to a trailing order with a warning.

## Quickstart

1. **Create a sorting-layer asset.** Right-click in the Project window → *Create → Isometric Zone Sorting → Sorting Layer Reference* and set its `Sorting Layer Name` to a layer you've defined in *Project Settings → Tags and Layers*.
2. **Add a `ZoneSortingService`** to an empty GameObject in the scene. Drag the `SortingLayerReference` asset into the `Dynamic Sorting Layer` field.
3. **Author sorting lines.** Create a GameObject for each line with a `ZoneSortingLine` component, plus two child GameObjects with `SortingPoint` components as endpoints. Set `Front Normal` to point toward whichever side should render in front. Optional: add `ZoneSortingLineGizmos` for scene-view visualization.
4. **Tag sortable objects.** Add a `ZoneSortable` component to anything that needs dynamic depth. It requires a `SortingGroup` (auto-enforced) and uses `transform.position` as its sort position.

Import the **Demo Scene** sample via the Package Manager for a working example.

## Key types

| Type | Role |
| --- | --- |
| `IZoneSortable` | Contract: exposes a `SortingGroup` and a `SortPosition` |
| `ZoneSortable` | Default MonoBehaviour implementation |
| `IZoneSortingService` / `ZoneSortingService` | Registers sortables and writes sorting orders each frame |
| `ZoneSortingLine` / `SortingPoint` | Authoring components that define zone boundaries |
| `ZoneGraph` | Computes zones, builds the DAG, runs the topological sort |
| `ZoneSignature` / `ZoneDefinition` | Immutable zone identity and resolved order |
| `SortingLayerReference` | ScriptableObject holding a sorting layer name |
| `ZoneSortingGizmos` / `ZoneSortingLineGizmos` | Editor visualization |

## Notes

- `ZoneSortingService.RebuildZones()` runs on `Awake`. If you load rooms/scenes additively at runtime, call it again so only the currently-loaded lines participate.
- Sorting lines are treated as infinite (extended beyond their endpoints) so zone boundaries stay continuous without fragmentation.
- Namespace: `IsometricZoneSorting`. Assembly: `IsaacBG77.IsometricZoneSorting`.

## License

MIT — see [LICENSE.md](LICENSE.md).
