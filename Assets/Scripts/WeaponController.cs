using System.Collections;
using UnityEngine;
using Unity.Profiling;


namespace Fpp
{
    [DefaultExecutionOrder(100)]
    public sealed class WeaponController : MonoBehaviour
    {
        public Transform view;
        public GameObject defaultViewmodel;
        public Transform weaponMount, muzzle, defaultSight;
        public Renderer[] pistolRenderers;
        public Material flashMaterial,smokeMaterial,tracerMaterial;
        public Camera weaponCamera;
        public static bool MenuOpen => MobileHud.Instance != null && MobileHud.Instance.Modal;
        public string DisplayName { get; private set; } = "Mark 23";
        public string NearbyName => nearest != null ? nearest.Asset.displayName : "";
        public int Ammo => rounds;
        public int Capacity => magazine;
        public int Reserve => reserve;
        public WeaponAsset ActiveWeapon {get;private set;}
        public WeaponState State {get;private set;}=WeaponState.Drawing;
        public float AdsT=>model.AdsT;
        public float SpreadDegrees {get;private set;}
        public float ReloadProgress=>Reloading?Mathf.Clamp01(1-(reloadEnd-Time.time)/reloadDuration):0;
        public readonly GunplayModel model=new GunplayModel();
        private TestPlayer motor;
        private int reserve=120,sidearmRounds=12,sidearmReserve=120;
        private bool previousADS, pendingSemi, emptyReload;
        private int reloadStage, burstRemaining;
        private float reloadFinished=-100,drawReady;
        private AnimationState drawState,reloadState,shootState;
        private Vector2 sway,swayVelocity;
        private Vector2 flinch,flinchVelocity;
        private bool wasDefaultLocked;
        private Transform defaultAction;private Vector3 defaultActionHome;
        public Vector3 CameraAngles=>new Vector3(-model.AimClimb.x-model.ViewKick.x,model.AimClimb.y+model.ViewKick.y,0);
        public int Hits => hits;
        public bool Reloading => reloadEnd > 0;
        private WeaponPickup[] pickups;
        private WeaponPickup nearest, equipped, primaryWorld;
        private GameObject heldVisual;
        private ShotEffects effects;
        private WeaponRigParts heldRig;
        private Animation rigAnimation;
        private Camera worldCamera;
        private AudioSource sound;
        private AudioClip shotSound;
        private int rounds=12, magazine=12, hits;
        private float reloadEnd, recoil, equipBlend;
        private bool equipping;
        private Vector3 basePosition;
        private Quaternion baseRotation;
        private Transform leftUpper, leftForearm, leftHand, gunBone, sourceMagazine, rightForearm, rightHand, leftMiddle, leftIndex, leftPinky;
        private Vector3 rightGripInGun;private Quaternion rightGripRotation;
        private Vector3 sourceMagRest, defaultMuzzleRest, defaultSightRest;
        private Quaternion sourceMagRotation;
        private float secondaryAimSide;
        private Quaternion upperRest, foreRest, handRest;
        private bool supportPoseCached, poseInitialized;
        private float reloadDuration=1.6f, secondaryAimLift=.08f;

