# FPP — mobile FPS firing-range prototype

Unity **2022.3.62f1 LTS**, Android 8+ ARM64/OpenGL ES 3, built-in renderer, landscape. Original project using user-supplied assets, not Call of Duty code/branding.

**Status: signed Android test build 38 published for the ten build-27 phone screenshots. Nineteen local regressions, native Unity audits/camera review and independent fresh-APK/signature gates passed. Physical-phone playtesting and profiling remain pending; custom reload hand/finger/wrist polish is not claimed complete.**

**[Download Fpp.apk — test build 38](https://github.com/black-glass-dev/Fpp/releases/download/test-build-38/Fpp.apk)** (43,832,655 bytes), [release notes and limits](Docs/TEST-BUILD-38.md). Android 8+ ARM64. This is a prototype firing range, not a finished campaign.

## What is implemented
- Test-map generator: six cottage instances, roads/courtyard, cover and boundary walls.
- Original skinned Mark 23 arms/pistol default; three scattered floor weapons; two trees, haystack, Chunakhola mosque and the cottage. Kriss arms/gun excluded per the latest request.
- Mobile walk/look, nearby ground weapon pickup/swap, raycast firing, ammo/reload and five reactive firing-range targets.
- Editable/saved mobile control layout, ADS, original pistol clips, articulated secondary reload retargeting, barrel flash/smoke/tracers, uneven terrain, a cleaned enterable mosque room, custom icon, credits and mobile texture/polygon conversion.

These implementations compile into the released APK. [Visual/control revision details](Docs/MOBILE-REVISION.md) distinguish editor verification from pending Android playtesting. Gameplay and performance still need phone testing. See [asset inventory and verification](Docs/ASSETS.md) for exact limitations, credits and triangle counts.

## Open/build in Unity
Install Unity 2022.3.62f1 through Unity Hub with Android Build Support (SDK/NDK/OpenJDK). Activate Unity legitimately through the supported flow for your license. Open this repository.

1. **FPP → Create cottage test map** generates `Assets/Scenes/CottageTest.unity` (asks to save open changes).
2. Play in Editor: WASD + right mouse; E pickup, R reload, left mouse fire. On mobile: left drag walk, right drag look, on-screen action buttons.
3. **FPP → Build Android APK** repairs imports, regenerates the map, audits/captures it and writes `build/Android/Fpp.apk`. The current build deliberately regenerates the scene; custom manual scene edits are not preserved by this generator.

## GitHub Actions
Pushes affecting code/assets/workflow on `arena/f3a68c52-fpp` run `.github/workflows/android.yml`:
1. Runner imports the 113 MB public Drive mosque file if not already imported and preserves embedded credits. Converted assets are committed only to this branch.
2. GameCI builder **v6.0.0**, pinned CLI **v0.1.72**, activates and builds in the same environment. With only `UNITY_EMAIL` and `UNITY_PASSWORD`, its newer auto strategy can select Personal licensing-client activation. No invented serial or license bypass is used.
3. Supplied supported `UNITY_LICENSE` or eligible serial credentials remain available as alternative strategies.
4. Successful builds upload an APK artifact and automatically publish a GitHub **prerelease** on branch pushes. Manual dispatch can optionally publish too.

[Research and owner assistance](Docs/UNITY-ACTIVATION.md) documents the correction to earlier activation conclusions. Run [42206325110](https://github.com/black-glass-dev/Fpp/actions/runs/42206325110) selected Personal and reported activation complete, then the wrapper failed despite Unity reporting successful APK generation. Run [42208662418](https://github.com/black-glass-dev/Fpp/actions/runs/42208662418) successfully verified and published the signed APK. A builder exit/cleanup warning remains documented; job success requires an independently verified fresh APK, not simply ignoring the builder exit. Earlier v4/editor-only failures did not test this newer route. Diagnostics are sanitized; never post credentials, license files, MFA codes or raw activation logs in chat.

## APK updates
Currently full APK updates, **not delta patches**. Keep package ID `dev.blackglass.fpp`, increment version code, and preserve the same signing certificate. CI passes run number to the custom build method; local builds default to 1 unless `ANDROID_VERSION_CODE` is set.

Current builds use Unity debug signing. Ephemeral runners may use different certificates, so in-place updates are not guaranteed. Configure and back up a persistent test/release keystore via encrypted secrets before routine distribution; never commit it. Downloadable resource patches need a separate versioned content/hosting plan.

## Local asset checks
```
python -m venv .venv
.venv/bin/pip install -r Tools/requirements-assets.txt
.venv/bin/python Tools/test_asset_pipeline.py
.venv/bin/python Tools/test_mobile_revision.py
.venv/bin/python Tools/test_static_weapon_repairs.py
python Tools/validate_assets.py Assets/PackModels/catalog.json Assets/MosquePack/catalog.json
```

Bespoke secondary-weapon animation rigs, enemy AI, campaign, production audio, multiplayer and device profiling remain future work. The Android build is verified; this latest revision still needs physical-device playtesting. Supply device model, RAM, Android version and target FPS for actual performance tuning.
