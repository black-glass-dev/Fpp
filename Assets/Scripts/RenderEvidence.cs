#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Fpp
{
    public static class RenderEvidence
    {
        // Capture the actual game camera stack and actual Canvas independently.
        // This avoids mutating the live URP stack/target during Canvas layout.
        public static void Into(Camera world,Camera overlay,RenderTexture destination,Canvas canvas=null)
        {
            if(GraphicsSettings.currentRenderPipeline==null)
            {
                var previous=world.targetTexture;var previousOverlay=overlay!=null?overlay.targetTexture:null;
                try{world.targetTexture=destination;world.Render();if(overlay!=null){overlay.targetTexture=destination;overlay.Render();}}
                finally{world.targetTexture=previous;if(overlay!=null)overlay.targetTexture=previousOverlay;}
                return;
            }
            bool canvasEnabled=canvas!=null&&canvas.enabled;
            try
            {
                if(canvas!=null)canvas.enabled=false;
                RenderPipeline.SubmitRenderRequest(world,new RenderPipeline.StandardRequest{destination=destination});
            }
            finally { if(canvas!=null)canvas.enabled=canvasEnabled; }
            if(canvas==null)return;
            const int uiLayer=30;var mode=canvas.renderMode;var previousCamera=canvas.worldCamera;float plane=canvas.planeDistance;
            var transforms=canvas.GetComponentsInChildren<Transform>(true);var layers=new int[transforms.Length];
            var auxiliary=new GameObject("Evidence-only actual Canvas camera"){hideFlags=HideFlags.HideAndDontSave};
            var camera=auxiliary.AddComponent<Camera>();
            if(overlay!=null){camera.CopyFrom(overlay);camera.transform.SetPositionAndRotation(overlay.transform.position,overlay.transform.rotation);}
            var ui=RenderTexture.GetTemporary(destination.width,destination.height,24,RenderTextureFormat.ARGB32);
            camera.cullingMask=1<<uiLayer;camera.targetTexture=ui;camera.enabled=false;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.allowHDR=false;camera.allowMSAA=false;
            camera.nearClipPlane=.01f;camera.farClipPlane=5;
            var data=camera.GetUniversalAdditionalCameraData();data.renderType=CameraRenderType.Base;data.renderPostProcessing=false;data.antialiasing=AntialiasingMode.None;
            Material blend=null;var oldActive=RenderTexture.active;bool oldSrgb=GL.sRGBWrite;
            try
            {
                for(int i=0;i<transforms.Length;i++){layers[i]=transforms[i].gameObject.layer;transforms[i].gameObject.layer=uiLayer;}
                canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.08f;Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=ui});
                var shader=Shader.Find("Hidden/FPP/Actual Canvas Composite");if(shader==null)throw new System.InvalidOperationException("Actual Canvas composite shader missing");
                blend=new Material(shader){hideFlags=HideFlags.HideAndDontSave};
                GL.sRGBWrite=QualitySettings.activeColorSpace==ColorSpace.Linear;Graphics.Blit(ui,destination,blend);
            }
            finally
            {
                GL.sRGBWrite=oldSrgb;RenderTexture.active=oldActive;canvas.renderMode=mode;canvas.worldCamera=previousCamera;canvas.planeDistance=plane;
                for(int i=0;i<transforms.Length;i++)transforms[i].gameObject.layer=layers[i];
                camera.targetTexture=null;Object.DestroyImmediate(auxiliary);if(blend!=null)Object.DestroyImmediate(blend);RenderTexture.ReleaseTemporary(ui);Canvas.ForceUpdateCanvases();
            }
        }
    }
}
#endif
