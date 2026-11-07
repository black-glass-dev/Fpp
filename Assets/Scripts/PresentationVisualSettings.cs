using UnityEngine;
namespace Fpp
{
    [CreateAssetMenu(menuName="FPP/Presentation visual settings")]
    public sealed class PresentationVisualSettings:ScriptableObject
    {
        public static PresentationVisualSettings Current=>Resources.Load<PresentationVisualSettings>("Presentation/Visual");
        [Header("Tier fallback and shadows")]
        public int mobileMsaa=4,pcMsaa=1,mobileCascades=2,pcCascades=4;
        public float mobileShadowDistance=80,pcShadowDistance=200,ssaoRadius=.35f,ssaoIntensity=1;
        [Header("Dynamic resolution (two consecutive measured frames)")]
        public float minScale=.7f,maxScale=1,downStep=.025f,upStep=.01f,slowRatio=1.04f,fastRatio=.94f;
        public int hysteresisFrames=2;
        [Header("Grade")]
        public float bloomThreshold=1.1f,bloomIntensity=.35f,bloomScatter=.6f,vignette=.25f,chromaticAberration=.08f,grain=.06f,motionBlur=.12f,saturation=-12,contrast=12;
        public Color shadowTint=new Color(.18f,.3f,.33f),highlightTint=new Color(.62f,.5f,.35f);
        [Header("Roof-probe exposure fallback; not a luminance histogram")]
        public float exteriorEV=.6f,interiorEV=1.1f,exposureTime=.5f,roofProbeDistance=4;
        [Header("Late-afternoon / noon / dusk")]
        public Vector3 sunElevation=new Vector3(28,65,12),sunTemperature=new Vector3(5500,6500,4200),sunIntensity=new Vector3(1.1f,1.1f,.65f);
        public float sunAzimuth=-36,fogDensity=.008f;
        public Color daylightFog=new Color(.65f,.69f,.7f),duskFog=new Color(.37f,.4f,.48f);
    }
}
