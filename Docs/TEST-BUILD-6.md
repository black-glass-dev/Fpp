# Android test build 6

- Download: https://github.com/black-glass-dev/Fpp/releases/download/test-build-6/Fpp.apk
- ARM64 Android 8+; APK 17,838,161 bytes (~17.8 MB).
- Source commit: `0139bc5ec0e6d5d5df07ca4efb7556850e2445c1`.
- SHA256: `03d20c2985bcf021dd8bffd17cc941acfa576d4ffa53d8de609930988d6bd2ee`.
- CI: https://github.com/black-glass-dev/Fpp/actions/runs/42208662418

## Included
Cottage test map with all models found in the three new archives: five weapon models, two trees, haystack and mosque. Mobile movement/look, ground pickup/swap, raycast firing, reload/ammo, five reactive targets and asset credits. Static mobile conversions, not original animated weapon rigs.

## Controls and phone test checklist
1. Install the APK. If prompted, allow installation for the trusted browser/file manager used to open this test file. No account login is required in the game.
2. Left side drag: walk. Right side drag: look.
3. Walk toward weapons near spawn, press PICK UP. Use FIRE on the targets; RELOAD when needed.
4. Try swapping to each of the other weapon models. Check that the previous model returns to its ground position.
5. Explore the lane/courtyard and view the cottage, trees, haystack and mosque. Props use coarse solid colliders; mosque interior is not navigable in this prototype.
6. Check target color feedback, hit count and ammo. Open CREDITS to inspect included notices.
7. Report phone model/RAM/Android version, install/launch errors, control behavior, weapon alignment, missing/pink textures, collision defects, heat and perceived frame rate.

## Verification / limitations
CI verifies required ARM64 Unity and IL2CPP APK contents, ZIP integrity and signing. This is **not** a real-device playtest and no FPS guarantee is made. No enemy AI, authored weapon animation, audio, multiplayer or campaign yet. 30 FPS target and conservative rendering defaults.

A GameCI builder wrapper exit/cleanup warning remains despite a signed APK being produced; publication is gated on independent APK verification. License return should be reviewed before repeated builds; no security controls or licensing checks are bypassed.

Initial debug-signed test distribution. Persistent signing and resource/delta patching are not yet configured; do not assume future APKs can update in place without matching certificates. Keep important save data out of this prototype.
