using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Fpp
{
    public sealed class WeaponController : MonoBehaviour
    {
        public Transform view;
        public GameObject defaultViewmodel;
        public Transform weaponMount, muzzle;
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
        private Transform leftUpper, leftForearm, leftHand;
        private Quaternion upperRest, foreRest, handRest;
        private bool supportPoseCached, poseInitialized;
        private float reloadDuration=1.6f, secondaryAimLift=.08f;
        private Vector3 supportRest;

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
            }
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
            var pose=WeaponPresentation.Sample(aiming && !Reloading,equipped!=null,progress,secondaryAimLift);
            Vector3 position=basePosition+pose.offset;
            position+=new Vector3(0,-equipBlend*.28f,-recoil*.028f);
            float walk=MobileHud.Instance!=null?MobileHud.Instance.Move.magnitude:Mathf.Clamp01(Mathf.Abs(Input.GetAxisRaw("Vertical"))+Mathf.Abs(Input.GetAxisRaw("Horizontal")));
            position+=new Vector3(Mathf.Sin(Time.time*7)*.003f,Mathf.Cos(Time.time*14)*.003f,0)*walk;
            defaultViewmodel.transform.localPosition=Vector3.Lerp(defaultViewmodel.transform.localPosition,position,Time.deltaTime*18);
            defaultViewmodel.transform.localRotation=baseRotation*Quaternion.Euler(pose.euler+new Vector3(-recoil*3+equipBlend*25,equipBlend*-12,0));
            if(worldCamera!=null)worldCamera.fieldOfView=Mathf.Lerp(worldCamera.fieldOfView,aiming?50:68,Time.deltaTime*12);
            if(weaponCamera!=null)weaponCamera.fieldOfView=Mathf.Lerp(weaponCamera.fieldOfView,aiming?45:60,Time.deltaTime*12);
            if(heldRig!=null)heldRig.Pose(progress,weaponMount);
            if(equipped!=null && supportPoseCached)
            {
                if(Reloading)SolveSupportHand(heldRig!=null?heldRig.HandTarget(progress,weaponMount,supportRest):weaponMount.TransformPoint(WeaponPresentation.SupportTarget(progress,supportRest,DisplayName.Contains("AK"))));
                else if(DisplayName.Contains("AK"))SolveSupportHand(weaponMount.TransformPoint(new Vector3(0,-.035f,.065f)));
                else RestoreHandRotations();
            }
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
            RestoreSupportPose();
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
                muzzle.localPosition=new Vector3(posedBounds.center.x,posedBounds.max.y-posedBounds.size.y*(DisplayName.Contains("AK")?.22f:.12f),posedBounds.max.z+.012f);
                secondaryAimLift=Mathf.Clamp(.16f-posedBounds.max.y+.004f,-.05f,.18f);
                heldRig=heldVisual.GetComponent<WeaponRigParts>();if(heldRig!=null)heldRig.Bind();
                next.gameObject.SetActive(false);
                HoldDrawPose();
                if(leftUpper!=null && leftForearm!=null && leftHand!=null)
                {
                    upperRest=leftUpper.localRotation;foreRest=leftForearm.localRotation;handRest=leftHand.localRotation;
                    supportPoseCached=true;supportRest=weaponMount.InverseTransformPoint(leftHand.position);
                    rigAnimation.enabled=false;
                }
            }
            if(sidearm)muzzle.localPosition=new Vector3(0,.05f,.16f);
            rounds=magazine;reloadEnd=0;
        }
        private void RestoreSupportPose()
        {
            if(!supportPoseCached)return;
            leftUpper.localRotation=upperRest;leftForearm.localRotation=foreRest;leftHand.localRotation=handRest;
            supportPoseCached=false;
        }
        private void RestoreHandRotations()
        {
            leftUpper.localRotation=upperRest;leftForearm.localRotation=foreRest;leftHand.localRotation=handRest;
        }
        private void SolveSupportHand(Vector3 target)
        {
            RestoreHandRotations();Quaternion palm=leftHand.rotation;
            Vector3 root=leftUpper.position;float upper=Vector3.Distance(root,leftForearm.position),fore=Vector3.Distance(leftForearm.position,leftHand.position);
            Vector3 delta=target-root;float distance=Mathf.Clamp(delta.magnitude,Mathf.Abs(upper-fore)+.001f,(upper+fore)*.985f);
            Vector3 direction=delta.normalized;
            Vector3 hint=weaponMount.TransformPoint(new Vector3(-.25f,-.35f,-.12f))-root;
            Vector3 bend=(hint-direction*Vector3.Dot(hint,direction)).normalized;
            float along=(upper*upper-fore*fore+distance*distance)/(2*distance);
            Vector3 elbow=root+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
            leftUpper.rotation=Quaternion.FromToRotation(leftForearm.position-root,elbow-root)*leftUpper.rotation;
            leftForearm.rotation=Quaternion.FromToRotation(leftHand.position-leftForearm.position,root+direction*distance-leftForearm.position)*leftForearm.rotation;
            leftHand.rotation=palm;
        }
        private void CompleteReload(float now)
        { if(reloadEnd>0 && now>=reloadEnd){rounds=magazine;reloadEnd=0;} }
        public void Reload()
        {
            if(rounds>=magazine || Reloading || equipping || MenuOpen)return;
            reloadDuration=equipped==null?1.6f:DisplayName.Contains("AK")?2.2f:2f;
            reloadEnd=Time.time+reloadDuration;if(equipped==null)PlayClip("reload",1.6f);
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
            InitializePose();ApplyWeapon(pickup);HoldDrawPose();rigAnimation.enabled=pickup==null;
            if(reloadProgress>0){reloadDuration=2;reloadEnd=Time.time+2*(1-reloadProgress);}
            var pose=WeaponPresentation.Sample(aim,pickup!=null,reloadProgress,secondaryAimLift);
            defaultViewmodel.transform.localPosition=basePosition+pose.offset;
            defaultViewmodel.transform.localRotation=baseRotation*Quaternion.Euler(pose.euler);
            if(heldRig!=null)heldRig.Pose(reloadProgress,weaponMount);
            if(pickup!=null && supportPoseCached)
            {
                if(reloadProgress>0)SolveSupportHand(heldRig!=null?heldRig.HandTarget(reloadProgress,weaponMount,supportRest):weaponMount.TransformPoint(WeaponPresentation.SupportTarget(reloadProgress,supportRest,DisplayName.Contains("AK"))));
                else if(DisplayName.Contains("AK"))SolveSupportHand(weaponMount.TransformPoint(new Vector3(0,-.035f,.065f)));
            }
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
