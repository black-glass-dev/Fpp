# Gameplay rebuild progress

2026-11-07 — baseline restored to published build 40 (e639c77). Stale initial-checkout files preserved in git stash, not applied over working gameplay.

## Gates

No new system is Play-Mode-verified yet. No physical Android device is attached. Local Unity executable is absent. Native Unity CI is available; its existing build audits are Edit Mode, not Play Mode. Device-only acceptance items remain blocked until measured. This log records actual results, not promised completion.

## Acceptance status

1. Recoil pattern CSV/wall: pending.
2. Sprint hip spread overlay: pending.
3. Per-weapon ADS timing: pending.
4. Four-frame tactical/empty reload and bolt: pending visual verification.
5. Five-pose exact finger contacts: pending asset/visual work.
6. Slide/dive/mantle/mount: pending.
7. Semi-auto cadence: pending.
8. Sprint-out/slide-cancel accuracy: pending.
9. Hitmarker/kill/damage numbers: pending.
10. Stateful dropped pickups/radius: pending.
11. Five-minute soak/profiler GC: pending.
12. Target-device gameplay CPU <=1.5 ms: blocked: no target device/profile.
13. Final report: intentionally absent until all preceding gates pass.

Implementation checkpoint (before native verification): introduced serialized weapon/config assets, unified input, CharacterController states, ADS/recoil/spread math, hitscan health/parts/penetration, persistent secondary ammo, reload cancellation/foley markers, feedback pools and event bindings. Added genuine Unity Play Mode test assembly and release-gating workflow. This is NOT a SYS-green completion commit: first native compile/Play Mode pass is pending. Exact finger contacts, full primary drop behavior, wall-pattern render and device budget remain red.

[15:58 +06] Run 41: native build step green; Play Mode wrapper failed before tests (`Unknown argument: noCoverageEnabled`). Release correctly blocked. Diagnostic workflow retrieved only filtered completed-run failures. Native compile is confirmed; Play Mode gameplay results are still unknown. Added explicit CLI syntax adapter and small XML result summaries for the next pass.

[16:10 +06] Commit fb7240e: native run 42 submitted with coverage-argument adapter, real 15m wall/oracle/CSV test, four-weapon ADS and reload conservation tests, CharacterController slide/dive/mantle tests, actual camera pose sequences, 300-second/500-shot/50-reload soak and Editor profiler markers. Results pending. Capturing screenshots is not equivalent to certifying exact finger contact. Added five additional local data/integration guardrails (27 total); hardware budgets remain unmeasured.

[16:14 +06] Run 42 recovered NUnit XML reports **0 test cases**, despite the wrapper's success exit. This is explicitly NOT a Play Mode pass. Corrected the Play Mode assembly's erroneous Editor-only platform restriction, guarded test code from Android builds, and added an explicit named-test gate rejecting empty/missing/skipped runs. Also replaced editor preview `SendMessage("Update")`: it was broadcasting Update to every player component, causing uninitialized input/pool exceptions during Edit Mode camera capture. Direct HUD-only refresh avoids invoking gameplay outside Play Mode. No release was published.

[16:16 +06] Publication failure isolated: Edit Mode camera PNGs were unstaged when the QA summary step rebased. Enable Git rebase auto-stash only inside CI so those tracked render outputs survive the pull and are published by the existing preview step. Run 43 is currently exercising the corrected non-empty Play Mode assembly; no result claimed yet.

[16:25 +06] Run 43: all **10 required named Play Mode checks actually executed and passed**, including the real 15m wall/oracle test, actual ADS timing on all four weapons, reload/drop ammo conservation, physical slide/dive/1.5m mantle, and 300-second 500-shot/50-reload soak. Capture-only tests explicitly do not certify finger contacts. Publication still failed on unstaged preview PNGs during rebase, so no APK release yet. Next pass adds CI auto-stash, actual physical mount and sprint-out tests, fast-tap timestamp CSV, correctly frame-scaled gyro, configurable support wrist/FOV, and intended 85%-return/15%-retained recoil. These changes require a fresh native pass.

Primary-drop implementation checkpoint: Mark23 now drops a gun-only textured mesh snapshot, carries mag/reserve, and restores the original authored slot on pickup. SWAP takes a nearby world weapon instead of conjuring a lost default pistol. Added explicit primary conservation/no-hands world-item test. Native verification pending; no new SYS-green claims.

[2026-11-07 16:50 +06] f0fd5df native run46 actually executed13 tests:12 passed,1 failed. New true screen-space ADS assertion exposed sight x=.469090223 instead of.5 (not a timing failure). Physical mount RMS ratio=.12, sprint-out=.1000061s, Mark23 textured gun-only drop5+65/reclaim/reload12+58,300-second500-shot/50-reload soak and retained-pattern wall RMS=.05063869° passed. Release correctly blocked. Fix replaces invalid mount-local XY subtraction with actual camera-space sight projection after posing, before procedural kick/sway; Editor review shares the same correction. Strict native sight/FOV assertions remain unchanged.27 local guardrails rerun; new native verification pending. Added explicit acceptance ledger; finger contacts and target-device profiling remain open, no final report.
