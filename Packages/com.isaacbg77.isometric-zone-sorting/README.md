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

## Usage

Minimum viable setup is three steps: add the service, author some sorting lines, tag the objects you want sorted.

### 1. Add a `ZoneSortingService`

Create an empty GameObject in the scene and add a `ZoneSortingService` component. In the inspector:

- **Dynamic Sorting Layer Name** — pick a sorting layer from the dropdown (populated from *Project Settings → Tags and Layers*). Every registered sortable is moved into this layer each frame.
- **Rebuild Zones On Awake** (default on) — when enabled, the service builds its zone graph in `Awake` using whatever sorting lines exist in the scene at that point. Turn it off if you load content additively (e.g. per-room) and want to rebuild explicitly; see *Rebuilding zones* below.

### 2. Author sorting lines

For each line that should partition depth:

1. Create an empty GameObject with a **`ZoneSortingLine`** component.
2. Add two child GameObjects with **`SortingPoint`** components and drag them into the line's `Sorting Point A` and `Sorting Point B` fields.
3. Set **`Front Normal`** to point toward the side you want rendered in front of the other.
4. *(Optional)* Add **`ZoneSortingLineGizmos`** on the line for Scene-view visualization, or **`ZoneSortingGizmos`** on the service GameObject to preview the resulting zones as a colored grid.

Lines are treated as infinite (extended beyond their endpoints), so you don't need to cover the full extent of the scene — just place the endpoints where the boundary *changes direction*.

### 3. Tag your sortable objects

Add a **`ZoneSortable`** component to anything that needs to be sorted dynamically. It requires a `SortingGroup` (auto-enforced) and uses `transform.position` as its sort position.

If you need a different sort anchor (e.g. a character's feet rather than their pivot), implement `IZoneSortable` yourself instead of using `ZoneSortable`. `ZoneSortable.cs` is the reference implementation; copy it and change `SortPosition`:

```csharp
using IsometricZoneSorting;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(SortingGroup))]
public class FootAnchoredSortable : MonoBehaviour, IZoneSortable
{
    [SerializeField] private Transform _feet;

    private SortingGroup _sortingGroup;
    public SortingGroup SortingGroup => _sortingGroup;
    public Vector2 SortPosition => _feet.position;

    private void Awake() => _sortingGroup = GetComponent<SortingGroup>();
    // Register with the IZoneSortingService in OnEnable / Unregister in OnDisable —
    // see ZoneSortable.cs for the full pattern.
}
```

### Rebuilding zones

The zone graph is a snapshot of the sorting lines present when it was last built. If lines are added, removed, or moved at runtime, call `RebuildZones()` on the service:

```csharp
var service = FindFirstObjectByType<ZoneSortingService>();
service.RebuildZones();
```

Typical triggers: finishing a room transition, loading a scene additively, or swapping a level chunk. For a static scene, `Rebuild Zones On Awake` is enough.

The **Demo Scene** sample (importable via Package Manager) shows all of this wired up, including a tiny `RebuildZonesOnAwake` helper illustrating how to drive rebuilds from an external script.

## Key types

| Type | Role |
| --- | --- |
| `IZoneSortable` | Contract: exposes a `SortingGroup` and a `SortPosition` |
| `ZoneSortable` | Default MonoBehaviour implementation |
| `IZoneSortingService` / `ZoneSortingService` | Registers sortables and writes sorting orders each frame |
| `ZoneSortingLine` / `SortingPoint` | Authoring components that define zone boundaries |
| `ZoneGraph` | Computes zones, builds the DAG, runs the topological sort |
| `ZoneSignature` / `ZoneDefinition` | Immutable zone identity and resolved order |
| `[SortingLayer]` attribute | Marks a string field to render as a sorting-layer dropdown in the inspector |
| `ZoneSortingGizmos` / `ZoneSortingLineGizmos` | Editor visualization |

## Notes

- The service updates sorting orders in `LateUpdate` so it sees each sortable's final position for the frame (after animation, physics, and user scripts).
- Cycles in the zone graph (caused by contradictory `Front Normal` orientations across lines) are detected and logged as a warning; affected zones get a fallback order.
- Namespace: `IsometricZoneSorting`. Assembly: `IsaacBG77.IsometricZoneSorting`.

## License

MIT — see [LICENSE.md](LICENSE.md).
