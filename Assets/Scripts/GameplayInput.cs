using System;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Fpp
{
    [Serializable] public sealed class SensitivitySettings
    {
        public float horizontal=1, vertical=1, baseSensitivity=1;
        public float iron=1, lowZoom=.85f, highZoom=.6f;
        public float worldFOV=-1;
        public bool gyro;
        public ResponseCurve curve=ResponseCurve.Linear;
        public bool damageNumbers;
        public enum ResponseCurve { Linear, Classic, Dynamic }
        public static SensitivitySettings Load(){try{return JsonUtility.FromJson<SensitivitySettings>(PlayerPrefs.GetString("fpp.sensitivity","{}"))??new SensitivitySettings();}catch{return new SensitivitySettings();}}
        public void Save(){PlayerPrefs.SetString("fpp.sensitivity",JsonUtility.ToJson(this));PlayerPrefs.Save();}
        public float Zoom(float fov)=>fov>=45?iron:fov>=30?lowZoom:highZoom;
    }
    public struct InputFrame
    {
        public Vector2 Move, Look;
        public bool Fire, FirePressed, ADS, Reload, Swap, Pickup, Jump, CrouchPressed, CrouchHeld, Sprint, Tactical, Prone, Mount;
    }
    public interface IInputProvider { InputFrame Poll(GunplayConfig config); void Reset(); }
    public sealed class KeyboardMouseInputProvider : IInputProvider
    {
        public InputFrame Poll(GunplayConfig c)
        {
            bool ui=EventSystem.current!=null && EventSystem.current.IsPointerOverGameObject();
            return new InputFrame {Move=Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical")),1),Look=Input.GetMouseButton(1)?new Vector2(Input.GetAxis("Mouse X"),Input.GetAxis("Mouse Y"))*c.mouseSensitivity:Vector2.zero,
            Fire=!ui&&Input.GetMouseButton(0),FirePressed=!ui&&Input.GetMouseButtonDown(0),ADS=Input.GetMouseButton(1),Reload=Input.GetKeyDown(KeyCode.R),Swap=Input.GetKeyDown(KeyCode.Alpha1),Pickup=Input.GetKeyDown(KeyCode.E),Jump=Input.GetKeyDown(KeyCode.Space),CrouchPressed=Input.GetKeyDown(KeyCode.C),CrouchHeld=Input.GetKey(KeyCode.C),Sprint=Input.GetKey(KeyCode.LeftShift),Tactical=Input.GetKey(KeyCode.LeftAlt),Prone=Input.GetKeyDown(KeyCode.Z),Mount=Input.GetKeyDown(KeyCode.F)};
        }
        public void Reset(){}
    }
    public sealed class TouchInputProvider : IInputProvider
    {
        private int lookFinger=-1; private bool previousFire,forward;private float forwardTap=-100;
        public InputFrame Poll(GunplayConfig c)
        {
            var hud=MobileHud.Instance;var frame=hud!=null?hud.ConsumeInput():default(InputFrame);
            frame.FirePressed=frame.Fire&&!previousFire;previousFire=frame.Fire;
            float magnitude=frame.Move.magnitude;
            frame.Move=magnitude<=c.joystickDeadZone?Vector2.zero:frame.Move.normalized*Mathf.Pow(Mathf.InverseLerp(c.joystickDeadZone,1,magnitude),c.joystickExponent);
            bool nowForward=frame.Move.y>c.forwardThreshold;
            if(nowForward&&!forward){if(Time.unscaledTime-forwardTap<c.doubleTapWindow)frame.Tactical=true;forwardTap=Time.unscaledTime;}forward=nowForward;
            for(int i=0;i<Input.touchCount;i++)
            {
                var t=Input.GetTouch(i);
                if(t.phase==TouchPhase.Began && t.position.x>=Screen.width*.5f && lookFinger<0 && (hud==null||!hud.BlocksLook(t.position)) && (EventSystem.current==null||!EventSystem.current.IsPointerOverGameObject(t.fingerId)))lookFinger=t.fingerId;
                if(t.fingerId!=lookFinger)continue;
                if(t.phase==TouchPhase.Ended||t.phase==TouchPhase.Canceled)lookFinger=-1;
                else frame.Look=t.deltaPosition*(c.lookSensitivity/Screen.height);
            }
            return frame;
        }
        public void Reset(){lookFinger=-1;previousFire=false;forward=false;forwardTap=-100;}
    }
    [DefaultExecutionOrder(50)]
    public sealed class GameplayInput : MonoBehaviour
    {
        public static GameplayInput Instance {get;private set;}
        public InputFrame Frame {get;private set;}
        public IInputProvider OverrideProvider;
        public SensitivitySettings Settings {get;private set;}
        private bool uiFire;
        private readonly IInputProvider touch=new TouchInputProvider(),desktop=new KeyboardMouseInputProvider();
        private void Awake(){Instance=this;Settings=SensitivitySettings.Load();if(Settings.worldFOV<GunplayConfig.Current.minFOV)Settings.worldFOV=GunplayConfig.Current.worldFOV;}
        private void Update()
        {
            if(WeaponController.MenuOpen){Frame=default(InputFrame);touch.Reset();desktop.Reset();return;}
            Frame=(OverrideProvider??(Input.touchSupported&&Application.isMobilePlatform?touch:desktop)).Poll(GunplayConfig.Current);
            if(OverrideProvider==null&&!Application.isMobilePlatform&&MobileHud.Instance!=null)
            {
                var ui=MobileHud.Instance.ConsumeInput();var f=Frame;f.Move=Vector2.ClampMagnitude(f.Move+ui.Move,1);f.Fire|=ui.Fire;f.FirePressed|=ui.Fire&&!uiFire;uiFire=ui.Fire;f.ADS|=ui.ADS;f.Reload|=ui.Reload;f.Swap|=ui.Swap;f.Pickup|=ui.Pickup;f.Jump|=ui.Jump;f.CrouchPressed|=ui.CrouchPressed;f.CrouchHeld|=ui.CrouchHeld;f.Sprint|=ui.Sprint;f.Prone|=ui.Prone;f.Mount|=ui.Mount;Frame=f;
            }
            if(SystemInfo.supportsGyroscope && Input.gyro.enabled!=Settings.gyro)Input.gyro.enabled=Settings.gyro;
            if(Settings.gyro&&SystemInfo.supportsGyroscope){var f=Frame;var rate=Input.gyro.rotationRateUnbiased;f.Look+=new Vector2(-rate.y,rate.x)*GunplayConfig.Current.gyroSensitivity*Time.unscaledDeltaTime*Mathf.Rad2Deg;Frame=f;}
        }
        public Vector2 Look(float ads,float fov)
        {
            var v=Frame.Look;float m=v.magnitude;
            if(m>0 && Settings.curve!=SensitivitySettings.ResponseCurve.Linear){float power=Settings.curve==SensitivitySettings.ResponseCurve.Classic?1.4f:.8f;v=v.normalized*Mathf.Pow(m,power);}
            return Vector2.Scale(v,new Vector2(Settings.horizontal*GunplayConfig.Current.horizontalSensitivity,Settings.vertical*GunplayConfig.Current.verticalSensitivity))*Settings.baseSensitivity*Mathf.Lerp(1,GunplayConfig.Current.adsSensitivity*Settings.Zoom(fov),ads);
        }
        private void OnApplicationFocus(bool focus){Frame=default(InputFrame);touch.Reset();desktop.Reset();}
        private void OnDestroy(){if(Instance==this)Instance=null;}
    }
}
