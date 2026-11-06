using UnityEngine;

namespace Fpp
{
    // Conservative defaults, not a substitute for profiling on target devices.
    public sealed class MobileBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            Application.targetFrameRate = 30;
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 0;
            QualitySettings.shadows = ShadowQuality.Disable;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

    }
}
