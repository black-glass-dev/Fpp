using UnityEngine;
namespace Fpp
{
    // Bounded greybox pools. Assets/audio can be replaced by named bus listeners in Prompt 2.
    public sealed class GunplayFeedback : MonoBehaviour
    {
        private struct Pooled {public Transform transform;public Vector3 velocity;public float until;public bool landed;public WeaponAsset weapon;}
        private Pooled[] shells,impacts;
        private int shellIndex,impactIndex;
        private Material brass,dust;
        private AudioSource click;
        private AudioClip clickClip;
        private void Awake()
        {
            shells=new Pooled[GunplayConfig.Current.shellPool];impacts=new Pooled[GunplayConfig.Current.impactPool];
            brass=new Material(Shader.Find("Standard")){color=new Color(.6f,.48f,.22f)};
            dust=new Material(Shader.Find("Sprites/Default")){color=new Color(.65f,.6f,.5f,.75f)};
            for(int i=0;i<shells.Length;i++)shells[i].transform=Make("PLACEHOLDER casing",PrimitiveType.Capsule,brass,new Vector3(.008f,.012f,.008f));
            for(int i=0;i<impacts.Length;i++)impacts[i].transform=Make("PLACEHOLDER impact puff",PrimitiveType.Quad,dust,Vector3.one*.06f);
            click=gameObject.AddComponent<AudioSource>();click.spatialBlend=0;click.volume=GunplayConfig.Current.fallbackClickVolume;
            const int rate=22050,samples=660;var data=new float[samples];uint seed=17;
            for(int i=0;i<samples;i++){seed=seed*1664525+1013904223;data[i]=((seed&65535)/32768f-1)*Mathf.Exp(-i/90f);}
            clickClip=AudioClip.Create("PLACEHOLDER mechanical click",samples,1,rate,false);clickClip.SetData(data,0);
            GunplayEvents.Raised+=OnEvent;
        }
        private Transform Make(string name,PrimitiveType type,Material material,Vector3 size)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.layer=0;Destroy(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial=material;go.transform.localScale=size;go.SetActive(false);return go.transform;
        }
        private void OnEvent(GunplayEvent e)
        {
            var c=GunplayConfig.Current;
            if(e.Kind==GunplayEventKind.ShellEject)
            {
                int i=shellIndex++%shells.Length;var p=shells[i];p.transform.position=e.Position;p.transform.gameObject.SetActive(true);p.velocity=(e.Normal+Vector3.up)*c.shellSpeed;p.until=Time.time+c.shellLife;p.landed=false;p.weapon=e.Weapon;shells[i]=p;
            }
            if(e.Kind==GunplayEventKind.Impact)
            {
                int i=impactIndex++%impacts.Length;var p=impacts[i];p.transform.position=e.Position+e.Normal*c.nearClip;p.transform.rotation=Quaternion.LookRotation(-e.Normal);p.transform.gameObject.SetActive(true);p.until=Time.time+c.impactLife;impacts[i]=p;
            }
            if(e.Kind==GunplayEventKind.DryFire||e.Kind==GunplayEventKind.ReloadFoley||e.Kind==GunplayEventKind.UI||e.Kind==GunplayEventKind.Pickup||e.Kind==GunplayEventKind.Mount)click.PlayOneShot(clickClip);
        }
        private void Update()
        {
            var c=GunplayConfig.Current;float now=Time.time,dt=Time.deltaTime;
            for(int i=0;i<shells.Length;i++)
            {
                var p=shells[i];if(!p.transform.gameObject.activeSelf)continue;
                if(now>=p.until){p.transform.gameObject.SetActive(false);continue;}
                if(!p.landed)
                {
                    p.velocity+=Vector3.down*c.gravity*dt;var travel=p.velocity*dt;
                    if(Physics.Raycast(p.transform.position,travel.normalized,out var hit,travel.magnitude,~(1<<8),QueryTriggerInteraction.Ignore))
                    {p.transform.position=hit.point;p.landed=true;GunplayEvents.Send(GunplayEventKind.ShellLand,hit.point,p.weapon,SurfaceProfile.Name(hit.collider));}
                    else p.transform.position+=travel;
                    p.transform.Rotate(Vector3.right*(c.shellSpeed*Mathf.Rad2Deg*dt));shells[i]=p;
                }
            }
            for(int i=0;i<impacts.Length;i++){var p=impacts[i];if(p.transform.gameObject.activeSelf){p.transform.localScale=Vector3.one*c.nearClip*(1+(1-(p.until-now)/c.impactLife)*4);if(now>=p.until)p.transform.gameObject.SetActive(false);}}
        }
        private void OnDestroy()
        {
            GunplayEvents.Raised-=OnEvent;
            if(shells!=null)foreach(var p in shells)if(p.transform!=null)Destroy(p.transform.gameObject);
            if(impacts!=null)foreach(var p in impacts)if(p.transform!=null)Destroy(p.transform.gameObject);
            if(brass!=null)Destroy(brass);if(dust!=null)Destroy(dust);if(clickClip!=null)Destroy(clickClip);
        }
    }
}
