# Optimization plan after candidate 89 feedback (planning document, no claims)

Status: plan + first corrective work. Nothing here is device acceptance. The supplied
phone screenshots and the startup report are the primary evidence; CI passes are not.

## What your feedback said

1. "Game is like before but size went down" → the 89 size reduction was real (234,965,172 B
   vs 631,625,961 B) but produced no visible benefit. Size is not frame rate; the APK got
   smaller because textures became ASTC and source references were stripped, not because the
   frame did less work.
2. "Optimize the game much better" → needs a measured order of work (below), not settings churn.
3. "Add fog, so distant things are less visible and use less processor power" → fog alone does
   not reduce GPU/CPU work by itself; the real saving is that fog lets us *hide the far clip*
   and make distance culling visually acceptable. Plan: fog + reduced far clip + distance
   culling/LOD bands, all authored and verified together.
4. "Some houses changed and pictures are missing" → confirmed as a real defect: several
   supplied houses have materials with **no base-colour image** in the source GLB
   (`materialsWithoutBaseColorImage` in `QA/village-source-inventory.json`: `Bd house 3`,
   `Bd house 4`, `Bd house 5`, `Bd house 8`, `Bd house 9`, `Bd house oo`). They currently
   render as flat colour or get a generic plaster/wood/concrete fill from
   `VillageSurfaceRepair`, so doors/windows/brick work disappear. The supplied images must be
   used, and a house must never be repainted by a generic material.
5. New startup report: crash `ArgumentNullException: Value cannot be null. Parameter name: shader`
   at `Fpp.GunplayFeedback.Awake()`, plus stage `world-render-submitted` (previous) versus
   `boot-menu` (current), GLES3, Adreno 619, build `6e1e7e54…`. That build GUID is **not** any
   build in this repository's history (CI 89/90 GUIDs differ), so I cannot attribute the crash
   to a specific commit. It is nevertheless reproducible from the code: `GunplayFeedback.Awake`
   calls `new Material(Shader.Find("Standard"))`. In a player build `Shader.Find` returns null
   when the shader is not referenced by any built scene and is not in Always Included Shaders;
   `Material(null)` throws exactly this `ArgumentNullException`. `Sprites/Default` is a second
   unguarded `Shader.Find` in the same method. **This is being fixed first** (see step 0).

## Measured starting point (not assumptions)

| Fact | Value | Source |
|---|---|---|
| APK bytes | 234,965,172 | `android-apk-budget.txt`, run 89 |
| World textures | 100 textures, 1.88 GB RGBA-equivalent → 248.9 MB ASTC | `android-payload.json` |
| Enabled triangle instances (all LOD0) | 592,827 | `world-lod-native.txt` |
| LOD groups / generated meshes | 35 / 48, near total ≤ 900k gate | `world-lod-audit.json` |
| Collision triangles | 2,914,598 → 372,064 | `android-payload.json` |
| Static occlusion bake | completed (`VISIBILITY BAKE END True`) | `build-progress.txt` |
| Main camera far clip | **700 m** in a 120 × 120 m district | `CottageMap.cs` |
| Distant geometry policy | "all distant meshes stay available … no distance disappearance" | `WorldDetailLod` report |
| Road surface | projected 2048×4096 atlas, ~97% coverage, near-black on phone | `road-bake-coverage.txt`, user screenshots |
| House-local submesh submissions | 155 after batching (was 598) | `house-material-batching.txt` |
| Phone frame time | **no valid measurement** for 89; 83 measured ~3.5 FPS | `FrameTimeEvidence` history |

Key reading: 700 m far clip, 592k near triangles, 100 uncompressed-source textures and 4× MSAA
on an Adreno 619 with GLES3 is a fill-rate and bandwidth problem before it is a polygon problem.
Fog + a shorter far clip is therefore the single largest correct lever, exactly as you said.

## Ordered plan

### Step 0 — fix the crash that stops the game (blocking everything else)
- Remove both unguarded `Shader.Find` calls from runtime code. Use URP shaders that are
  guaranteed present because they are referenced by built scene materials, or resolve through
  a serialized material reference baked into the payload.
- Add an editor-side gate that fails the build if any runtime `Shader.Find` result is missing in
  the player (compile-time list of required shaders + presence assertion in the payload audit).
