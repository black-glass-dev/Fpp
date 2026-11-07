# Gameplay rebuild decisions

- 2026-11-07: Treat MWII as a feel reference, not a claim of employment, copied proprietary assets or indistinguishable results.
- Preserve build-40 model assets, Mark23 authored reload, pickup/drop, saved layouts and legacy hit counter. The supplied diagnosis is a hypothesis: build 40 already has FOV changes, flash, tracers and smoke.
- The performance target is 60 FPS with adaptive resolution; do not call device criteria green without physical Android profiler evidence.
- Hip fire uses bloom and visual kick but no deterministic aim-pattern displacement; ADS uses deterministic learnable pattern. A shot is at most once per rendered frame; lag cannot bank shots.
- Tactical sprint plus crouch hold means dive; regular sprint plus crouch hold means slide. ADS exits sprint, allows aiming during slide.
- Keep exact existing artist-authored Mark23 animation. Socket/IK improvements must not silently replace it with a guessed pose.
- Existing legacy assets do not supply production finger/contact and movement animations; placeholder sockets/events cannot establish zero floating fingers.
- Store acceptance evidence separately from implementation checks. Never generate FINAL_REPORT.md until all 13 acceptance groups are verified green.
- 15:58 +06: Native run 41 compiled and built successfully; Play Mode did not start because the pinned test-runner wrapper emits `--no-coverageEnabled`, rejected by CLI v0.1.72. Adapt that single argument to `--coverageEnabled=false`; keep both upstream wrapper and working Personal CLI version pinned. Do not label this infrastructure failure a gameplay test pass.
- A Passed XML with zero discovered cases is a red gate, not success. Require every named gameplay test to execute; keep Editor-only QA entry points behind compiler guards while the test assembly itself is Play Mode eligible.
- Edit Mode preview code must invoke only its HUD refresh method, never GameObject.SendMessage("Update") across all sibling gameplay components.
- Sprint firing conflict: acceptance requests shots "while sprinting", but SYS-2/SYS-4 require sprint-out blocking. Preserve MWII-like sprint interruption/delay; sprint-held hip fire transitions out of sprint rather than firing at full sprint speed. This interpretation is not silently counted as a literal full-speed sprint-fire screenshot pass.
- Default starts as the authored Mark23 slot. On replacement, create a normalized, gun-only textured world snapshot (no arms); picking it back up restores the original animated slot and saved ammo. Runtime startup bake applies only to the readable Mark23 skin, never the unreadable AK. Dedicated Play Mode coverage is required.

## ADS sight-space correction (2026-11-07)
Native46 showed that scalar ADS timing and field-of-view success do not prove sight alignment. An authored rotated root makes mount-local XY subtraction invalid. Lateral ADS placement now projects the actual serialized sight into the overlay camera's coordinate space after neutral posing; interpolate its XY correction with adsT, preserving depth. Apply procedural kick/sway afterward instead of cancelling their motion. Runtime and Editor reviews use the same helper. Keep strict screen-space assertions for every weapon; do not loosen the test to match the bug.

## Prompt 2 presentation baseline / AUD-1 foundation
- Prompt 1 has13 green automated cases, not green anatomical contacts or physical-device performance. Preserve that distinction; do not assume the new prompt removes these blockers.
- MWII2022 is an art-direction reference only; no affiliation, proprietary assets or parity claim.
- Current renderer is Built-in, not URP. Migration must also port the custom ground/brick/smoke shaders and manual Camera.Render QA captures, otherwise a package-only switch produces pink materials or blank evidence. Do not call the existing two Built-in cameras a URP stack.
- Start event-audio independently while the renderer migration is staged. Original synthesized clips are named SYNTH_PLACEHOLDER, not represented as authentic weapon recordings. Four shot layers, four variants, positional tails/reflection delays, occlusion low-pass, foley and ducked ambience use one bounded32-source pool (ambience reserves one slot). Clip generation is startup-only.
- No ffmpeg executable is installed locally; no rendered-output loudnorm report is fabricated. Individual clip energy/peak tests are not LUFS, true-peak, mixer-limiter, or recorded Android output acceptance.
- Full final report remains blocked until every requested acceptance has actual evidence.
