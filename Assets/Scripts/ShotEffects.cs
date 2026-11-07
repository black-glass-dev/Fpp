using UnityEngine;
namespace Fpp
{
    [DefaultExecutionOrder(100)]
    public sealed class ShotEffects : MonoBehaviour
    {
        private Transform muzzle;
        private Camera overlay,world;
        private GameObject flash;
        private ParticleSystem smoke;
        private Material tracerMaterial;
        private LineRenderer[] lines=new LineRenderer[6];
        private Vector3[] starts=new Vector3[6],ends=new Vector3[6];
        private float[] times=new float[6];
        private float flashUntil;
        private int next;
        private bool pending;private Vector3 pendingHit;
        public void Initialize(Transform source,Camera view,Camera scene,Material material,Material smokeSource,Material tracerSource)
        {
            muzzle=source;overlay=view;world=scene;
            flash=new GameObject("Barrel flash",typeof(MeshFilter),typeof(MeshRenderer));flash.layer=8;flash.transform.SetParent(muzzle,false);flash.transform.localScale=Vector3.one*.075f;
            var mesh=new Mesh();mesh.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)};
            mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateBounds();
            flash.GetComponent<MeshFilter>().sharedMesh=mesh;flash.GetComponent<MeshRenderer>().sharedMaterial=material;flash.SetActive(false);if(!Application.isPlaying)flash.hideFlags=HideFlags.DontSave;
            var go=new GameObject("Barrel smoke");go.layer=8;go.transform.SetParent(muzzle,false);smoke=go.AddComponent<ParticleSystem>();smoke.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            if(!Application.isPlaying)go.hideFlags=HideFlags.DontSave;
            var main=smoke.main;main.playOnAwake=false;main.loop=false;main.duration=1;main.startLifetime=new ParticleSystem.MinMaxCurve(.65f,1.05f);main.startSpeed=new ParticleSystem.MinMaxCurve(.35f,.7f);
            main.startSize=new ParticleSystem.MinMaxCurve(.065f,.105f);main.startColor=new Color(.48f,.51f,.54f,.65f);main.maxParticles=96;main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=smoke.emission;emission.enabled=false;
            var shape=smoke.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=14;shape.radius=.008f;
            var size=smoke.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.65f),new Keyframe(.35f,1.9f),new Keyframe(1,3.2f)));
            var drift=smoke.velocityOverLifetime;drift.enabled=true;drift.space=ParticleSystemSimulationSpace.World;drift.x=.025f;drift.y=.10f;drift.z=.015f;
            var turbulence=smoke.noise;turbulence.enabled=true;turbulence.strength=.065f;turbulence.frequency=1.2f;
            var spin=smoke.main;spin.startRotation=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);
            var color=smoke.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(.95f,0),new GradientAlphaKey(0,1)});color.color=gradient;
            smoke.GetComponent<ParticleSystemRenderer>().sharedMaterial=new Material(smokeSource);smoke.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            tracerMaterial=new Material(tracerSource);tracerMaterial.color=new Color(1,.9f,.65f,.9f);
            for(int i=0;i<lines.Length;i++){var line=new GameObject("Fast bullet streak").AddComponent<LineRenderer>();if(!Application.isPlaying)line.gameObject.hideFlags=HideFlags.DontSave;line.sharedMaterial=tracerMaterial;line.positionCount=2;line.startWidth=.007f;line.endWidth=.002f;line.useWorldSpace=true;line.enabled=false;lines[i]=line;}
        }
        public void Play(Vector3 hit){pendingHit=hit;pending=true;}
        private void LateUpdate(){if(pending){pending=false;if(!WeaponController.MenuOpen)EmitNow(pendingHit);}}
        private void EmitNow(Vector3 hit)
        {
            flashUntil=Time.time+.075f;flash.SetActive(true);flash.transform.localRotation=Quaternion.Euler(0,0,Random.Range(0,360));
            if(!smoke.isPlaying)smoke.Play();smoke.Emit(6);
            int id=next++%lines.Length;
            // Overlay FOV differs from the world camera: project the real barrel onto
            // the scene ray instead of drawing a world tracer behind the gun.
            Vector3 viewport=overlay.WorldToViewportPoint(muzzle.position);
            Ray ray=world.ViewportPointToRay(viewport);float hitDistance=Vector3.Distance(world.transform.position,hit);
            float nearOffset=Vector3.Distance(world.transform.position,ray.origin);
            if(hitDistance<=nearOffset){lines[id].enabled=false;return;}
            starts[id]=ray.GetPoint(Mathf.Max(0,Mathf.Min(.8f,hitDistance*.6f)-nearOffset));ends[id]=hit;times[id]=Time.time;
            RenderTrace(id,0);
        }
        private void RenderTrace(int id,float age)
        {
            float length=Vector3.Distance(starts[id],ends[id]);float head=Mathf.Min(length,2+age*420),tail=Mathf.Max(0,head-2);
            lines[id].enabled=age<.18f && age<(length+2)/420 && length>.1f;
            Vector3 direction=(ends[id]-starts[id]).normalized;lines[id].SetPosition(0,starts[id]+direction*tail);lines[id].SetPosition(1,starts[id]+direction*head);
        }
        private void Update()
        {
            if(flash==null)return;flash.SetActive(Time.time<flashUntil && !WeaponController.MenuOpen);
            for(int i=0;i<lines.Length;i++)if(lines[i].enabled)RenderTrace(i,Time.time-times[i]);
        }
#if UNITY_EDITOR
        public void Review(Vector3 hit)
        {EmitNow(hit);smoke.Simulate(.1f,true,false,false);flash.SetActive(true);}
#endif
        private void OnDestroy()
        {foreach(var line in lines)if(line!=null)Release(line.gameObject);if(tracerMaterial!=null)Release(tracerMaterial);if(flash!=null)Release(flash);if(smoke!=null)Release(smoke.gameObject);}
        private static void Release(Object value){if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
    }
}
