# Candidate 89 — phone test build (NOT accepted gameplay)

Published for your device test: https://github.com/black-glass-dev/Fpp/releases/tag/diagnostic-map-89
- File: Fpp.apk, size 234,965,172 bytes (87 was 631,625,961; about 62.8% smaller)
- Source: fa3c3d40bed15f9903e75e9fd1d754dcfa28604a, CI run 42778238592
- Signing verified by apksigner in CI; SHA256.txt is attached to the same release.

## What actually passed (CI, not your phone)
- Unity Play Mode suite on the prepared Android scene: 24/24 cases, 0 failures
  (includes the two cases that failed in 88: spawn floor probe and full pose/reload capture).
- APK budget gate: 234,965,172 bytes, below the 500,000,000 byte limit.
- Independent APK verification: ARM64 manifest/classes/libunity/libil2cpp present, ZIP integrity, apksigner.
- Payload checks: ASTC block compression with unchanged image dimensions and mip counts,
  position-only collision meshes, 35 LOD groups / 32 reducing groups, no source/debug scene references.

## What is still unknown and needs your phone
- Real startup on SM-E236B / Galaxy F23 (Adreno 619, Vulkan, Android 14). 87 crashed before
  gameplay; that cause was never confirmed from device logs, so 89 is a candidate, not a fix proof.
- Whether the black screen / crash is gone.
- Measured FPS during actual play (83 measured 3.5 FPS; no newer device measurement exists).
- Road appearance, texture quality after ASTC, model silhouettes at distance, seams.
- Rapid turns, elevated camera, hand/viewmodel overlap.

## Known limitation of this run
- The full-quality 28-image review gallery ran out of its 180 minute allowance; LOD medium/far and
  pond captures were not finished. Fixed by raising the allowance and recording per-image status.
- CI therefore reported failure overall. That failure is review evidence, not the APK you install.

## Please report back
1. Does it install and reach the menu, then gameplay?
2. If it stops: black screen, freeze, or crash to home? Immediately or after START?
3. FPS feel and whether the road/houses/textures look right.
