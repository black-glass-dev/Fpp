#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Rendering;
namespace Fpp
{
    public static class RenderEvidence
    {
        // A StandardRequest renders the URP camera stack; Camera.Render is Built-in only.
        public static void Into(Camera world,Camera overlay,RenderTexture destination)
        {
            if(GraphicsSettings.currentRenderPipeline!=null)
                RenderPipeline.SubmitRenderRequest(world,new RenderPipeline.StandardRequest{destination=destination});
            else
            {
                world.targetTexture=destination;world.Render();
                if(overlay!=null){overlay.targetTexture=destination;overlay.Render();overlay.targetTexture=null;}
                world.targetTexture=null;
            }
        }
    }
}
#endif
