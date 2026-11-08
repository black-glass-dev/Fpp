# Prompt ১ + Prompt ২ — অসম্পূর্ণ/ত্রুটিপূর্ণ কাজের কোডভিত্তিক অডিট

Baseline: প্রকাশিত build51, source18dcf74; branch checkpointf5ce1d7. আগের15টি automated case pass = দুই prompt সম্পূর্ণ নয়। ব্যবহারকারীর বাস্তব bug report-কে অগ্রাধিকার দেওয়া হয়েছে। তালিকায় **কাজই হয়নি**, **আংশিক**, এবং **কোড আছে কিন্তু বাস্তবে অযাচাইকৃত** আলাদা রাখা হয়েছে। এটি FINAL_REPORT নয়।

## আগে পাওয়া ভুল এবং এই repair pass

| সমস্যা | আসল কারণ | পরিবর্তন / অবস্থা |
|---|---|---|
| AK নিলে default pistol হারায়; SWAP-এ আসে না | ApplyWeapon আগের gun auto-drop করত; SWAP owned inventory নয়, nearby pickup খুঁজত | চারটি persistent owned slot; distance-independent SWAP; প্রতি gun-এর mag/reserve; build52: actual native collection→100m-away→4-way SWAP/ammo case PASS; device/user play validation separate |
| আগের Glock/Field/AK-ও হারায় | একই auto-drop আচরণ, pickup button ও swap-এর অর্থ গুলিয়ে গিয়েছিল | Pickup সংগ্রহ করে; SWAP শুধু owned gun cycle করে; DROP আলাদা SETTINGS action |
| Draw চলাকালে SWAP tap উপেক্ষিত | equipping হলে নিঃশব্দ return | একটি queued SWAP; source repaired; build52 native suite17/17 PASS |
| Crouch থেকে sprint কাজ করে না | crouched state sprint-এর আগে branch নিত | headroom থাকলে sprint uncrouch করে |
| Sprint-slide দেরি/অস্বাভাবিক | crouch hold timer প্রয়োজন ছিল | single press slide / tactical press dive; entry-frame self-cancel বন্ধ |
| দেয়ালে ঠেকেও running speed/footsteps | desired momentum-কে Speed বলা হতো | actual controller displacement থেকে Speed |
| কাছে গেলে নিজের থেকে cover-এ উঠে যায় | forward movement-এই mantle | forward + jump intent আবশ্যক |
| কম ছাদের নিচে উঠে আটকে যেতে পারে | SphereCast শুরুতেই overlap হলে ceiling miss; fallback-ও বেশি উঁচু | overlap-based nonalloc clearance, self-filter, বর্তমান নিচু height রাখা |
| Prone capsule অস্থিতিশীল | configured height diameter-এর চেয়ে কম | collider height অন্তত2×radius |
| Air movement হঠাৎ brake | ground acceleration-ই air-এ চলত | configurable reduced air control; neutral air input momentum রাখে |
| Focus/menu-এর পরে অযাচিত tactical tap / overspeed | double-tap timestamp reset হয়নি; desktop+HUD vector unclamped | reset timestamp; clamp combined move |

## Prompt ১ — কোন অংশ এখনো পূর্ণ নয়

