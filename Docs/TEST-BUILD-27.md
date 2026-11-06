# Android test build 27 — screenshot-driven mesh and reload repairs

- **APK:** https://github.com/black-glass-dev/Fpp/releases/download/test-build-27/Fpp.apk
- **Release:** https://github.com/black-glass-dev/Fpp/releases/tag/test-build-27
- Android 8+ / ARM64 / OpenGL ES 3; package `dev.blackglass.fpp`; version 0.0.2, version code 27.
- Size: **42,662,533 bytes** (42.7 MB decimal).
- SHA-256: `4077af9da9ebbdf84f6bd7534837dee6ae16b7d8182a500253a728a3b2dbd5bd` (GitHub release asset digest).
- Build source: `febc01fad1dcf129396e566b4d56e16bc7368a58`.
- Successful build/publication: https://github.com/black-glass-dev/Fpp/actions/runs/42250380335

## Changes after the seven phone screenshots of build 18

- Reconverted Glock and field pistol with texture-aware mesh simplification, 32,000 triangles each. Both are closed and consistently wound after welding UV/normal seams in a test copy. The old torn conversions had thousands of open edges. Opaque two-sided rendering prevents additional backface/alpha disappearances in held and floor versions.
- Reconverted the mosque from its original archive with original atlas-chunk boundaries preserved. The scan is now 384,658 triangles, with 2K atlases rather than 1K. Joined chunks have no open boundary edges in the regression check; this is not a claim that the scan is a clean manifold or a complete interior reconstruction.
- Removed the stretched scanned ground skirt without deforming wall vertices; added a level textured plinth and approach steps. Primitive collision retains a north entrance. No facade-covering replacement brick walls are used.
- Replaced repeated sideways secondary-weapon reload sway with one controlled lower/return cycle, a procedural support-hand target sequence and timed ammo refill/fire blocking. **The supplied static guns do not gain authored magazine/slide animation tracks or a visibly detached magazine.** These remain approximate procedural animations, not MWII assets.
- Preserved the original Mark 23 rig and authored reload. Corrected AK facing and secondary ADS alignment. Three scattered pickups, editable HUD, original icon and raycast firing remain.

## Verification

Ten local regression tests pass (4 mobile, 2 pipeline, 4 mesh/reload/scan checks); catalog references and triangle counts validate. Unity rig/material/HUD/secondary-weapon audits and GPU editor captures run before packaging. Reviewed held/floor Glock, AK direction, ADS, reload poses, and both angled and close entrance mosque views. The latest exterior no longer has the conspicuous large triangular tearing visible in the earlier phone screenshots and intermediate candidates.

Actual Unity images: [mosque entrance](https://github.com/black-glass-dev/Fpp/blob/arena/f3a68c52-fpp/Docs/Previews/unity-mosque-entry.png), [angled mosque](https://github.com/black-glass-dev/Fpp/blob/arena/f3a68c52-fpp/Docs/Previews/unity-mosque.png), [held Glock](https://github.com/black-glass-dev/Fpp/blob/arena/f3a68c52-fpp/Docs/Previews/unity-glock-held.png), [floor Glock](https://github.com/black-glass-dev/Fpp/blob/arena/f3a68c52-fpp/Docs/Previews/unity-glock-floor.png). These are editor renders, not phone screenshots.

The fresh APK ZIP, ARM64 Unity/IL2CPP contents and Android signing passed, and the prerelease was published successfully. This build has **not** been played on a physical Android phone here. FPS, heat, touch feel, support-hand motion and approximate collisions still require device testing. The original scan's interior, fine cracks and incomplete photogrammetry detail are not reconstructed.

## Install and controls

Full APK, not a delta patch. If Android refuses an in-place update because an older test has a different debug certificate, uninstall that test app first; this removes saved layout/settings. A persistent signing keystore is still needed for reliable future in-place updates.

Move with the left joystick; drag empty space on the right to look. Approach a floor gun and tap TAKE; PISTOL returns to the original sidearm. To edit controls: LAYOUT → drag a control → SIZE → SAVE; RESET restores defaults.
