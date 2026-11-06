using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Fpp.Editor
{
    // Exercises the actual generated HUD and pointer handlers in Unity, not source-text mocks.
    // This is an editor smoke test, not a substitute for physical Android touch testing.
    public static class HudAudit
    {
        private static MobileHud hud;
        private static Canvas canvas;
        private static bool hadEvents;
        private const string Key="fpp.controls.v2";
        private static void Invoke(string name) => typeof(MobileHud).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(hud,null);
        private static void Require(bool condition,string message) { if(!condition)throw new Exception("HUD smoke test: "+message); }
        public static Canvas Prepare()
        {
            bool hadPref=PlayerPrefs.HasKey(Key);string oldPref=PlayerPrefs.GetString(Key,"");
            hadEvents=UnityEngine.Object.FindObjectOfType<EventSystem>()!=null;
            try
            {
                PlayerPrefs.DeleteKey(Key);
                hud=UnityEngine.Object.FindObjectOfType<MobileHud>();Require(hud!=null,"component missing");
                Invoke("Awake");Invoke("Start");
                canvas=GameObject.Find("Mobile HUD").GetComponent<Canvas>();Canvas.ForceUpdateCanvases();
                Require(GameObject.Find("Reticle").GetComponentsInChildren<Image>().Length==6,"font-independent reticle marks");
                var controls=canvas.GetComponentsInChildren<HudControl>(true);
                Require(controls.Length==6,"expected six action/movement controls");
                foreach(string id in new[]{"move","fire","reload","pickup","aim","sidearm"})
                    Require(controls.Count(c=>c.placement.id==id)==1,"missing or duplicate "+id);
                foreach(var text in canvas.GetComponentsInChildren<Text>(true))Require(text.font!=null,"font missing: "+text.name);
                var fire=controls.Single(c=>c.placement.id=="fire");var aim=controls.Single(c=>c.placement.id=="aim");
                var events=UnityEngine.Object.FindObjectOfType<EventSystem>();
                var pointer=new PointerEventData(events) { pointerId=7 };
                fire.OnPointerDown(pointer);Require(hud.FireHeld,"fire pointer down");
                fire.OnPointerUp(pointer);Require(!hud.FireHeld,"fire pointer release");
                aim.OnPointerDown(pointer);Require(hud.AimHeld,"ADS pointer down");
                aim.OnPointerUp(pointer);Require(!hud.AimHeld,"ADS pointer release");
                hud.SetMove(Vector2.right);Require(hud.Move.x==1,"movement input");hud.Release("move");
                hud.SetEditing(true);Require(hud.Modal,"layout must block gameplay");hud.Select(fire);
                var slider=canvas.GetComponentInChildren<Slider>(true);slider.value=1.4f;
                fire.placement.x=.66f;fire.placement.y=.28f;fire.Apply();
                hud.SetEditing(false);Require(PlayerPrefs.GetString(Key,"").Contains("fire"),"layout save");
                fire.placement.scale=1;fire.placement.x=.8f;Invoke("LoadLayout");
                Require(Mathf.Abs(fire.placement.scale-1.4f)<.001f && Mathf.Abs(fire.placement.x-.66f)<.001f,"layout reload/scale");
                hud.SetEditing(true);Invoke("ResetLayout");
                foreach(var control in controls)Require(control.placement.scale==1 && Mathf.Abs(control.placement.x-control.defaultPosition.x)<.001f,"layout reset");
                hud.SetEditing(false);
                Debug.Log("FPP HUD smoke test passed: six controls, fonts, pointer fire/ADS, move, modal blocking, size slider, save/load/reset");
                return canvas;
            }
            catch { Cleanup();throw; }
            finally
            {
                if(hadPref)PlayerPrefs.SetString(Key,oldPref);else PlayerPrefs.DeleteKey(Key);
                PlayerPrefs.Save();
            }
        }
        public static void Cleanup()
        {
            if(canvas!=null)UnityEngine.Object.DestroyImmediate(canvas.gameObject);
            else { var root=GameObject.Find("Mobile HUD");if(root!=null)UnityEngine.Object.DestroyImmediate(root); }
            if(!hadEvents){var events=GameObject.Find("Touch events");if(events!=null)UnityEngine.Object.DestroyImmediate(events);}
            if(hud!=null)Invoke("OnDestroy");hud=null;canvas=null;
        }
    }
}
