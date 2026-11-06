using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
#if UNITY_ANDROID
using UnityEditor.Android;
#endif

namespace Fpp.Editor
{
    public static class AppIconSetup
    {
        public static void Apply()
        {
            var icon=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Icon/FppIcon.png");
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown,new[]{icon});
#if UNITY_ANDROID
            var platform=NamedBuildTarget.Android;
            foreach(var kind in new[]{AndroidPlatformIconKind.Legacy,AndroidPlatformIconKind.Round,AndroidPlatformIconKind.Adaptive})
            {
                var icons=PlayerSettings.GetPlatformIcons(platform,kind);
                foreach(var slot in icons)
                {
                    if(kind==AndroidPlatformIconKind.Adaptive)
                    {
                        var background=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Icon/Background.png");
                        slot.SetTextures(new[]{icon,icon});
                    }
                    else slot.SetTexture(icon);
                }
                PlayerSettings.SetPlatformIcons(platform,kind,icons);
            }
#endif
        }
    }
}
