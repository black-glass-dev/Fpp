# Asset inventory — current state (2026-11-06)

## Integrated sources
| User source | Contents | Status |
|---|---|---|
| `1BQBCjgOYnjfayF0iXylK9mIUbdcj5UUm` — cottage RAR | One cottage, four textures; alternate exports of same model | Original Unity material/FBX GUIDs retained; mobile source textures resized to 1024 |
| `1vU_MdH2D1P8hEqXs8HY0fjoAZONIR2BX` — Asset o.zip | Gun pistol m, Kriss Vector, Mark 23, Tree1, Tree2, haystack | All six models converted to static textured OBJ parts |
| `1Sykikr17jBuzRYziYSC5I8_YFNvkE1ot` — Ak block.zip | AK-47, Glock 17 | Both models converted to static textured OBJ parts |
| `16rIZD0UNZ-NS_C6mxbNA4J1xqBFQdDmn` — mosque ZIP | Chunakhola mosque | **112,897,326-byte archive successfully downloaded on GitHub runner**, converted and committed as `b16483e` |

The old unsplit `1JBbNG5VETMu8S5upACUI7g1y5pOYsdeW` archive was not downloaded. The table covers every model found in the new archives and the cottage; not an assertion that their contents exactly match the old ZIP.

## Performance conversion
- Eight models: 125,607 triangles total after conversion. Glock: 1,223,390 → 16,000; pistol: 500,000 → 18,341.
- Mosque: 1,014,222 → 54,916 triangles (open/disconnected geometry limits simplification).
- Source GLBs/archives kept outside Git. `Tools/prepare_models.py` bakes static scene-node transforms, preserves diffuse texture/color, downsizes textures, simplifies heavy parts, transfers UVs from nearest original vertices, writes catalogs with source hashes. This is an approximate mobile conversion, not a lossless import.
- Original rig/animations, metallic/normal maps and advanced glTF materials are not imported for these packs. Nearest-vertex UV transfer can introduce artifacts requiring device/visual review. Imported cottage retains its four texture maps.
- Android ETC2 1024 import settings, mipmaps, no CPU-readable textures, 30 FPS cap, no realtime shadows. Actual FPS, memory and thermal behavior unmeasured.

## Test scene code
`FPP/Create cottage test map` generates six cottages, a central lane/courtyard, cover and boundary walls. Catalog-driven population places all five weapon models near spawn, both trees, haystack and mosque. Props currently have coarse solid box collisions (mosque interior is not navigable); cottages use mesh collisions.

Left touch drag walks; right drag looks. Near a weapon, use PICK UP; FIRE raycasts at five targets; RELOAD restores a 30-round test magazine after 1.5 seconds. Swapping puts the previous weapon back at its ground position. Desktop uses WASD/right mouse, E/R/left mouse. Hit targets briefly turn red and the hit counter increments. This is a firing-range prototype: no enemy AI, multiplayer, campaign, audio, authored weapon alignment/animation, ADS or combat balancing.

Credits button displays included source notices. Cottage original usage statement is retained in `Cottage-original-README.pdf`. Embedded Sketchfab authors/license/source links are retained in `Assets/Resources/THIRD_PARTY_NOTICES.txt`; mosque metadata is captured by runner import. For files without metadata, the project owner must verify redistribution rights before public distribution. No standalone asset resale is intended.

## Verification and blockers
- Package JSON, Python syntax, shell syntax, catalog references, triangle counts and OBJ indices checked locally.
- Two Python pipeline tests pass (texture export/transformed geometry and source catalog/hash/color checks).
- No Unity editor is installed locally: C# compilation, Unity material/model appearance, generated scene, weapon alignment, collisions and Android/device playtest remain unverified.
- Real CI attempt: https://github.com/black-glass-dev/Fpp/actions/runs/42204606156
- Mosque download/import succeeded. Email/password-only Unity Personal activation was actually attempted in Unity's Android container; it exited 1 without producing a license. It passed the missing-credentials check, so both account secrets were available. No claim that account credentials were accepted by Unity, or that an existing local Hub entitlement was activated.
- APK compilation was skipped; **no APK or release exists from this attempt**. Raw private activation logs and secrets are not published.
- A legitimate supported license/activation arrangement is required. Options include building with an already Hub-activated compatible self-hosted Unity Android editor, or supplying a supported CI license setup. Email/password alone cannot be assumed to activate Unity Personal headlessly. No license bypass is implemented.

### Final verification of this integration
Final run: https://github.com/black-glass-dev/Fpp/actions/runs/42205600751 — model import/metadata succeeded; actual Personal activation again exited 1 with no license. Artifact API reports zero artifacts. All nine new model catalogs validate and both local pipeline tests pass. Mosque attribution captured: FUD-UJEP, CC-BY-4.0, source link in `Assets/Resources/MOSQUE_NOTICE.txt`. Compilation/device verification remains blocked, not completed.
