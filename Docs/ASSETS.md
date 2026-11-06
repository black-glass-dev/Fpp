# Asset inventory and integration status

## Cottage (imported)
Source supplied by user: Drive `1BQBCjgOYnjfayF0iXylK9mIUbdcj5UUm`, `45-cottage_free_other.rar` (88,962,666 bytes).
Author: Gerhald3D. Original included usage statement retained in `Cottage-original-README.pdf`: use for any purpose except re-sale. Do not sell the source model as a standalone asset.

Archive inspected: one cottage in FBX, MAX, OBJ/MTL, STL, 3DS, DAE and Unitypackage formats. These are alternate exports of the same model, not distinct models. Imported FBX plus all four cottage textures and original material, preserving original Unity GUIDs. Excluded redundant exports, example scene, unrelated museum HDR environment/material. Source archive remains local and ignored, not duplicated in Git.

Menu `FPP/Create cottage test map` generates six cottage instances along a central lane, a courtyard, temporary primitive cover and boundary walls. Mesh colliders preserve building geometry; walkable doorways/interiors and texture appearance still need Unity/device verification. Scale normalized to a 10m footprint; not an assertion of real-world dimensions. No FPS enemies or weapons yet.

Android texture import capped at 1024, ETC2, mipmaps enabled, CPU readability disabled. Original texture source preserved. Test controls: left touch drag to walk, right touch drag to look; WASD + right mouse in Editor. Flat lighting/no realtime shadows initially. Need real-device CPU/GPU/memory/thermal profiling.

## Operation asset pack (blocked)
Drive `1JBbNG5VETMu8S5upACUI7g1y5pOYsdeW`, `Oparation red line asset test.zip` (236,946,234 bytes).
Metadata retrieved only; contents not downloaded/inspected. Drive tool raw download limit is 100 MiB. Sandbox outbound internet does not allow direct Google Drive HTTP download. User must attach archive in chat or split into smaller standalone archives (prefer under 90 MB each), or provide an allowed accessible source. Do not claim all pack models are integrated until inspected. Weapon identification, ground pickups/equip/fire await those actual assets.

## Verification
No Unity Editor installed in this sandbox. Scene generation, C# compilation, FBX/material import, Android build and device playtest are NOT yet verified. Scene is generated in Unity rather than saved by an unavailable editor; subsequent builds preserve a saved CottageTest scene instead of overwriting it.

## CI attempt — 2026-11-06
- Pushed project to session branch (`23e13b8`).
- Real Actions run: https://github.com/black-glass-dev/Fpp/actions/runs/42202276546
- Checkout succeeded; **Check Unity license configuration** failed. CI did not receive `UNITY_LICENSE` or the complete `UNITY_SERIAL` + `UNITY_EMAIL` + `UNITY_PASSWORD` set required by this workflow. This does not reveal whether other-named account secrets exist.
- Unity compilation was skipped; no APK/artifact/release was produced.
- Manual workflow dispatch and listing repository secrets return HTTP 403 for this session's GitHub integration. Reconnect/adjust the integration's Actions/Secrets access, or run the configured workflow manually in GitHub after supplying a supported license.
