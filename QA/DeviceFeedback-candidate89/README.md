# Candidate 89 (installed) → what was fixed after your report

## What your report changed

1. **Crash found and fixed in code.**
   `ArgumentNullException: Value cannot be null. Parameter name: shader` at `Fpp.GunplayFeedback.Awake()`
   was `new Material(Shader.Find("Standard"))` returning a null shader in the player.
   `Shader.Find("Sprites/Default")` in the same method had the same flaw. Both now go through
   `RuntimeShader`, which falls back to a URP shader that is always present and never throws.
   A new build gate (`RequiredShaderGate`) puts every runtime-by-name shader into
   Always Included Shaders and **fails the build** if one is still missing, so this class of
   crash cannot be shipped again. A Play Mode test (`FeedbackEffectShadersResolveInPreparedPayload`)
   rebuilds the pools inside the prepared payload.
   Your report's `build=6e1e7e54…` is not any build in this repository, so I cannot prove that
   build was produced from this branch — but the code path is identical and is now fixed.
2. **Fog + shorter draw distance (your request).**
   The game was drawing to **700 m** in a 120 m village. Now: linear fog 40 m → fully opaque at
   130 m, camera far clip **145 m**, LOD medium at 55 m and far at 95 m, and a distance-band
   component that stops drawing world renderers past the fog point and puts them back when you
   walk closer. First-person, LOD-group members and anything with collision are never hidden.
   A Play Mode test asserts fog, far clip and LOD bands agree, so this cannot silently regress.
3. **"Some houses changed / pictures missing" — now measured, not guessed.**
   Some supplied GLBs genuinely contain materials with **no base-colour image**
   (`materialsWithoutBaseColorImage`): `Bd house 3, 4, 5, 8, 9, oo` (and several props/`Iron`).
   Those render as flat paint — that is the white/blank look in your screenshots, not a broken
   import. A new build-time `ModelTextureAudit` records, per house and per material, exactly which
   image is bound, and counts materials that are untextured *and* plain white, so the next build
   tells us precisely which house needs work instead of me guessing.
   I have not invented replacement textures for supplied art.
4. **Fill-rate cost on the phone.**
   Mobile builds now default to 2× MSAA instead of 4× (4× remains selectable in Settings → QUALITY).
   This is the cheapest correct saving for an Adreno 619-class GPU at native resolution.
5. **Texture residency measured, not assumed.**
   The build now writes `QA/runtime/texture-budget.txt` with the actual bytes bound by world
   materials, binned by texture size, so the next step is chosen from data. No texture was
   resized or downgraded in this round.

## Honest status
- Nothing here is phone acceptance. Startup, FPS and the visual result still need your device.
- The 89 APK remains the only downloadable build. A new build is running now; I will only hand it
  to you once its native Play Mode suite is green.
- The road still looked near-black in the gallery (mean RGB ≈ 40,42,30 against grass 50,62,30).
  That is a real defect and is queued as the next visual fix; it is not fixed in this commit.
