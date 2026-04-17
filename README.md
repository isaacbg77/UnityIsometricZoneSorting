# UnityIsometricZoneSorting

A general-purpose depth sorting solution for 2D isometric Unity games.

Authored in **Unity 6 (6000.3.2f1)** using the Universal Render Pipeline and `SortingGroup`-based rendering.

## What it does

Isometric scenes often have ambiguous depth: a character standing next to a wall, behind a tree, and in front of a crate all need to be layered correctly, but a single "sort by Y" rule doesn't cover it. This package lets you hand-author the depth relationships in the scene by dropping in **sorting lines**, and then computes a consistent integer sorting order for anything you tag as sortable.

At runtime, each sortable is assigned a `sortingOrder` every `LateUpdate` based on which side of each sorting line it's currently on.

## How it works

- A **`ZoneSortingLine`** is a line segment (two `SortingPoint` endpoints) plus a `FrontNormal` indicating which side renders on top.
- `N` lines partition the scene into up to `2^N` **zones**, one per front/back combination.
- A **`ZoneGraph`** builds a DAG from these zones (zones differing by one line have a directed edge: back → front) and runs Kahn's topological sort to assign each zone an integer depth.
- A **`ZoneSortingService`** registers all `ZoneSortable` components in the scene. In `LateUpdate` it looks up each sortable's current zone and writes the resulting order into its `SortingGroup.sortingOrder`.

Cycles (contradictory line orientations) are detected and the affected zones fall back to a trailing order with a warning.

## Usage

1. **Add a `ZoneSortingService`** to a scene object and assign a `SortingLayerReference` for the dynamic sorting layer.
2. **Author sorting lines.** Create `GameObject`s with a `ZoneSortingLine` component, two child `SortingPoint` objects, and set `FrontNormal` to point toward whichever side should render on top.
3. **Tag sortable objects** with `ZoneSortable`. It requires a `SortingGroup` (auto-enforced via `[RequireComponent]`) and uses `transform.position` as its sort position.
4. Open `Assets/Sorting/ZoneSortingDemo.unity` for a working example with gizmos.

## Key types

| Type | Role |
| --- | --- |
| `IZoneSortable` | Contract: exposes a `SortingGroup` and a `SortPosition` |
| `ZoneSortable` | Default MonoBehaviour implementation |
| `IZoneSortingService` / `ZoneSortingService` | Registers sortables and writes sorting orders each frame |
| `ZoneSortingLine` / `SortingPoint` | Authoring components that define zone boundaries |
| `ZoneGraph` | Computes zones, builds the DAG, runs the topological sort |
| `ZoneSignature` / `ZoneDefinition` | Immutable zone identity and resolved order |
| `ZoneSortingGizmos` / `ZoneSortingLineGizmos` | Editor visualization |

## Notes

- The service calls `RebuildZones()` on `Awake`. In production, rebuild when loading a new room/scene so only that room's lines participate.
- Sorting lines are treated as infinite (extended beyond their endpoints) so zone boundaries stay continuous without fragmentation.
- Namespace: `YoWorld.Core.Sorting`. Assembly definition: `Core.Sorting.asmdef`.
