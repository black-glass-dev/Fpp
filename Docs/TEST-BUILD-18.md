# Android test build 18 — visual and mobile control revision

- **APK:** https://github.com/black-glass-dev/Fpp/releases/download/test-build-18/Fpp.apk
- **Release:** https://github.com/black-glass-dev/Fpp/releases/tag/test-build-18
- Android 8+ / ARM64 / OpenGL ES 3; package `dev.blackglass.fpp`; version 0.0.2, version code 18.
- Size: **30,196,671 bytes** (30.2 MB decimal).
- SHA-256: `c83bf4201a3108008bb9188756d13e47e0e8bef02542d1b1b5a42325a676c834` (GitHub release asset digest).
- Build source: `63a8fa2cb994d26873bcc5a35ee494ea864e4af9`.
- Successful build/publication: https://github.com/black-glass-dev/Fpp/actions/runs/42223482808

## Included repairs

- Original textured, skinned Mark 23 pistol held by both hands by default. The other arms-and-gun model is excluded.
- Three scattered collectible guns, authored pistol clips, procedural lower/raise swap transitions and approximate AK support-hand IK.
- Correct 2D texture imports and per-submesh materials; texture-aware mosque atlas conversion (130,177 triangles); larger cottages and ambient fill; textured ground.
- Tiny star flash/raycast firing instead of a projectile ball; image-based visible reticle.
- Mobile MOVE/FIRE/ADS/RELOAD/TAKE/PISTOL controls, editable positions/sizes with saved layouts; original FPP application icon.

**To edit controls:** tap LAYOUT → drag a control → adjust SIZE → SAVE. RESET restores defaults. Near a floor gun, tap TAKE; PISTOL returns to the default sidearm.

## What was verified

Fresh APK ZIP integrity, ARM64 Unity/IL2CPP contents and Android signing passed independently of the builder cleanup warning. Six local source/asset tests passed. Unity rig/clip/material/pickup audits and real HUD pointer/state/slider/preference smoke tests completed. Actual GPU editor renders were inspected: both hands/pistol are visible and correctly gripping, cottages are textured/lit, and the mosque no longer has the large atlas-mapping artifacts from the previous conversion.

See [revision details and limits](MOBILE-REVISION.md), [actual first-person editor render](Previews/unity-spawn.png) and [mosque editor render](Previews/unity-mosque.png).

## Important limits / installation

- This is a test firing-range prototype, not finished MWII-quality gameplay. Other-weapon transitions are original procedural animation, **not MWII animation data** or weapon-specific motion capture.
- This revision has not been physically played on a phone here; FPS, heat, touch feel, pickup/reload behavior and all weapon grips still need device testing. Build 6 was previously phone-tested by the user.
- Full APK, not a delta patch. Debug certificates from different runners can differ. If Android refuses an in-place update, uninstall the older test app before installing this one; that removes old settings/layout preferences.
- Tiny holes/scan imperfections remaining in the source photogrammetry are not reconstructed by texture/UV repair.
