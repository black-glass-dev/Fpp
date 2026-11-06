# Asset inventory — current state (2026-11-06)

**Current delivery:** signed [Android test build 18](TEST-BUILD-18.md). Structure/integrity/signature checks, Unity audits, HUD smoke tests and inspected editor renders passed. This revision needs phone testing; the user played build 6. See [visual/control revision](MOBILE-REVISION.md) for exact scope and limitations.

## Integrated sources

| User source | Contents | Current use |
|---|---|---|
| `1BQBCjgOYnjfayF0iXylK9mIUbdcj5UUm` — cottage RAR | Cottage, four textures; alternate exports | Original FBX/material GUIDs retained; mobile textures; six 14 m footprint cottages |
| `1vU_MdH2D1P8hEqXs8HY0fjoAZONIR2BX` — Asset o.zip | Field pistol, Kriss Vector, Mark 23, Tree1, Tree2, haystack | Mark 23 original skin/54-bone rig/clips imported separately; field pistol collectible; trees/haystack placed; Kriss excluded per user instruction |
| `1Sykikr17jBuzRYziYSC5I8_YFNvkE1ot` — Ak block.zip | AK-47, Glock 17 | Static textured models used as floor pickups and held weapons |
| `16rIZD0UNZ-NS_C6mxbNA4J1xqBFQdDmn` — mosque ZIP | Chunakhola mosque | 112,897,326-byte archive downloaded on runner; reconverted with texture-aware QEM preserving atlas UVs |

The old unsplit `1JBbNG5VETMu8S5upACUI7g1y5pOYsdeW` archive was not downloaded. This inventory covers the replacement archives and cottage, not proof that they exactly match the old ZIP. Unused static Mark 23/Kriss source conversions remain in the import catalog but are not instantiated in the generated map.

## Conversion and performance

- Static pack inventory: eight models, 125,607 triangles total, including unused source conversions. The runtime default Mark 23 uses the original skinned FBX instead.
- Mosque: 1,014,222 → **130,177 triangles**. Nearest-vertex atlas UV transfer in the earlier 54,916-triangle version caused visible artifacts; the new textured QEM conversion retains chart UVs and normals. Some original scan geometry imperfections remain.
- Original archives/GLBs stay outside Git. Static transforms, diffuse colors, source hashes and catalogs are retained; textures are downsized to 1024. Other static weapon conversions still use approximate nearest-source UV transfer; metallic/normal/advanced glTF material features are not fully reproduced. The cottage retains its four maps.
- Mark 23 includes Hand/Glove/Pistol textures and authored Draw/Hide/Shoot/Reload clips. Other guns use procedural transitions and approximate grip placement, not bespoke weapon-specific animation rigs.
- Explicit 2D sRGB import repair prevents historical Cubemap imports; submesh material bindings are validated. Filtering, mipmaps, ETC2 import settings, limited readable mesh data, a simple scan shader, no realtime shadows and a 30 FPS cap target mobile use. Actual FPS/memory/thermal performance remains unmeasured.

## Test scene and controls

The generator creates six cottages, lane/courtyard, cover/boundary walls, the mosque, haystack, both trees, original pistol/arms and three floor guns. Props have coarse box collisions (mosque interior is not navigable); cottages use mesh collisions.

Mobile: left joystick walk, empty right-side drag look, FIRE, ADS, RELOAD, TAKE and PISTOL. LAYOUT allows drag/size/save/reset. Magazine capacities depend on the weapon. Swapping returns the previous gun to its floor position. Desktop supports WASD/right mouse, E/R/left mouse. Five targets flash red and increment the hit counter. This is a prototype: no campaign, enemies or multiplayer.

Credits retain original notices, mosque FUD-UJEP CC-BY-4.0 attribution/source link, and the bundled DejaVu font license. Cottage usage statement remains in `Cottage-original-README.pdf`. For sources without redistribution metadata, the project owner must verify rights before public distribution; no standalone asset resale is intended.

## Verification

- Four mobile source/data tests and two conversion pipeline tests pass; catalogs/OBJ references/counts validate locally.
- Unity `RevisionAudit`, `HudAudit` and actual editor camera captures run before APK packaging. Reviewed GPU view shows proper two-handed default grip and textured map. These are not physical Android tests.
- The fresh signed APK gate and publication passed in [run 42223482808](https://github.com/black-glass-dev/Fpp/actions/runs/42223482808).
- Earlier activation failures are historical, superseded by the working Personal activation route documented in [UNITY-ACTIVATION.md](UNITY-ACTIVATION.md). Do not interpret old failures as the current build state.