| ID | বিষয় | অবস্থা / বাকি কাজ |
|---|---|---|
| P1-01 | Unified touch/keyboard, joystick, right-look | আছে; বাস্তব multitouch/cancel/notched-device UX ব্যবহারকারীর যাচাই প্রয়োজন |
| P1-02 | Gyro / sensitivity / editable layout | আছে; gyro axis/calibration/device sampling অযাচাইকৃত |
| P1-03 | Movement FSM | আছে কিন্তু উপরোক্ত বাস্তব bugs ছিল; build52 নতুন physical regression PASS; arbitrary map/device behaviour এখনও পূর্ণ প্রমাণ নয় |
| P1-04 | Terrain/slope/step/low-ceiling/landing transitions | সীমিত coverage; সব collision geometry-তে স্থিতি প্রতিষ্ঠিত নয় |
| P1-05 | Slide/dive/mantle/mount timing | নির্দিষ্ট primitive scene-এ আগের pass; arbitrary map edges, spam inputs ও cancellation পূর্ণ যাচাই হয়নি |
| P1-06 | Sprint-এর10 hip-shot capture | sprint-out blocking-এর সঙ্গে মূল acceptance-এর conflict; decision আছে, literal capture সম্পন্ন নয় |
| P1-07 | ADS FOV/timing/center sight | actual native assertions pass করেছে; front marker centering পূর্ণ rear/front/bore alignment ও পাঁচ-pose anatomical contact প্রমাণ নয় |
| P1-08 | Ordered camera/weapon springs | মূল code আছে; exact spring-order / impact stacking / all-state visual limits-এর পূর্ণ independent audit নেই |
| P1-09 | Config-only tunables | gameplay SO আছে; legacy model-space calibration, UI/VFX/audio-তে hard-coded values এখনো আছে |
| P1-10 | Weapon FSM / drawing / same-frame fire / scheduler | implemented এবং আগের automated pass; সব rapid input interaction নয় |
| P1-11 | Owned inventory / SWAP | **build51-এ ভুল ছিল**; এই repair-এ বাস্তব inventory যোগ করা হয়েছে, আগের test সরাসরি QAEquip করায় bug ধরেনি |
| P1-12 | Physical drop/pickup / ammo | code + earlier conservation proof; এখন explicit-drop semantics ও complete inventory cycle নতুন করে যাচাই দরকার |
| P1-13 | Recoil/spread/pattern / wall CSV | মূল algorithm ও15m wall proof আছে; all-weapons movement spread/reticle-distance overlays সম্পূর্ণ নয় |
| P1-14 | Hitscan / body parts / health / hit counter | code + core pass; সব surfaces/body-boundary/penetration-thickness combination-এর বাস্তব proof নেই |
| P1-15 | One penetration / friendly fire / impact fallback | implemented logic; full material matrix independent coverage অসম্পূর্ণ |
| P1-16 | Hitmarker/kill/damage numbers/direction/feed | code আছে; feed currently generic, timed/bounded complete kill feed UX নয়; directional feedback ও device readability অসম্পূর্ণ |
| P1-17 | Reload conservation/cancel/debounce | core tests pass; sprint/dive/swap/empty-lock marker সব frame combination নয় |
| P1-18 | Four-frame tactical/empty reload art | captured; secondary mag/hand contact এখনো believable production animation নয় |
| P1-19 | Every-frame finger IK / zero floaters | **অসম্পূর্ণ**; approximate sockets/forearm retarget, পূর্ণ finger chains solved নয় |
| P1-20 | Authored state animations | default Mark23 reload রাখা হয়েছে; secondary reload ও sprint/slide/dive/mantle/mount/holster art approximate |
| P1-21 | Event bus | typed routes আছে; সব routes-এ production listener/asset নেই |
| P1-22 | Five-minute soak | native500shots/50reloads/50switches pass; helper-driven equip বাস্তব inventory bug ধরেনি, নতুন actual-input test প্রয়োজন |
| P1-23 | CPU≤1.5ms / GC≤.5KB target Android | physical device profiler নেই; Editor sample device proof নয়; কিছু CPU marker0 unusable |
| P1-24 | SYS-by-SYS fully green/final report | করা হয়নি; implementation commits + limited real proofs আছে, full acceptance/final report নেই |

## Prompt ২ — কাজই হয়নি/আংশিক রয়ে গেছে

