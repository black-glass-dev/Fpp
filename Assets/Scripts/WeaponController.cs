using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Fpp
{
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
        public string NearbyName => nearest != null ? nearest.displayName : "";
        public int Ammo => rounds;
        public int Capacity => magazine;
        public int Hits => hits;
        public bool Reloading => reloadEnd > 0;
        private WeaponPickup[] pickups;
        private WeaponPickup nearest, equipped;
        private GameObject heldVisual;
        private ShotEffects effects;
        private WeaponRigParts heldRig;
        private Animation rigAnimation;
        private Camera worldCamera;
        private AudioSource sound;
        private AudioClip shotSound;
        private int rounds=12, magazine=12, hits;
        private float nextShot, reloadEnd, recoil, equipBlend;
        private bool equipping;
        private Vector3 basePosition;
        private Quaternion baseRotation;
        private Transform leftUpper, leftForearm, leftHand, gunBone, sourceMagazine;
        private Vector3 sourceMagRest, defaultMuzzleRest, defaultSightRest;
        private Quaternion sourceMagRotation;
        private float secondaryAimSide;
        private Quaternion upperRest, foreRest, handRest;
        private bool supportPoseCached, poseInitialized;
        private float reloadDuration=1.6f, secondaryAimLift=.08f;

        private void Start()
        {
            InitializePose();
            PlayClip("draw",.65f);
            EnsureEffects();
            sound=gameObject.AddComponent<AudioSource>();sound.spatialBlend=0;sound.volume=.2f;shotSound=MakeShotSound();
        }
        private void EnsureEffects()
        {if(effects==null){effects=gameObject.AddComponent<ShotEffects>();if(!Application.isPlaying)effects.hideFlags=HideFlags.DontSave;effects.Initialize(muzzle,weaponCamera,worldCamera,flashMaterial,smokeMaterial,tracerMaterial);}}
        private void InitializePose()
        {
            if(poseInitialized)return;poseInitialized=true;
            pickups=FindObjectsOfType<WeaponPickup>();
            rigAnimation=defaultViewmodel.GetComponentInChildren<Animation>();
            foreach(var bone in defaultViewmodel.GetComponentsInChildren<Transform>())
            {
                if(bone.name=="LeftArm1_04")leftUpper=bone;
                if(bone.name=="LeftForeArm1_05")leftForearm=bone;
                if(bone.name=="LeftHand1_00")leftHand=bone;
                if(bone.name=="main_j_050")gunBone=bone;
                if(bone.name=="Mag_j_052")sourceMagazine=bone;
            }
            if(gunBone && sourceMagazine){sourceMagRest=gunBone.InverseTransformPoint(sourceMagazine.position);sourceMagRotation=Quaternion.Inverse(gunBone.rotation)*sourceMagazine.rotation;}
            if(muzzle)defaultMuzzleRest=weaponMount.InverseTransformPoint(muzzle.position);
            if(defaultSight)defaultSightRest=weaponMount.InverseTransformPoint(defaultSight.position);
            worldCamera=view.GetComponent<Camera>();
            basePosition=defaultViewmodel.transform.localPosition;baseRotation=defaultViewmodel.transform.localRotation;
        }
        private void Update()
        {
            if(MenuOpen)return;
            nearest=null;float distance=2.6f;
            foreach(var pickup in pickups)
            {
                if(pickup==null || pickup==equipped || !pickup.gameObject.activeSelf)continue;
                float d=Vector3.Distance(transform.position,pickup.transform.position);
                if(d<distance){ nearest=pickup;distance=d; }
            }
            CompleteReload(Time.time);
            if(Input.GetKeyDown(KeyCode.E))EquipNearest();
            if(Input.GetKeyDown(KeyCode.R))Reload();
            if(Input.GetKeyDown(KeyCode.Alpha1))UseSidearm();
            bool mouse=Input.touchCount==0 && Input.GetMouseButton(0) && (EventSystem.current==null || !EventSystem.current.IsPointerOverGameObject());
            if(mouse || MobileHud.Instance!=null && MobileHud.Instance.FireHeld)Fire();
        }
        private void LateUpdate()
        {
            if(defaultViewmodel==null)return;
            recoil=Mathf.MoveTowards(recoil,0,Time.deltaTime*7);
            bool aiming=!MenuOpen && (MobileHud.Instance!=null && MobileHud.Instance.AimHeld || Input.touchCount==0 && Input.GetMouseButton(1));
            float progress=Reloading?Mathf.Clamp01(1-(reloadEnd-Time.time)/reloadDuration):0;
            if(equipped!=null && !Reloading)HoldDrawPose();
            var pose=Presentation(aiming && !Reloading,progress);
            Vector3 position=basePosition+pose.offset;
            position+=new Vector3(0,-equipBlend*.28f,-recoil*.028f);
            float walk=MobileHud.Instance!=null?MobileHud.Instance.Move.magnitude:Mathf.Clamp01(Mathf.Abs(Input.GetAxisRaw("Vertical"))+Mathf.Abs(Input.GetAxisRaw("Horizontal")));
            position+=new Vector3(Mathf.Sin(Time.time*7)*.003f,Mathf.Cos(Time.time*14)*.003f,0)*walk;
            defaultViewmodel.transform.localPosition=Vector3.Lerp(defaultViewmodel.transform.localPosition,position,Time.deltaTime*18);
            defaultViewmodel.transform.localRotation=baseRotation*Quaternion.Euler(pose.euler+new Vector3(-recoil*3+equipBlend*25,equipBlend*-12,0));
            if(worldCamera!=null)worldCamera.fieldOfView=Mathf.Lerp(worldCamera.fieldOfView,aiming?50:68,Time.deltaTime*12);
            if(weaponCamera!=null)weaponCamera.fieldOfView=Mathf.Lerp(weaponCamera.fieldOfView,aiming?45:60,Time.deltaTime*12);
            PoseSecondary(progress);
        }
        private WeaponPresentation.Pose Presentation(bool aim,float progress)
        {
            var pose=WeaponPresentation.Sample(aim,equipped!=null,progress,secondaryAimLift);
            if(aim){pose.offset.x=-basePosition.x-(equipped!=null?secondaryAimSide:defaultSightRest.x);pose.offset.y=equipped!=null?secondaryAimLift:-basePosition.y-defaultSightRest.y;}
            return pose;
        }
        private void PoseSecondary(float progress)
        {
            if(heldRig!=null)
            {
                heldRig.Pose(progress,muzzle);
                if(gunBone && sourceMagazine)heldRig.RetargetMagazine(gunBone,sourceMagazine,sourceMagRest,sourceMagRotation);
            }
            if(equipped!=null && supportPoseCached && DisplayName.Contains("AK"))
                SolveSupportHand(progress>0 && heldRig!=null?heldRig.magazine.position+muzzle.rotation*new Vector3(-.025f,.015f,0):weaponMount.TransformPoint(new Vector3(0,-.035f,.065f)));
        }

        public void EquipNearest()
        {
            if(nearest!=null && !equipping && !Reloading)StartCoroutine(Equip(nearest));
        }
        public void UseSidearm()
        {
            if(equipped!=null && !equipping && !Reloading)StartCoroutine(Equip(null));
        }
        private IEnumerator Equip(WeaponPickup next)
        {
            equipping=true;
            // Original procedural lower/raise motion, not copied MWII animation data.
            for(float t=0;t<.18f;t+=Time.deltaTime){equipBlend=Mathf.SmoothStep(0,1,t/.18f);yield return null;}
            ApplyWeapon(next);
            for(float t=0;t<.32f;t+=Time.deltaTime){equipBlend=1-Mathf.SmoothStep(0,1,t/.32f);yield return null;}
            equipBlend=0;equipping=false;nextShot=Time.time+.1f;
        }
        private void ApplyWeapon(WeaponPickup next)
        {
            RestoreSupportPose();HoldDrawPose();
            if(equipped!=null)equipped.gameObject.SetActive(true);
            if(heldVisual!=null)Remove(heldVisual);
            equipped=next;heldRig=null;
            bool sidearm=next==null;
            foreach(var renderer in pistolRenderers)renderer.enabled=sidearm;
            if(sidearm)
            {
                DisplayName="Mark 23";magazine=12;PlayClip("draw",.55f);
            }
            else
            {
                DisplayName=next.displayName;magazine=DisplayName.Contains("AK")?30:17;
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
                heldRig.Bind();
                next.gameObject.SetActive(false);
                HoldDrawPose();
                if(leftUpper!=null && leftForearm!=null && leftHand!=null)
                {
                    upperRest=leftUpper.localRotation;foreRest=leftForearm.localRotation;handRest=leftHand.localRotation;
                    supportPoseCached=true;
                    rigAnimation.enabled=true;
                }
            }
            if(!sidearm && gunBone)heldVisual.transform.SetParent(gunBone,true);
            if(sidearm)PositionMuzzle(defaultMuzzleRest);
            rounds=magazine;reloadEnd=0;
        }
        private void RestoreSupportPose()
        {
            if(!supportPoseCached)return;
            leftUpper.localRotation=upperRest;leftForearm.localRotation=foreRest;leftHand.localRotation=handRest;
            supportPoseCached=false;
        }
        private void SolveSupportHand(Vector3 target)
        {
            // Keep the authored shoulder/elbow pose. Upper-arm IK exposed the cut-off
            // shoulder mesh in front of the camera; a limited forearm correction cannot.
            Quaternion palm=leftHand.rotation;
            Quaternion correction=Quaternion.FromToRotation(leftHand.position-leftForearm.position,target-leftForearm.position);
            float angle=Quaternion.Angle(Quaternion.identity,correction);
            leftForearm.rotation=Quaternion.Slerp(Quaternion.identity,correction,angle>35f?35f/angle:1f)*leftForearm.rotation;
            leftHand.rotation=palm;
        }
        private void PositionMuzzle(Vector3 point)
        {
            muzzle.SetParent(weaponMount,false);muzzle.localPosition=point;muzzle.localRotation=Quaternion.identity;muzzle.localScale=Vector3.one;
            if(gunBone)muzzle.SetParent(gunBone,true);
        }
        private void SampleReload(float progress)
        {
            if(rigAnimation==null)return;
            foreach(AnimationState state in rigAnimation)if(state.name.ToLowerInvariant().Contains("reload")){rigAnimation.enabled=true;rigAnimation.Play(state.name);state.speed=0;state.time=state.length*progress;rigAnimation.Sample();return;}
        }
        private void CompleteReload(float now)
        { if(reloadEnd>0 && now>=reloadEnd){rounds=magazine;reloadEnd=0;} }
        public void Reload()
        {
            if(rounds>=magazine || Reloading || equipping || MenuOpen)return;
            reloadDuration=equipped==null?1.6f:DisplayName.Contains("AK")?2.2f:2f;
            reloadEnd=Time.time+reloadDuration;if(equipped==null)PlayClip("reload",1.6f);else PlayClip("reload",reloadDuration);
        }
        private void Fire()
        {
            if(rounds<=0 || Reloading || equipping || Time.time<nextShot)return;
            nextShot=Time.time+(equipped!=null && DisplayName.Contains("AK")?.12f:.22f);rounds--;recoil=1;
            if(equipped==null)PlayClip("shoot",.18f);
            Vector3 end=view.position+view.forward*100;
            if(Physics.Raycast(view.position,view.forward,out RaycastHit hit,100,~(1<<8),QueryTriggerInteraction.Ignore))
            {
                end=hit.point;var target=hit.collider.GetComponent<TestTarget>();if(target!=null){target.Hit();hits++;}
            }
            EnsureEffects();effects.Play(end);
            sound.PlayOneShot(shotSound);
        }
        private void HoldDrawPose()
        {
            if(rigAnimation==null)return;
            rigAnimation.enabled=true;
            foreach(AnimationState state in rigAnimation)
            {
                if(!state.name.ToLowerInvariant().Contains("draw"))continue;
                state.wrapMode=WrapMode.ClampForever;rigAnimation.Play(state.name);
                state.time=Mathf.Max(0,state.length-.001f);state.speed=0;rigAnimation.Sample();return;
            }
        }
        private void PlayClip(string word,float duration)
        {
            if(rigAnimation==null)return;rigAnimation.enabled=true;
            foreach(AnimationState state in rigAnimation)
            {
                if(!state.name.ToLowerInvariant().Contains(word))continue;
                state.wrapMode=WrapMode.ClampForever;state.speed=state.length/Mathf.Max(.1f,duration);state.time=0;rigAnimation.Play(state.name);return;
            }
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
        public void ReviewRetargetAudit(WeaponPickup pickup)
        {
            ReviewPose(pickup,false,0);
            if(gunBone==null || sourceMagazine==null || heldRig==null)throw new System.Exception("Missing authored retarget bones");
            Vector3 home=gunBone.InverseTransformPoint(heldRig.magazine.position);float travel=0;
            foreach(float t in new[]{.2f,.4f,.6f,.8f})
            {
                ReviewPose(pickup,false,t);
                travel=Mathf.Max(travel,Vector3.Distance(home,gunBone.InverseTransformPoint(heldRig.magazine.position))*Mathf.Abs(gunBone.lossyScale.x));
            }
            if(travel<.03f)throw new System.Exception("Authored magazine retarget did not move: "+pickup.displayName+" travel="+travel);
            Debug.Log("FPP authored magazine retarget: "+pickup.displayName+" travel="+travel);
        }
        public void ReviewAmmoAudit(WeaponPickup pickup)
        {
            ReviewPose(pickup,false,0);rounds=1;Reload();
            if(!Reloading || rounds!=1)throw new System.Exception("Reload must wait before refilling ammo");
            Fire();if(rounds!=1)throw new System.Exception("Firing was not blocked during reload");
            CompleteReload(Time.time+reloadDuration+.01f);
            if(Reloading || rounds!=magazine)throw new System.Exception("Reload did not refill the active weapon");
        }
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
            worldCamera.fieldOfView=aim?50:68;weaponCamera.fieldOfView=aim?45:60;
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
