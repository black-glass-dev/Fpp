using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace Fpp
{
    // Tiny first scene, no world/model references. A visible start/retry/report screen
    // replaces automatic heavy-world loading and preserves the last managed checkpoint.
    public sealed class StartupGate : MonoBehaviour
    {
        public Canvas menu;public Text status;public Camera menuCamera;
        private string errorPath,checkpointPath;
        private string previous="No prior startup checkpoint",current;private bool loading,worldActive;
        private readonly object sync=new object();
        private string Checkpoint=>checkpointPath;
        private void Awake()
        {
            checkpointPath=Path.Combine(Application.persistentDataPath,"startup-checkpoint.txt");errorPath=Path.Combine(Application.persistentDataPath,"startup-errors.txt");
            Application.targetFrameRate=30;DontDestroyOnLoad(gameObject);
            try{if(File.Exists(Checkpoint))previous=File.ReadAllText(Checkpoint);}catch(IOException){}
            Application.logMessageReceivedThreaded+=OnLog;Application.lowMemory+=LowMemory;
            SceneManager.sceneLoaded+=Loaded;RenderPipelineManager.endFrameRendering+=Rendered;
            Record("boot-menu");
            if(status!=null)status.text="Ready. Tap START GAME.\nPrevious checkpoint: "+previous;
        }
        private void Record(string stage)
        {
            lock(sync)
            {
                current=DateTime.UtcNow.ToString("O")+"; stage="+stage+"; device="+SystemInfo.deviceModel+"; GPU="+SystemInfo.graphicsDeviceName+"; API="+SystemInfo.graphicsDeviceType+"; build="+Application.buildGUID;
                try{File.WriteAllText(Checkpoint,current);}catch(IOException){}
            }
        }
        private void OnLog(string message,string stack,LogType type)
        {
            if(type!=LogType.Exception&&type!=LogType.Error&&type!=LogType.Assert)return;
            lock(sync){try{var path=errorPath;if(File.Exists(path)&&new FileInfo(path).Length>65536)File.WriteAllText(path,"");File.AppendAllText(path,message+"\n"+stack+"\n");}catch(IOException){}}
        }
        private void LowMemory(){Record(worldActive?"low-memory-world":"low-memory-loading");}
        public void Begin()
        {
            if(loading)return;
            if(Application.isMobilePlatform&&(!SystemInfo.SupportsTextureFormat(TextureFormat.ASTC_4x4)||!SystemInfo.SupportsTextureFormat(TextureFormat.ASTC_6x6)))
            {status.text="This build requires hardware ASTC texture support. World not loaded. COPY STARTUP REPORT records this device.";Record("unsupported-ASTC");return;}
            loading=true;status.text="Loading world… Please wait.";Record("loading-world");
            SceneManager.LoadSceneAsync("CottageTest",LoadSceneMode.Single);
        }
        public void CopyReport()
        {
            string errors="";try{var path=errorPath;if(File.Exists(path))errors=File.ReadAllText(path);}catch(IOException){}
            lock(sync)GUIUtility.systemCopyBuffer="FPP startup report\nPrevious: "+previous+"\nCurrent: "+current+"\nManaged errors: "+errors+"\nCheckpoints do not diagnose a native crash or prove a completed GPU frame.";
            if(status!=null)status.text="Startup report copied.\n"+previous;
        }
        private void Loaded(Scene scene,LoadSceneMode mode)
        {
            if(scene.name!="CottageTest")return;worldActive=true;Record("world-activated");
            if(menu!=null)Destroy(menu.gameObject);if(menuCamera!=null)Destroy(menuCamera.gameObject);
        }
        private void Rendered(ScriptableRenderContext context,Camera[] cameras)
        {if(!worldActive)return;Record("world-render-submitted");RenderPipelineManager.endFrameRendering-=Rendered;}
        private void OnDestroy(){Application.logMessageReceivedThreaded-=OnLog;Application.lowMemory-=LowMemory;SceneManager.sceneLoaded-=Loaded;RenderPipelineManager.endFrameRendering-=Rendered;}
    }
}
