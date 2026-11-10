# Startup rejection87

User reports black screen before gameplay and crash; no Android logcat/native crash reason supplied. Treat87 as rejected. Do NOT infer a confirmed OOM or Vulkan driver bug from this report alone.

Source audit found avoidable payload/residency risks:
- Editor diagnostic WorldVisualSource.originalMesh references shipped full source meshes.
- Disabled source renderers/filters remained alongside generated/batched visual meshes.
- Full source collision meshes retained UV/normal/color attributes; some were hundreds of thousands of triangles.
- glTF embedded Texture2D subassets and projected road atlases were not covered by conventional Android TextureImporter compression. Earlier editor RGBA texture estimate3,005,043,604bytes was concerning, NOT an Android PSS measurement.
- World was the first scene: no lightweight retry/menu/checkpoint before allocating its resources.
- Mesh.UploadMeshData(true) mutated mesh residency synchronously in first gameplay Start.

Candidate correction: build a separate Android-only scene copy, unpack source prefab links, strip debug/source-only render components and original-source metadata, standalone visual mesh assets, position-only collision assets (dense sources simplified preserving authored open boundaries), no startup mesh mutation. Original repository/editor scene/assets preserved. GPU-native high-quality ASTC compression, original texture dimensions/mips retained:4x4 normals,6x6 color/data. This is LOSSY compression and needs normal/alpha/visual review, not mathematically identical image pixels. Cache shared textures/materials; no runtime transcoding. GLES3-only diagnostic baseline replaces the failed Adreno Vulkan route; not proof of a driver defect. Re-bake visibility on the actual payload scene GUID.

New tiny startup scene: START GAME / COPY STARTUP REPORT, hardware ASTC check, async world loading, previous managed stage and managed error/low-memory events persisted. Native driver abort/OS kill may produce no managed exception; checkpoints cannot diagnose the cause or certify a GPU frame. No automatic retry/crash loop. This is a reliability/diagnostic improvement, not crash acceptance.

APK must be<500,000,000bytes (87 was631,625,961). Independent workflow verifier requires payload and size receipts so a merely signed oversized/ungated APK cannot bypass build failure. Triangle budgets target6k per textured mesh/4k tree, remove same-UV fold locks while actual UV seams and open boundaries remain protected. Targets are NOT actual achieved counts. Source road sample now uses the projected96triangle surface too. Player collision/doorways, alpha foliage, normals, shape error, GPU format/API, actual startup and warmed-up FPS still need native/device validation. No resolution/MSAA/lighting/shadow-distance cuts in this revision.

All gameplay cases now load the ACTUAL prepared Android payload scene explicitly, rather than testing the full editor/source scene. The old source-reference LOD case is superseded by a prepared-mesh/actual-ASTC/position-only-collision case; startup-menu-to-prepared-world smoke test added (24 required cases total). Major-axis silhouette bounds must retain>=90% span and<=5% centre shift; this is a conservative structural check, NOT full mesh error/visual acceptance. UV-island seams and authored open boundaries remain protected. No source-scene fallback if preparation is missing.

Native QA runs before the slow full gallery; public candidate publication is moved AFTER the named native gate. Complete gallery remains required for overall workflow release. No more phone-test public APK merely because a signature succeeded when native checks never executed.

## Candidate 88 actual CI result (42770984114)

- Signed APK budget receipt: 234,948,972 bytes (87: 631,625,961); not device acceptance.
- Actual prepared-scene native tests: 22/24 passed. Public release correctly blocked.
- Prepared visual mesh / ASTC / position-only collision case passed: 35 LOD groups,
  32 reducing groups, 592,827 all-near group triangle instances (not visible GPU triangles).
- Payload receipt: 100 textures, 248,898,800 raw compressed bytes; unchanged dimensions/mips,
  lossy ASTC. Collision instances 2,914,598 -> 372,064 triangles.
- Boot scene activated, but floor check hit height 1.73m consistent with player capsule.
  Follow-up excludes only player hierarchy, retaining ground height and adding normal check.
  This is a test correction, not evidence of a device-crash fix.
- Full pose/reload capture exceeded its 120-second software-GPU test allowance
  (reported duration 689s). Follow-up allows 30 minutes without dropping poses,
  resolution, assertions or any required case. This is not a phone performance pass.
- Physical SM-E236B launch, texture/road/shape review, rapid-turn acceptance and FPS
  remain unverified. Candidate 88 must not be presented as gameplay accepted.
