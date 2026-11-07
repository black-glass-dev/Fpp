# Prompt 2 matched-capture ledger — work in progress

Baseline: verified test-build-47 source `6ecabc09427d7f905cf6e04502e79c7cc9c0abf1`; preserved preview commit `31ca449`. Links below are genuine Unity camera frames, not generated art. No before image is copied into Git: snapshot storage is bounded. After column remains empty until matched native renders exist. Capturing a different angle is not a matched comparison.

| Camera/state | Before | After / acceptance |
|---|---|---|
| Village street | [street](https://github.com/black-glass-dev/Fpp/blob/31ca449/Docs/Previews/unity-environment.png) | Pending |
| Heritage dome entry | [entry](https://github.com/black-glass-dev/Fpp/blob/31ca449/Docs/Previews/unity-mosque-entry.png) | Pending |
| Heritage interior | [interior](https://github.com/black-glass-dev/Fpp/blob/31ca449/Docs/Previews/unity-mosque-interior.png) | Pending |
| Mark23 ADS | [ADS](https://github.com/black-glass-dev/Fpp/blob/31ca449/Docs/Previews/unity-mark23-ads.png) | Pending |
| AK ADS / burst | [ADS baseline](https://github.com/black-glass-dev/Fpp/blob/31ca449/QA/Previews/ak47-ads.png) | Matched burst frame still required |
| AK reload midpoint | [reload](https://github.com/black-glass-dev/Fpp/blob/31ca449/Docs/Previews/unity-ak-reload.png) | Pending |
| Dusk preset | Not captured in baseline; must use same street camera before/after preset addition | Pending |
| Terrain seam diagnostic | [ground detail](https://github.com/black-glass-dev/Fpp/blob/31ca449/Docs/Previews/unity-ground-detail.png) | Two exact diagnosed seam coordinates not established; do not claim removed |

## Gates

- AUD-1 first integration adds real native non-silent clip/four-layer/pool tests alongside all13 gameplay cases; native result pending.
- URP/lighting/PBR/terrain/decals/LOD/post/hands/HUD quality gates: OPEN.
- Recorded mixed-output loudness/stems/spectrum/reflection proof: OPEN.
- Three-minute device DRS/frame/draw/triangle/memory/GC/SRP-batcher profile: BLOCKED without a physical Android profile; Editor results cannot substitute.
- FINAL_REPORT.md intentionally absent while these gates remain open.
