using UnityEngine;
namespace Fpp
{
    // Adaptive resolution is a fallback, not a claim of measured mid-range device performance.
    public sealed class MobileBootstrap : MonoBehaviour
    {
        private int frames,nativeWidth,nativeHeight;
        private float elapsed,scale=1;
        private void Awake()
        {
            if(GetComponent<PresentationAudio>()==null)gameObject.AddComponent<PresentationAudio>();
            if(GetComponent<PresentationRenderer>()==null)gameObject.AddComponent<PresentationRenderer>();
            var c=GunplayConfig.Current;Application.targetFrameRate=c.targetFPS;
            QualitySettings.vSyncCount=0;QualitySettings.antiAliasing=0;
            QualitySettings.shadows=ShadowQuality.HardOnly;QualitySettings.shadowDistance=28;QualitySettings.shadowCascades=0;QualitySettings.shadowResolution=ShadowResolution.Medium;
            Screen.sleepTimeout=SleepTimeout.NeverSleep;
            nativeWidth=Screen.width;nativeHeight=Screen.height;
            if(Application.isMobilePlatform)ApplyResolution(c);
        }
        private void Update()
        {
            var c=GunplayConfig.Current;if(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline!=null||!Application.isMobilePlatform||!c.dynamicResolution)return;
            elapsed+=Time.unscaledDeltaTime;if(++frames<c.resolutionSampleFrames)return;
            float target=1f/c.targetFPS,average=elapsed/frames;elapsed=0;frames=0;
            float next=scale;
            if(average>target*c.resolutionHysteresis)next-=c.resolutionStep;
            else if(average<target/c.resolutionHysteresis)next+=c.resolutionStep;
            next=Mathf.Clamp(next,c.minResolutionScale,c.maxResolutionScale);
            if(next!=scale){scale=next;ApplyResolution(c);}
        }
        private void ApplyResolution(GunplayConfig c)
        {
            float baseScale=Mathf.Min(1,Mathf.Min(c.baseWidth/(float)nativeWidth,c.baseHeight/(float)nativeHeight));
            Screen.SetResolution(Mathf.RoundToInt(nativeWidth*baseScale*scale),Mathf.RoundToInt(nativeHeight*baseScale*scale),true);
        }
    }
}