        private void OnEnable(){GunplayEvents.Raised+=Feedback;}
        private void OnDisable(){GunplayEvents.Raised-=Feedback;}
        private void Feedback(GunplayEvent e){if(e.Kind==GunplayEventKind.Damage)flinch+=new Vector2(1,Mathf.Sin(e.Value*Mathf.Deg2Rad))*GunplayConfig.Current.flinchDegrees;}
        private void Start()
        {
            InitializePose();
            motor=GetComponent<TestPlayer>();ActiveWeapon=Resources.Load<WeaponAsset>("Gunplay/mark23");
            reserve=ActiveWeapon.reserveMax;rounds=magazine=ActiveWeapon.magSize;
            HoldDrawPose();
            primaryWorld=new GameObject("Mark23 world pickup (gun only)").AddComponent<RuntimePickupMesh>().Build(this,ActiveWeapon);
            var expanded=new WeaponPickup[pickups.Length+1];System.Array.Copy(pickups,expanded,pickups.Length);expanded[pickups.Length]=primaryWorld;pickups=expanded;
            drawReady=Time.time+ActiveWeapon.drawSec;
            PlayClip("draw",ActiveWeapon.drawSec);
            EnsureEffects();
            sound=gameObject.AddComponent<AudioSource>();sound.spatialBlend=0;sound.volume=.2f;shotSound=MakeShotSound();
        }
        private void EnsureEffects()
        {if(effects==null){effects=gameObject.AddComponent<ShotEffects>();if(!Application.isPlaying)effects.hideFlags=HideFlags.DontSave;effects.Initialize(muzzle,weaponCamera,worldCamera,flashMaterial,smokeMaterial,tracerMaterial);}}
        private void InitializePose()
        {
            if(poseInitialized)return;poseInitialized=true;
            pickups=FindObjectsOfType<WeaponPickup>();
            if(ActiveWeapon==null)ActiveWeapon=Resources.Load<WeaponAsset>("Gunplay/mark23");
            rigAnimation=defaultViewmodel.GetComponentInChildren<Animation>();
            foreach(AnimationState clip in rigAnimation){var n=clip.name.ToLowerInvariant();if(n.Contains("draw"))drawState=clip;if(n.Contains("reload"))reloadState=clip;if(n.Contains("shoot"))shootState=clip;}
            foreach(var bone in defaultViewmodel.GetComponentsInChildren<Transform>())
            {
                if(bone.name=="LeftArm1_04")leftUpper=bone;
                if(bone.name=="LeftForeArm1_05")leftForearm=bone;
                if(bone.name=="LeftHand1_00")leftHand=bone;
                        if(bone.name=="LeftHandMiddle1_1_014")leftMiddle=bone;
                        if(bone.name=="LeftHandIndex1_1_010")leftIndex=bone;
                        if(bone.name=="LeftHandPinky1_1_022")leftPinky=bone;
                if(bone.name=="main_j_050")gunBone=bone;
                if(bone.name=="side_j_051"){defaultAction=bone;defaultActionHome=bone.localPosition;}
                if(bone.name=="Mag_j_052")sourceMagazine=bone;
                if(bone.name=="RightForeArm1_028")rightForearm=bone;
                if(bone.name=="RightHand1_029")rightHand=bone;
            }
            // FBX imports may contain separate skeleton copies for the gun and arms.
            // Resolve weighted joints from each renderer's actual bone palette, not the
            // last same-named transform in the full model hierarchy.
            foreach(var skin in defaultViewmodel.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                bool arms=false,pistol=false;
                foreach(var material in skin.sharedMaterials)if(material!=null)
                {arms|=material.name.Contains("Hand") || material.name.Contains("Glove");pistol|=material.name.Contains("Mark23");}
                foreach(var bone in skin.bones)
                {
                    if(bone==null)continue;
                    if(arms)
                    {
                        if(bone.name=="LeftArm1_04")leftUpper=bone;
                        if(bone.name=="LeftForeArm1_05")leftForearm=bone;
                        if(bone.name=="LeftHand1_00")leftHand=bone;
                        if(bone.name=="LeftHandMiddle1_1_014")leftMiddle=bone;
                        if(bone.name=="LeftHandIndex1_1_010")leftIndex=bone;
                        if(bone.name=="LeftHandPinky1_1_022")leftPinky=bone;
                        if(bone.name=="RightForeArm1_028")rightForearm=bone;
                        if(bone.name=="RightHand1_029")rightHand=bone;
                    }
                    if(pistol){if(bone.name=="main_j_050")gunBone=bone;if(bone.name=="Mag_j_052")sourceMagazine=bone;}
                }
            }
            if(gunBone && sourceMagazine){sourceMagRest=gunBone.InverseTransformPoint(sourceMagazine.position);sourceMagRotation=Quaternion.Inverse(gunBone.rotation)*sourceMagazine.rotation;}
            if(gunBone && rightHand){rightGripInGun=gunBone.InverseTransformPoint(rightHand.position);rightGripRotation=Quaternion.Inverse(gunBone.rotation)*rightHand.rotation;}
            if(muzzle)defaultMuzzleRest=weaponMount.InverseTransformPoint(muzzle.position);
            if(defaultSight)defaultSightRest=weaponMount.InverseTransformPoint(defaultSight.position);
            worldCamera=view.GetComponent<Camera>();
            basePosition=defaultViewmodel.transform.localPosition;baseRotation=defaultViewmodel.transform.localRotation;
        }
        private static readonly ProfilerMarker UpdateMarker=new ProfilerMarker("FPP.Weapons");
        private void Update(){using(UpdateMarker.Auto())TickGameplay();}
        private void TickGameplay()
        {
            if(MenuOpen || ActiveWeapon==null){pendingSemi=false;burstRemaining=0;return;}
            var c=GunplayConfig.Current;var hub=GameplayInput.Instance;
            var input=hub!=null?hub.Frame:default(InputFrame);float now=Time.time;
            nearest=null;float distance=c.pickupRadius;
            foreach(var pickup in pickups)
            {
                if(pickup==null || pickup==equipped || !pickup.gameObject.activeSelf)continue;
                float d=Vector3.Distance(transform.position,pickup.transform.position);
                if(d<=distance){nearest=pickup;distance=d;}
            }
            if(input.Pickup)EquipNearest();if(input.Swap)UseSidearm();if(input.Reload)Reload();
            if(Reloading && motor!=null && (motor.IsSprinting || motor.State==MovementState.Dive))CancelReload();
            if(rigAnimation==null)TickReloadMarkers();
            else if(Reloading)GunplayEvents.Send(GunplayEventKind.ReloadProgress,view.position,ActiveWeapon,null,ReloadProgress);
            CompleteReload(now);
            flinch=GunplayModel.Spring(flinch,ref flinchVelocity,c.viewOmega,c.viewDamping,Time.deltaTime);
            bool aiming=input.ADS && !Reloading && !equipping && (motor==null || !motor.BlocksFire);
            if(aiming!=previousADS){GunplayEvents.Send(aiming?GunplayEventKind.ADSIn:GunplayEventKind.ADSOut,view.position,ActiveWeapon);previousADS=aiming;}
            model.Tick(Time.deltaTime,now,aiming,input.Move.sqrMagnitude>0,ActiveWeapon,c);
            var state=motor!=null?motor.State:MovementState.Idle;
            SpreadDegrees=model.Spread(now,ActiveWeapon,c,state,motor!=null?motor.Speed:0,motor!=null?motor.LandingPenalty:0);
            if(motor!=null)view.localRotation=Quaternion.Euler(motor.LookEuler+CameraAngles);
            if(State==WeaponState.Drawing && now>=drawReady)State=WeaponState.Idle;
            if(!Reloading&&!equipping&&State!=WeaponState.Drawing)State=aiming?WeaponState.Aiming:WeaponState.Idle;
            if(input.FirePressed && !Reloading && !equipping && State!=WeaponState.Drawing){pendingSemi=true;if(ActiveWeapon.fireMode==FireMode.Burst && burstRemaining==0)burstRemaining=ActiveWeapon.burstCount;}
            bool wantsFire=ActiveWeapon.fireMode==FireMode.Auto?input.Fire:ActiveWeapon.fireMode==FireMode.Burst?burstRemaining>0:pendingSemi;
            if(wantsFire && motor!=null && motor.IsSprinting)motor.CancelSprint();
            if(wantsFire && (motor==null || !motor.BlocksFire && now-motor.SprintExitedAt>=ActiveWeapon.sprintOutFireDelaySec))
            {
                int before=rounds;Fire();if(rounds<before){pendingSemi=false;if(burstRemaining>0)burstRemaining--;}
                if(rounds==0 && input.FirePressed){GunplayEvents.Send(GunplayEventKind.DryFire,muzzle.position,ActiveWeapon);pendingSemi=false;burstRemaining=0;}
            }
        }
        private static readonly ProfilerMarker LateMarker=new ProfilerMarker("FPP.Viewmodel");
        private void LateUpdate(){using(LateMarker.Auto())TickViewmodel();}
        private void TickViewmodel()
        {
            if(defaultViewmodel==null)return;
            recoil=Mathf.MoveTowards(recoil,0,Time.deltaTime*GunplayConfig.Current.recoilReturn);
            bool aiming=!MenuOpen && model.AdsT>0;
            float progress=Reloading?Mathf.Clamp01(1-(reloadEnd-Time.time)/reloadDuration):0;
            if(equipped!=null && !Reloading)HoldDrawPose();
            var pose=Presentation(aiming && !Reloading,progress);
            pose.offset*=Mathf.SmoothStep(0,1,model.AdsT);pose.euler*=Mathf.SmoothStep(0,1,model.AdsT);
            var config=GunplayConfig.Current;
            if(motor!=null && motor.IsSprinting){pose.euler.z=config.sprintCant;pose.offset.y+=config.sprintRaise;}
            if(motor!=null && (motor.State==MovementState.Dive||motor.State==MovementState.Mantle)){pose.offset.y-=config.sprintRaise;pose.euler.x+=config.collisionTilt;}
            if(Physics.CapsuleCast(view.position+view.up*config.gunProbeHalf,view.position-view.up*config.gunProbeHalf,config.nearClip,view.forward,out _,config.collisionDistance,~(1<<8),QueryTriggerInteraction.Ignore))pose.euler+=new Vector3(config.collisionTilt,0,-config.collisionTilt);
            var look=GameplayInput.Instance!=null?GameplayInput.Instance.Frame.Look:Vector2.zero;
            sway+=look*Time.deltaTime;sway=GunplayModel.Spring(sway,ref swayVelocity,config.swayOmega,config.viewDamping,Time.deltaTime);
            pose.euler+=new Vector3(sway.y,-sway.x,0);
            pose.offset.x-=Mathf.Clamp(look.x,-1,1)*config.movementLag;
            pose.euler.x-=model.GunKick.x;pose.euler.y+=model.GunKick.y;
            Vector3 position=basePosition+pose.offset;
            position+=new Vector3(0,-equipBlend*config.holsterLower,-recoil*config.recoilPosition);
            float walk=GameplayInput.Instance!=null?GameplayInput.Instance.Frame.Move.magnitude:0;
            position+=new Vector3(Mathf.Sin(Time.time*config.viewmodelBobFrequency)*config.viewmodelBob,Mathf.Cos(Time.time*config.viewmodelBobFrequency*2)*config.viewmodelBob,0)*walk;
            defaultViewmodel.transform.localPosition=position;
            defaultViewmodel.transform.localRotation=baseRotation*Quaternion.Euler(pose.euler+new Vector3(-recoil*config.recoilGunDegrees+equipBlend*config.holsterPitch,equipBlend*config.holsterYaw,0));
            if(worldCamera!=null){worldCamera.fieldOfView=Mathf.Lerp(Mathf.Clamp(GameplayInput.Instance!=null?GameplayInput.Instance.Settings.worldFOV:config.worldFOV,config.minFOV,config.maxFOV),ActiveWeapon.sightFOV,Mathf.SmoothStep(0,1,model.AdsT));worldCamera.nearClipPlane=config.nearClip;}
            if(motor!=null){float breath=Mathf.PerlinNoise(Time.time*config.breathHz,0)*2-1;view.localRotation=Quaternion.Euler(motor.LookEuler+CameraAngles+new Vector3(breath*config.breathDegrees+(motor!=null?motor.LandingPenalty*config.landingPitch:0)-flinch.x,flinch.y,0));}
            if(weaponCamera!=null)weaponCamera.fieldOfView=config.viewmodelFOV;
            PoseSecondary(progress);
            bool locked=rounds==0&&(!Reloading||progress<config.boltMarker);
            if(heldRig!=null)heldRig.LockEmpty(locked,muzzle,config,Reloading&&!emptyReload);
            else if(defaultAction!=null && !Reloading){if(locked||wasDefaultLocked)defaultAction.localPosition=defaultActionHome;if(locked)defaultAction.position-=muzzle.forward*config.emptyPistolActionTravel;wasDefaultLocked=locked;}
        }
        private WeaponPresentation.Pose Presentation(bool aim,float progress)
        {
            var pose=WeaponPresentation.Sample(aim,equipped!=null,progress,secondaryAimLift);
            if(aim){pose.offset.x=-basePosition.x-(equipped!=null?secondaryAimSide:defaultSightRest.x);pose.offset.y=equipped!=null?secondaryAimLift:-basePosition.y-defaultSightRest.y;}
            if(aim && ActiveWeapon!=null){pose.offset+=ActiveWeapon.aimPoseOffset;pose.euler+=ActiveWeapon.aimPoseEuler;}
            return pose;
        }
        private void PoseSecondary(float progress)
        {
            if(heldRig==null)return;
            // Snapshot the source motion before IK: gun/mag joints may descend from a hand.
            Vector3 delta=Vector3.zero;Quaternion spin=Quaternion.identity;
            if(gunBone && sourceMagazine)
            {
                delta=gunBone.TransformVector(gunBone.InverseTransformPoint(sourceMagazine.position)-sourceMagRest);
                spin=sourceMagazine.rotation*Quaternion.Inverse(gunBone.rotation*sourceMagRotation);
            }
            if(progress>0 && gunBone && rightHand && rightForearm)
            {
                Vector3 grip=gunBone.TransformPoint(rightGripInGun);Quaternion rotation=gunBone.rotation*rightGripRotation;
                RetargetForearm(rightForearm,rightHand,grip,rotation);
            }
            heldRig.Pose(progress,muzzle);heldRig.RetargetMagazine(delta,spin);
            if(supportPoseCached)
            {
                if(progress>0)SolveSupportHand(heldRig.magazine.position+muzzle.rotation*new Vector3(-.025f,.015f,0));
                else if(DisplayName.Contains("AK"))SolveRifleSupport();
            }
        }

