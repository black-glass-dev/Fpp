#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Fpp
{
    public static class RenderEvidence
    {
        // The review-only UI camera renders after the FPV grade, like the runtime
        // ScreenSpaceOverlay HUD. Never tone-map the HUD just to capture it.
        public static void Into(Camera world,Camera overlay,RenderTexture destination,Canvas canvas=null)
        {
            if(GraphicsSettings.currentRenderPipeline==null)
            {
                world.targetTexture=destination;world.Render();
                if(overlay!=null){overlay.targetTexture=destination;overlay.Render();overlay.targetTexture=null;}
                world.targetTexture=null;return;
            }
            if(canvas==null||overlay==null)
            {RenderPipeline.SubmitRenderRequest(world,new RenderPipeline.StandardRequest{destination=destination});return;}
            const int uiLayer=30;int worldMask=world.cullingMask,overlayMask=overlay.cullingMask;
            var mode=canvas.renderMode;var previousCamera=canvas.worldCamera;float plane=canvas.planeDistance;
            var transforms=canvas.GetComponentsInChildren<Transform>(true);var layers=new int[transforms.Length];
            var auxiliary=new GameObject("Evidence-only ungraded UI camera"){hideFlags=HideFlags.HideAndDontSave};
            var camera=auxiliary.AddComponent<Camera>();camera.CopyFrom(overlay);camera.transform.SetPositionAndRotation(overlay.transform.position,overlay.transform.rotation);camera.cullingMask=1<<uiLayer;camera.targetTexture=destination;camera.enabled=true;
            var data=camera.GetUniversalAdditionalCameraData();data.renderType=CameraRenderType.Overlay;data.renderPostProcessing=false;
            var serialized=new UnityEditor.SerializedObject(overlay.GetUniversalAdditionalCameraData());data.SetRenderer(serialized.FindProperty("m_RendererIndex").intValue);
            var stack=world.GetUniversalAdditionalCameraData().cameraStack;
            for(int i=0;i<transforms.Length;i++){layers[i]=transforms[i].gameObject.layer;transforms[i].gameObject.layer=uiLayer;}
            try
            {
                world.cullingMask&=~(1<<uiLayer);overlay.cullingMask&=~(1<<uiLayer);stack.Add(camera);
                canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.08f;Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(world,new RenderPipeline.StandardRequest{destination=destination});
            }
            finally
            {
                stack.Remove(camera);world.cullingMask=worldMask;overlay.cullingMask=overlayMask;canvas.renderMode=mode;canvas.worldCamera=previousCamera;canvas.planeDistance=plane;
                for(int i=0;i<transforms.Length;i++)transforms[i].gameObject.layer=layers[i];Object.DestroyImmediate(auxiliary);
            }
        }
    }
}
#endif
