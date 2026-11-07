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