- Add a Play Mode test that the feedback pools build without exceptions in the built payload.
- **Honest limit:** I cannot run Unity here. This fix is code-level and must be confirmed by
  the next CI Play Mode run and then by your phone.

### Step 1 — fog and distance design (your explicit request)
- Author a proper linear-distance fog (near/far) matched to a **reduced far clip** (~180–220 m
  for the 120 m district plus a little sky margin) instead of `ExponentialSquared 0.008` with
  700 m clip. Keep the exponential-squared look if you prefer it, but the far clip and fog must
  agree so the horizon fades instead of popping.
- Move fog into the URP presentation settings so all three day presets share it, and make it
  runtime-adjustable (Settings menu) without rebuilding the pipeline.
- Add distance bands: beyond ~60 m → LOD medium, beyond ~110 m → LOD far, beyond the fog
  saturation point → culled. This is where the CPU/GPU saving actually comes from.
- Verify no visible hard cut: fog colour must match sky at the horizon for each preset.

### Step 2 — make the far clip and LOD bands real (perf, visibly justified)
- Lower `Camera.farClipPlane` to the fog saturation distance; keep the skybox visible.
- Use `Camera.layerCullDistances` for small props (stones, pebbles, hay, debris cards) so they
  vanish a short distance into the fog rather than being drawn to 700 m.
- Retune LOD screen-relative heights against the new far clip so medium/far bands are hit at
  the distances above; keep hard-transition honesty for shaders without crossfade keywords.
- Gate: a Play Mode assertion that the far clip, fog saturation distance and LOD thresholds are
  mutually consistent (no geometry drawn beyond fog opacity ~0.98).

### Step 3 — texture residency (biggest memory/bandwidth cost)
- 100 world textures at up to 4K/8K source resolution compressed to ASTC 6×6 is still 249 MB of
  GPU texture plus mip traffic. Add **texture streaming or explicit per-material max size** so
  distant houses use lower mips, and drop mips to what the new far clip can actually show.
- Re-check the projection atlas for the road: 2048×4096 at ~97% coverage is fine for near, but
  the road is the most screen-covering surface; measure its real cost before changing size.
- Never lower quality silently: every reduction gets a before/after still and a receipt.

### Step 4 — the missing textures / changed houses (visual correctness)
- Stop `VillageSurfaceRepair` from substituting generic plaster/wood/concrete for materials that
  intended a supplied image. For the six affected houses, re-import the source base-colour
  images from the original GLB and bind them; where the GLB truly has no image, keep the
  authored `baseColorFactor` colour and **say so** in the receipt instead of inventing a texture.
- Reproduce the "changed" houses visually: before/after gallery frames for all 18 models.
- Gate: per-model receipt listing every material, its source image GUID, and whether it is
  authored or missing. Fail the build on any "missing" material silently falling back.

### Step 5 — the near-black road (visible, cheap to fix)
- Measured: road crop mean RGB ≈ (40, 42, 30) against grass (50, 62, 30) — the road is not red
  brick as intended, it is nearly black. Likely the projected atlas samples shadowed scan texels,
  or `_Smoothness` in the albedo alpha overrides the tint. Investigate the bake shader's
  height/side filter and the source scan's exposure before repainting anything.
- Fix only from the original scan; do not substitute generic brick art.

### Step 6 — measure on the phone (acceptance, not inference)
- Use the existing `FrameTimeEvidence` + startup checkpoint to export real frame intervals,
  `Screen` resolution, PSS and GPU name/API. The 89 screenshots prove the game runs; only a
  measured build can say whether it is playable.
- Compare two builds at the same viewpoint: (a) current 89, (b) fog + far clip + LOD bands.
  Report p50/p95 frame time and triangle/draw counts, not just "feels better".

## Explicit non-goals for this round
- No Unity version change, no render-pipeline swap.
- No blanket texture resize, no lowering MSAA/shadow/resolution without an explicit decision
  recorded here.
- No "lossless" labels on lossy work; ASTC stays ASTC.
- No claim of phone startup/FPS/visual acceptance from CI.

## Execution order I will follow
0 (crash) → 1 (fog/far clip) → 2 (bands + cull distances) → 4 (textures correctness, because it
is a visible defect you reported) → 5 (road) → 3 (residency) → 6 (device measurement), with the
CI Play Mode suite and gallery re-run at each step and no release published until the crash fix
is confirmed by a green native run.
