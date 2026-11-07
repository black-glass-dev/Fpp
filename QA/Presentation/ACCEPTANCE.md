# Prompt 2 acceptance ledger (not a final report)

Baseline: Prompt 1 native47, 13/13 automated cases passing. This did not certify production finger contact or a physical Android profile.

## Native checkpoints

- Native48 /42368561986 / sourceee9b143: **14/14 passed**, including unchanged13 gameplay checks and added four-layer/non-silent-clip/32-slot pool saturation test. Test-build-48 published. This is synthesized event-audio foundation evidence, not final mix loudness evidence.
- Native49 /42369556175 / source691005d: **14/15 passed**. New UI test failed on missing `prone` atlas glyph; release blocked. Do not call the UI pass green because the APK compiled.
- Native50 /42370505886 / sourceb70cacf: atlas import repair and stronger Editor audit submitted. **15/15 passed**, build50 published. Human screenshot review then caught top-counter clipping, so it was not treated as final UI acceptance.
- Native51 /42371511250 / source18dcf74: **15/15 passed** with strengthened top-counter bounds test; build51 published. Actual AK ADS screenshot reviewed: counter no longer clips, all displayed touch glyphs and bottom-right ammo present.

## Requested acceptance gates

| §12 | State / evidence |
|---|---|
| 1 Eight matched before/after views | OPEN. Baseline links in `before_after.md`; no fabricated dusk baseline or unmatched comparison labelled matched. |
| 2 Stable shadows / interior SSAO | OPEN. Existing Built-in sun shadows do not certify requested URP/SSAO treatment. |
| 3 Variation at5/15/40m / two seam spots | OPEN. Existing ground normal/blending and scan repairs retained; requested distance-controlled proof not captured. |
| 4 Flash/light/tracer/casing/decal capture | PARTIAL inherited muzzle/smoke/tracers and greybox casing/impact pools. Full material-specific VFX/decal acceptance OPEN. |
| 5 Four reload frames / gripping fingers | OPEN. Supplied Mark23 animation preserved, secondary contacts remain approximate. |
| 6 ACES/LUT pixel diff / UI contrast | OPEN. No post-processing or WCAG result claimed. |
| 7 Audio loudness / stems / spectrum / slaps | OPEN. Synth clips and bounded event routing tested, but mixed Unity output/stems/occlusion spectrum and ffmpeg loudnorm reports absent. No -14LUFS/-1dBTP claim. |
| 8 Pressed touch / LAYOUT / notched safe area | Native49 correctly failed missing icon; native50/51 passed actual pointer/drag/icon/safe-bounds checks. Native51 also passes top-counter bounds after screenshot-discovered fix. Pointer-state/actual drag/default-safe-area test added. Physical notch-profile proof remains separate. |
| 9 Device3min DRS/perf/GC | BLOCKED: no target Android device/profile connection. Editor allocations do not certify0 B/frame. |
| 10 Prompt1 regression suite | Native48/49/50/51 passed all13 original cases. Repeat after every presentation change. |
| 11 Complete docs/final report | Progress/decisions/assets/partial gallery maintained. FINAL_REPORT intentionally absent while mandatory gates remain open. |

## Remaining implementation, not external-only blockers

URP pipeline/shader/capture migration, per-tier render features, GI/probes, world PBR/LOD/clutter pass, complete VFX art, production hands/reloads, cinematic grading, full mixer/limiter/recordings, audio/settings/haptics menus, condensed font and measured UI batching are still work to do. These are **not** described as complete or hidden behind the device blocker.

## Rendering migration investigation

Verified Unity Graphics `2022.3/staging` sources: URP14 supports `RenderPipeline.StandardRequest` (camera stack) and `UniversalRenderPipeline.SingleCameraRequest` (single camera). Current QA uses manual `Camera.Render` and would need the stack request adapter. Current custom ground/brick/scan shaders are Built-in; preserve scan clipping/entry holes when porting. Package-only activation is not a safe verified migration.

## Verified current experimental APK

https://github.com/black-glass-dev/Fpp/releases/download/test-build-51/Fpp.apk

Source18dcf74fecb0ec263073c46759e96bb4cca207cb,48,764,991bytes. GitHub release asset digest SHA256664d4d2d398a2e0117a20db556f4e943048539b218350911b1a55ff5fa577d08. Native51 fresh signed ZIP/APK verification succeeded; wrapper license-return cleanup warning is not silently treated as cleanup success.

Fresh soak:300.0117s /500shots /50reloads /50switches; Editor max total-frame GC281bytes. Custom CPU marker sample0 is unusable. These are **not**0-GC/device-frame-budget acceptance. No master loudness or true-peak measurement exists yet.
