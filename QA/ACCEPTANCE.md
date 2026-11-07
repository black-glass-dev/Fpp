# Acceptance ledger — not a final report

Baseline native evidence: `native-results-42363070548.json` / run 43, 10 actual Play Mode cases, all passed (351.55 seconds total). Later source changes require a new native result; do not transfer this pass to untested commits.

| # | Acceptance requirement | Current evidence / remaining gate |
|---|---|---|
| 1 | 30-round AK / fixed 15m wall / <=0.5° RMS | Run 43 actual wall RMS **0.05064458°**, original CSV in CI artifact, camera PNG in Previews. Retained-recoil revision needs new pass. |
| 2 | Ten sprint hip shots / matching crosshair | Dynamic angular-cone crosshair implemented. Literal firing at full sprint conflicts with required sprint-out blocking; sprint interruption wins. Dedicated 10-shot overlay proof remains open. |
| 3 | ADS timing all weapons +/-20ms | Run 43 pistols **166.6565ms** vs160ms; AK **283.3252ms** vs280ms. Additional actual camera-FOV and sight-axis assertions added; pending fresh results. |
| 4 | Tactical/empty visible magazine / action lock / four frames | Actual Play Mode four-frame sequences captured for all four weapons. Clips/parts exist, but secondary hand-to-mag contact still approximate; not artist-approved. |
| 5 | Five poses / zero floating fingers | Five actual AK pose frames reviewed; reload/palm contact remains approximate. **RED**. Current sockets are explicitly labelled approximate. Prompt 2 rig/animation assets are not supplied. |
| 6 | Slide/dive/1.5m mantle / mount >=80% | Run 43 real slide entry **9.315001m/s** from6.9m/s; dive lands prone; mantle reaches1.58m. Recoil-model mount test passes. Physical mount/shot RMS test added, pending new pass. |
| 7 | Semi-auto fast-tap RPM caps | Run 43 scheduler at15/30/60/120FPS and actual held-semi edge test pass. Timestamp CSV and per-cycle actual fast-tap bounds added to soak. |
| 8 | Sprint-out / ADS cancels sprint / slide accuracy | Implemented; fresh physical-input tests added and pending. |
| 9 | Synchronous hit/kill markers / damage setting | Run 43 synchronous health/head/kill feedback test passes. Number toggle, direction arc and feed hooks present. Phone visual UX still needs review. |
| 10 | All dropped weapons preserve ammo /1.6m | Run 43 secondary7+73 drop/pickup verified. Full gun-only Mark23 world-drop/restoration test added, pending new pass. |
| 11 | 5min /500 rounds /50 reloads / zero errors / GC | Run 43 **300.0105s**,500 shots,50 reloads,50 switches, no unexpected exceptions. Editor total-frame max GC **322 bytes**. Includes Editor/test instrumentation; not an Android deep profiler capture. |
| 12 | <=1.5ms gameplay on target Android /60FPS | **BLOCKED: no physical target device/profile**. Run 43 Editor marker max76,543ns is recorded only as Editor evidence, not device proof.60FPS target/adaptive resolution is configuration, not a measured guarantee. |
| 13 | Progress/decisions/assets/final report | PROGRESS.md, DECISIONS.md and TODO_ASSETS.md exist. FINAL_REPORT.md intentionally absent while any required gate remains red. |

Original uncompressed Play Mode PNG sequences and CSVs remain GitHub Actions artifacts (`gameplay-qa-43`). `Review-42363070548/*.jpg` are resized/compressed copies of actual camera frames, not AI illustrations. Snapshot storage is bounded; no APK or large profiler capture is committed.

## External limits and honest scope

- No physical Android device, adb connection or target-device profiler trace is available in this workspace.
- Production secondary finger-contact and movement animation assets are absent. Keep the working original Mark23 reload; do not claim a generic procedural retarget is a bespoke rifle animation.
- New core handling tunables are serialized ScriptableObjects. Existing artist/model-space geometry calibration is not comprehensively migrated; a literal zero-number audit of all legacy code is not claimed.
- An APK can be an experimental test build without being the requested 100%-green MWII-grade completion. Do not label the work finished until the ledger is actually green.
