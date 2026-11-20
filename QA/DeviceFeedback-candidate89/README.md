# After candidate 89 — what I found and what changed

## 1. The crash in your startup report — found and fixed

`Managed errors: ArgumentNullException: Value cannot be null. Parameter name: shader`
at `Fpp.GunplayFeedback.Awake()`.

That is `new Material(Shader.Find("Standard"))` where `Shader.Find` returned **null**.
In a player build, `Shader.Find` only sees shaders that a built scene references or that are in
*Always Included Shaders*; the editor finds everything, the phone does not. `Material(null)`
throws exactly the exception you pasted. The same method had a second unguarded
`Shader.Find("Sprites/Default")`.

Fixed three ways so it cannot come back:
- `RuntimeShader` resolves the shader and falls back to a URP shader that is always present, so a
  missing cosmetic shader degrades one effect instead of killing the player.
- `RequiredShaderGate` adds every runtime-by-name shader to Always Included Shaders during the
  build and **fails the build** if any is still missing.
- New Play Mode test `FeedbackEffectShadersResolveInPreparedPayload` rebuilds the pools inside the
  real prepared payload, which is where it crashed.

Note: your report says `build=6e1e7e54…`, which matches no build in this repository, so I cannot
prove that APK came from this branch. The code path is identical, and it is now fixed regardless.

## 2. Fog + distance (what you asked for)

The game was drawing out to **700 m** in a 120 m village. Now:
- Linear fog starts at **40 m**, is fully opaque at **130 m**.
- Camera far clip is **145 m** — geometry past that is not submitted at all.
- LOD medium at **55 m**, far at **95 m**, computed per object from the real FOV projection.
- `WorldDistanceBands` stops drawing world renderers past the fog point and restores them when
  you walk back. It never hides first-person geometry, LOD-group members, or anything with a
  collider (so no invisible walls).
- A Play Mode test asserts fog, far clip and LOD bands all agree, so this cannot silently drift.

Fog by itself does not save work — it is what makes the shorter draw distance invisible. The
actual saving is the far clip and the cull.

## 3. "Some houses changed and the pictures are missing"

I checked the supplied archives' recorded metadata: these houses genuinely contain materials with
**no base-colour image** — `Bd house 3`, `4`, `5`, `8`, `9`, `oo`, plus some props
(`Iron`, glass/metal parts). Those render as flat paint, which is what you see as blank walls.
That is missing source art, not a broken import or a compression bug.

New in this build: `ModelTextureAudit` writes a per-house, per-material receipt of exactly which
image is bound, and counts untextured plain-white materials. So instead of guessing, the next
build tells us which house needs work. I have **not** invented replacement textures for your art.

## 4. Fill rate on your phone

Mobile builds now default to **2× MSAA** instead of 4× (4× is still selectable in Settings →
QUALITY). At native 1080p on an Adreno 619-class GPU, 4× MSAA plus bloom is a large per-pixel
cost, and it multiplies every pixel the fog and far clip just saved.

## 5. Road still too dark — not fixed yet, deliberately

Measured from the committed gallery still: road ≈ RGB (46,49,33) against grass (50,62,30) — the
"red brick road" reads as near-black. I could not download the source pack from this environment
(Google Drive is blocked here), so I did not guess a texture fix. The build now records the
projected source albedo's own mean RGB: if that mean is dark, the supplied scan is dark; if it is
bright, the fault is in shading. That single number picks the right fix next round.

## Status right now
- CI run 92 is building the APK with all of the above. I am not pushing anything else to avoid
  cancelling it (run 91 was cancelled by exactly that — GitHub ran the newer queued run instead).
- **Nothing here is phone acceptance.** Startup, frame rate and looks still need your device.
- The only APK you can install today is still candidate 89. I will hand you a new one only after
  its native Play Mode suite is green.