        public void EquipNearest()
        {
            if(nearest!=null && !equipping){CancelReload();StartCoroutine(Equip(nearest));}
        }
        public void UseSidearm()
        {
            if(equipping)return;
            CancelReload();
            if(primaryWorld!=null && primaryWorld.gameObject.activeSelf && Vector3.Distance(transform.position,primaryWorld.transform.position)<=GunplayConfig.Current.pickupRadius)StartCoroutine(Equip(primaryWorld));
            else if(nearest!=null)StartCoroutine(Equip(nearest));
            else if(primaryWorld==null && equipped!=null)StartCoroutine(Equip(null));
        }
        private IEnumerator Equip(WeaponPickup next)
        {
            equipping=true;State=WeaponState.SwitchingOut;GunplayEvents.Send(GunplayEventKind.WeaponSwap,view.position,ActiveWeapon,"holster");
            // Original procedural lower/raise motion, not copied MWII animation data.
            for(float t=0;t<ActiveWeapon.switchOutSec;t+=Time.deltaTime){equipBlend=Mathf.SmoothStep(0,1,t/ActiveWeapon.switchOutSec);yield return null;}
            ApplyWeapon(next);State=WeaponState.Drawing;drawReady=Time.time+ActiveWeapon.switchInSec;
            for(float t=0;t<ActiveWeapon.switchInSec;t+=Time.deltaTime){equipBlend=1-Mathf.SmoothStep(0,1,t/ActiveWeapon.switchInSec);yield return null;}
            equipBlend=0;equipping=false;State=WeaponState.Idle;GunplayEvents.Send(GunplayEventKind.WeaponSwap,view.position,ActiveWeapon,"draw");
        }
        private void ApplyWeapon(WeaponPickup next)
        {
            RestoreSupportPose();HoldDrawPose();
            bool sidearm=next==null || next.Asset.id=="mark23";
            if(equipped!=null)
            {
                equipped.Store(rounds,reserve);
                if(Application.isPlaying)equipped.Drop(transform.position+transform.forward*GunplayConfig.Current.pickupRadius,transform.forward);
                else equipped.gameObject.SetActive(true);
            }
            else
            {
                sidearmRounds=rounds;sidearmReserve=reserve;
                if(Application.isPlaying && primaryWorld!=null && !sidearm)
                {primaryWorld.Store(rounds,reserve);primaryWorld.transform.rotation=Quaternion.Euler(0,transform.eulerAngles.y,90);primaryWorld.Drop(transform.position+transform.forward*GunplayConfig.Current.pickupRadius,transform.forward);}
            }
            if(sidearm && primaryWorld!=null)
            {
                if(next!=null){sidearmRounds=next.Rounds;sidearmReserve=next.Reserve;}
                primaryWorld.gameObject.SetActive(false);
            }
            if(heldVisual!=null)Remove(heldVisual);
            equipped=sidearm?null:next;heldRig=null;
            foreach(var renderer in pistolRenderers)renderer.enabled=sidearm;
            if(sidearm)
            {
                DisplayName="Mark 23";ActiveWeapon=Resources.Load<WeaponAsset>("Gunplay/mark23");magazine=ActiveWeapon.magSize;PlayClip("draw",ActiveWeapon.drawSec);
            }
            else
            {
                ActiveWeapon=next.Asset;DisplayName=ActiveWeapon.displayName;magazine=ActiveWeapon.magSize;
                heldVisual=Instantiate(next.gameObject,weaponMount,false);Remove(heldVisual.GetComponent<WeaponPickup>());
                foreach(var collider in heldVisual.GetComponentsInChildren<Collider>())Remove(collider);
                heldVisual.transform.localPosition=Vector3.zero;heldVisual.transform.localRotation=Quaternion.identity;heldVisual.transform.localScale=Vector3.one;
                SetLayer(heldVisual,8);
                Bounds bound=LocalBounds(heldVisual.transform);
                Vector3 axis=bound.size.x>bound.size.y && bound.size.x>bound.size.z?Vector3.right:(bound.size.y>bound.size.z?Vector3.up:Vector3.forward);
                float scale=(DisplayName.Contains("AK")?.65f:.27f)/Mathf.Max(bound.size.x,bound.size.y,bound.size.z);
                heldVisual.transform.localRotation=Quaternion.FromToRotation(axis,Vector3.forward);
                heldVisual.transform.localScale=Vector3.one*scale;
                heldVisual.transform.localPosition=-(heldVisual.transform.localRotation*(bound.center*scale));
                var posedBounds=BoundsInSpace(heldVisual.transform,weaponMount);
                PositionMuzzle(new Vector3(posedBounds.center.x,posedBounds.max.y-posedBounds.size.y*(DisplayName.Contains("AK")?.22f:.12f),posedBounds.max.z+.012f));
                heldRig=heldVisual.GetComponent<WeaponRigParts>();
                if(heldRig==null || heldRig.sight==null)throw new System.Exception("Missing precomputed gun sight");
                Vector3 sight=weaponMount.InverseTransformPoint(heldRig.sight.position);
                secondaryAimLift=-basePosition.y-sight.y;secondaryAimSide=sight.x;
                heldRig.Bind();heldVisual.AddComponent<WeaponSockets>().Build(muzzle,heldRig.sight,ActiveWeapon);
                next.gameObject.SetActive(false);
                HoldDrawPose();
                if(leftUpper!=null && leftForearm!=null && leftHand!=null)
                {
                    upperRest=leftUpper.localRotation;foreRest=leftForearm.localRotation;handRest=leftHand.localRotation;
                    supportPoseCached=true;
                    rigAnimation.enabled=true;
                }
            }
            if(!sidearm && rightHand)heldVisual.transform.SetParent(rightHand,true);
            if(sidearm)PositionMuzzle(defaultMuzzleRest);
            rounds=sidearm?sidearmRounds:next.Rounds;reserve=sidearm?sidearmReserve:next.Reserve;reloadEnd=0;
            model.Reset();pendingSemi=false;burstRemaining=0;
            if(Application.isPlaying)GunplayEvents.Send(GunplayEventKind.Pickup,view.position,ActiveWeapon);
        }
        private void RestoreSupportPose()
        {
            if(!supportPoseCached)return;
            leftUpper.localRotation=upperRest;leftForearm.localRotation=foreRest;leftHand.localRotation=handRest;
            supportPoseCached=false;
        }
        private void SolveRifleSupport()
        {
            Vector3 target=weaponMount.TransformPoint(ActiveWeapon.supportHand);
            Vector3 root=leftUpper.position;float a=Vector3.Distance(root,leftForearm.position),b=Vector3.Distance(leftForearm.position,leftHand.position);
            Vector3 delta=target-root;float distance=Mathf.Clamp(delta.magnitude,Mathf.Abs(a-b)+.001f,(a+b)*.985f);Vector3 direction=delta.normalized;
            // Use the authored elbow plane, never a guessed camera-space pole that swings
            // the cut shoulder toward the near plane. Upper-arm correction is bounded.
            Vector3 bend=Vector3.ProjectOnPlane(leftForearm.position-root,direction).normalized;
            if(bend.sqrMagnitude<.1f)bend=Vector3.ProjectOnPlane(-weaponMount.right-weaponMount.up,direction).normalized;
            float along=(a*a-b*b+distance*distance)/(2*distance);
            Vector3 elbow=root+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            Quaternion upper=Quaternion.FromToRotation(leftForearm.position-root,elbow-root);float angle=Quaternion.Angle(Quaternion.identity,upper);
            Quaternion palm=leftHand.rotation;
            if(leftMiddle && leftIndex && leftPinky)
            {
                Vector3 alongHand=(leftMiddle.position-leftHand.position).normalized;
                Vector3 across=leftIndex.position-leftPinky.position;
                Vector3 palmNormal=Vector3.Cross(across,alongHand).normalized;
                if(palmNormal.sqrMagnitude>.5f)
                    palm=Quaternion.LookRotation((weaponMount.right+weaponMount.forward*.25f).normalized,weaponMount.up)*Quaternion.Inverse(Quaternion.LookRotation(alongHand,palmNormal))*leftHand.rotation;
            }
            leftUpper.rotation=Quaternion.Slerp(Quaternion.identity,upper,angle>10f?10f/angle:1f)*leftUpper.rotation;
            leftForearm.rotation=Quaternion.FromToRotation(leftHand.position-leftForearm.position,root+direction*distance-leftForearm.position)*leftForearm.rotation;
            leftHand.rotation=palm;
        }
        private void SolveSupportHand(Vector3 target)
        {
            // Keep the authored shoulder/elbow pose. Upper-arm IK exposed the cut-off
            // shoulder mesh in front of the camera; a limited forearm correction cannot.
            RetargetForearm(leftForearm,leftHand,target,leftHand.rotation);
        }
        private static void RetargetForearm(Transform forearm,Transform hand,Vector3 target,Quaternion palm)
        {
            Quaternion correction=Quaternion.FromToRotation(hand.position-forearm.position,target-forearm.position);
            float angle=Quaternion.Angle(Quaternion.identity,correction);
            forearm.rotation=Quaternion.Slerp(Quaternion.identity,correction,angle>35f?35f/angle:1f)*forearm.rotation;
            hand.rotation=palm;
        }
        private void PositionMuzzle(Vector3 point)
        {
            muzzle.SetParent(weaponMount,false);muzzle.localPosition=point;muzzle.localRotation=Quaternion.identity;muzzle.localScale=Vector3.one;
            if(equipped!=null && rightHand)muzzle.SetParent(rightHand,true);else if(gunBone)muzzle.SetParent(gunBone,true);
        }
        private void SampleReload(float progress)
        {
            if(rigAnimation==null)return;
            foreach(AnimationState state in rigAnimation)if(state.name.ToLowerInvariant().Contains("reload")){rigAnimation.enabled=true;rigAnimation.Play(state.name);state.speed=0;state.time=state.length*progress;rigAnimation.Sample();return;}
        }
        private void CompleteReload(float now)
        {
            if(reloadEnd<=0 || now<reloadEnd)return;
            int transfer=Mathf.Min(magazine-rounds,reserve);rounds+=transfer;reserve-=transfer;
            reloadEnd=0;reloadFinished=now;State=WeaponState.Idle;
            GunplayEvents.Send(GunplayEventKind.ReloadProgress,view.position,ActiveWeapon,"complete",1);
        }
        public void CancelReload()
        {
            if(!Reloading)return;reloadEnd=0;State=WeaponState.Idle;HoldDrawPose();
            GunplayEvents.Send(GunplayEventKind.ReloadProgress,view.position,ActiveWeapon,"cancel",0);
        }
        public void Reload()
        {
            if(ActiveWeapon==null || rounds>=magazine || reserve<=0 || Reloading || equipping || MenuOpen || Time.time-reloadFinished<GunplayConfig.Current.reloadDebounce)return;
            emptyReload=rounds==0;reloadStage=0;pendingSemi=false;burstRemaining=0;
            reloadDuration=emptyReload?ActiveWeapon.reloadEmptySec:ActiveWeapon.reloadTacticalSec;
            State=emptyReload?WeaponState.ReloadEmpty:WeaponState.ReloadTactical;
            reloadEnd=Time.time+reloadDuration;PlayClip("reload",reloadDuration);
        }
        public void ReloadFoleyMarker(string stage)
        {if(!Reloading || stage=="boltRelease"&&!emptyReload)return;GunplayEvents.Send(GunplayEventKind.ReloadFoley,muzzle.position,ActiveWeapon,stage);}
        private void TickReloadMarkers()
        {
            if(!Reloading)return;var c=GunplayConfig.Current;float p=ReloadProgress;
            if(reloadStage==0&&p>=c.magOutMarker){reloadStage=1;ReloadFoleyMarker("magOut");}
            if(reloadStage==1&&p>=c.magInMarker){reloadStage=2;ReloadFoleyMarker("magIn");}
            if(reloadStage==2&&p>=c.boltMarker){reloadStage=3;if(emptyReload)ReloadFoleyMarker("boltRelease");}
            GunplayEvents.Send(GunplayEventKind.ReloadProgress,view.position,ActiveWeapon,null,p);
        }
        public int GrantAmmo(int amount){int added=Mathf.Min(Mathf.Max(0,amount),ActiveWeapon.reserveMax-reserve);reserve+=added;return added;}
        private void Fire()
        {
            if(rounds<=0 || Reloading || equipping || State==WeaponState.Drawing && Time.time<drawReady || !model.CanShoot(Time.time))return;
            model.Schedule(Time.time,ActiveWeapon);rounds--;recoil=1;State=WeaponState.Firing;
            if(equipped==null)PlayClip("shoot",60/ActiveWeapon.rpm);
            Vector3 direction=model.ShotDirection(view,SpreadDegrees);
            Vector3 end=Ballistics.Fire(view.position,direction,ActiveWeapon,ref hits);
            Vector2 impulse=model.Shot(Time.time,ActiveWeapon,GunplayConfig.Current,model.AdsT>=1,motor!=null&&motor.Mounted);
            EnsureEffects();effects.Play(end,model.ShotIndex%ActiveWeapon.tracerEveryN==0);
            if(sound!=null)sound.PlayOneShot(shotSound);
            GunplayEvents.Emit(new GunplayEvent{Kind=GunplayEventKind.ShotFired,Address=ActiveWeapon.shotEvent,Weapon=ActiveWeapon,Position=muzzle.position,ADS=model.AdsT>=1,Context=Physics.Raycast(view.position,Vector3.up,GunplayConfig.Current.roomProbe,~(1<<8))?"indoor":"outdoor",Index=model.ShotIndex-1,Pattern=impulse,Spread=SpreadDegrees});
            GunplayEvents.Send(GunplayEventKind.ShotTailRequest,muzzle.position,ActiveWeapon,Physics.Raycast(view.position,Vector3.up,GunplayConfig.Current.roomProbe,~(1<<8))?"indoor":"outdoor");
            GunplayEvents.Send(GunplayEventKind.MechanicalClick,muzzle.position,ActiveWeapon,"cycle");
            GunplayEvents.Emit(new GunplayEvent{Kind=GunplayEventKind.ShellEject,Address="ShellEject",Position=(heldVisual!=null?heldVisual.GetComponent<WeaponSockets>():defaultViewmodel.GetComponent<WeaponSockets>()).ejectionPort.position,Normal=muzzle.right,Weapon=ActiveWeapon});
            if(rounds==0)GunplayEvents.Send(GunplayEventKind.MechanicalClick,muzzle.position,ActiveWeapon,"lockEmpty");
        }
        private void HoldDrawPose()
        {
            if(rigAnimation==null||drawState==null)return;rigAnimation.enabled=true;
            drawState.wrapMode=WrapMode.ClampForever;rigAnimation.Play(drawState.name);drawState.time=Mathf.Max(0,drawState.length-.001f);drawState.speed=0;rigAnimation.Sample();
        }
        private void PlayClip(string word,float duration)
        {
            if(rigAnimation==null)return;rigAnimation.enabled=true;
            var state=word=="draw"?drawState:word=="reload"?reloadState:shootState;if(state==null)return;
            state.wrapMode=WrapMode.ClampForever;state.speed=state.length/Mathf.Max(.1f,duration);state.time=0;rigAnimation.Play(state.name);
        }
        public static void SetLayer(GameObject root,int layer)
        { foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=layer; }
        public static Bounds LocalBounds(Transform root)
        {
            bool first=true;Bounds bound=new Bounds();
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                if(filter.sharedMesh==null)continue;var b=filter.sharedMesh.bounds;
                for(int i=0;i<8;i++)
                {
                    var p=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                    p=root.InverseTransformPoint(filter.transform.TransformPoint(p));if(first){bound=new Bounds(p,Vector3.zero);first=false;}else bound.Encapsulate(p);
                }
            }
            return bound;
        }
        // Called by the editor only: runtime uses the serialized marker, not unreadable mesh data.
        public static Vector3 CentralFrontSight(System.Collections.Generic.List<Vector3> points)
        {
            float front=float.MinValue;foreach(var point in points)front=Mathf.Max(front,point.z);
            float minX=float.MaxValue,maxX=float.MinValue;
            foreach(var point in points)if(point.z>front*.45f){minX=Mathf.Min(minX,point.x);maxX=Mathf.Max(maxX,point.x);}
            float centre=(minX+maxX)*.5f,top=float.MinValue;
            foreach(var point in points)if(point.z>front*.45f && Mathf.Abs(point.x-centre)<.006f)top=Mathf.Max(top,point.y);
            var sight=Vector3.zero;int count=0;
            foreach(var point in points)if(point.z>front*.45f && Mathf.Abs(point.x-centre)<.006f && point.y>top-.001f){sight+=point;count++;}
            if(count==0)throw new System.Exception("Missing central front sight vertices");
            return sight/count;
        }
        private static void Remove(Object value)
        {
            if(value==null)return;
            if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);
        }
        public static Bounds BoundsInSpace(Transform root,Transform space)
        {
            bool first=true;Bounds result=new Bounds();
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var b=filter.sharedMesh.bounds;
                for(int i=0;i<8;i++)
                {
                    Vector3 p=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                    p=space.InverseTransformPoint(filter.transform.TransformPoint(p));
                    if(first){result=new Bounds(p,Vector3.zero);first=false;}else result.Encapsulate(p);
                }
            }
            return result;
        }
