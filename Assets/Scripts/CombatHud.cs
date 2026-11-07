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
        private float hitUntil,numberUntil,damageUntil;
        private int oldRounds=-1,oldReserve=-1;
        private string oldWeapon;
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
            bindings=Text("Ammo reserve / fire mode",new Vector2(0,-280),20);
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
                if(GameplayInput.Instance!=null && GameplayInput.Instance.Settings.damageNumbers){damageNumber.text=Mathf.RoundToInt(e.Value).ToString();numberUntil=Time.time+GunplayConfig.Current.damageNumberSeconds;}
            }
            if(e.Kind==GunplayEventKind.Feed && feed!=null)feed.text=e.Context=="kill"?"TARGET ELIMINATED":e.Context=="head"?"HEADSHOT":"TARGET HIT";
            if(e.Kind==GunplayEventKind.Damage && damageArc!=null)
            {damageArc.text="⌃";damageArc.color=Color.red;damageUntil=Time.time+GunplayConfig.Current.damageNumberSeconds;damageArc.rectTransform.localRotation=Quaternion.Euler(0,0,-e.Value);}
        }
        private void LateUpdate()
        {
            if(weapons==null||weapons.ActiveWeapon==null||marker==null)return;var c=GunplayConfig.Current;
            marker.enabled=Time.time<hitUntil;float hitT=Mathf.Clamp01((hitUntil-Time.time)/c.hitmarkerSeconds);marker.rectTransform.localScale=Vector3.one*Mathf.Lerp(1,c.hitmarkerPunch,hitT);
            damageNumber.enabled=Time.time<numberUntil;damageArc.enabled=Time.time<damageUntil;
            if(oldRounds!=weapons.Ammo||oldReserve!=weapons.Reserve||oldWeapon!=weapons.DisplayName)
            {oldRounds=weapons.Ammo;oldReserve=weapons.Reserve;oldWeapon=weapons.DisplayName;bindings.text=weapons.ActiveWeapon.fireMode+"   "+oldRounds+" / "+oldReserve;}
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
