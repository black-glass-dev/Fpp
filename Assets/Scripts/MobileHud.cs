using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Fpp
{
    [Serializable] public class ControlPlacement
    {
        public string id;
        public float x, y, scale = 1;
    }
    [Serializable] public class ControlLayout { public ControlPlacement[] controls; }

    public sealed class MobileHud : MonoBehaviour
    {
        public static MobileHud Instance { get; private set; }
        public bool Editing { get; private set; }
        public bool Modal => Editing || creditsPanel != null && creditsPanel.activeSelf;
        public Vector2 Move { get; private set; }
        public bool FireHeld { get; private set; }
        public bool AimHeld { get; private set; }
        private const string PrefKey = "fpp.controls.v2";
        private readonly List<HudControl> controls = new List<HudControl>();
        private RectTransform safeRoot;
        private Canvas canvas;
        private Font font;
        private Sprite circle;
        private WeaponController weapons;
        private Text status, pickupLabel, editLabel;
        private GameObject editorPanel, creditsPanel;
        private Slider scaleSlider;
        private HudControl selected;
        private Rect lastSafe;
        private HudControl joystick;

        private void Awake() { Instance = this; weapons = GetComponent<WeaponController>(); }
        private void Start()
        {
            font = Resources.Load<Font>("Fonts/Hud");
            if(font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            circle = CircleSprite();
            if (EventSystem.current == null)
            {
                var events = new GameObject("Touch events");
                events.AddComponent<EventSystem>(); events.AddComponent<StandaloneInputModule>();
            }
            var go = new GameObject("Mobile HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280,720); scaler.matchWidthOrHeight = .5f;
            safeRoot = Panel("Safe area", go.transform, Color.clear); safeRoot.GetComponent<Image>().raycastTarget = false;
            status = Label("Weapon status", safeRoot, "MARK 23  •  READY", 22, TextAnchor.UpperLeft);
            Rect(status.rectTransform, new Vector2(0,1), new Vector2(0,1), new Vector2(270,-50), new Vector2(500,70));
            var crosshair = Label("Reticle", safeRoot, "+", 27, TextAnchor.MiddleCenter);
            Rect(crosshair.rectTransform, Vector2.one*.5f, Vector2.one*.5f, Vector2.zero, new Vector2(30,30));
            pickupLabel = Label("Pickup hint", safeRoot, "", 20, TextAnchor.MiddleCenter);
            Rect(pickupLabel.rectTransform, new Vector2(.2f,.36f), new Vector2(.8f,.36f), Vector2.zero, new Vector2(0,40));
            joystick = AddControl("move", "MOVE", .14f,.22f,150);
            AddControl("fire", "FIRE", .89f,.23f,108);
            AddControl("reload", "RELOAD", .79f,.15f,82);
            AddControl("pickup", "TAKE", .78f,.35f,82);
            AddControl("sidearm", "PISTOL", .68f,.15f,76);
            AddControl("aim", "ADS", .9f,.45f,80);
            TopButton("LAYOUT", new Vector2(-192,-30), () => SetEditing(true));
            TopButton("CREDITS", new Vector2(-65,-30), ShowCredits);
            LoadLayout();
            CreateEditor(); CreateCredits(); ApplySafeArea();
        }
        private void Update()
        {
            if (safeRoot == null) return;
            if (lastSafe != Screen.safeArea) ApplySafeArea();
            if (weapons != null)
            {
                status.text = weapons.DisplayName.ToUpperInvariant() + "   " + weapons.Ammo + " / " + weapons.Capacity +
                    (weapons.Reloading ? "   RELOADING" : "") + "\nTARGET HITS  " + weapons.Hits;
                pickupLabel.text = weapons.NearbyName.Length > 0 ? "TAKE  •  " + weapons.NearbyName : "";
            }
        }
        private void ApplySafeArea()
        {
            lastSafe = Screen.safeArea;
            safeRoot.anchorMin = new Vector2(lastSafe.xMin/Screen.width,lastSafe.yMin/Screen.height);
            safeRoot.anchorMax = new Vector2(lastSafe.xMax/Screen.width,lastSafe.yMax/Screen.height);
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
        }
        private HudControl AddControl(string id,string text,float x,float y,float size)
        {
            var root = Panel(id,safeRoot,new Color(.07f,.1f,.12f,.65f));
            var image = root.GetComponent<Image>(); image.sprite = circle;
            var outline=root.gameObject.AddComponent<Outline>(); outline.effectColor=new Color(.8f,.87f,.84f,.8f);outline.effectDistance=Vector2.one*2;
            var control=root.gameObject.AddComponent<HudControl>();
            control.owner=this;control.placement=new ControlPlacement { id=id,x=x,y=y,scale=1 };control.defaultPosition=new Vector2(x,y);control.baseSize=size;
            var label=Label(id+" label",root,text,id=="fire"?24:17,TextAnchor.MiddleCenter);label.raycastTarget=false;
            label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;
            if(id=="move")
            {
                var knob=Panel("Joystick thumb",root,new Color(.78f,.85f,.8f,.6f));knob.GetComponent<Image>().sprite=circle;knob.GetComponent<Image>().raycastTarget=false;
                Rect(knob,Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,Vector2.one*55);control.knob=knob;
            }
            controls.Add(control);control.Apply();return control;
        }
        private void TopButton(string text,Vector2 position,UnityEngine.Events.UnityAction action)
        {
            var root=Panel(text,safeRoot,new Color(.06f,.09f,.1f,.85f));Rect(root,Vector2.one,Vector2.one,position,new Vector2(115,42));
            var label=Label(text+" text",root,text,17,TextAnchor.MiddleCenter);label.raycastTarget=false;
            label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;
            root.gameObject.AddComponent<Button>().onClick.AddListener(action);
        }
        private void CreateEditor()
        {
            var root=Panel("Layout editor",safeRoot,new Color(.035f,.05f,.065f,.97f));
            Rect(root,new Vector2(.15f,.73f),new Vector2(.85f,.94f),Vector2.zero,Vector2.zero);editorPanel=root.gameObject;
            editLabel=Label("Help",root,"Drag any control. Select it, then adjust SIZE.",19,TextAnchor.UpperCenter);
            Rect(editLabel.rectTransform,new Vector2(0,.65f),new Vector2(1,.98f),Vector2.zero,Vector2.zero);
            var sliderRoot=Panel("Size slider",root,new Color(.3f,.35f,.35f,1));
            Rect(sliderRoot,new Vector2(.07f,.35f),new Vector2(.6f,.55f),Vector2.zero,Vector2.zero);
            scaleSlider=sliderRoot.gameObject.AddComponent<Slider>();scaleSlider.minValue=.6f;scaleSlider.maxValue=1.6f;
            var handle=Panel("Handle",sliderRoot,new Color(.9f,.76f,.4f,1));Rect(handle,new Vector2(.5f,0),new Vector2(.5f,1),Vector2.zero,new Vector2(24,0));
            scaleSlider.handleRect=handle;scaleSlider.targetGraphic=handle.GetComponent<Image>();
            scaleSlider.onValueChanged.AddListener(value=> { if(selected!=null) { selected.placement.scale=value;selected.Apply(); } });
            SmallButton(root,"RESET",new Vector2(.65f,.12f),new Vector2(.8f,.55f),ResetLayout);
            SmallButton(root,"SAVE",new Vector2(.82f,.12f),new Vector2(.98f,.55f),()=>SetEditing(false));
            editorPanel.SetActive(false);
        }
        private void SmallButton(Transform parent,string text,Vector2 min,Vector2 max,UnityEngine.Events.UnityAction callback)
        {
            var p=Panel(text,parent,new Color(.22f,.3f,.3f,1));Rect(p,min,max,Vector2.zero,Vector2.zero);
            var t=Label(text,p,text,18,TextAnchor.MiddleCenter);t.raycastTarget=false;t.rectTransform.anchorMin=Vector2.zero;t.rectTransform.anchorMax=Vector2.one;t.rectTransform.offsetMin=t.rectTransform.offsetMax=Vector2.zero;
            p.gameObject.AddComponent<Button>().onClick.AddListener(callback);
        }
        private void CreateCredits()
        {
            var p=Panel("Credits",safeRoot,new Color(.035f,.05f,.065f,.98f));Rect(p,new Vector2(.1f,.1f),new Vector2(.9f,.9f),Vector2.zero,Vector2.zero);creditsPanel=p.gameObject;
            SmallButton(p,"CLOSE",new Vector2(.83f,.89f),new Vector2(.98f,.98f),()=>creditsPanel.SetActive(false));
            var viewport=Panel("Scroll area",p,Color.clear);Rect(viewport,new Vector2(.03f,.03f),new Vector2(.97f,.85f),Vector2.zero,Vector2.zero);
            viewport.gameObject.AddComponent<RectMask2D>();var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.viewport=viewport;
            var text=Label("Notices",viewport,"",18,TextAnchor.UpperLeft);text.rectTransform.anchorMin=new Vector2(0,1);text.rectTransform.anchorMax=Vector2.one;text.rectTransform.pivot=new Vector2(.5f,1);text.rectTransform.sizeDelta=new Vector2(0,2200);
            var main=Resources.Load<TextAsset>("THIRD_PARTY_NOTICES");var mosque=Resources.Load<TextAsset>("MOSQUE_NOTICE");text.text=(main!=null?main.text:"")+"\n\n"+(mosque!=null?mosque.text:"")+"\n\nHUD font: DejaVu Sans Bold. Bitstream/DejaVu license notice bundled at Fonts/LICENSE.";
            scroll.content=text.rectTransform;creditsPanel.SetActive(false);
        }
        public void Select(HudControl control)
        {
            selected=control;scaleSlider.SetValueWithoutNotify(control.placement.scale);editLabel.text=control.placement.id.ToUpperInvariant()+"  •  Drag to move  •  SIZE "+control.placement.scale.ToString("0.0");
        }
        public void SetEditing(bool editing)
        {
            Editing=editing;Move=Vector2.zero;FireHeld=AimHeld=false;editorPanel.SetActive(editing);
            foreach(var c in controls)c.ResetInput();
            if(editing) Select(controls[0]);else SaveLayout();
        }
        public void Press(string id,bool down)
        {
            if(Modal)return;
            if(id=="fire")FireHeld=down;
            if(id=="aim")AimHeld=down;
            if(down && id=="reload")weapons.Reload();
            if(down && id=="pickup")weapons.EquipNearest();
            if(down && id=="sidearm")weapons.UseSidearm();
        }
        public bool BlocksLook(Vector2 position)
        {
            if(Modal)return true;
            foreach(var control in controls)
                if(RectTransformUtility.RectangleContainsScreenPoint((RectTransform)control.transform,position,null))return true;
            return position.y>Screen.safeArea.yMax-Screen.height*.1f && position.x>Screen.width*.7f;
        }
        public void SetMove(Vector2 value) { Move=Modal?Vector2.zero:value; }
        public void Release(string id)
        {
            if(id=="fire")FireHeld=false;if(id=="aim")AimHeld=false;if(id=="move")Move=Vector2.zero;
        }
        private void ShowCredits() { FireHeld=AimHeld=false;Move=Vector2.zero;creditsPanel.SetActive(true); }
        private void ResetLayout()
        {
            foreach(var c in controls) { c.placement.x=c.defaultPosition.x;c.placement.y=c.defaultPosition.y;c.placement.scale=1;c.Apply(); }
            PlayerPrefs.DeleteKey(PrefKey);Select(controls[0]);
        }
        private void SaveLayout()
        {
            var layout=new ControlLayout { controls=controls.ConvertAll(c=>c.placement).ToArray() };
            PlayerPrefs.SetString(PrefKey,JsonUtility.ToJson(layout));PlayerPrefs.Save();
        }
        private void LoadLayout()
        {
            try
            {
                var layout=JsonUtility.FromJson<ControlLayout>(PlayerPrefs.GetString(PrefKey,"{}"));
                if(layout==null || layout.controls==null)return;
                foreach(var saved in layout.controls)
                foreach(var c in controls) if(c.placement.id==saved.id)
                { c.placement.x=Mathf.Clamp(saved.x,.07f,.93f);c.placement.y=Mathf.Clamp(saved.y,.09f,.85f);c.placement.scale=Mathf.Clamp(saved.scale,.6f,1.6f);c.Apply(); }
            }
            catch(Exception) { PlayerPrefs.DeleteKey(PrefKey); }
        }
        private void OnApplicationFocus(bool focused)
        {
            if(!focused) { FireHeld=AimHeld=false;Move=Vector2.zero;foreach(var c in controls)c.ResetInput(); }
        }
        private void OnDestroy() { if(Instance==this)Instance=null; }
        private Text Label(string name,Transform parent,string content,int size,TextAnchor align)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);
            var t=go.GetComponent<Text>();t.font=font;t.text=content;t.fontSize=size;t.color=new Color(.93f,.94f,.89f);t.alignment=align;t.raycastTarget=false;
            return t;
        }
        private static RectTransform Panel(string name,Transform parent,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);go.GetComponent<Image>().color=color;
            var r=go.GetComponent<RectTransform>();r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;return r;
        }
        private static void Rect(RectTransform r,Vector2 min,Vector2 max,Vector2 pos,Vector2 size)
        { r.anchorMin=min;r.anchorMax=max;r.anchoredPosition=pos;r.sizeDelta=size; }
        private static Sprite CircleSprite()
        {
            const int size=64;var tex=new Texture2D(size,size,TextureFormat.RGBA32,false);tex.name="HUD circle";
            var pixels=new Color[size*size];for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            { float d=Vector2.Distance(new Vector2(x,y),Vector2.one*31.5f);pixels[y*size+x]=new Color(1,1,1,Mathf.Clamp01(31.5f-d)); }
            tex.SetPixels(pixels);tex.Apply();return Sprite.Create(tex,new UnityEngine.Rect(0,0,size,size),Vector2.one*.5f);
        }
    }
}
