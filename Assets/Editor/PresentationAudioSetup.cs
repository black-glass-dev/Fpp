using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
namespace Fpp.Editor
{
    public static class PresentationAudioSetup
    {
        private const string Root="Assets/Generated/Resources/Presentation";
        private const BindingFlags Public=BindingFlags.Public|BindingFlags.Instance|BindingFlags.Static;
        public static void Apply()
        {
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();string path=Root+"/Combat.mixer";
            var mixer=AssetDatabase.LoadAssetAtPath<AudioMixer>(path);
            if(mixer==null)
            {
                Type controller=null;foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies()){controller=assembly.GetType("UnityEditor.Audio.AudioMixerController");if(controller!=null)break;}
                if(controller==null)throw new InvalidOperationException("Pinned Unity2022 mixer authoring API unavailable");
                mixer=(AudioMixer)controller.GetMethod("CreateMixerControllerAtPath",Public).Invoke(null,new object[]{path});
                var master=controller.GetProperty("masterGroup",Public).GetValue(mixer);var groupType=master.GetType();
                string[] buses={"Master","Weapons","Foley","Ambience","UI","MusicStub"};
                var parameterType=controller.Assembly.GetType("UnityEditor.Audio.ExposedAudioParameter");var parameters=Array.CreateInstance(parameterType,buses.Length);
                for(int i=0;i<buses.Length;i++)
                {
                    object group=master;if(i>0){group=controller.GetMethod("CreateNewGroup",Public).Invoke(mixer,new object[]{buses[i],false});controller.GetMethod("AddChildToParent",Public).Invoke(mixer,new[]{group,master});}
                    object parameter=Activator.CreateInstance(parameterType);parameterType.GetField("guid").SetValue(parameter,groupType.GetMethod("GetGUIDForVolume",Public).Invoke(group,null));parameterType.GetField("name").SetValue(parameter,"Bus"+buses[i]);parameters.SetValue(parameter,i);
                }
                controller.GetProperty("exposedParameters",Public).SetValue(mixer,parameters);EditorUtility.SetDirty(mixer);
            }
            var settings=Resources.Load<PresentationAudioSettings>("Presentation/Audio");
            string signature=Hash128.Compute(EditorJsonUtility.ToJson(settings)).ToString(),signaturePath=Root+"/synthesis-signature.txt";bool regenerate=!File.Exists(signaturePath)||File.ReadAllText(signaturePath)!=signature;
            for(int layer=0;layer<settings.layerSeconds.Length;layer++)for(int variant=0;variant<settings.variants;variant++)
            {
                string clipPath=Root+"/SYNTH_PLACEHOLDER_layer_"+layer+"_variant_"+variant+".wav";
                if(regenerate||!File.Exists(clipPath))
                {
                    var data=PresentationAudio.GenerateSamples(settings,layer,variant);
                    using(var writer=new BinaryWriter(File.Create(clipPath)))
                    {
                        writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+data.Length*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(settings.sampleRate);writer.Write(settings.sampleRate*2);writer.Write((short)2);writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(data.Length*2);foreach(float sample in data)writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(sample,-1,1)*32767));
                    }
                    AssetDatabase.ImportAsset(clipPath,ImportAssetOptions.ForceSynchronousImport);
                }
                var importer=(AudioImporter)AssetImporter.GetAtPath(clipPath);var importSettings=importer.defaultSampleSettings;importSettings.compressionFormat=AudioCompressionFormat.Vorbis;importSettings.quality=settings.vorbisQuality;importSettings.loadType=AudioClipLoadType.DecompressOnLoad;importSettings.preloadAudioData=true;importSettings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;importer.defaultSampleSettings=importSettings;importer.forceToMono=true;importer.SaveAndReimport();
            }
            File.WriteAllText(signaturePath,signature);AssetDatabase.SaveAssets();
        }
    }
}
