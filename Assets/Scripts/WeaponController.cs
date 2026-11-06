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
        public Material flashMaterial;
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
        private GameObject heldVisual, flash;
        private Animation rigAnimation;
        private Camera worldCamera;
        private LineRenderer tracer;
        private AudioSource sound;
        private AudioClip shotSound;
        private int rounds=12, magazine=12, hits;
        private float nextShot, reloadEnd, flashEnd, recoil, equipBlend;
        private bool equipping;
        private Vector3 basePosition;
        private Quaternion baseRotation;
        private Transform leftUpper, leftForearm, leftHand;
        private Quaternion upperRest, foreRest, handRest;
        private bool supportPoseCached;

        private void Start()
        {
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
            PlayClip("draw",.65f);
            // Flat, short-lived star flash and a thin world-space tracer, never a projectile sphere.
            flash=new GameObject("Muzzle flash",typeof(MeshFilter),typeof(MeshRenderer));
            flash.transform.SetParent(muzzle,false);flash.transform.localScale=Vector3.one*.045f;flash.layer=8;
            var mesh=new Mesh();mesh.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)};
            mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateBounds();
            flash.GetComponent<MeshFilter>().sharedMesh=mesh;flash.GetComponent<MeshRenderer>().sharedMaterial=flashMaterial;flash.SetActive(false);
            var line=new GameObject("Brief shot tracer");tracer=line.AddComponent<LineRenderer>();tracer.sharedMaterial=flashMaterial;
            tracer.positionCount=2;tracer.startWidth=.006f;tracer.endWidth=.002f;tracer.useWorldSpace=true;tracer.enabled=false;
            sound=gameObject.AddComponent<AudioSource>();sound.spatialBlend=0;sound.volume=.2f;shotSound=MakeShotSound();
        }
        private void Update()
        {
            if(flash!=null)flash.SetActive(!MenuOpen && Time.time<flashEnd);
            if(tracer!=null)tracer.enabled=!MenuOpen && Time.time<flashEnd;
            if(MenuOpen)return;
            nearest=null;float distance=2.6f;
            foreach(var pickup in pickups)
            {
                if(pickup==null || pickup==equipped || !pickup.gameObject.activeSelf)continue;
                float d=Vector3.Distance(transform.position,pickup.transform.position);
                if(d<distance){ nearest=pickup;distance=d; }
            }
            if(reloadEnd>0 && Time.time>=reloadEnd){ rounds=magazine;reloadEnd=0; }
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
            Vector3 position=basePosition+(aiming?new Vector3(-.115f,.11f,.03f):Vector3.zero);
            position+=new Vector3(0,-equipBlend*.28f,-recoil*.028f);
            float walk=MobileHud.Instance!=null?MobileHud.Instance.Move.magnitude:Mathf.Clamp01(Mathf.Abs(Input.GetAxisRaw("Vertical"))+Mathf.Abs(Input.GetAxisRaw("Horizontal")));
            position+=new Vector3(Mathf.Sin(Time.time*7)*.003f,Mathf.Cos(Time.time*14)*.003f,0)*walk;
            defaultViewmodel.transform.localPosition=Vector3.Lerp(defaultViewmodel.transform.localPosition,position,Time.deltaTime*18);
            defaultViewmodel.transform.localRotation=baseRotation*Quaternion.Euler(-recoil*3+equipBlend*25,equipBlend*-12,Reloading?Mathf.Sin((reloadEnd-Time.time)*5)*10:0);
            if(worldCamera!=null)worldCamera.fieldOfView=Mathf.Lerp(worldCamera.fieldOfView,aiming?50:68,Time.deltaTime*12);
            if(weaponCamera!=null)weaponCamera.fieldOfView=Mathf.Lerp(weaponCamera.fieldOfView,aiming?45:60,Time.deltaTime*12);
            if(equipped!=null && DisplayName.Contains("AK") && supportPoseCached)SolveSupportHand();
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
            RestoreSupportPose();
            if(equipped!=null)equipped.gameObject.SetActive(true);
            if(heldVisual!=null)Destroy(heldVisual);
            equipped=next;
            bool sidearm=next==null;
            foreach(var renderer in pistolRenderers)renderer.enabled=sidearm;
            if(sidearm)
            {
                DisplayName="Mark 23";magazine=12;PlayClip("draw",.55f);
            }
            else
            {
                DisplayName=next.displayName;magazine=DisplayName.Contains("AK")?30:17;
                heldVisual=Instantiate(next.gameObject,weaponMount,false);Destroy(heldVisual.GetComponent<WeaponPickup>());
                foreach(var collider in heldVisual.GetComponentsInChildren<Collider>())Destroy(collider);
                heldVisual.transform.localPosition=Vector3.zero;heldVisual.transform.localRotation=Quaternion.identity;heldVisual.transform.localScale=Vector3.one;
                SetLayer(heldVisual,8);
                Bounds bound=LocalBounds(heldVisual.transform);
                Vector3 axis=bound.size.x>bound.size.y && bound.size.x>bound.size.z?Vector3.right:(bound.size.y>bound.size.z?Vector3.up:Vector3.forward);
                if(DisplayName.Contains("AK"))axis=-axis;
                float scale=(DisplayName.Contains("AK")?.65f:.27f)/Mathf.Max(bound.size.x,bound.size.y,bound.size.z);
                heldVisual.transform.localRotation=Quaternion.FromToRotation(axis,Vector3.forward);
                heldVisual.transform.localScale=Vector3.one*scale;
                heldVisual.transform.localPosition=-(heldVisual.transform.localRotation*(bound.center*scale));
                next.gameObject.SetActive(false);
                HoldDrawPose();
                if(leftUpper!=null && leftForearm!=null && leftHand!=null)
                {
                    upperRest=leftUpper.localRotation;foreRest=leftForearm.localRotation;handRest=leftHand.localRotation;
                    supportPoseCached=true;
                }
            }
            muzzle.localPosition=new Vector3(0,.05f,!sidearm && DisplayName.Contains("AK")?.33f:.16f);
            rounds=magazine;reloadEnd=0;
            for(float t=0;t<.32f;t+=Time.deltaTime){equipBlend=1-Mathf.SmoothStep(0,1,t/.32f);yield return null;}
            equipBlend=0;equipping=false;nextShot=Time.time+.1f;
        }
        private void RestoreSupportPose()
        {
            if(!supportPoseCached)return;
            leftUpper.localRotation=upperRest;leftForearm.localRotation=foreRest;leftHand.localRotation=handRest;
            supportPoseCached=false;
        }
        private void SolveSupportHand()
        {
            leftUpper.localRotation=upperRest;leftForearm.localRotation=foreRest;leftHand.localRotation=handRest;
            Quaternion palm=leftHand.rotation;
            Vector3 target=weaponMount.TransformPoint(new Vector3(0,-.035f,.12f));
            for(int i=0;i<4;i++)
            {
                Vector3 from=leftHand.position-leftForearm.position,to=target-leftForearm.position;
                if(from.sqrMagnitude>.000001f && to.sqrMagnitude>.000001f)
                    leftForearm.rotation=Quaternion.FromToRotation(from,to)*leftForearm.rotation;
                from=leftHand.position-leftUpper.position;to=target-leftUpper.position;
                if(from.sqrMagnitude>.000001f && to.sqrMagnitude>.000001f)
                    leftUpper.rotation=Quaternion.FromToRotation(from,to)*leftUpper.rotation;
            }
            leftHand.rotation=palm;
        }
        public void Reload()
        {
            if(rounds>=magazine || Reloading || equipping || MenuOpen)return;
            reloadEnd=Time.time+1.6f;if(equipped==null)PlayClip("reload",1.6f);
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
            flashEnd=Time.time+.025f;
            flash.transform.localRotation=Quaternion.Euler(0,0,Random.Range(0,360));
            tracer.SetPosition(0,muzzle.position);tracer.SetPosition(1,end);
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
            if(rigAnimation==null)return;
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
        private static AudioClip MakeShotSound()
        {
            const int samples=4410;var data=new float[samples];var random=new System.Random(51);
            for(int i=0;i<samples;i++){float t=(float)i/samples;data[i]=(float)(random.NextDouble()*2-1)*Mathf.Exp(-t*14)*.5f+Mathf.Sin(i*.05f)*Mathf.Exp(-t*20)*.25f;}
            var clip=AudioClip.Create("Original synthetic test shot",samples,1,44100,false);clip.SetData(data,0);return clip;
        }
    }
}
