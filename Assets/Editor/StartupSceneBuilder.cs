using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
namespace Fpp.Editor
{
    public static class StartupSceneBuilder
    {
        public const string Path="Assets/Scenes/Startup.unity";
        public static void Create(string worldPath)
        {
            Directory.CreateDirectory("Assets/Scenes");var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var owner=new GameObject("Startup checkpoint and menu");var gate=owner.AddComponent<StartupGate>();
            var camera=new GameObject("Startup camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.04f,.06f);camera.cullingMask=0;camera.allowHDR=false;camera.allowMSAA=false;camera.GetUniversalAdditionalCameraData().SetRenderer(2);gate.menuCamera=camera;
            var canvas=new GameObject("Startup menu",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;gate.menu=canvas;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);
            var events=new GameObject("Menu input",typeof(UnityEngine.EventSystems.EventSystem),typeof(UnityEngine.EventSystems.StandaloneInputModule));
            Text Label(string name,string text,float y,float height)
            {
                var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(canvas.transform,false);var t=go.GetComponent<Text>();t.font=Resources.Load<Font>("Fonts/Hud");t.text=text;t.fontSize=25;t.alignment=TextAnchor.MiddleCenter;t.color=Color.white;t.raycastTarget=false;
                var rect=go.GetComponent<RectTransform>();rect.anchorMin=new Vector2(.1f,y);rect.anchorMax=new Vector2(.9f,y+height);rect.offsetMin=rect.offsetMax=Vector2.zero;return t;
            }
            Label("Title","FPP",.76f,.16f);gate.status=Label("Startup status","Ready",.36f,.25f);
            void Button(string title,float y,UnityEngine.Events.UnityAction action)
            {
                var go=new GameObject(title,typeof(RectTransform),typeof(Image),typeof(UnityEngine.UI.Button));go.transform.SetParent(canvas.transform,false);go.GetComponent<Image>().color=new Color(.12f,.25f,.3f);
                var r=go.GetComponent<RectTransform>();r.anchorMin=new Vector2(.25f,y);r.anchorMax=new Vector2(.75f,y+.1f);r.offsetMin=r.offsetMax=Vector2.zero;
                var text=Label(title+" label",title,0,1);text.transform.SetParent(go.transform,false);var tr=text.rectTransform;tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=tr.offsetMax=Vector2.zero;
                go.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(action);
                // Serialized persistent listeners survive scene serialization/build.
                UnityEditor.Events.UnityEventTools.AddPersistentListener(go.GetComponent<UnityEngine.UI.Button>().onClick,action);
                go.GetComponent<UnityEngine.UI.Button>().onClick.RemoveListener(action);
            }
            Button("START GAME",.22f,gate.Begin);Button("COPY STARTUP REPORT",.08f,gate.CopyReport);
            EditorSceneManager.SaveScene(scene,Path);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Path,true),new EditorBuildSettingsScene(worldPath,true)};
        }
    }
}
