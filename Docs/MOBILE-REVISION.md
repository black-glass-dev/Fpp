# Mobile visual/control revision

Latest: [test build 38](TEST-BUILD-38.md) supersedes the older revision details below. It addresses the ten build-27 phone screenshots with calibrated sights, articulated secondary reload retargets, late-pose muzzle effects, terrain relief, narrow tree collision and an approximate cleaned mosque interior. See its verification limits and phone checklist; hand/finger/wrist polish and physical-device profiling remain pending.

Updated in [test build 27](TEST-BUILD-27.md) after the seven build-18 phone screenshots.

## Changes

- The supplied Mark 23 **original skinned arms and pistol** are the default viewmodel, retaining the 54-bone source rig and authored Draw/Shoot/Reload/Hide clips. The other arms-and-gun model (Kriss) is excluded from the generated scene. Its excluded static conversion and the redundant static Mark 23 duplicate are removed from repository inventory; the original animated Mark 23 remains.
- A separate first-person camera and calibration parent keep the viewmodel independent of world FOV and clip transforms. Calibration uses the authored hand/pistol grip bones (12.2 cm displayed separation), avoiding scale-sensitive editor `BakeMesh` bounds that previously collapsed the model.
- Three other guns are scattered on the ground and can be taken/swapped. Other-weapon reload uses one controlled procedural lower/return cycle with approximate support-hand CCD IK, not repeated sideways sway. Static guns have no separate authored magazine/slide tracks or visible magazine removal. AK facing and secondary ADS alignment are corrected. **These are not MWII animation files or bespoke weapon-specific motion capture.**
- Texture import repair explicitly requires 2D, sRGB albedo imports, filtering and anisotropy. Bare historical `.meta` files had allowed Cubemap imports, so loading the same PNG as `Texture2D` returned null. Materials are assigned to every mesh submesh.
- The mosque is reconverted from the original archive using texture-aware QEM, preserving atlas UVs rather than nearest-vertex UV transfer. The converted scan has **384,658 triangles** versus 1,014,222 source triangles. Original atlas-chunk boundaries are preserved, 2K atlases are retained on Android, and low scanned ground is clipped without stretching walls. A textured plinth replaces the irregular skirt, with primitive wall collision and a north doorway. Thin/open scanned surfaces use an inexpensive opaque two-sided textured shader; this does not reconstruct geometry missing from the original photogrammetry.
- Cottages have a 14 m footprint, flat ambient fill prevents black unlit facades, and ground/road/cover use original generated textures. Mosque placement avoids the enlarged cottages.
- Raycast firing uses a short star-shaped flash and thin tracer, not a projectile sphere. ADS, recoil, ammo, reload and target-hit feedback remain available.
- Six safe-area action controls: MOVE, FIRE, ADS, RELOAD, TAKE, PISTOL. A bundled licensed DejaVu font and image-based reticle avoid missing built-in fonts or clipped text reticles.
- Original FPP icon replaces the Unity default, including Android adaptive/round slots.

## Change control positions and sizes

1. Tap **LAYOUT**.
2. Drag an action control to move it. Select the control and adjust **SIZE** (0.6–1.6).
3. Tap **SAVE**. The layout persists through normal app restarts.
4. **RESET** restores the default layout. Editing and credits block gameplay input.

Move with the left joystick; drag an empty area on the right to look. Hold FIRE or ADS. Approach a floor gun and tap TAKE. PISTOL returns to the default sidearm.

## Verification and limits

- Glock and field-pistol conversions now use texture-aware reduction (32,000 triangles each), with closed consistently wound geometry and an opaque two-sided shader for floor and held versions.
- Local source/asset checks: `Tools/test_mobile_revision.py` (4 tests), `Tools/test_asset_pipeline.py` (2 tests), `Tools/test_static_weapon_repairs.py` (4 tests), and catalog/reference/triangle validation.
- Unity `RevisionAudit` checks the skinned default, animation clips, material bindings, excluded model and exactly three pickups.
- Unity `HudAudit` constructs the actual HUD and tests fonts, fire/ADS pointer down/up, movement state, modal input blocking, size slider, preference save/load and reset. This is an **editor smoke test**, not Android touchscreen automation.
- `Docs/Previews/unity-spawn.png`, `unity-environment.png`, and `unity-mosque.png` are actual Unity editor camera renders. `unity-baked-pose.png` is a scale-sensitive CPU-skinned diagnostic; its framing differs from the real GPU render and it is **not** gameplay/framing proof or a phone screenshot.
- The APK gate checks a newly generated APK, ZIP integrity, ARM64 Unity/IL2CPP contents and Android signature independently of a known builder cleanup exit warning.
- Build 18 was played on the user's phone and its screenshot defects drove these repairs. This revision still needs physical-device playtesting and performance profiling. A 30 FPS cap is a target, not a measured performance guarantee. MWII visual/animation fidelity is not claimed.
- Updates are full APKs, not delta patches. Ephemeral debug signing can prevent installation over an older build; uninstalling the old app removes saved control preferences. A stable signing keystore is needed for reliable future in-place updates.