#if UNITY_EDITOR
        public void QAEquip(WeaponPickup pickup){InitializePose();CancelReload();ApplyWeapon(pickup);drawReady=0;State=WeaponState.Idle;reloadFinished=-100;}
        public void QAAmmo(int mag,int spare){rounds=Mathf.Clamp(mag,0,magazine);reserve=Mathf.Clamp(spare,0,ActiveWeapon.reserveMax);reloadFinished=-100;}
        public void QARequestEquip(WeaponPickup pickup){if(!equipping){CancelReload();StartCoroutine(Equip(pickup));}}
        public bool QASwitching=>equipping;
        public void ReviewRetargetAudit(WeaponPickup pickup)
        {
            ReviewPose(pickup,false,0);
            if(gunBone==null || sourceMagazine==null || heldRig==null)throw new System.Exception("Missing authored retarget bones");
            Vector3 home=heldVisual.transform.InverseTransformPoint(heldRig.magazine.position);float travel=0;
            foreach(float t in new[]{.2f,.4f,.6f,.8f})
            {
                ReviewPose(pickup,false,t);
                travel=Mathf.Max(travel,Vector3.Distance(home,heldVisual.transform.InverseTransformPoint(heldRig.magazine.position))*Mathf.Abs(heldVisual.transform.lossyScale.x));
            }
            if(travel<.03f)throw new System.Exception("Authored magazine retarget did not move: "+pickup.displayName+" travel="+travel);
            Debug.Log("FPP authored magazine retarget: "+pickup.displayName+" travel="+travel);
        }
        public void ReviewAmmoAudit(WeaponPickup pickup)
        {
            ReviewPose(pickup,false,0);rounds=1;reserve=ActiveWeapon.reserveMax;reloadFinished=-100;Reload();
            if(!Reloading || rounds!=1)throw new System.Exception("Reload must wait before refilling ammo");
            Fire();if(rounds!=1)throw new System.Exception("Firing was not blocked during reload");
            CompleteReload(Time.time+reloadDuration+.01f);
            if(Reloading || rounds!=magazine)throw new System.Exception("Reload did not refill the active weapon");
        }
        public void ReviewSmokeBurst(){InitializePose();EnsureEffects();effects.ReviewBurst(view.position+view.forward*35);}
        public void ReviewShotEffects()
        {InitializePose();EnsureEffects();effects.Review(view.position+view.forward*35);}
        // Drives the same loadout, pose and IK code for deterministic editor review images.
        public void ReviewPose(WeaponPickup pickup,bool aim,float reloadProgress)
        {
            InitializePose();ApplyWeapon(pickup);HoldDrawPose();rigAnimation.enabled=true;
            if(reloadProgress>0){reloadDuration=2;reloadEnd=Time.time+2*(1-reloadProgress);}
            if(reloadProgress>0)SampleReload(reloadProgress);
            var pose=Presentation(aim,reloadProgress);
            defaultViewmodel.transform.localPosition=basePosition+pose.offset;
            defaultViewmodel.transform.localRotation=baseRotation*Quaternion.Euler(pose.euler);
            PoseSecondary(reloadProgress);
            worldCamera.fieldOfView=aim?ActiveWeapon.sightFOV:GunplayConfig.Current.worldFOV;weaponCamera.fieldOfView=GunplayConfig.Current.viewmodelFOV;
        }
#endif
        private static AudioClip MakeShotSound()
        {
            const int samples=4410;var data=new float[samples];var random=new System.Random(51);
            for(int i=0;i<samples;i++){float t=(float)i/samples;data[i]=(float)(random.NextDouble()*2-1)*Mathf.Exp(-t*14)*.5f+Mathf.Sin(i*.05f)*Mathf.Exp(-t*20)*.25f;}
            var clip=AudioClip.Create("Original synthetic test shot",samples,1,44100,false);clip.SetData(data,0);return clip;
        }
    }
}
