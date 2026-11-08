using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using UnityEngine;
namespace Fpp
{
    // Bounded wall-clock sampling; no per-frame allocation, file IO, sorting or screen overlay.
    // This reports real observed frame intervals, not a simulated FPS promise or CPU/GPU attribution.
    public sealed class FrameTimeEvidence : MonoBehaviour
    {
        private readonly float[] intervals=new float[600];
        private int count,next;private long previous;private float ready;
        private void OnEnable(){previous=Stopwatch.GetTimestamp();ready=Time.realtimeSinceStartup+3;}
        private void OnApplicationPause(bool paused){previous=0;ready=Time.realtimeSinceStartup+1;}
        private void LateUpdate()
        {
            long now=Stopwatch.GetTimestamp();long elapsed=now-previous;previous=now;
            if(Time.realtimeSinceStartup<ready||WeaponController.MenuOpen||elapsed<=0)return;
            float ms=(float)(elapsed*1000.0/Stopwatch.Frequency);
            intervals[next]=ms;next=(next+1)%intervals.Length;count=Mathf.Min(count+1,intervals.Length);
        }
        public string CopyReport()
        {
            if(count<60)return "Need at least60 gameplay frames after startup. Close settings, play, then copy again.";
            var ordered=new float[count];Array.Copy(intervals,ordered,count);Array.Sort(ordered);double sum=0;foreach(float ms in ordered)sum+=ms;
            string report=string.Format(CultureInfo.InvariantCulture,"FPP observed gameplay frame intervals; device={0}; API={1}; framebuffer={2}x{3}; frames={4}; meanMs={5:F2}; p95Ms={6:F2}; meanFPS={7:F1}; maxMs={9:F2}; build={8}. No CPU/GPU bottleneck attribution. May include target-frame-rate limiting; not a device certification.",SystemInfo.deviceModel,SystemInfo.graphicsDeviceType,Screen.width,Screen.height,count,sum/count,ordered[(int)Math.Ceiling(count*.95)-1],1000/(sum/count),Application.buildGUID,ordered[count-1]);
            GUIUtility.systemCopyBuffer=report;try{File.WriteAllText(Path.Combine(Application.persistentDataPath,"frame-time-evidence.txt"),report);}catch(IOException){ }return report;
        }
    }
}
