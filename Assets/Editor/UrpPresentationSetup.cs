using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Fpp.Editor
{
    public static class UrpPresentationSetup
    {
        private static PresentationVisualSettings Visual=>PresentationVisualSettings.Current;
        private const string Root="Assets/Generated/Resources/Rendering";
        public static void Apply(WeaponController owner)
        {
            System.IO.Directory.CreateDirectory(Root);AssetDatabase.Refresh();
            var fpv=Renderer("FPVRenderer",false,false);var mobile=Renderer("MobileRenderer",false,true);var pc=Renderer("PCRenderer",true,true);var low=Renderer("LowRenderer",false,false);
            foreach(PresentationTier tier in Enum.GetValues(typeof(PresentationTier)))
            {
                string path=Root+"/"+tier+".asset";var asset=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                if(asset==null){asset=UniversalRenderPipelineAsset.Create(tier==PresentationTier.Low?low:mobile);AssetDatabase.CreateAsset(asset,path);}
                var serialized=new SerializedObject(asset);var list=serialized.FindProperty("m_RendererDataList");list.arraySize=3;list.GetArrayElementAtIndex(0).objectReferenceValue=tier==PresentationTier.Low?low:mobile;list.GetArrayElementAtIndex(1).objectReferenceValue=pc;list.GetArrayElementAtIndex(2).objectReferenceValue=fpv;
                serialized.FindProperty("m_SoftShadowsSupported").boolValue=tier!=PresentationTier.Low;serialized.FindProperty("m_MainLightShadowsSupported").boolValue=true;serialized.FindProperty("m_AdditionalLightShadowsSupported").boolValue=false;serialized.ApplyModifiedPropertiesWithoutUndo();
                asset.msaaSampleCount=tier==PresentationTier.PCUltra?Visual.pcMsaa:Visual.mobileMsaa;asset.supportsHDR=tier!=PresentationTier.Low;asset.supportsCameraDepthTexture=true;asset.supportsCameraOpaqueTexture=tier>=PresentationTier.High;asset.useSRPBatcher=true;asset.shadowDistance=tier==PresentationTier.PCUltra?Visual.pcShadowDistance:Visual.mobileShadowDistance;asset.shadowCascadeCount=tier==PresentationTier.PCUltra?Visual.pcCascades:Visual.mobileCascades;asset.renderScale=1;EditorUtility.SetDirty(asset);
            }
            var high=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Root+"/High.asset");GraphicsSettings.defaultRenderPipeline=high;QualitySettings.renderPipeline=high;
            var sun=RenderSettings.sun;if(sun!=null){sun.useColorTemperature=true;sun.colorTemperature=Visual.sunTemperature.x;sun.color=Color.white;sun.intensity=Visual.sunIntensity.x;sun.transform.rotation=Quaternion.Euler(Visual.sunElevation.x,Visual.sunAzimuth,0);sun.shadows=LightShadows.Soft;}
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=Visual.fogDensity;RenderSettings.fogColor=Visual.daylightFog;
            ConvertMaterials();PresentationRenderer.Stack(owner.view.GetComponent<Camera>(),owner.weaponCamera);
            // Editor review uses a deterministic fixed profile; runtime creates a private adaptive clone.
            var volume=new GameObject("Authored presentation volume").AddComponent<Volume>();volume.isGlobal=true;
            string profilePath=Root+"/GoldenGrade.asset";var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if(profile==null){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,profilePath);}
            if(!profile.TryGet(out Tonemapping tone)){tone=profile.Add<Tonemapping>();AssetDatabase.AddObjectToAsset(tone,profile);}tone.mode.Override(TonemappingMode.ACES);
            if(!profile.TryGet(out Bloom bloom)){bloom=profile.Add<Bloom>();AssetDatabase.AddObjectToAsset(bloom,profile);}bloom.threshold.Override(Visual.bloomThreshold);bloom.intensity.Override(Visual.bloomIntensity);bloom.scatter.Override(Visual.bloomScatter);
            if(!profile.TryGet(out ColorAdjustments grade)){grade=profile.Add<ColorAdjustments>();AssetDatabase.AddObjectToAsset(grade,profile);}grade.saturation.Override(Visual.saturation);grade.contrast.Override(Visual.contrast);grade.postExposure.Override(Visual.exteriorEV);
            if(!profile.TryGet(out Vignette vignette)){vignette=profile.Add<Vignette>();AssetDatabase.AddObjectToAsset(vignette,profile);}vignette.intensity.Override(Visual.vignette);
            volume.sharedProfile=profile;AssetDatabase.SaveAssets();
        }
        private static UniversalRendererData Renderer(string name,bool plus,bool ao)
        {
            string path=Root+"/"+name+".asset";var data=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            if(data==null){data=ScriptableObject.CreateInstance<UniversalRendererData>();AssetDatabase.CreateAsset(data,path);}
            data.renderingMode=plus?RenderingMode.ForwardPlus:RenderingMode.Forward;
            ResourceReloader.ReloadAllNullIn(data,UniversalRenderPipelineAsset.packagePath);
            if(ao&&data.rendererFeatures.Count==0)
            {
                var type=typeof(UniversalRenderPipeline).Assembly.GetType("UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion");
                if(type==null)throw new InvalidOperationException("Pinned URP14 SSAO feature unavailable");
                var feature=(ScriptableRendererFeature)ScriptableObject.CreateInstance(type);feature.name="Mobile downsampled SSAO";AssetDatabase.AddObjectToAsset(feature,data);
                var settings=new SerializedObject(feature);settings.FindProperty("m_Settings.Radius").floatValue=Visual.ssaoRadius;settings.FindProperty("m_Settings.Intensity").floatValue=Visual.ssaoIntensity;settings.FindProperty("m_Settings.Downsample").boolValue=true;settings.FindProperty("m_Settings.Source").intValue=0;settings.ApplyModifiedPropertiesWithoutUndo();
                ResourceReloader.ReloadAllNullIn(feature,UniversalRenderPipelineAsset.packagePath);data.rendererFeatures.Add(feature);feature.Create();
            }
            EditorUtility.SetDirty(data);return data;
        }
        private static void ConvertMaterials()
        {
            var lit=Shader.Find("Universal Render Pipeline/Lit");if(lit==null)throw new InvalidOperationException("URP Lit shader is missing");
            var converted=new Dictionary<Material,Material>();
            foreach(var renderer in UnityEngine.Object.FindObjectsOfType<Renderer>(true))
            {
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    var original=materials[i];if(original==null||original.shader.name!="Standard")continue;
                    if(!converted.TryGetValue(original,out var material))
                    {
                        string source=AssetDatabase.GetAssetPath(original);string key=Hash128.Compute(source+original.name+original.color.ToString("R")+original.mainTextureScale.ToString("R")+original.mainTextureOffset.ToString("R")+(original.mainTexture!=null?AssetDatabase.GetAssetPath(original.mainTexture):"none")+original.GetFloat("_Metallic").ToString("R")+original.GetFloat("_Glossiness").ToString("R")).ToString()+"_"+original.name.Replace('/','_');string path=Root+"/Lit_"+key+".mat";
                        material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material==null){material=new Material(lit);AssetDatabase.CreateAsset(material,path);}material.shader=lit;
                        material.SetTexture("_BaseMap",original.mainTexture);material.SetColor("_BaseColor",original.color);material.SetTextureScale("_BaseMap",original.mainTextureScale);material.SetTextureOffset("_BaseMap",original.mainTextureOffset);
                        foreach(string property in new[]{"_BumpMap","_MetallicGlossMap","_OcclusionMap","_EmissionMap"})if(original.HasProperty(property))material.SetTexture(property,original.GetTexture(property));
                        material.SetFloat("_Metallic",original.GetFloat("_Metallic"));material.SetFloat("_Smoothness",original.GetFloat("_Glossiness"));
                        if(material.GetTexture("_BumpMap")!=null)material.EnableKeyword("_NORMALMAP");if(material.GetTexture("_MetallicGlossMap")!=null)material.EnableKeyword("_METALLICSPECGLOSSMAP");if(material.GetTexture("_OcclusionMap")!=null)material.EnableKeyword("_OCCLUSIONMAP");
                        material.enableInstancing=true;EditorUtility.SetDirty(material);converted.Add(original,material);
                    }
                    materials[i]=material;
                }
                renderer.sharedMaterials=materials;
            }
        }
    }
}
