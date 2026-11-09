# User rejects85 visual result

Screenshot shows remaining dark grid/vertical banding on lower-right ground and broken/serrated scan road tile edges and centre seams. No new performance report supplied; no claim that FPS improved or remained3.5 based on this still alone.

Two corrections in candidate code:
1. Actual URP2022.3 source enum retrieved from Unity-Technologies/Graphics `2022.3/staging` ScreenSpaceAmbientOcclusion.cs: Depth=0, DepthNormals=1. Existing setup used0, reconstructing normals from depth, NOT actual mesh normals. Explicitly update existing AO features to1. Earlier overlay-only hypothesis did not resolve the artifact. Source change is not phone proof.
2. Stop decimating across scan UV islands. Rasterize the original cleaned road into high-resolution albedo/smoothness and normal derivative atlases BEFORE simplifying geometry. Continuous rectangular96triangle tiles ensure shared straight edges; lane-union clipping still prevents duplicate intersections. Source assets/images untouched. Bake coverage gate>=98%; no generic road texture substituted; extra atlas residency is a known tradeoff. Original off-network source patch keeps original near topology for reference. GPU color/normal orientation and edge continuity require actual render review.

Temporary ARTIFACT TEST settings cycle Normal/NoAO/NoShadows/Neither; default always Normal, no saved quality cut. COPY PERF includes mode and resets samples after changing mode, so device comparisons can isolate the cause instead of repeatedly guessing. These controls are diagnostic, not an accepted workaround of disabling lighting.

Projected derivative atlas opposite edges are feathered over16pixels (albedo in linear color, encoded normals renormalized) for repeating UV continuity. This is an explicit derivative-border edit, not a change to the original source images. Coverage is checked before feathering so the edge blend cannot hide a failed/no-op bake.

Candidate87 source0025a0b / run42641099687: native Unity ARM64 build, fresh APK/signature verification and public upload PASS. Thus GPU bake >=98% coverage and scene geometry gates executed successfully, but exact atlas coverage/output images are not yet retrieved. APK631,625,961bytes; GitHub asset SHA2560c4a3babf99db01aaf00a0a8146cfda1678189e6d6def24e4f0c46fbf1549990. Full gallery/native gameplay remains running; no phone striping/road appearance/FPS acceptance claim. Local56Tools+1crop tests passed before native build.

Source reference for enum: https://github.com/Unity-Technologies/Graphics/blob/2022.3/staging/Packages/com.unity.render-pipelines.universal/Runtime/RendererFeatures/ScreenSpaceAmbientOcclusion.cs . Branch retrieved for inspection; not a claim to have profiled Unity internals on the phone.
