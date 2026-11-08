using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Fpp
{
    public enum PresentationTier { Low, Medium, High, PCUltra }
    public enum DayPreset { LateAfternoonGolden, Noon, Dusk }
    // Runtime clones: DRS and user choices never mutate the imported pipeline assets.
    public sealed class PresentationRenderer : MonoBehaviour
    {
        public static PresentationRenderer Active {get;private set;}
        public PresentationTier Tier {get;private set;}
        public DayPreset Day {get;private set;}
        public bool DynamicResolution {get;private set;}
        public float ResolutionScale=>pipeline!=null?pipeline.renderScale:1;
        private UniversalRenderPipelineAsset pipeline;
        private PresentationVisualSettings visual;private VolumeProfile profile;private ColorAdjustments grade;private Bloom bloom;
        private FilmGrain grain;private MotionBlur blur;private float exposureVelocity;
        private int slowFrames,fastFrames;private Light sun;
        public static void Stack(Camera world,Camera overlay)
        {
            var baseData=world.GetUniversalAdditionalCameraData();baseData.renderType=CameraRenderType.Base;
            world.allowHDR=true;world.allowMSAA=true;baseData.renderPostProcessing=overlay==null;baseData.requiresDepthTexture=true;
            if(overlay!=null)
            {
                overlay.allowHDR=true;overlay.allowMSAA=true;var data=overlay.GetUniversalAdditionalCameraData();data.renderType=CameraRenderType.Overlay;data.renderPostProcessing=true; // URP14 clearDepth is read-only, defaults true.
                if(!baseData.cameraStack.Contains(overlay))baseData.cameraStack.Add(overlay);
            }
        }
        private void Start()
        {
            Active=this;sun=RenderSettings.sun;visual=PresentationVisualSettings.Current;DynamicResolution=PlayerPrefs.GetInt("fpp.visual.drs",GunplayConfig.Current.dynamicResolution?1:0)!=0;
            var authored=GameObject.Find("Authored presentation volume");if(authored!=null)authored.SetActive(false);
            var volume=new GameObject("Presentation grade (HUD remains ScreenSpaceOverlay)").AddComponent<Volume>();volume.transform.SetParent(transform,false);volume.isGlobal=true;volume.priority=10;
            profile=ScriptableObject.CreateInstance<VolumeProfile>();volume.sharedProfile=profile;
            profile.Add<Tonemapping>().mode.Override(TonemappingMode.ACES);
            bloom=profile.Add<Bloom>();bloom.threshold.Override(visual.bloomThreshold);bloom.intensity.Override(visual.bloomIntensity);bloom.scatter.Override(visual.bloomScatter);
            profile.Add<Vignette>().intensity.Override(visual.vignette);profile.Add<ChromaticAberration>().intensity.Override(visual.chromaticAberration);
            grade=profile.Add<ColorAdjustments>();grade.saturation.Override(visual.saturation);grade.contrast.Override(visual.contrast);grade.postExposure.Override(visual.exteriorEV);
            var split=profile.Add<SplitToning>();split.shadows.Override(visual.shadowTint);split.highlights.Override(visual.highlightTint);
            grain=profile.Add<FilmGrain>();grain.intensity.Override(visual.grain);
            blur=profile.Add<MotionBlur>();blur.intensity.Override(visual.motionBlur);
            SetTier((PresentationTier)Mathf.Clamp(PlayerPrefs.GetInt("fpp.visual.tier",2),0,3));
            SetDay((DayPreset)Mathf.Clamp(PlayerPrefs.GetInt("fpp.visual.day",0),0,2));
            var owner=GetComponent<WeaponController>();if(owner!=null)Stack(owner.view.GetComponent<Camera>(),owner.weaponCamera);
        }
        public void SetTier(PresentationTier tier)
        {
            var source=Resources.Load<UniversalRenderPipelineAsset>("Rendering/"+tier);
            if(source==null){Debug.LogError("Generate the URP scene and tier assets before running");return;}
            var old=pipeline;pipeline=Instantiate(source);Tier=tier;
            GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
            if(old!=null)Destroy(old);
            pipeline.upscalingFilter=UpscalingFilterSelection.FSR;
            // URP14 TAA explicitly does not support camera stacks. PC uses documented SMAA fallback.
            // Forward+ requires a capable API; GLES3 gets the Forward renderer as an explicit fallback.
            bool plus=SystemInfo.supportsComputeShaders&&SystemInfo.graphicsDeviceType!=GraphicsDeviceType.OpenGLES3;
            var owner=GetComponent<WeaponController>();if(owner!=null)
            {
                int renderer=(tier>=PresentationTier.High&&plus)?1:0;
                owner.view.GetComponent<Camera>().GetUniversalAdditionalCameraData().SetRenderer(renderer);owner.weaponCamera.GetUniversalAdditionalCameraData().SetRenderer(renderer);
                owner.weaponCamera.GetUniversalAdditionalCameraData().antialiasing=tier==PresentationTier.PCUltra?AntialiasingMode.SubpixelMorphologicalAntiAliasing:AntialiasingMode.None;
            }
            if(grain!=null)grain.active=tier>=PresentationTier.High&&PlayerPrefs.GetInt("fpp.visual.grain",1)!=0;
            if(blur!=null)blur.active=tier>=PresentationTier.High&&PlayerPrefs.GetInt("fpp.visual.blur",0)!=0;
            if(bloom!=null)bloom.active=tier!=PresentationTier.Low;
            PlayerPrefs.SetInt("fpp.visual.tier",(int)tier);slowFrames=fastFrames=0;
        }
        public void ToggleDynamicResolution(){DynamicResolution=!DynamicResolution;PlayerPrefs.SetInt("fpp.visual.drs",DynamicResolution?1:0);if(!DynamicResolution&&pipeline!=null)pipeline.renderScale=visual.maxScale;slowFrames=fastFrames=0;}
        public void ToggleGrain(){PlayerPrefs.SetInt("fpp.visual.grain",PlayerPrefs.GetInt("fpp.visual.grain",1)==0?1:0);SetTier(Tier);}
        public void ToggleBlur(){PlayerPrefs.SetInt("fpp.visual.blur",PlayerPrefs.GetInt("fpp.visual.blur",0)==0?1:0);SetTier(Tier);}
        public void SetDay(DayPreset day)
        {
            Day=day;if(sun==null)sun=RenderSettings.sun;
            if(sun!=null)
            {
                sun.useColorTemperature=true;sun.color=Color.white;sun.colorTemperature=visual.sunTemperature[(int)day];
                sun.transform.rotation=Quaternion.Euler(visual.sunElevation[(int)day],visual.sunAzimuth,0);sun.intensity=visual.sunIntensity[(int)day];sun.shadows=LightShadows.Soft;
            }
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=visual.fogDensity;RenderSettings.fogColor=day==DayPreset.Dusk?visual.duskFog:visual.daylightFog;
            PlayerPrefs.SetInt("fpp.visual.day",(int)day);
        }
        private void Update()
        {
            if(pipeline==null)return;
            if(Application.isMobilePlatform&&DynamicResolution)
            {
                float target=1f/GunplayConfig.Current.targetFPS,frame=Time.unscaledDeltaTime;
                slowFrames=frame>target*visual.slowRatio?slowFrames+1:0;fastFrames=frame<target*visual.fastRatio?fastFrames+1:0;
                if(slowFrames>=visual.hysteresisFrames){pipeline.renderScale=Mathf.Max(visual.minScale,pipeline.renderScale-visual.downStep);slowFrames=0;}
                else if(fastFrames>=visual.hysteresisFrames){pipeline.renderScale=Mathf.Min(visual.maxScale,pipeline.renderScale+visual.upStep);fastFrames=0;}
            }
            // Roof-probe fallback exposure, not a claim of histogram-based luminance metering.
            if(grade!=null)
            {
                var owner=GetComponent<WeaponController>();bool interior=owner!=null&&Physics.Raycast(owner.view.position,Vector3.up,visual.roofProbeDistance,~(1<<8),QueryTriggerInteraction.Ignore);
                grade.postExposure.value=Mathf.SmoothDamp(grade.postExposure.value,interior?visual.interiorEV:visual.exteriorEV,ref exposureVelocity,visual.exposureTime,Mathf.Infinity,Time.unscaledDeltaTime);
            }
        }
        private void OnDestroy(){if(Active==this)Active=null;if(pipeline!=null){if(GraphicsSettings.defaultRenderPipeline==pipeline){var source=Resources.Load<UniversalRenderPipelineAsset>("Rendering/High");GraphicsSettings.defaultRenderPipeline=source;QualitySettings.renderPipeline=source;}Destroy(pipeline);}if(profile!=null){foreach(var component in profile.components)Destroy(component);Destroy(profile);}}
    }
}