| ID | বিষয় | বর্তমান অবস্থা / বাকি কাজ |
|---|---|---|
| P2-01 | Unity LTS + URP / Forward+ / MobileRenderer / PCRenderer | URP14/Forward+ capability fallback, Forward/GLES, custom depth/shadow ports: build54 native18/18 PASS; পূর্ণ art/perf acceptance নয় |
| P2-02 | Low/Medium/High/PC-Ultra complete tiers | চারটি serialized pipeline tier build54-এ native-tested; সব requested feature/budget mapping এখনো সম্পূর্ণ নয় |
| P2-03 | DRS .70–1.00 /2-frame hysteresis / MSAA4 / PC TAA+CAS | .70–1.00/two-frame DRS ও4×mobile MSAA config build54 native-tested; PC camera-stack TAA unsupported, SMAA fallback; literal TAA+CAS red |
| P2-04 | FPV camera stack | Base+layer8 Overlay/independent FPV/source capture build54 PASS; আসল PNG-তে HUD নেই—ungraded UI capture/pixel repair native55 pending |
| P2-05 | SSAO/decal/volumetric renderer features | Med+ downsampled URP SSAO আছে; decals/volumetric features ও দৃশ্যমান quality acceptance বাকি |
| P2-06 | Sun shadow80/200m,2/4 cascades,5500K | 80/200m,2/4cascades config native-tested54;5500K sun/soft shadows source; matched shadow/device-quality proof বাকি |
| P2-07 | Baked GI64/128, light probes, zone reflection probes | **নেই** |
| P2-08 | Real PBR glass windows | পূর্ণ conversion/zone reflections **নেই** |
| P2-09 | Layered clouds/aureole/haze; Golden/Noon/Dusk presets | Golden/Noon/Dusk sun/fog presets আছে; layered cloud/aureole/height haze পূর্ণ নয় |
| P2-10 | Height fog / shafts / dust motes | base fog আছে; requested height/volumetric/shafts ambience **নেই** |
| P2-11 | Adaptive exposure | roof-probe EV fallback আছে; histogram/luminance-metered adaptive exposure নয় |
| P2-12 | ALL URP Lit albedo/normal/MRAO/detail;2K hero | **হয়নি**; existing ground normals আর supplied textures ≠ complete material pass |
| P2-13 | Object variation / UV jitter / macro / triplanar | ground macro blending কিছু আছে; all-object/distance proof নেই |
| P2-14 | Terrain4–8 layer height splat / seam fixes / wear decals | existing2-layer custom ground; requested rebuild ও দুই diagnosed seam proof নেই |
| P2-15 | Instanced clutter / modular house props / bevel covers /3 LOD trees | existing scattered assets আছে; complete pass **নেই**, checker covers রয়ে গেছে |
| P2-16 | Heritage arch UV/settle/edge wear | previous scan cleaning/entry repairs আছে; new UV/PBR/weathering pass নেই |
| P2-17 |128 pooled25s decals / material variants | **নেই**;16 short-lived generic puff pool আছে |
| P2-18 | Crossed flash + inner glow +40ms light/range4 + suppressed variant | inherited single flash/point light/smoke আছে; requested full effect/config নেই |
| P2-19 | Tracer350m/s, droop,60m fade | inherited every-N tracer আছে; exact requested flight/art/variant নেই |
| P2-20 | Rigid casings8s/cap12/bounce |24 greybox ballistic capsules/2s; Rigidbody/true bounce/material art নেই |
| P2-21 | Dirt/concrete/wood/metal/glass impacts + decal + high light | generic puff আছে; material-specific systems নেই |
| P2-22 | Sustained heat shimmer / target puff+spark / soft particles cap400 | পূর্ণ implementation/proof নেই |
| P2-23 | Low-poly production hands, glove normals/wear/finger contacts | **অসম্পূর্ণ**, approximate inherited rig; visible floating/unbelievable fingers |
| P2-24 | Per-weapon authored mag exchange / retention / all animation states | **অসম্পূর্ণ**; no complete artist-authored secondary clip set |
| P2-25 | Weapon material sheen/wood coat/screw normals | complete pass **নেই** |
| P2-26 | Viewmodel motion blur / world blur / ADS DOF tiers | High optional URP camera motion blur; separate viewmodel/object blur ও ADS DOF নেই |
| P2-27 | ACES/LUT/bloom/vignette/CA/grain/sharpen | ACES/bloom/vignette/CA/grain/grade build54 foundation; Low LUT/sharpen/native CAS এবং complete contrast proof বাকি |
| P2-28 | Real AudioMixer buses / limiter / ducking | নতুন6-bus AudioMixer/source routing/3dB bus duck source committed; fresh native pending; true-peak limiter নেই |
| P2-29 | Four shot layers / variant pools | original synthesized placeholders আছে; per-weapon licensed/production sounds নেই |
| P2-30 | Environment tails/reflections |4 rays/delayed placeholders আছে; full zone-tag/indoor/open/village mix নেই |
| P2-31 | Distance classes/occlusion/muffled variants | generic distance/lowpass আছে; pistol120m vs AR200m/custom attenuation/zone variants অসম্পূর্ণ |
| P2-32 | Surface×gait foley / exact reload foley / breathing | generic layer families; distinct production banks ও breathing নেই |
| P2-33 | Bird/cloth/wind/radio zone ambience | synthetic wind only; zone crossfades/random birds/radio stub integration নেই |
| P2-34 | Hit/kill/surface/ricochet audio direction | generic event sounds; complete differentiated mix নেই |
| P2-35 | −14LUFS /−1dBTP / stems/spectrum proof | actual Unity stereo PCM/stem exporter + independent FFmpeg BS1770/spectrum source added; native output/−14LUFS/−1dBTP compliance এখনও অপ্রমাণিত |
| P2-36 | Vorbis imports / priority32 voices | 32-source pool; নতুন editor-baked Vorbis placeholder import source/native codec gate pending; production recordings নেই |
| P2-37 | New HUD / glyphs / safe area / pressed states | foundation আছে; full HUD/fonts/feed/low-ammo pulse/menus polish বাকি |
| P2-38 | Haptics | **নেই** |
| P2-39 | Condensed licensed font / typography / WCAG-lite | licensed Roboto Condensed SemiBold subset/OFL source added; fresh native UI/typography validation pending; WCAG measurement বাকি |
| P2-40 | New settings toggles / audio sliders / full menu restyle | নতুন quality/day/grain/blur/persisted DRS ও6 mixer slider source/modal native regression added; fresh native pending, পূর্ণ menu polish নয় |
| P2-41 | UI atlas single batch / no per-frame layout rebuild | icon atlas আছে; text/circle আলাদা batches, exact draw-call/perf proof নেই |
| P2-42 | Occlusion culling / every building/tree LOD | **নেই** |
| P2-43 | Device58–60FPS3min /220 draws /350k tris /400 particles /900MB /0GC /95% SRP | বাস্তব Android budgets অযাচাইকৃত; SRP now implemented but95%/0GC/58–60FPS/texture/triangle gates measured নয় |
| P2-44 |8 matched gallery / shadows / tiling / VFX / reload / post proof | partial real gallery7 pairs; dusk missing; remaining visual acceptance হয়নি |
| P2-45 | Complete FINAL_REPORT | তৈরি হয়নি, কারণ full implementation/acceptance green নয় |

