using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using UnityEngine;
using Unity.Profiling;
namespace Fpp
{
    // Bounded wall-clock sampling; no per-frame allocation, file IO, sorting or screen overlay.
    // This reports real observed frame intervals, not a simulated FPS promise or CPU/GPU attribution.
    public sealed class FrameTimeEvidence : MonoBehaviour
    {
        private readonly float[] intervals=new float[600];
        private int count,next;private long previous;private float ready;
        private readonly FrameTiming[] nativeBuffer=new FrameTiming[1];
        private FrameTiming nativeLatest;private int nativeCaptureAt,nativeReadAt;private bool nativeEnabled;
        private ProfilerRecorder drawCalls,renderTriangles,setPass;
        private long lastDrawCalls=-1,lastTriangles=-1,lastSetPass=-1;
        private void SampleNativeTiming()
        {
            // Optional native instrumentation only; never enable GPU-query overhead or change quality here.
            if(!nativeEnabled)return;
            if(Time.frameCount>=nativeCaptureAt){FrameTimingManager.CaptureFrameTimings();nativeCaptureAt=Time.frameCount+30;}
            if(FrameTimingManager.GetLatestTimings(1,nativeBuffer)>0&&nativeBuffer[0].cpuFrameTime>0&&nativeBuffer[0].frameStartTimestamp!=nativeLatest.frameStartTimestamp)
            {nativeLatest=nativeBuffer[0];nativeReadAt=Time.frameCount;}
        }
        private string NativeReport()
        {
            string pss="unavailable";
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using(var debug=new AndroidJavaClass("android.os.Debug"))
                using(var info=new AndroidJavaObject("android.os.Debug$MemoryInfo"))
                {debug.CallStatic("getMemoryInfo",info);pss=info.Call<int>("getTotalPss").ToString(CultureInfo.InvariantCulture);}
            }
            catch(AndroidJavaException){pss="unavailable";}
#endif
            string timings=nativeLatest.cpuFrameTime>0?string.Format(CultureInfo.InvariantCulture,
                "nativeLatestCPUms={0:F2}; nativeMainMs={1:F2}; nativeRenderMs={2:F2}; nativePresentWaitMs={3:F2}; nativeGPUms={4}; nativeReadAgeFrames={5}",
                nativeLatest.cpuFrameTime,nativeLatest.cpuMainThreadFrameTime,nativeLatest.cpuRenderThreadFrameTime,nativeLatest.cpuMainThreadPresentWaitTime,
                nativeLatest.gpuFrameTime>0?nativeLatest.gpuFrameTime.ToString("F2",CultureInfo.InvariantCulture):"unavailable",Time.frameCount-nativeReadAt):"nativeTimings=unavailable";
            return string.Format(CultureInfo.InvariantCulture,
                " sampleQuality={0}; GPU={1}; driver={2}; OS={3}; systemRAMMiB={4}; AndroidProcessPSSKiB={5}; unityAllocatedBytes={6}; {7}. PSS is OS process accounting, not dedicated GPU memory; native values are a delayed snapshot, NOT wall-window averages or automatic bottleneck diagnosis.",
                count<60?"preliminary-under60frames":"at-least60frames",SystemInfo.graphicsDeviceName,SystemInfo.graphicsDeviceVersion,SystemInfo.operatingSystem,SystemInfo.systemMemorySize,pss,UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong(),timings);
        }
        private static ProfilerRecorder RenderCounter(string name)
        {
            try{return ProfilerRecorder.StartNew(ProfilerCategory.Render,name,1);}
            catch(ArgumentException){return default(ProfilerRecorder);}
            catch(InvalidOperationException){return default(ProfilerRecorder);}
        }
        private void OnDisable(){drawCalls.Dispose();renderTriangles.Dispose();setPass.Dispose();}
        private static long ReadCounter(ProfilerRecorder recorder){return recorder.Valid&&recorder.Count>0?recorder.LastValue:-1;}
        private static string Counter(long value){return value>=0?value.ToString(CultureInfo.InvariantCulture):"unavailable";}
        private void OnEnable(){drawCalls=RenderCounter("Draw Calls Count");renderTriangles=RenderCounter("Triangles Count");setPass=RenderCounter("SetPass Calls Count");nativeEnabled=FrameTimingManager.IsFeatureEnabled();previous=Stopwatch.GetTimestamp();ready=Time.realtimeSinceStartup+3;}
        private void OnApplicationPause(bool paused){previous=0;ready=Time.realtimeSinceStartup+1;}
        private void LateUpdate()
        {
            long now=Stopwatch.GetTimestamp();long elapsed=now-previous;previous=now;
            if(Time.realtimeSinceStartup<ready||WeaponController.MenuOpen||elapsed<=0)return;
            float ms=(float)(elapsed*1000.0/Stopwatch.Frequency);
            SampleNativeTiming();
            lastDrawCalls=ReadCounter(drawCalls);lastTriangles=ReadCounter(renderTriangles);lastSetPass=ReadCounter(setPass);
            intervals[next]=ms;next=(next+1)%intervals.Length;count=Mathf.Min(count+1,intervals.Length);
        }
        public void ResetSamples(){count=next=0;ready=Time.realtimeSinceStartup+3;previous=Stopwatch.GetTimestamp();}
        public string CopyReport()
        {
            if(count==0)return "No gameplay samples yet. Close settings, play briefly after startup, then copy again.";
            var ordered=new float[count];Array.Copy(intervals,ordered,count);Array.Sort(ordered);double sum=0;foreach(float ms in ordered)sum+=ms;
            string report=string.Format(CultureInfo.InvariantCulture,"FPP observed gameplay frame intervals; device={0}; API={1}; framebuffer={2}x{3}; frames={4}; meanMs={5:F2}; p95Ms={6:F2}; meanFPS={7:F1}; maxMs={9:F2}; build={8}. No CPU/GPU bottleneck attribution. May include target-frame-rate limiting; not a device certification.",SystemInfo.deviceModel,SystemInfo.graphicsDeviceType,Screen.width,Screen.height,count,sum/count,ordered[(int)Math.Ceiling(count*.95)-1],1000/(sum/count),Application.buildGUID,ordered[count-1]);
            report+=" artifactTest="+(PresentationRenderer.Active!=null?PresentationRenderer.Active.ArtifactMode.ToString():"unavailable");
            report+=NativeReport();
            report+=" latestGameplayRenderingCounters drawCalls="+Counter(lastDrawCalls)+"; trianglesAcrossPasses="+Counter(lastTriangles)+"; setPass="+Counter(lastSetPass)+". Counters include the rendering passes/cameras counted by Unity, not unique visible world geometry; unavailable is not zero.";
            GUIUtility.systemCopyBuffer=report;try{File.WriteAllText(Path.Combine(Application.persistentDataPath,"frame-time-evidence.txt"),report);}catch(IOException){ }return report;
        }
    }
}
