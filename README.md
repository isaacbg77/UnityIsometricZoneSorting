# UnityIsometricZoneSorting

A general-purpose depth sorting solution for 2D isometric Unity games, distributed as a Unity Package Manager (UPM) package.

## Install

In Unity's Package Manager → **+** → **Install package from git URL…**:

```
https://github.com/isaacbg77/UnityIsometricZoneSorting.git?path=/Packages/com.isaacbg77.isometric-zone-sorting
```

Or add to your project's `Packages/manifest.json`:

```json
"com.isaacbg77.isometric-zone-sorting": "https://github.com/isaacbg77/UnityIsometricZoneSorting.git?path=/Packages/com.isaacbg77.isometric-zone-sorting"
```

Requires Unity 6.0+. See the package [README](Packages/com.isaacbg77.isometric-zone-sorting/README.md) for usage, the key types reference, and a quickstart.

## Repo layout

This repository is both the package source **and** a Unity 6 project you can open directly to develop and test it.

```
.
├── Assets/                                          — demo project (SampleScene, URP settings)
├── Packages/
│   └── com.isaacbg77.isometric-zone-sorting/        — the package itself
│       ├── Runtime/                                 — source + asmdef
│       ├── Samples~/Demo/                           — importable demo scene
│       ├── package.json, README.md, CHANGELOG.md, LICENSE.md
└── ProjectSettings/
```

To work on the package, open the repo root in Unity 6 (6000.3.2f1 or newer). The package is an embedded package, so edits to files under `Packages/com.isaacbg77.isometric-zone-sorting/` are live.

## License

MIT — see [LICENSE.md](Packages/com.isaacbg77.isometric-zone-sorting/LICENSE.md).