## কাজের অগ্রাধিকার

1. অস্ত্র হারানো, inventory/ammo/drop, movement control/collision — আগে gameplay-blocking repair।
2. বর্তমান animation/contact, readable UI ও settings/audio integration।
3. URP migration-এ custom ground/scan clipping shaders এবং Camera.Render-based QA capture একসঙ্গে port করা; package-only switch নয়।
4. lighting/probes/PBR/terrain/clutter/VFX/post/tier fallbacks।
5. renderer/audio/actual-device evidence। ব্যবহারকারীর হাতে পরীক্ষা হলেও compile/crash/regression sanity developer-এর দায়িত্ব।

বাকি সফটওয়্যার কাজকে শুধু “device নেই” বলে লুকানো হয়নি। উপরের missing items এখনো missing, যতক্ষণ না source implementation ও উপযুক্ত evidence আসে।

## ২০২৬-১১-০৭ — যাচাইকৃত repair checkpoint
Build52/run42384788788 actual Unity PlayMode17/17 PASS, including real pickup→100m-away inventory roundtrip and physical movement regression. APK48,767,351B, SHA256c680ba3da9a46b90801ffd78893560882b069078fa2b76120e0cd4604b8102a0. এটি graphics/audio/device acceptance নয়। নতুন URP source checkpoint এখনো native-pending; build52-এ URP নেই।

## ২০২৬-১১-০৮ — সর্বশেষ অবস্থা
Build54/run42386616140 actual18/18 PASS এবং signed APK প্রকাশিত। কিন্তু নিজের দেখা AK ADS PNG-তে HUD missing; এই red visual finding-কে green test দিয়ে ঢাকিনি। Native55-এ review-only ungraded UI camera ও actual magenta pixel assertion চলছে। নতুন AUD/UI/SYS-11 source—real mixer/Vorbis/PCM/meter/menu/condensed font/ceiling-impact repair—এর পরের fresh native gate21টি case; এখনো accepted বলা হয়নি।
