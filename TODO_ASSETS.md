# Missing assets / integration hooks

- Production rifle-specific hands/finger contact and tactical/empty magazine exchange animations: current authored Mark23 rig is retained; secondary retarget is approximate.
- Sprint/slide/dive/mantle/mount animation clips: use named procedural poses until real clips exist.
- Per-weapon shot/tail, mechanics, dry-fire, reload, surface footsteps, impacts, ricochets, shell landing, movement and UI sounds: procedural click/shot fallback, named event bus routes for Prompt 2.
- Dedicated shell and impact VFX/decal assets: pooled greybox fallback; Prompt 2 can subscribe without changing damage logic.
- Body-part target art: labelled greybox colliders are sufficient for gameplay tests, not final art.
- No remote runtime listener may write this repository document on Android; missing audio routes are recorded here at integration time, runtime emits events without failing.

## Prompt 2 audio placeholders
`SYNTH_PLACEHOLDER_layer_0..6_variant_0..3`: original deterministic crack/body/mechanical/LFE/tail/hit/wind synthesized at startup. Replace with licensed recordings for each weapon/surface/zone. Current foley shares procedural layer families, not distinct boot/cloth or pouch recordings. Missing: mixer asset and true-peak limiter, measured loudness-compliant recorded mix, zone volumes and tailored indoor/outdoor variants, breathing/birds/cloth/radio beds, compressed Vorbis import replacements, audio/haptic UI integration. Procedural clip peaks are bounded individually; final summed-output headroom still requires actual output capture.

## Prompt 2 rendering / rig blockers
URP14 coordinated shader/material/camera/capture/tier source migration added, awaiting fresh native verification; this is not complete material/GI/visual acceptance. Missing: complete hero MRAO/detail maps, baked GI and zone probes, authored layered sky/clouds, height-fog/shaft pass, decals, PC camera-stack-compatible TAA/CAS solution. Production secondary hand contact/rig/reload exchange remains missing. No target Android profiler attachment/device is available.
