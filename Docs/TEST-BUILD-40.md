# Android test build 40 — grip, smoke, ground and daylight

- **APK:** https://github.com/black-glass-dev/Fpp/releases/download/test-build-40/Fpp.apk
- **Release:** https://github.com/black-glass-dev/Fpp/releases/tag/test-build-40
- **48,658,799 bytes** (48.7 MB decimal), Android 8+, ARM64 / GLES 3.
- Package `dev.blackglass.fpp`, version 0.0.2, version code 40.
- GitHub asset SHA-256: `49c29069acd994831e43d20bfaad9216d587195d3c552550cd1976651002be6f`.
- Source gameplay commit: `ce92595`; workflow: https://github.com/black-glass-dev/Fpp/actions/runs/42350970011

## What the four build-38 phone pictures showed

The AK support hand was hanging near the trigger hand instead of establishing a useful support hold; glove/wrist pose and open fingers were conspicuous. The custom upper-left FIRE button overlapped the status readout. Lighting/sky were very flat, boundary texture scale was stretched, and ground detail/relief was difficult to read. Smoke visibility was reported by the user; the supplied stills themselves were not active firing frames.

## Changes

- AK now uses an approximate **magwell/receiver support hold**, calibrated from actual wrist, index, middle and pinky landmarks. A bounded upper-arm correction follows the authored elbow plane; it is not the earlier guessed-pole arm swing. The palm is oriented from anatomical landmarks instead of an arbitrary roll. Intermediate poses with fingers above the sight were rejected. This is still a procedural retarget, not professionally authored rifle animation; exact finger contact/wrist skinning can still need polish.
- Kept original Mark23 reload and the build-38 secondary reload system.
- Smoke now has 0.65–1.05-second particle lifetimes, larger expanding puffs, drift/turbulence, billowy shader breakup and a 96-particle cap. Repeated shots accumulate visible gas instead of an almost invisible brief spot. Effects still emit at the final posed muzzle; no projectile sphere was added.
- Tracers use shorter 2-metre streaks at 420 m/s with a thinner pale-warm line, instead of long laser-like segments. Damage remains hitscan.
- New original **2048×2048 grass and soil albedo** with **1024×1024 height-derived normal maps**. The supplied source model textures were not downscaled to pay for these. Ground art is deterministic procedural art, **not photographic PBR scans and not MWII assets**.
- World-scale layered ground shader blends exposed earth/grass, larger patch variation and subtle tracks. Road edges feather into surrounding grass. Increased relief outside the intentionally flat road/building/mosque pads; mesh collision still matches terrain.
- Warm angled sunlight, sky/equator/ground ambient lighting, procedural sky, light distance haze and cottage shadows. Runtime configuration retains hard shadows with **28 m distance, one shadow region/no cascades and medium resolution**, while retaining the 30 FPS cap and no MSAA. These are bounded defaults, not a claim of measured phone performance.
- Boundary/cover texture uses world scale instead of being stretched across a long wall.
- Status readout shifts out of the way when the saved layout puts controls at upper-left. **The user's controls are not reset.** Native HUD audit exercises this case.

## Checks and truthful limits

- **22 local tests passed**; model catalog references/counts validate: 135,088 pack triangles plus 384,658 mosque triangles.
- Native Unity generated the APK and ran the existing rig/texture/HUD/reload/ammo/physics checks; fresh APK ZIP/ARM64 contents and signature passed, and the release was published.
- Inspected actual Unity camera renders of hip/ADS AK, ground close-up, daylight environment, single-shot effects and three-shot lingering smoke. These are editor camera images, **not phone screenshots**.
- Source-storage optimization deduplicated exact mosque position/normal entries and shortened numeric notation. Every ordered triangle corner's float32 **position, UV and normal** was compared against the prior source: all 384,658 triangles matched exactly. This is not a geometry quality reduction.
- The builder wrapper again reports a cleanup/build-exit warning. Release acceptance depends on independently verified fresh signed APK output; licensing cleanup is not claimed fixed.
- No physical-phone test, FPS/heat/battery measurement or MWII-quality parity claim. More demanding lighting/materials must be checked on the user's device. Finger placement remains approximate.

### Current native review images

- [AK held](Previews/unity-ak-held.png), [AK ADS](Previews/unity-ak-ads.png)
- [Ground close-up](Previews/unity-ground-detail.png), [lighting/environment](Previews/unity-environment.png)
- [Single shot](Previews/unity-shot-effects.png), [lingering smoke](Previews/unity-smoke-burst.png)

The smoke image uses the same effect code with three emissions simulated 0.12 seconds apart, flash/streaks hidden afterward to inspect gas visibility. It is a deterministic effect review, not a captured phone burst.

## Phone check / install

Check AK support hand while looking around and aiming, smoke after single shots and bursts, ground detail while walking, shade near cottages and FPS stability. Default Mark23 should retain its original reload.

Full APK, not a delta patch. If a differing debug signing certificate prevents updating, uninstalling the prior test may be necessary; **uninstalling removes the saved HUD layout/settings**. Persistent signing remains to be configured.

## If the user supplies ground textures

For a more photographic finish, send two tileable sets: **dry/compacted dirt with gravel** and **short mixed grass with exposed soil**. Ideally each set has 2K or 4K albedo/base color without baked directional shadows, OpenGL (+Y) tangent normal, roughness, and optional height/AO. PNG/TGA or a ZIP is fine. Keep a consistent real-world tile size (for example 2×2 metres). Android can use 2K albedo and a smaller normal map where appropriate; source resolution alone does not guarantee realistic material quality.
