using UnityEngine;
namespace Fpp
{
    // Original synthesized placeholders, not recorded MWII sounds. Fixed source/clip pools.
    [DisallowMultipleComponent]
    public sealed class PresentationAudio : MonoBehaviour
    {
        public static PresentationAudio Active {get;private set;}
        public int VoiceCapacity=>sources.Length;
        public int ActiveVoices {get {int n=0;for(int i=0;i<until.Length;i++)if(until[i]>Time.unscaledTime)n++;return n;}}
        public int ShotLayersPlayed {get;private set;}
        private PresentationAudioSettings settings;
        private AudioSource[] sources;private AudioLowPassFilter[] filters;
        private float[] until;private int[] priorities;
        private AudioClip[,] clips;private AudioSource wind;private float duckUntil;
        private uint random=7919;
        private float Random01(){random=1664525*random+1013904223;return (random&65535)/65535f;}
        private void Awake()
        {
            settings=Resources.Load<PresentationAudioSettings>("Presentation/Audio");
            if(settings==null){enabled=false;Debug.LogError("Missing Presentation/Audio settings");return;}
            Active=this;int count=Mathf.Clamp(settings.maxVoices,8,32);
            sources=new AudioSource[count];filters=new AudioLowPassFilter[count];until=new float[count];priorities=new int[count];
            for(int i=0;i<count;i++)
            {
                var go=new GameObject("Pooled presentation voice "+i);go.transform.SetParent(transform,false);
                sources[i]=go.AddComponent<AudioSource>();sources[i].playOnAwake=false;sources[i].dopplerLevel=0;
                filters[i]=go.AddComponent<AudioLowPassFilter>();filters[i].enabled=false;
            }
            clips=new AudioClip[settings.layerSeconds.Length,settings.variants];
            for(int layer=0;layer<settings.layerSeconds.Length;layer++)for(int variant=0;variant<settings.variants;variant++)clips[layer,variant]=Synthesize(layer,variant);
            // Ambience occupies one of the 32 voices; no separate unbounded PlayOneShot calls.
            wind=sources[count-1];wind.clip=clips[6,0];wind.loop=true;wind.spatialBlend=0;wind.volume=settings.ambience;wind.Play();until[count-1]=float.PositiveInfinity;priorities[count-1]=0;
        }
        private AudioClip Synthesize(int layer,int variant)
        {
            int samples=Mathf.CeilToInt(settings.sampleRate*settings.layerSeconds[layer]);var data=new float[samples];float previous=0;
            for(int i=0;i<samples;i++)
            {
                float t=i/(float)settings.sampleRate;float noise=Random01()*2-1;
                previous=Mathf.Lerp(previous,noise,layer==6?settings.windSmoothing:settings.noiseSmoothing);
                float envelope=layer==6?1:Mathf.Exp(-settings.decay[layer]*t)*Mathf.Min(1,t*settings.attackHz);
                float tone=Mathf.Sin(2*Mathf.PI*settings.frequencies[layer]*(1+variant*settings.variantFrequency)*t);
                data[i]=Mathf.Clamp((previous*settings.noiseMix[layer]+tone*(1-settings.noiseMix[layer]))*envelope*settings.clipHeadroom,-settings.clipHeadroom,settings.clipHeadroom);
            }
            var clip=AudioClip.Create("SYNTH_PLACEHOLDER_layer_"+layer+"_variant_"+variant,samples,1,settings.sampleRate,false);clip.SetData(data,0);return clip;
        }
        private void OnEnable(){GunplayEvents.Raised+=OnEvent;}
        private void OnDisable(){GunplayEvents.Raised-=OnEvent;}
        private void OnEvent(GunplayEvent e)
        {
            if(settings==null||sources==null)return;
            if(e.Kind==GunplayEventKind.ShotFired)
            {
                for(int i=0;i<4;i++){Play(i,e.Position,settings.layerGain[i]*settings.weapons,3,0,true);ShotLayersPlayed++;}
                duckUntil=Time.unscaledTime+settings.duckSeconds;
                for(int i=0;i<4;i++)
                {
                    Vector3 direction=i==0?transform.right:i==1?-transform.right:i==2?transform.forward:-transform.forward;
                    if(Physics.Raycast(e.Position,direction,out var hit,settings.reflectionDistance,~(1<<8),QueryTriggerInteraction.Ignore)&&!hit.transform.IsChildOf(transform))
                        Play(4,hit.point,settings.reflectionGain*settings.weapons,2,Mathf.Min(settings.maxReflectionDelay,hit.distance/settings.soundSpeed),false);
                }
            }
            else if(e.Kind==GunplayEventKind.ShotTailRequest)Play(4,e.Position,settings.tailGain*settings.weapons,2,0,true);
            else if(e.Kind==GunplayEventKind.Hitmarker)Play(5,e.Position,settings.hitGain,3,0,true);
            else if(e.Kind==GunplayEventKind.Footstep||e.Kind==GunplayEventKind.LandImpact||e.Kind==GunplayEventKind.Slide||e.Kind==GunplayEventKind.Dive)Play(3,e.Position,settings.foley,1,0,false);
            else if(e.Kind==GunplayEventKind.ReloadFoley||e.Kind==GunplayEventKind.ShellLand||e.Kind==GunplayEventKind.MechanicalClick||e.Kind==GunplayEventKind.DryFire||e.Kind==GunplayEventKind.ADSIn||e.Kind==GunplayEventKind.ADSOut||e.Kind==GunplayEventKind.WeaponSwap||e.Kind==GunplayEventKind.Pickup||e.Kind==GunplayEventKind.UI)Play(2,e.Position,settings.foley,1,0,true);
            else if(e.Kind==GunplayEventKind.Impact||e.Kind==GunplayEventKind.Ricochet)Play(0,e.Position,settings.impactGain,2,0,false);
        }
        private void Play(int layer,Vector3 position,float gain,int priority,float delay,bool local)
        {
            int chosen=-1;float oldest=float.PositiveInfinity;
            for(int i=0;i<sources.Length-1;i++)
            {
                if(until[i]<=Time.unscaledTime){chosen=i;break;}
                if(priorities[i]<=priority&&until[i]<oldest){chosen=i;oldest=until[i];}
            }
            if(chosen<0)return;
            var source=sources[chosen];source.Stop();source.transform.position=position;source.loop=false;
            source.clip=clips[layer,Mathf.Min(settings.variants-1,(int)(Random01()*settings.variants))];
            source.pitch=1+(Random01()*2-1)*settings.pitchVariation;
            bool occluded=!local&&Physics.Linecast(transform.position,position,~(1<<8),QueryTriggerInteraction.Ignore);
            filters[chosen].enabled=occluded;filters[chosen].cutoffFrequency=settings.occlusionHz;
            source.spatialBlend=local?0:1;source.minDistance=settings.minDistance;source.maxDistance=settings.maxDistance;source.rolloffMode=AudioRolloffMode.Logarithmic;
            source.volume=Mathf.Clamp01(gain*settings.master*Mathf.Pow(10,(Random01()*2-1)*settings.volumeVariationDB/20)*(occluded?Mathf.Pow(10,settings.occlusionDB/20):1));
            source.priority=priority==3?0:priority==2?64:128;source.PlayDelayed(delay);
            priorities[chosen]=priority;until[chosen]=Time.unscaledTime+delay+source.clip.length/source.pitch;
        }
        private void Update(){if(wind!=null)wind.volume=settings.master*settings.ambience*(Time.unscaledTime<duckUntil?Mathf.Pow(10,settings.duckDB/20):1);}
        private void OnDestroy()
        {
            if(Active==this)Active=null;
            if(clips!=null)foreach(var clip in clips)if(clip!=null)Destroy(clip);
        }
    }
}
