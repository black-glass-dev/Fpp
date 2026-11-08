using UnityEngine;
namespace Fpp
{
    [CreateAssetMenu(menuName="FPP/Presentation/Audio mix")]
    public sealed class PresentationAudioSettings : ScriptableObject
    {
        [Range(8,32)]public int maxVoices=32;
        public int sampleRate=24000,variants=4;
        [Range(0,1)]public float master=.65f,weapons=.7f,foley=.12f,ambience=.035f;
        [Range(0,1)]public float ui=.12f,music=.5f,vorbisQuality=.7f;
        public float[] layerSeconds={.08f,.24f,.065f,.18f,.65f,.055f,2};
        public float[] frequencies={2400,135,900,55,180,2000,70};
        public float[] decay={75,18,90,24,7,95,0};
        public float[] noiseMix={.85f,.7f,.7f,.15f,.9f,.1f,1};
        public float[] layerGain={.27f,.32f,.12f,.2f};
        public float attackHz=2400,clipHeadroom=.7f,noiseSmoothing=.7f,windSmoothing=.015f,variantFrequency=.017f;
        public float pitchVariation=.03f,volumeVariationDB=1.5f;
        public float minDistance=1,maxDistance=200,occlusionHz=3500,occlusionDB=-8;
        public float duckDB=-3,duckSeconds=.12f,reflectionDistance=85,soundSpeed=343,maxReflectionDelay=.25f;
        public float tailGain=.14f,reflectionGain=.065f,hitGain=.16f,impactGain=.12f;
    }
}
