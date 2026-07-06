# Isometric Zone Sorting

A general-purpose depth sorting solution for 2D isometric Unity games.

Drop **sorting pivots** into your scene to hand-author which side of each pivot's axes renders in front of the other. At runtime, any object tagged with `DynamicZoneSortable` (for movers), `StaticZoneSortable` (for stationary props), or `BoundaryZoneSortable` (for walls and other things sitting on a pivot) gets a correct integer `sortingOrder` — no per-object tweaking, no "sort by Y" hacks, no fighting with `SortingGroup` priorities.

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

- A **`ZoneSortingPivot`** defines an isometric origin point: two axes project from it in a "V" shape at a configurable isometric angle, each arm capped by a configurable length, with horizontal boundaries extending outward from each arm's tip. Together they partition the space around the pivot into a "front" and "back" side.
- Every **`BoundaryZoneSortable`** in the scene contributes its pivot to the **`ZoneGraph`**. The graph sorts the pivots by Y position (higher = further back) into back-to-front strips and assigns each pivot a base sorting order.
- Resolving a sortable's order is a binary search over the pivots' Y positions followed by a front/back test against the found pivot's V shape: behind the V keeps the pivot's base order, in front of it advances to the next order.
- A **`ZoneSortingService`** registers every sortable in the scene as either an `IDynamicZoneSortable` (re-resolved every `LateUpdate`; a cached pivot index makes the common didn't-change-strips case O(1)) or an `IStaticZoneSortable` (stamped once per `RebuildZones()` and skipped during the frame loop). Boundary geometry like walls stays out of the frame loop entirely.

## Usage

Minimum viable setup is three steps: add the service, author boundary sortables with their pivots, tag the rest of the objects you want sorted.

### 1. Add a `ZoneSortingService`

Create an empty GameObject in the scene and add a `ZoneSortingService` component. In the inspector:

- **Zone Sorting Layer** — pick a sorting layer from the dropdown (populated from *Project Settings → Tags and Layers*). Every registered sortable is moved into this layer.
- **Rebuild Zones On Awake** (default on) — when enabled, the service builds its zone graph in `Awake` using whatever boundary sortables exist in the scene at that point. Turn it off if you load content additively (e.g. per-room) and want to rebuild explicitly; see *Rebuilding zones* below.

A preconfigured `ZoneSortingService.prefab` ships with the package.

### 2. Author boundary sortables and their pivots

For each object that sits on a depth boundary (walls, fences, doors, railings):

1. Add a **`BoundaryZoneSortable`** component to it. A child GameObject with a **`ZoneSortingPivot`** is created automatically (an existing child pivot is picked up instead if there is one).
2. Move the pivot to where the object meets the ground — its position defines the boundary between the zone behind it and the zone in front of it.
3. Adjust the pivot's **Isometric Angle** (default 26.565°) and **Right/Left Vector Length**s so the V arms trace the object's base. Scene gizmos show the arms, the horizontal boundaries at each arm's tip, and the front-facing normals.

### 3. Tag your sortable objects

Two more stock MonoBehaviours cover the remaining cases:

- **`DynamicZoneSortable`** (`IDynamicZoneSortable`) for anything that moves (characters, props, items). `SortPosition` tracks `transform.position` each frame and the service re-resolves the order every `LateUpdate`. Requires a `SortingGroup` (auto-enforced).
- **`StaticZoneSortable`** (`IStaticZoneSortable`) for stationary objects that don't define a boundary of their own (rugs, rocks, scattered props). The order is stamped once at registration and again on every rebuild. Tilemap-aware: on a `Tilemap`, the sort position is the average position of all painted tiles.

A `SortingGroup` is optional for static and boundary sortables — when absent, the order is written directly to the object's renderers. To tag existing content in bulk, call `service.AddSortableToRenderers(root)`: it adds a `StaticZoneSortable` to every `SpriteRenderer`, `TilemapRenderer`, and `SortingGroup` on the zone sorting layer that isn't covered by a sortable yet.

If you need a different sort anchor (e.g. a character's feet rather than their pivot), implement `IDynamicZoneSortable` (or `IStaticZoneSortable` if the position never changes) yourself. `DynamicZoneSortable.cs` is the reference implementation; copy it and change `SortPosition`:

```csharp
using System;
using IsometricZoneSorting;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(SortingGroup))]
public class FootAnchoredSortable : MonoBehaviour, IDynamicZoneSortable
{
    [SerializeField] private Transform _feet;

    public SortingGroup? SortingGroup { get; private set; }
    public Renderer[]? Renderers { get; private set; }
    public Vector2 SortPosition => _feet.position;
    public int CachedPivotIndex { get; set; } = -1;

    public event Action<IZoneSortable>? Destroyed;

    private void Awake()
    {
        SortingGroup = GetComponent<SortingGroup>();
        Renderers = GetComponentsInChildren<Renderer>();
        // Register with the IZoneSortingService here and invoke Destroyed in OnDestroy —
        // see DynamicZoneSortable.cs for the full pattern.
    }
}
```

### Rebuilding zones

The zone graph is a snapshot of the boundary sortables present when it was last built. If boundaries are added, removed, or moved at runtime, rebuild it:

```csharp
service.RebuildZones();      // rediscover boundary and static sortables scene-wide
service.RebuildZones(root);  // only look under a specific hierarchy
```

Typical triggers: finishing a room transition, loading a scene additively, or swapping a level chunk. For a static scene, `Rebuild Zones On Awake` is enough.

The **Demo Scene** sample (importable via Package Manager) shows all of this wired up.

## Key types

| Type | Role |
| --- | --- |
| `IZoneSortable` | Base contract: exposes an optional `SortingGroup`, fallback `Renderers`, a `SortPosition`, and a `Destroyed` event |
| `IDynamicZoneSortable` / `IStaticZoneSortable` / `IBoundaryZoneSortable` | Marker interfaces extending `IZoneSortable`. Dynamic = re-resolved every frame (adds `CachedPivotIndex`); static and boundary = stamped once per graph build |
| `DynamicZoneSortable` | Default implementation for movers; `SortPosition` = `transform.position` |
| `StaticZoneSortable` | Implementation for stationary props and tilemaps; tilemaps sort by average painted-tile position |
| `BoundaryZoneSortable` | Implementation for walls/fences/doors; `SortPosition` and sorting axes derived from a `ZoneSortingPivot` |
| `IZoneSortingService` / `ZoneSortingService` | Registers sortables, walks dynamics each frame, and stamps statics on rebuild |
| `ZoneSortingPivot` | Authoring component that defines an isometric zone boundary; draws its own scene gizmos |
| `ZoneGraph` | Y-sorts the pivots into strips and resolves positions via binary search plus a per-pivot V-shape front/back test |
| `[SortingLayer]` attribute | Marks a string field to render as a sorting-layer dropdown in the inspector |

## Notes

- The service updates sorting orders in `LateUpdate` so it sees each sortable's final position for the frame (after animation, physics, and user scripts).
- Sorting orders are only written when the value actually changed, so registered sortables don't dirty their renderers every frame.
- Namespace: `IsometricZoneSorting`. Assembly: `IsometricZoneSorting.Runtime`.

## License

MIT — see [LICENSE.md](LICENSE.md).
