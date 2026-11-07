# Gameplay rebuild decisions

- 2026-11-07: Treat MWII as a feel reference, not a claim of employment, copied proprietary assets or indistinguishable results.
- Preserve build-40 model assets, Mark23 authored reload, pickup/drop, saved layouts and legacy hit counter. The supplied diagnosis is a hypothesis: build 40 already has FOV changes, flash, tracers and smoke.
- The performance target is 60 FPS with adaptive resolution; do not call device criteria green without physical Android profiler evidence.
- Hip fire uses bloom and visual kick but no deterministic aim-pattern displacement; ADS uses deterministic learnable pattern. A shot is at most once per rendered frame; lag cannot bank shots.
- Tactical sprint plus crouch hold means dive; regular sprint plus crouch hold means slide. ADS exits sprint, allows aiming during slide.
- Keep exact existing artist-authored Mark23 animation. Socket/IK improvements must not silently replace it with a guessed pose.
- Existing legacy assets do not supply production finger/contact and movement animations; placeholder sockets/events cannot establish zero floating fingers.
- Store acceptance evidence separately from implementation checks. Never generate FINAL_REPORT.md until all 13 acceptance groups are verified green.
