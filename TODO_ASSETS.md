# Missing assets / integration hooks

- Production rifle-specific hands/finger contact and tactical/empty magazine exchange animations: current authored Mark23 rig is retained; secondary retarget is approximate.
- Sprint/slide/dive/mantle/mount animation clips: use named procedural poses until real clips exist.
- Per-weapon shot/tail, mechanics, dry-fire, reload, surface footsteps, impacts, ricochets, shell landing, movement and UI sounds: procedural click/shot fallback, named event bus routes for Prompt 2.
- Dedicated shell and impact VFX/decal assets: pooled greybox fallback; Prompt 2 can subscribe without changing damage logic.
- Body-part target art: labelled greybox colliders are sufficient for gameplay tests, not final art.
- No remote runtime listener may write this repository document on Android; missing audio routes are recorded here at integration time, runtime emits events without failing.
