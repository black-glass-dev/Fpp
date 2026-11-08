#if UNITY_EDITOR
using System.Collections;
using System.IO;
using Fpp;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Fpp.Tests
{
    public sealed class PresentationAudioOutputTests
    {
        [SetUp] public void ReportNativeStart(){Debug.Log("FPP QA START "+TestContext.CurrentContext.Test.FullName);Directory.CreateDirectory("QA/runtime");File.AppendAllText("QA/runtime/test-progress.txt","START "+TestContext.CurrentContext.Test.FullName+"\n");}
        [TearDown] public void ReportNativeFinish(){Debug.Log("FPP QA END "+TestContext.CurrentContext.Test.FullName+" "+TestContext.CurrentContext.Result.Outcome.Status);File.AppendAllText("QA/runtime/test-progress.txt","END "+TestContext.CurrentContext.Test.FullName+" "+TestContext.CurrentContext.Result.Outcome.Status+"\n");}

        private static void Header(BinaryWriter writer,int frames,int rate)
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+frames*8);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)3);writer.Write((short)2);writer.Write(rate);writer.Write(rate*8);writer.Write((short)8);writer.Write((short)32);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(frames*8);
        }
        [UnityTest,Timeout(120000)] public IEnumerator AudioMixerRoutesVorbisClipsAndExportsActualStereoPCM()
        {
            var previous=AudioSettings.GetConfiguration();var configuration=previous;configuration.speakerMode=AudioSpeakerMode.Stereo;configuration.sampleRate=48000;configuration.numRealVoices=32;configuration.numVirtualVoices=32;Assert.True(AudioSettings.Reset(configuration));
            yield return SceneManager.LoadSceneAsync("CottageTest");yield return null;yield return null;
            var audio=PresentationAudio.Active;Assert.NotNull(audio);Assert.NotNull(audio.Mixer);Assert.AreEqual(32,audio.VoiceCapacity);
            foreach(string bus in new[]{"Master","Weapons","Foley","Ambience","UI","MusicStub"})Assert.Greater(audio.Mixer.FindMatchingGroups(bus).Length,0,bus);
            foreach(var source in audio.GetComponentsInChildren<AudioSource>())if(source.clip!=null)
            {
                Assert.NotNull(source.outputAudioMixerGroup);var importer=(UnityEditor.AudioImporter)UnityEditor.AssetImporter.GetAtPath(UnityEditor.AssetDatabase.GetAssetPath(source.clip));Assert.NotNull(importer);Assert.AreEqual(AudioCompressionFormat.Vorbis,importer.defaultSampleSettings.compressionFormat);
            }
            var gains=new float[6];for(int i=0;i<6;i++)gains[i]=audio.BusVolume(i);int oldCapture=Time.captureFramerate;bool rendering=false;Directory.CreateDirectory("QA/runtime/audio");
            try
            {
                Time.captureFramerate=20;rendering=AudioRenderer.Start();Assert.True(rendering,"Actual mixed graph capture required, not a clip-energy substitute");
                for(int stem=0;stem<3;stem++)
                {
                    foreach(var source in audio.GetComponentsInChildren<AudioSource>())if(!source.loop)source.Stop();
                    for(int bus=0;bus<6;bus++)audio.SetBusVolume(bus,stem==0?gains[bus]:bus==0||bus==(stem==1?1:2)?1:0,false);
                    string name=stem==0?"combat-mix":stem==1?"weapons-stem":"foley-stem";float energy=0,peak=0;int total=0;
                    using(var writer=new BinaryWriter(File.Create("QA/runtime/audio/"+name+".wav")))
                    {
                        Header(writer,0,AudioSettings.outputSampleRate);
                        for(int frame=0;frame<80;frame++)
                        {
                            if(stem!=2&&frame%2==0)GunplayEvents.Send(GunplayEventKind.ShotFired,audio.transform.position,Resources.Load<WeaponAsset>("Gunplay/ak47"));
                            if(stem!=1&&frame%10==0)GunplayEvents.Send(frame%20==0?GunplayEventKind.Footstep:GunplayEventKind.ReloadFoley,audio.transform.position);
                            yield return null;int samples=AudioRenderer.GetSampleCountForCaptureFrame();Assert.Greater(samples,0);var buffer=new NativeArray<float>(samples*2,Allocator.Temp);
                            try{Assert.True(AudioRenderer.Render(buffer));for(int i=0;i<buffer.Length;i++){float value=buffer[i];writer.Write(value);energy+=value*value;peak=Mathf.Max(peak,Mathf.Abs(value));}total+=buffer.Length;}
                            finally{buffer.Dispose();}
                        }
                        writer.Seek(0,SeekOrigin.Begin);Header(writer,total/2,AudioSettings.outputSampleRate);
                    }
                    Assert.Greater(energy/total,.00000001f,"actual graph must not be silent");File.WriteAllText("QA/runtime/audio/"+name+"-capture.txt","Actual Unity stereo float PCM: frames="+total/2+"; rate="+AudioSettings.outputSampleRate+"; RMS="+Mathf.Sqrt(energy/total)+"; sample peak="+peak+". Not a LUFS/true-peak certificate.");
                }
            }
            finally{if(rendering)AudioRenderer.Stop();Time.captureFramerate=oldCapture;for(int i=0;i<6;i++)audio.SetBusVolume(i,gains[i],false);AudioSettings.Reset(previous);}
        }
    }
}
#endif
