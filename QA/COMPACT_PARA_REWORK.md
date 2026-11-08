# Compact para rework — requested after device screenshots

## Acceptance status
Source implementation and local guards only until the next real Unity build, camera gallery and complete gameplay XML are reviewed. NOT an FPS/device/art acceptance report. Diagnostic71 is the rejected oversized prototype; do not recommend it as the corrected map. Diagnostic72 never became an accepted APK.

## What the device screenshots established
- The85m blanket road target produced a28.3m-wide brick road; source UV projection made brick texels look like giant slabs.
-18m default house footprints inflated several modest houses/shops to11–16m heights. Exact-name `Door` calibration missed authored `Door_Main_L`, `Door_Entry_L` and similar panels.
-600x440m placed houses/trees far apart. This did not implement the requested compact, populated neighbourhood.
- Missing material images, intentional plain colours and poor source scan textures are different problems. A shader that renders without magenta is not proof of adequate texture quality.
- The supplied house3 includes alternate `House_LOD1/2` and UCX proxy meshes; drawing/colliding all of them simultaneously is not a full-quality presentation.

## Research basis, not an exact surveyed reconstruction
- Courtyard/uthan as the organizing space of the extended-family homestead; the cited study also discusses an Ullahpara/Sirajganj homestead: [3](https://www.researchgate.net/publication/237711110_The_rural_Bangladeshi_courtyard).
- Longitudinal studies show homesteads changing through additions around shared courtyards, including Tangail and Sirajganj examples: [4](https://www.researchgate.net/publication/321702845_The_Continuous_Process_and_Purposes_of_the_Transformation_of_Rural_Settlements_in_Bangladesh).
- Old Dhaka research describes a hierarchy of courtyards, lanes, intersections and bazaars, with compact streetside houses: [2](https://www.scipedia.com/public/Sazzad-Hossain_2013a).

Applied as a fictional mixed para: compact two-storey streetside block on the east, small rural houses around a shared west uthan with surrounding trees, central existing mosque/courtyard and a small northeast pond. This is NOT a literal model of Old Dhaka or Sirajganj. Numeric game dimensions below are design assumptions, not surveyed standards.

## Explicit physical layout
`Assets/Editor/VillageLayout.json` maps every original18 model by exact source name; no usable model silently omitted.
- Playable rectangle120x120m, versus600x440m. Area14,400m2 versus264,000m2. Smaller map is an expressly requested layout change, NOT claimed as a quality-preserving optimization by itself.
- City roads4.2m wide; connected inner alleys2.4m, with cross-links between rows.
- Authored individual door-panel median2.1m when available; named cases accepted only inside a bounded parcel. Scan-only models use explicit per-asset height references, pending close-up review, not a global18m footprint.
- Road scales are uniform to4.2m width, preserving original geometry/aspect ratio. Brick detail is world-metric:1.92m map repeat contains8cells/16brick widths (~24x12cm), independent of source UV stretching.
- Rural shared courtyard7x11m;21 supplied tree instances placed around west homes/gardens and3 beside the pond, not a distant perimeter ring. Existing two tree props moved away from new house interiors.
- Pond water18x14m with sloped collision basin and ghat. Water shading remains provisional, no swimming claim.
- Old cottages fit10m footprints, with extra mosque clearance for the northern pair. Spawn, weapon ownership, rig, controls and core range remain.

## Lossless performance work, separate from the redesign
- Full-detail geometry stays intact. Disable author-supplied alternate LOD/proxy presentation; no distance-based low LOD, simplification, texture downsize, resolution/shadow/lighting/MSAA cut.
- Exclude UCX/alternate LODs from newly generated triangle colliders; retain actual full-detail door/window geometry. Tree trunk helpers remain collision-only.
- Canonicalize textures only when nonzero engine content hash, dimensions, graphics format, mip count and all sampler states agree. Clone generated materials; do not alter original source assets. Report actual reference/object counts; no invented savings when hashes are unset.
- Retain shared buffers for repeated, instancing-eligible URP/glTF meshes instead of expanding static copies. SRP Batcher can take precedence; eligibility is NOT a measured draw-call reduction.
- Conservative Unity static occlusion bake, smallest hole.25m; trees do not become solid static occluders. FPV is still excluded from world occlusion. Full geometry remains available; no manual distance hiding.
- Cache HUD motor lookup and avoid unchanged SetActive calls. Keep existing rendering effects/hand stabilization.
- Fix legacy mobile bootstrap silently capping URP framebuffer to1280x720. URP now keeps the native framebuffer; DRS remains explicit opt-in, default off. Restoring native pixels can itself cost GPU time; do NOT call this a measured FPS improvement.
- Bounded600-frame wall-clock sampler, no per-frame strings/files/sorting or forced overlay. SETTINGS > COPY PERF exports observed mean/p95/max frame intervals and framebuffer size. Includes genuine slow frames, excludes startup/menu/pause, not CPU/GPU bottleneck attribution or device certification.

## Texture notice and scope
User notified before this stage. Original ZIPs/GLBs and existing supplied PBR images remain unchanged. Deterministic original2048px plaster/concrete/wood colour+normal maps are shared among untextured opaque semantic materials, using world-metric projection. Intentional metal/glass/leaf colours and cutout/transparent materials are not blanket-replaced. Shop's untextured scan gets provisional wooden surface detail. Existing blurry scan images are not falsely labelled fixed merely because they exist; review each close-up. Generated PNG maps use explicit normal-map import and uncompressed originals, not a hidden lossy texture downgrade.

## Route validation
All14 planned lane/courtyard rectangles form a single connected walk network, including cross-links through gaps between old cottages, the west uthan approach and pond ghat approach. Conservative city parcels leave2.5m between rows for the2.4m alley and separate the east street from the north cottage pair. Fixed ground shading incorrectly restricting assigned dirt paths to world x=0. Rectangle connectivity is not an interior/doorway walk-through proof.

## Required real evidence
`QA/runtime/helper-exclusions.txt`, `material-detail-repairs.txt`, `lossless-world-audit.json`, `visibility-bake.txt`, `build-progress.txt`, `test-progress.txt`; actual18-model/pond PNGs; independent signed APK verification; full22-case XML. Review road contacts/steps, house scale/entry paths, interior clearances and culling at doorway/elevation. Compare the same device/tier/route before/after, with native pixels/effects unchanged. No final report or highly-optimized/device-FPS claim until measured.
