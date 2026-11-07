# Android test build 38 — build-27 phone feedback

- **APK:** https://github.com/black-glass-dev/Fpp/releases/download/test-build-38/Fpp.apk
- **Release:** https://github.com/black-glass-dev/Fpp/releases/tag/test-build-38
- Android 8+, ARM64, OpenGL ES 3; package `dev.blackglass.fpp`, version 0.0.2, version code 38.
- Size: **43,832,655 bytes** (43.8 MB decimal).
- GitHub release asset SHA-256: `12d02227ad6be0be46c40abfa474e5a9646496b3c39093b74219cd4deeb9c156`.
- Build source: `03953fc997cf7337f9e043e9c58c623ef1fe8735`.
- Build/publication: https://github.com/black-glass-dev/Fpp/actions/runs/42345876208

## Changes

### ADS and reload
- The reticle now uses the full camera viewport instead of the notch-safe controls rectangle. Central front-sight landmarks calibrate horizontal/vertical ADS placement for each weapon. The original skinned pistol muzzle and sight are calculated from actual skin matrices rather than guessed bounds.
- **The original Mark23 authored reload clip is preserved.** The supplied secondary guns were static meshes, not complete independently animated FPS rigs. Their magazines and action/slide/bolt are now separate transforms; fused pistol meshes retain their source faces, UVs and normals, with an internal magazine extension.
- Secondary reloads reuse the supplied skeleton's authored motion with retargeted magazine travel, limited forearm corrections and trigger-hand attachment. They are no longer only a repeated root sway. These are custom approximate retargets, **not bespoke AK/Glock authored clips or MWII animation assets**. Rifle finger placement, wrist skinning and contact details still need polish.
- Skin matrices refresh per render so late hand corrections appear in both camera review and the build. This also exposed and allowed correction of misleading cached editor arm poses. The large shoulder/arm intrusion of an intermediate candidate is removed.

### Environment and effects
- Removed the incorrect scan-normal rejection that discarded column mouldings. A level stone plinth, clean approximate interior, ceiling and three front passages replace broken interior scan clutter. The original exterior arches remain visible; replacement passage surfaces are recessed behind the facade.
- Exterior collision is measured from area-weighted vertical scan planes rather than the scan's debris-filled bounding box. Inner walls, exterior walls, corner pillars and approach steps have collision. The interior is a practical approximate room, not a reconstruction of the historical domed interior or a repair of every photogrammetry crack.
- Trees use narrow trunk capsules instead of canopy-sized boxes.
- Added real uneven ground geometry, matching mesh collision, textured small stones and ground normal maps. Road, building and mosque pads remain level. This is a mobile prototype terrain treatment inspired by the request, not MWII-quality art.
- Added pooled fast tracer streaks, brief muzzle flash and barrel smoke. Effects emit after the final weapon pose; the original pistol marker follows its animated gun joint. Overlay-to-world projection keeps tracer starts aligned with the visible barrel, with near-plane/close-hit guards. Firing remains hitscan, not a physical projectile-ball system.
- Opaque source PNG storage was optimized with identical decoded RGBA pixels; no intentional texture appearance change.

## Verification and limits

- **19 local regression tests passed.**
- Native Unity compilation, texture/rig/HUD checks, terrain/trunk/interior/portal physics audits, actual secondary magazine retarget travel, action/reset checks, reload ammo timing and firing block checks ran before APK generation.
- Reviewed actual Unity camera images for secondary held/ADS/reload poses, original pistol ADS, muzzle effects, mosque exterior/entry/interior. Current images are under `Docs/Previews/`; these are **editor camera renders, not Android phone screenshots**.
- Workflow independently verified a fresh APK, ZIP integrity, ARM64 Unity/IL2CPP contents and Android signature, then published the release. The GameCI wrapper still reports a cleanup/build-exit warning; APK verification is not proof that licensing cleanup succeeded. That warning has not been disguised as fixed.
- **No physical Android playtest or measured FPS/heat/battery results here.** Device ADS, touch feel, all reload stages and traversal still require phone testing. Custom hands are not claimed to be AAA-quality animation.

## Install / phone checks

This is a **full APK**, not a delta patch. If Android rejects an update because a previous test used a different debug certificate, uninstall that test first. **Uninstalling removes the saved HUD layout/settings.** A persistent signing keystore is still needed for dependable future in-place updates.

Please check on the phone:
1. Mark23 and AK front sights against the central reticle while standing still.
2. Fire and reload each of the three floor pickups; check magazine removal/insertion, hands and return to the grip. Default Mark23 should retain its original reload.
3. Walk in and back out through all three mosque front passages; inspect column bases and inner walls.
4. Approach both tree trunks; confirm the canopy no longer blocks the player at a distance.
5. Fire at near and far surfaces; look for flash, smoke and short forward streaks from the barrel.

LAYOUT edits/saves control positions and sizes. TAKE collects nearby floor guns; PISTOL restores the original sidearm.
