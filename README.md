# FPP — mobile FPS firing-range prototype

Unity **2022.3.62f1 LTS**, Android 8+ ARM64/OpenGL ES 3, built-in renderer, landscape. Original project using user-supplied assets, not Call of Duty code/branding.

**Status: assets + prototype code integrated; Android build blocked by Unity Personal activation. No tested APK yet.**

## What is implemented
- Test-map generator: six cottage instances, roads/courtyard, cover and boundary walls.
- All nine models from the three newly supplied archives: five weapons, two trees, haystack and Chunakhola mosque, plus the earlier cottage model.
- Mobile walk/look, nearby ground weapon pickup/swap, raycast firing, ammo/reload and five reactive firing-range targets.
- Credits screen, mobile texture/polygon conversion and conservative rendering defaults.

These are source implementations, not claims of a verified playable build. See [asset inventory and verification](Docs/ASSETS.md) for exact limitations, credits and triangle counts.

## Open/build in Unity
Install Unity 2022.3.62f1 through Unity Hub with Android Build Support (SDK/NDK/OpenJDK). Activate Unity legitimately through the supported flow for your license. Open this repository.

1. **FPP → Create cottage test map** generates `Assets/Scenes/CottageTest.unity` (asks to save open changes).
2. Play in Editor: WASD + right mouse; E pickup, R reload, left mouse fire. On mobile: left drag walk, right drag look, on-screen action buttons.
3. **FPP → Build Android APK** writes `build/Android/Fpp.apk`. Generates the scene if missing; preserves later saved scene edits. To incorporate changed catalogs into an existing scene, regenerate the scene explicitly.

## GitHub Actions
Pushes affecting code/assets/workflow on `arena/f3a68c52-fpp` run `.github/workflows/android.yml`:
1. Runner imports the 113 MB public Drive mosque file if not already imported and preserves embedded credits. Converted assets are committed only to this branch.
2. If no license/serial is supplied, a bounded actual email/password-only Personal activation is attempted with `UNITY_EMAIL` and `UNITY_PASSWORD`. Logs/credentials stay private; failure stops the build. **This is an attempt, not a guaranteed supported Personal activation method.**
3. GameCI uses a supplied `UNITY_LICENSE`, eligible serial credentials (`UNITY_SERIAL` + email/password), or a successfully produced license, if available.
4. Successful builds upload an APK artifact and automatically publish a GitHub **prerelease** on branch pushes. Manual dispatch can optionally publish too.

Actual run [42204606156](https://github.com/black-glass-dev/Fpp/actions/runs/42204606156): mosque import succeeded; email/password-only activation produced no license, so compilation did not run. Both account secrets were available. Do not post passwords or license files in chat or commit them. Supplying credentials is not equivalent to activating an editor. A compatible Hub-activated self-hosted editor or another supported CI licensing arrangement is needed if this activation fails.

## APK updates
Currently full APK updates, **not delta patches**. Keep package ID `dev.blackglass.fpp`, increment version code, and preserve the same signing certificate. CI passes run number to the custom build method; local builds default to 1 unless `ANDROID_VERSION_CODE` is set.

Current builds use Unity debug signing. Ephemeral runners may use different certificates, so in-place updates are not guaranteed. Configure and back up a persistent test/release keystore via encrypted secrets before routine distribution; never commit it. Downloadable resource patches need a separate versioned content/hosting plan.

## Local asset checks
```
python -m venv .venv
.venv/bin/pip install -r Tools/requirements-assets.txt
.venv/bin/python Tools/test_asset_pipeline.py
python Tools/validate_assets.py Assets/PackModels/catalog.json Assets/MosquePack/catalog.json
```

Animations, enemy AI, campaign, audio, multiplayer and device profiling remain future work. The runtime scripts and Unity import/build still need validation in an activated editor. Supply device model, RAM, Android version and target FPS for actual performance tuning.
