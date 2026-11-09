# Studio-scale optimization principles applied to Fpp

Research review, 2026-11-06. This is an engineering decision record, not evidence that a studio's engine has been reproduced or that a phone frame-time target has passed.

## Primary references

1. Unity + Arm, *Tackling profiling for mobile games*: https://unity.com/blog/games/tackling-profiling-for-mobile-games-with-unity-and-arm . Describes on-device analysis, CPU fixed-update catch-up, allocation/instantiation spikes, and distant geometry consuming GPU processing/DRAM bandwidth. Its example timings belong to its example project, not Fpp.
2. Unity, *Optimize your mobile game performance: graphics and assets*: https://unity.com/blog/games/optimize-your-mobile-game-performance-expert-tips-on-graphics-and-assets . Distinguishes automatic camera-frustum rejection from baked hidden-object occlusion, and describes mesh LOD. Its optional resolution/frame-rate cuts are NOT adopted here because of the user's original-quality constraints.
3. Guerrilla, *GPU-Based Procedural Placement in Horizon Zero Dawn*: https://www.guerrilla-games.com/read/gpu-based-procedural-placement-in-horizon-zero-dawn ; official publication index https://www.guerrilla-games.com/tags?after=636371270400000000&jsonPath=TaxonomyPart.Terms%5B0%5D . Official index describes dynamically creating the world around the player. Only the indexed description was retrieved; no claim to have watched the presentation/slides. Principle: bound active work by locality. Copying this custom compute pipeline into a small Unity 2022 Android scene is not justified without API/device measurements.
4. Epic, *Playtesting on Mobile Devices*: https://dev.epicgames.com/documentation/fortnite/playtesting-on-mobile-devices-in-fortnite?lang=en-US . Recommends real mobile traversal with timing and memory captures to identify hitches and residency problems.
5. Epic, *Mobile Preview*: https://dev.epicgames.com/documentation/fortnite/mobile-preview-session-in-unreal-editor-for-fortnite?lang=en-US . Explicitly warns cooked-PC preview timing/memory is not physical-mobile evidence. The same distinction is essential to interpreting Fpp editor captures.

## Decisions for this project

| Bottleneck / risk | Implemented action | Evidence / limit |
|---|---|---|
| Camera-excluded geometry | Retain Unity renderer frustum culling, bounded renderers, no custom per-frame full-world visibility scan | Native camera/bounds checks; does not imply unloaded assets |
| Fully hidden buildings | Conservative baked occlusion; trees not used as solid occluders, source near geometry retained | Bake receipt required; CPU cost and doorway visibility still need device traversal |
| Distant dense meshes | Editor-generated medium/far derivatives, screen-size LOD, original near mesh | Actual triangle audit/native identity checks, not target ratios as results |
| Bad automatic simplification | Reject derivatives outside original bounds rather than silently accepting distorted silhouettes | First candidate failed on MudBody_mesh; corrected build retains source for rejected derivatives and records why |
| Repeated geometry/material CPU submission | Shared materials and original repeated meshes; instancing eligibility retained | Eligibility is not measured batching or draw-call reduction; SRP Batcher can take priority |
| Mesh CPU copies | One-shot release for generated visual-only LOD meshes only after collision-reference check | Original collision and near meshes untouched; GPU derivative memory remains |
| Texture residency | No arbitrary downsizing/mip-quality changes | Prior editor estimate is concerning, but not Android PSS; device report required before claiming resolved |
| Unexplained performance claims | Gameplay frame interval percentiles + OS memory/device/API info + available rendering counters | GPU timing may be unavailable; engine counters count multiple passes, not unique objects |
| Visual regressions | Same-camera forced LOD comparison plus native gameplay/turn/elevation tests | Still images do not establish temporal popping/ghosting acceptance |

## Why not blindly add more systems?

A world-wide combined mesh can destroy useful culling granularity. Static batching can duplicate vertex data. A GPU-driven renderer/engine migration adds shader/API compatibility risk and does not fix oversized texture residency. Aggressive occluders can hide doorways. Dynamic texture streaming can violate the requested original near quality. Lower physics frequency changes gameplay behavior. None are automatically safe improvements merely because a large studio uses a related technique.

## Target-device acceptance

Galaxy F23, 6GB RAM, Android 14. Record actual GPU/API/driver rather than assuming them. After loading, walk the same route through dense streets and authored interiors; turn rapidly and test elevated views; gather warmed-up frame interval percentiles and PSS, then repeat after sustained play to expose thermal drift. Compare the same route/camera/settings, not FPS from unrelated shots. A build/signature/native-test pass is necessary but cannot substitute for this physical-device test. No access to this phone exists in the build environment.

## Mobile hardware detail: why geometry LOD is more than a polygon statistic

6. Arm, *Finding (and fixing) hidden performance problems in mobile games with Arm Performance Studio*: https://developer.arm.com/community/arm-community-blogs/b/mobile-graphics-and-gaming-blog/posts/finding-and-fixing-hidden-performance-problems-in-mobile-games-with-arm-performance-studio . Discusses binning bandwidth, sample-culling rate and partial-quad coverage as evidence of micro-triangle inefficiency. LOD addresses processing that may contribute little visible detail; shadow passes must also be counted. Do not transplant the article's measured millisecond savings into this game.
7. Arm, *Console quality game rendering on mobile*: https://developer.arm.com/community/arm-community-blogs/b/mobile-graphics-and-gaming-blog/posts/console-quality-game-rendering-on-mobile . Describes Afterpulse's compact vertex streams and local tile-buffer lighting, with opaque front-to-back and transparent back-to-front ordering. Fpp retains URP's ordering; rewriting its lighting to imitate Afterpulse is not a safe unmeasured patch. Geometry packing would require explicit normal/tangent/UV precision and shader-compatibility validation, so source attributes are preserved instead.

Arm-specific counters are examples of the profiling approach, NOT an assumption that the Galaxy F23 uses a Mali GPU. Actual GPU and active Vulkan/GLES driver are collected by the APK.
