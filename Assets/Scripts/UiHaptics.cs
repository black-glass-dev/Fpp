using UnityEngine;
namespace Fpp
{
    // Optional UI-only pulses; never a per-shot allocation in the combat audio loop.
    public sealed class UiHaptics:MonoBehaviour
    {
        private static UiHaptics active;
        public static bool Enabled {get=>PlayerPrefs.GetInt("fpp.ui.haptics",1)!=0;set=>PlayerPrefs.SetInt("fpp.ui.haptics",value?1:0);}
#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject vibrator,effect;private System.IntPtr method;private jvalue[] arguments;
#endif
        private void Awake()
        {
            active=this;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                var settings=Resources.Load<PresentationAudioSettings>("Presentation/Audio");
                using(var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))using(var activity=unity.GetStatic<AndroidJavaObject>("currentActivity"))vibrator=activity.Call<AndroidJavaObject>("getSystemService","vibrator");
                if(vibrator==null||!vibrator.Call<bool>("hasVibrator"))return;arguments=new jvalue[1];
                using(var version=new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    if(version.GetStatic<int>("SDK_INT")>=26)
                    {
                        using(var type=new AndroidJavaClass("android.os.VibrationEffect"))effect=type.CallStatic<AndroidJavaObject>("createOneShot",(long)settings.hapticMilliseconds,settings.hapticAmplitude);
                        method=AndroidJNI.GetMethodID(vibrator.GetRawClass(),"vibrate","(Landroid/os/VibrationEffect;)V");arguments[0].l=effect.GetRawObject();
                    }
                    else{method=AndroidJNI.GetMethodID(vibrator.GetRawClass(),"vibrate","(J)V");arguments[0].j=settings.hapticMilliseconds;}
                }
            }
            catch(System.Exception){method=System.IntPtr.Zero;}
#endif
        }
        public static void Pulse()
        {
            if(active==null||!Enabled)return;
#if UNITY_ANDROID && !UNITY_EDITOR
            if(active.method!=System.IntPtr.Zero)AndroidJNI.CallVoidMethod(active.vibrator.GetRawObject(),active.method,active.arguments);
#endif
        }
        private void OnDestroy()
        {
            if(active==this)active=null;
#if UNITY_ANDROID && !UNITY_EDITOR
            effect?.Dispose();vibrator?.Dispose();
#endif
        }
    }
}
