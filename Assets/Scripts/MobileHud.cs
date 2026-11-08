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
        public float x, y, scale = 1, opacity = 1;
    }
    [Serializable] public class ControlLayout { public ControlPlacement[] controls; }

    public sealed class MobileHud : MonoBehaviour
    {
        public static MobileHud Instance { get; private set; }
        public bool Editing { get; private set; }
        public bool FloatingJoystick
        {
            get => PlayerPrefs.GetInt("fpp.controls.floating",1)!=0;
            set { if(joystick!=null)joystick.ResetInput();PlayerPrefs.SetInt("fpp.controls.floating",value?1:0);PlayerPrefs.Save(); }
        }
        public bool Modal => Editing || settingsPanel!=null && settingsPanel.activeSelf || visualPanel!=null && visualPanel.activeSelf || creditsPanel != null && creditsPanel.activeSelf;
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
        private GameObject editorPanel, creditsPanel,settingsPanel,visualPanel;
        private Text visualTitle;private readonly Slider[] gainSliders=new Slider[6];
        private Slider scaleSlider, opacitySlider;
        private InputFrame pending;private bool sprintHeld,crouchHeld;
        private HudControl selected;
        private Rect lastSafe;
        private HudControl joystick;
        private string cachedName,cachedNearby;private int cachedAmmo=-1,cachedHits=-1;private bool cachedReload;

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
            status = Label("Weapon status", safeRoot, "RANGE  //  000 HITS", 28, TextAnchor.UpperCenter);
            Rect(status.rectTransform, new Vector2(.5f,1), new Vector2(.5f,1), new Vector2(0,-18), new Vector2(500,40));
            status.rectTransform.pivot=new Vector2(.5f,1);
            // Aim uses the camera viewport, not the asymmetric notch-safe control area.
            var reticle=Panel("Reticle",go.transform,Color.clear);reticle.GetComponent<Image>().raycastTarget=false;
            Rect(reticle,Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,new Vector2(20,20));
            foreach(var offset in new[]{new Vector2(-6,0),new Vector2(6,0),new Vector2(0,-6),new Vector2(0,6),Vector2.zero})
            {
                var mark=Panel("Reticle mark",reticle,new Color(.93f,.94f,.89f,.6f));
                var shadow=mark.gameObject.AddComponent<Outline>();shadow.effectColor=new Color(0,0,0,.85f);shadow.effectDistance=Vector2.one;mark.GetComponent<Image>().raycastTarget=false;
                var size=offset==Vector2.zero?new Vector2(2,2):(offset.x==0?new Vector2(2,5):new Vector2(5,2));
                Rect(mark,Vector2.one*.5f,Vector2.one*.5f,offset,size);
            }
            pickupLabel = Label("Pickup hint", safeRoot, "", 20, TextAnchor.MiddleCenter);
            Rect(pickupLabel.rectTransform, new Vector2(.2f,.36f), new Vector2(.8f,.36f), Vector2.zero, new Vector2(0,40));
            var zone=Panel("Floating joystick zone",safeRoot,Color.clear);Rect(zone,Vector2.zero,new Vector2(.5f,1),Vector2.zero,Vector2.zero);
            joystick = AddControl("move", "MOVE", .14f,.22f,150);
            zone.gameObject.AddComponent<FloatingJoystickZone>().control=joystick;
            AddControl("fire", "FIRE", .89f,.23f,108);
            AddControl("reload", "RELOAD", .79f,.15f,82);
            AddControl("pickup", "TAKE", .78f,.35f,82);
            AddControl("sidearm", "SWAP", .68f,.15f,76);
            AddControl("aim", "ADS", .9f,.45f,80);
            AddControl("jump","JUMP",.62f,.28f,68);
            AddControl("crouch","CROUCH",.52f,.15f,68);
            AddControl("sprint","SPRINT",.27f,.30f,68);
            AddControl("mount","MOUNT",.68f,.40f,68);
            AddControl("prone","PRONE",.43f,.15f,60);
            go.AddComponent<CombatHud>().weapons=weapons;
            TopButton("LAYOUT", new Vector2(-192,-30), () => SetEditing(true));
            TopButton("CREDITS", new Vector2(-65,-30), ShowCredits);
            TopButton("SETTINGS",new Vector2(-320,-30),()=>{FireHeld=AimHeld=false;Move=Vector2.zero;settingsPanel.SetActive(true);});
            LoadLayout();
            CreateEditor(); CreateCredits();CreateSettings(); ApplySafeArea();
        }
        public void ReviewRefresh(){Update();}
        private void Update()
        {
            if (safeRoot == null) return;
            if (lastSafe != Screen.safeArea) ApplySafeArea();
            var motor=weapons!=null?weapons.GetComponent<TestPlayer>():null;
            foreach(var control in controls)if(control.placement.id=="mount")control.gameObject.SetActive(Editing||motor!=null&&(motor.MountAvailable||motor.Mounted));
            // Objective is top-center; user control placements are never reset by styling.
            if (weapons != null)
            {
                if(cachedAmmo!=weapons.Ammo||cachedHits!=weapons.Hits||cachedName!=weapons.DisplayName||cachedReload!=weapons.Reloading)
                {cachedAmmo=weapons.Ammo;cachedHits=weapons.Hits;cachedName=weapons.DisplayName;cachedReload=weapons.Reloading;
                status.text = "RANGE  //  " + weapons.Hits.ToString("000") + " HITS";}
                if(cachedNearby!=weapons.NearbyName){cachedNearby=weapons.NearbyName;pickupLabel.text=weapons.NearbyName.Length>0?"TAKE  •  "+weapons.NearbyName:"";}
            }
        }
        private void ApplySafeArea()
        {
            lastSafe = Screen.safeArea;
            safeRoot.anchorMin = new Vector2(lastSafe.xMin/Screen.width,lastSafe.yMin/Screen.height);
            safeRoot.anchorMax = new Vector2(lastSafe.xMax/Screen.width,lastSafe.yMax/Screen.height);
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
            foreach(var control in controls)control.Apply();
        }
        private HudControl AddControl(string id,string text,float x,float y,float size)
        {
            var root = Panel(id,safeRoot,new Color(.07f,.1f,.12f,.38f));
            var image = root.GetComponent<Image>(); image.sprite = circle;
            var outline=root.gameObject.AddComponent<Outline>(); outline.effectColor=new Color(.8f,.87f,.84f,.8f);outline.effectDistance=Vector2.one*2;
            var control=root.gameObject.AddComponent<HudControl>();
            control.owner=this;control.placement=new ControlPlacement { id=id,x=x,y=y,scale=1 };control.defaultPosition=new Vector2(x,y);control.baseSize=size;
            var label=Label(id+" label",root,text,id=="fire"?24:17,TextAnchor.MiddleCenter);label.raycastTarget=false;
            label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;
            var glyph=TouchIconAtlas.Get(id);
            if(glyph!=null)
            {
                var icon=Panel("Icon",root,new Color(.91f,.9f,.88f,.95f));icon.GetComponent<Image>().sprite=glyph;icon.GetComponent<Image>().raycastTarget=false;
                Rect(icon,Vector2.one*.26f,Vector2.one*.74f,Vector2.zero,Vector2.zero);label.gameObject.SetActive(false);
                var iconEdge=icon.gameObject.AddComponent<Outline>();iconEdge.effectColor=new Color(0,0,0,.85f);iconEdge.effectDistance=Vector2.one;
            }
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
            root.gameObject.AddComponent<Button>().onClick.AddListener(()=>{UiHaptics.Pulse();action();});
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
            scaleSlider.onValueChanged.AddListener(value=> { if(selected!=null) { selected.placement.scale=value;selected.Apply();Select(selected); } });
            var opacityRoot=Panel("Opacity slider",root,new Color(.3f,.35f,.35f,1));Rect(opacityRoot,new Vector2(.07f,.1f),new Vector2(.6f,.25f),Vector2.zero,Vector2.zero);
            opacitySlider=opacityRoot.gameObject.AddComponent<Slider>();opacitySlider.minValue=.15f;opacitySlider.maxValue=1;
            var opacityHandle=Panel("Opacity handle",opacityRoot,new Color(.9f,.76f,.4f,1));Rect(opacityHandle,new Vector2(.5f,0),new Vector2(.5f,1),Vector2.zero,new Vector2(24,0));
            opacitySlider.handleRect=opacityHandle;opacitySlider.targetGraphic=opacityHandle.GetComponent<Image>();
            opacitySlider.onValueChanged.AddListener(value=>{if(selected!=null){selected.placement.opacity=value;selected.Apply();}});
            SmallButton(root,"RESET",new Vector2(.65f,.12f),new Vector2(.8f,.55f),ResetLayout);
            SmallButton(root,"SAVE",new Vector2(.82f,.12f),new Vector2(.98f,.55f),()=>SetEditing(false));
            editorPanel.SetActive(false);
        }
        private void SmallButton(Transform parent,string text,Vector2 min,Vector2 max,UnityEngine.Events.UnityAction callback)
        {
            var p=Panel(text,parent,new Color(.22f,.3f,.3f,1));Rect(p,min,max,Vector2.zero,Vector2.zero);
            var t=Label(text,p,text,18,TextAnchor.MiddleCenter);t.raycastTarget=false;t.rectTransform.anchorMin=Vector2.zero;t.rectTransform.anchorMax=Vector2.one;t.rectTransform.offsetMin=t.rectTransform.offsetMax=Vector2.zero;
            p.gameObject.AddComponent<Button>().onClick.AddListener(()=>{UiHaptics.Pulse();callback();});
        }
        private void CreateCredits()
        {
            var p=Panel("Credits",safeRoot,new Color(.035f,.05f,.065f,.98f));Rect(p,new Vector2(.1f,.1f),new Vector2(.9f,.9f),Vector2.zero,Vector2.zero);creditsPanel=p.gameObject;
            SmallButton(p,"CLOSE",new Vector2(.83f,.89f),new Vector2(.98f,.98f),()=>creditsPanel.SetActive(false));
            var viewport=Panel("Scroll area",p,Color.clear);Rect(viewport,new Vector2(.03f,.03f),new Vector2(.97f,.85f),Vector2.zero,Vector2.zero);
            viewport.gameObject.AddComponent<RectMask2D>();var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.viewport=viewport;
            var text=Label("Notices",viewport,"",18,TextAnchor.UpperLeft);text.rectTransform.anchorMin=new Vector2(0,1);text.rectTransform.anchorMax=Vector2.one;text.rectTransform.pivot=new Vector2(.5f,1);text.rectTransform.sizeDelta=new Vector2(0,2200);
            var main=Resources.Load<TextAsset>("THIRD_PARTY_NOTICES");var mosque=Resources.Load<TextAsset>("MOSQUE_NOTICE");text.text=(main!=null?main.text:"")+"\n\n"+(mosque!=null?mosque.text:"")+"\n\nHUD font: Roboto Condensed SemiBold (subset), SIL Open Font License. Notice bundled at Fonts/RobotoCondensed-OFL.";
            scroll.content=text.rectTransform;creditsPanel.SetActive(false);
        }
        private void CreateSettings()
        {
            var p=Panel("Sensitivity settings",safeRoot,new Color(.035f,.05f,.065f,.98f));Rect(p,new Vector2(.12f,.15f),new Vector2(.88f,.86f),Vector2.zero,Vector2.zero);settingsPanel=p.gameObject;
            var settings=GameplayInput.Instance!=null?GameplayInput.Instance.Settings:SensitivitySettings.Load();
            SettingSlider(p,"Base look",.82f,settings.baseSensitivity,.1f,3,v=>settings.baseSensitivity=v,settings);
            SettingSlider(p,"Horizontal",.68f,settings.horizontal,.1f,3,v=>settings.horizontal=v,settings);
            SettingSlider(p,"Vertical",.54f,settings.vertical,.1f,3,v=>settings.vertical=v,settings);
            SettingSlider(p,"ADS iron",.40f,settings.iron,.1f,2,v=>settings.iron=v,settings);
            SettingSlider(p,"ADS low zoom",.26f,settings.lowZoom,.1f,2,v=>settings.lowZoom=v,settings);
            SettingSlider(p,"ADS high zoom",.12f,settings.highZoom,.1f,2,v=>settings.highZoom=v,settings);
            SettingSlider(p,"World FOV",.005f,settings.worldFOV<0?GunplayConfig.Current.worldFOV:settings.worldFOV,GunplayConfig.Current.minFOV,GunplayConfig.Current.maxFOV,v=>settings.worldFOV=v,settings);
            SmallButton(p,"DROP WEAPON",new Vector2(.69f,.83f),new Vector2(.81f,.96f),()=>{settingsPanel.SetActive(false);weapons.DropActiveWeapon();});
            SmallButton(p,"VISUAL/AUDIO",new Vector2(.82f,.83f),new Vector2(.98f,.96f),()=>{settingsPanel.SetActive(false);visualPanel.SetActive(true);RefreshVisualTitle();for(int i=0;i<6;i++)if(PresentationAudio.Active!=null)gainSliders[i].SetValueWithoutNotify(PresentationAudio.Active.BusVolume(i));});
            SmallButton(p,"GYRO",new Vector2(.69f,.67f),new Vector2(.96f,.80f),()=>{settings.gyro=!settings.gyro;settings.Save();});
            SmallButton(p,"DAMAGE #",new Vector2(.69f,.50f),new Vector2(.96f,.63f),()=>{settings.damageNumbers=!settings.damageNumbers;settings.Save();});
            SmallButton(p,"CURVE",new Vector2(.69f,.33f),new Vector2(.96f,.46f),()=>{settings.curve=(SensitivitySettings.ResponseCurve)(((int)settings.curve+1)%3);settings.Save();});
            SmallButton(p,"SAVE / CLOSE",new Vector2(.69f,.1f),new Vector2(.96f,.25f),()=>{settings.Save();settingsPanel.SetActive(false);});
            settingsPanel.SetActive(false);CreateVisualSettings();
        }
        private void RefreshVisualTitle()
        {
            var r=PresentationRenderer.Active;if(visualTitle==null||r==null)return;
            visualTitle.text=r.Tier+"  /  "+r.Day+"\nDRS "+(r.DynamicResolution?"ON":"OFF")+" | GRAIN "+(PlayerPrefs.GetInt("fpp.visual.grain",1)!=0?"ON":"OFF")+" | BLUR "+(PlayerPrefs.GetInt("fpp.visual.blur",0)!=0?"ON":"OFF")+" | HAPTICS "+(UiHaptics.Enabled?"ON":"OFF")+" | STICK "+(FloatingJoystick?"FLOAT":"FIXED");
        }
        private void CreateVisualSettings()
        {
            var root=Panel("Graphics and audio mix",safeRoot,new Color(.035f,.05f,.065f,.98f));Rect(root,new Vector2(.1f,.1f),new Vector2(.9f,.9f),Vector2.zero,Vector2.zero);visualPanel=root.gameObject;
            visualTitle=Label("Graphics options",root,"GRAPHICS / AUDIO",20,TextAnchor.MiddleCenter);Rect(visualTitle.rectTransform,new Vector2(.02f,.87f),new Vector2(.98f,.99f),Vector2.zero,Vector2.zero);
            string[] options={"QUALITY","DAYLIGHT","GRAIN","MOTION BLUR","DRS"};
            for(int i=0;i<options.Length;i++)
            {
                int option=i;float x=.02f+i*.195f;
                SmallButton(root,options[i],new Vector2(x,.73f),new Vector2(x+.18f,.85f),()=>{var r=PresentationRenderer.Active;if(r==null)return;switch(option){case 0:r.SetTier((PresentationTier)(((int)r.Tier+1)%4));break;case 1:r.SetDay((DayPreset)(((int)r.Day+1)%3));break;case 2:r.ToggleGrain();break;case 3:r.ToggleBlur();break;default:r.ToggleDynamicResolution();break;}RefreshVisualTitle();});
            }
            string[] names={"MASTER","WEAPONS","FOLEY","AMBIENCE","UI","MUSIC STUB"};
            for(int i=0;i<names.Length;i++)
            {
                int bus=i;float y=.60f-i*.085f;var label=Label(names[i],root,names[i],17,TextAnchor.MiddleLeft);Rect(label.rectTransform,new Vector2(.03f,y),new Vector2(.24f,y+.065f),Vector2.zero,Vector2.zero);
                var track=Panel(names[i]+" gain",root,new Color(.2f,.27f,.28f));Rect(track,new Vector2(.27f,y+.015f),new Vector2(.94f,y+.055f),Vector2.zero,Vector2.zero);
                var slider=track.gameObject.AddComponent<Slider>();gainSliders[i]=slider;slider.minValue=0;slider.maxValue=1;slider.SetValueWithoutNotify(PresentationAudio.Active!=null?PresentationAudio.Active.BusVolume(bus):.5f);
                var handle=Panel("Gain handle",track,new Color(.9f,.76f,.4f));Rect(handle,new Vector2(.5f,0),new Vector2(.5f,1),Vector2.zero,new Vector2(20,0));slider.handleRect=handle;slider.targetGraphic=handle.GetComponent<Image>();slider.onValueChanged.AddListener(v=>{if(PresentationAudio.Active!=null)PresentationAudio.Active.SetBusVolume(bus,v);});
            }
            SmallButton(root,"HAPTICS",new Vector2(.02f,.03f),new Vector2(.3f,.12f),()=>{UiHaptics.Enabled=!UiHaptics.Enabled;RefreshVisualTitle();});
            SmallButton(root,"FLOATING STICK",new Vector2(.32f,.03f),new Vector2(.63f,.12f),()=>{FloatingJoystick=!FloatingJoystick;RefreshVisualTitle();});
            SmallButton(root,"SAVE / CLOSE",new Vector2(.65f,.03f),new Vector2(.97f,.12f),()=>{PlayerPrefs.Save();visualPanel.SetActive(false);});visualPanel.SetActive(false);
        }
        private void SettingSlider(Transform parent,string title,float y,float value,float min,float max,UnityEngine.Events.UnityAction<float> update,SensitivitySettings settings)
        {
            var label=Label(title,parent,title+": "+value.ToString("0.00"),16,TextAnchor.MiddleLeft);Rect(label.rectTransform,new Vector2(.02f,y),new Vector2(.25f,y+.1f),Vector2.zero,Vector2.zero);
            var root=Panel(title+" slider",parent,new Color(.3f,.35f,.35f,1));Rect(root,new Vector2(.27f,y+.025f),new Vector2(.63f,y+.075f),Vector2.zero,Vector2.zero);
            var slider=root.gameObject.AddComponent<Slider>();slider.minValue=min;slider.maxValue=max;slider.SetValueWithoutNotify(value);
            var handle=Panel("Handle",root,new Color(.9f,.76f,.4f));Rect(handle,new Vector2(.5f,0),new Vector2(.5f,1),Vector2.zero,new Vector2(20,0));slider.handleRect=handle;slider.targetGraphic=handle.GetComponent<Image>();slider.onValueChanged.AddListener(v=>{update(v);label.text=title+": "+v.ToString("0.00");settings.Save();});
        }
        public void Select(HudControl control)
        {
            selected=control;if(opacitySlider!=null)opacitySlider.SetValueWithoutNotify(control.placement.opacity);scaleSlider.SetValueWithoutNotify(control.placement.scale);editLabel.text=control.placement.id.ToUpperInvariant()+"  •  Drag to move  •  SIZE "+control.placement.scale.ToString("0.0");
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
            if(down&&id!="move")UiHaptics.Pulse();
            if(down && weapons!=null)GunplayEvents.Send(GunplayEventKind.UI,weapons.view.position,weapons.ActiveWeapon,id);
            if(id=="fire")FireHeld=down;
            if(id=="aim")AimHeld=down;
            if(id=="sprint")sprintHeld=down;if(id=="crouch")crouchHeld=down;
            if(down && id=="reload")pending.Reload=true;
            if(down && id=="jump")pending.Jump=true;
            if(down && id=="crouch")pending.CrouchPressed=true;
            if(down && id=="mount")pending.Mount=true;
            if(down && id=="prone")pending.Prone=true;
            if(down && id=="pickup")pending.Pickup=true;
            if(down && id=="sidearm")pending.Swap=true;
        }
        public InputFrame ConsumeInput()
        {
            var frame=pending;pending=default(InputFrame);frame.Move=Move;frame.Fire=FireHeld;frame.ADS=AimHeld;frame.Sprint=sprintHeld;frame.CrouchHeld=crouchHeld;return frame;
        }
        public bool BlocksLook(Vector2 position)
        {
            if(Modal)return true;
            foreach(var control in controls)
                if(control.gameObject.activeSelf && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)control.transform,position,null))return true;
            return position.y>Screen.safeArea.yMax-Screen.height*.1f && position.x>Screen.width*.7f;
        }
        public void SetMove(Vector2 value) { Move=Modal?Vector2.zero:value; }
        public void Release(string id)
        {
            if(id=="sprint")sprintHeld=false;if(id=="crouch")crouchHeld=false;
            if(id=="fire")FireHeld=false;if(id=="aim")AimHeld=false;if(id=="move")Move=Vector2.zero;
        }
        private void ShowCredits() { FireHeld=AimHeld=false;Move=Vector2.zero;creditsPanel.SetActive(true); }
        private void ResetLayout()
        {
            foreach(var c in controls) { c.placement.x=c.defaultPosition.x;c.placement.y=c.defaultPosition.y;c.placement.scale=1;c.placement.opacity=1;c.Apply(); }
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
                var json=PlayerPrefs.GetString(PrefKey,"{}");bool opacitySaved=json.Contains("\"opacity\"");var layout=JsonUtility.FromJson<ControlLayout>(json);
                if(layout==null || layout.controls==null)return;
                foreach(var saved in layout.controls)
                foreach(var c in controls) if(c.placement.id==saved.id)
                { c.placement.x=Mathf.Clamp(saved.x,.07f,.93f);c.placement.y=Mathf.Clamp(saved.y,.09f,.85f);c.placement.scale=Mathf.Clamp(saved.scale,.6f,1.6f);c.placement.opacity=opacitySaved?Mathf.Clamp(saved.opacity,.15f,1):1;c.Apply(); }
            }
            catch(Exception) { PlayerPrefs.DeleteKey(PrefKey); }
        }
        private void OnApplicationFocus(bool focused)
        {
            if(!focused) { FireHeld=AimHeld=sprintHeld=crouchHeld=false;pending=default(InputFrame);Move=Vector2.zero;foreach(var c in controls)c.ResetInput(); }
        }
        private void OnDestroy() { if(Instance==this)Instance=null; }
        private Text Label(string name,Transform parent,string content,int size,TextAnchor align)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);
            var t=go.GetComponent<Text>();t.font=font;t.text=content;t.fontSize=size;t.color=new Color(.93f,.94f,.89f);t.alignment=align;t.raycastTarget=false;
            var shadow=go.AddComponent<Outline>();shadow.effectColor=new Color(0,0,0,.85f);shadow.effectDistance=new Vector2(1,-1);
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
