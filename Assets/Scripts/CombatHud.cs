using UnityEngine;
using UnityEngine.UI;
namespace Fpp
{
    public sealed class CombatHud : MonoBehaviour
    {
        public WeaponController weapons;
        private Text marker,damageNumber,damageArc,feed,bindings;
        private RectTransform reticle;
        private Image[] ticks;
        private Canvas canvas;
        private Vector3 damageWorld;
        private float hitUntil,numberUntil,damageUntil;
        private int oldRounds=-1,oldReserve=-1;
        private string oldWeapon;private bool oldReload;
        private void OnEnable(){GunplayEvents.Raised+=OnEvent;}
        private void OnDisable(){GunplayEvents.Raised-=OnEvent;}
        private Text Text(string name,Vector2 position,int size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(transform,false);
            var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=Vector2.one*.5f;r.anchoredPosition=position;r.sizeDelta=new Vector2(600,80);
            var text=go.GetComponent<Text>();text.font=Resources.Load<Font>("Fonts/Hud");text.fontSize=size;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;return text;
        }
        private void Start()
        {
            canvas=GetComponent<Canvas>();reticle=transform.Find("Reticle") as RectTransform;ticks=reticle!=null?reticle.GetComponentsInChildren<Image>():null;
            marker=Text("Hitmarker",Vector2.zero,38);damageNumber=Text("Damage numbers",new Vector2(50,45),20);
            damageArc=Text("Incoming damage",new Vector2(0,100),60);feed=Text("Hit feed",new Vector2(0,230),18);
            bindings=Text("Ammo reserve / fire mode",new Vector2(-170,64),20);
            var safe=transform.Find("Safe area");if(safe!=null){bindings.transform.SetParent(safe,false);feed.transform.SetParent(safe,false);}
            bindings.rectTransform.anchorMin=bindings.rectTransform.anchorMax=new Vector2(1,0);bindings.rectTransform.pivot=new Vector2(1,0);bindings.rectTransform.anchoredPosition=new Vector2(-20,18);bindings.rectTransform.sizeDelta=new Vector2(200,84);bindings.alignment=TextAnchor.MiddleRight;bindings.supportRichText=true;
            bindings.gameObject.AddComponent<Outline>().effectColor=new Color(0,0,0,.85f);
            feed.rectTransform.anchorMin=feed.rectTransform.anchorMax=Vector2.one;feed.rectTransform.anchoredPosition=new Vector2(-190,-100);feed.rectTransform.sizeDelta=new Vector2(330,70);feed.alignment=TextAnchor.UpperRight;
            var reset=Text("Reset targets",new Vector2(-480,280),16);reset.text="RESET TARGETS";reset.raycastTarget=true;
            reset.gameObject.AddComponent<Button>().onClick.AddListener(ResetTargets);
        }
        private void ResetTargets(){foreach(var target in FindObjectsOfType<TestTarget>())target.ResetTarget();GunplayEvents.Send(GunplayEventKind.UI,Vector3.zero,null,"resetTargets");}
        private void OnEvent(GunplayEvent e)
        {
            if(e.Kind==GunplayEventKind.Hitmarker && marker!=null)
            {
                hitUntil=Time.time+GunplayConfig.Current.hitmarkerSeconds;marker.text=e.Context=="head"?"× •":"×";
                marker.color=e.Context=="kill"?Color.red:Color.white;marker.fontStyle=e.Context=="kill"?FontStyle.Bold:FontStyle.Normal;
                if(GameplayInput.Instance!=null && GameplayInput.Instance.Settings.damageNumbers){damageWorld=e.Position;damageNumber.text=Mathf.RoundToInt(e.Value).ToString();numberUntil=Time.time+GunplayConfig.Current.damageNumberSeconds;}
            }
            if(e.Kind==GunplayEventKind.Feed && feed!=null)feed.text=e.Context=="kill"?"TARGET ELIMINATED":e.Context=="head"?"HEADSHOT":"TARGET HIT";
            if(e.Kind==GunplayEventKind.Damage && damageArc!=null)
            {damageArc.text="⌃";damageArc.color=Color.red;damageUntil=Time.time+GunplayConfig.Current.damageNumberSeconds;damageArc.rectTransform.localRotation=Quaternion.Euler(0,0,-e.Value);}
        }
        private void LateUpdate()
        {
            if(weapons==null||weapons.ActiveWeapon==null||marker==null)return;var c=GunplayConfig.Current;
            marker.enabled=Time.time<hitUntil;float hitT=Mathf.Clamp01((hitUntil-Time.time)/c.hitmarkerSeconds);marker.rectTransform.localScale=Vector3.one*Mathf.Lerp(1,c.hitmarkerPunch,hitT);
            damageNumber.enabled=Time.time<numberUntil;
            if(damageNumber.enabled){var viewport=Camera.main.WorldToViewportPoint(damageWorld);var size=((RectTransform)transform).rect.size;damageNumber.rectTransform.anchoredPosition=new Vector2((viewport.x-.5f)*size.x,(viewport.y-.5f)*size.y+20*(1-(numberUntil-Time.time)/c.damageNumberSeconds));damageNumber.enabled=viewport.z>0;}
            damageArc.enabled=Time.time<damageUntil;
            if(oldRounds!=weapons.Ammo||oldReserve!=weapons.Reserve||oldWeapon!=weapons.DisplayName||oldReload!=weapons.Reloading)
            {oldRounds=weapons.Ammo;oldReserve=weapons.Reserve;oldWeapon=weapons.DisplayName;oldReload=weapons.Reloading;string tint=oldRounds==0?"#FF3B30":oldRounds<weapons.Capacity*.3f?"#FFB000":"#E8E6E1";
                bindings.text="<size=42><color="+tint+">"+oldRounds+"</color></size>  /  "+oldReserve+"\n"+"<size=14>"+oldWeapon.ToUpperInvariant()+"  |  "+(oldReload?"RELOAD":weapons.ActiveWeapon.fireMode.ToString().ToUpperInvariant())+"</size>";}
            if(ticks!=null)
            {
                float gap=Screen.height*.5f*Mathf.Tan(weapons.SpreadDegrees*Mathf.Deg2Rad)/Mathf.Tan(Camera.main.fieldOfView*.5f*Mathf.Deg2Rad)/Mathf.Max(canvas.scaleFactor,.01f);
                for(int i=1;i<ticks.Length;i++)
                {
                    var r=ticks[i].rectTransform;Vector2 direction=i==1?Vector2.left:i==2?Vector2.right:i==3?Vector2.down:i==4?Vector2.up:Vector2.zero;
                    r.anchoredPosition=direction*gap;ticks[i].enabled=weapons.AdsT<1;
                }
            }
        }
    }
}
