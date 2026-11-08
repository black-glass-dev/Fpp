#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
TEXTURE2D(_SoilTex); SAMPLER(sampler_SoilTex);
TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
TEXTURE2D(_SoilNormal); SAMPLER(sampler_SoilNormal);
CBUFFER_START(UnityPerMaterial)
float4 _MainTex_ST, _Color, _Entry, _InteriorRect, _SkirtRect;
float _Road, _DetailScale, _TrimBelow, _ScanGroundBelow, _InteriorTop, _Smoothness, _Metallic;
CBUFFER_END
struct FppVertex {float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;
#if defined(FPP_VERTEX_COLOR)
 half4 color:COLOR;
#endif
};
struct FppVaryings {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;half3 normal:TEXCOORD1;float2 uv:TEXCOORD2;half fog:TEXCOORD3;
#if defined(FPP_VERTEX_COLOR)
 half4 color:TEXCOORD4;
#endif
};
FppVaryings FppVert(FppVertex v)
{
 FppVaryings o=(FppVaryings)0;o.world=TransformObjectToWorld(v.positionOS.xyz);o.normal=TransformObjectToWorldNormal(v.normalOS);o.positionCS=TransformWorldToHClip(o.world);o.uv=v.uv*_MainTex_ST.xy+_MainTex_ST.zw;o.fog=ComputeFogFactor(o.positionCS.z);
#if defined(FPP_VERTEX_COLOR)
 o.color=v.color;
#endif
 return o;
}
void FppClip(float3 p)
{
#if defined(FPP_SCAN)
 clip(p.y-_TrimBelow);
 if(p.y<_ScanGroundBelow)clip(min(min(p.x-_SkirtRect.x,_SkirtRect.z-p.x),min(p.z-_SkirtRect.y,_SkirtRect.w-p.z)));
 if(p.y<_InteriorTop&&p.x>_InteriorRect.x&&p.x<_InteriorRect.z&&p.z>_InteriorRect.y&&p.z<_InteriorRect.w)clip(-1);
 float doorX=p.x-_Entry.x;
 if(p.y<_Entry.w&&p.z>_Entry.y&&p.z<_Entry.z&&min(abs(doorX),min(abs(doorX-3.55),abs(doorX+3.55)))<.88)clip(-1);
#endif
}
float FppHash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
float FppNoise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(FppHash(i),FppHash(i+float2(1,0)),f.x),lerp(FppHash(i+float2(0,1)),FppHash(i+1),f.x),f.y);}
half4 FppFragment(FppVaryings i):SV_Target
{
 FppClip(i.world);half3 n=normalize(i.normal);SurfaceData s=(SurfaceData)0;s.alpha=1;s.occlusion=1;s.smoothness=.15;s.normalTS=half3(0,0,1);
#if defined(FPP_GROUND)
 float2 p=i.world.xz,uv=p*_DetailScale;
 half macro=FppNoise(p*.18)*.65+FppNoise(p*.57)*.35;
 half lane=1-smoothstep(4.1,5.0,abs(p.x));half yard=(1-smoothstep(15.8,17.5,abs(p.x)))*(1-smoothstep(4.6,6,abs(p.y)));
 half road=_Road*max(lane,yard),soil=lerp(smoothstep(.38,.72,macro)*.78,1,road);
 half ruts=pow(saturate(1-abs(abs(p.x)-2.05)*2.6),3)*road;
 s.albedo=lerp(SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv).rgb,SAMPLE_TEXTURE2D(_SoilTex,sampler_SoilTex,uv*1.13+float2(.32,.71)).rgb,soil)*lerp(.78,1.16,FppNoise(p*.075+17))*(1-ruts*.17);
 half3 detail=normalize(lerp(UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,uv)),UnpackNormal(SAMPLE_TEXTURE2D(_SoilNormal,sampler_SoilNormal,uv*1.13+float2(.32,.71))),soil));
 n=normalize(n+half3(detail.x,0,detail.y)*.65);s.smoothness=lerp(.08,.16,ruts);s.occlusion=.93;
#elif defined(FPP_METRIC_DETAIL)
 // Metre-based projection: source UV scale cannot turn bricks into metre-wide slabs.
 half3 axis=abs(n);bool top=axis.y>=axis.x&&axis.y>=axis.z;bool side=axis.x>axis.z;
 float2 uv=(top?i.world.xz:side?i.world.zy:i.world.xy)*_DetailScale;
 s.albedo=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv).rgb*_Color.rgb;
 half3 bump=UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,uv));
 n=normalize(n+(top?half3(bump.x,0,bump.y):side?half3(0,bump.y,bump.x):half3(bump.x,bump.y,0))*.3);
 s.smoothness=_Smoothness;s.metallic=_Metallic;
#elif defined(FPP_VERTEX_COLOR)
 s.albedo=i.color.rgb*_Color.rgb;
#elif defined(FPP_BRICK)
 float2 uv=abs(n.x)>.5?i.world.zy:i.world.xy;uv.y*=1.6;s.albedo=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv).rgb*_Color.rgb;
#else
 s.albedo=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).rgb*_Color.rgb;
#endif
 InputData data=(InputData)0;data.positionWS=i.world;data.normalWS=n;data.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.world);data.shadowCoord=TransformWorldToShadowCoord(i.world);data.bakedGI=SampleSH(n);data.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);data.shadowMask=half4(1,1,1,1);
 half4 color=UniversalFragmentPBR(data,s);color.rgb=MixFog(color.rgb,i.fog);return color;
}
FppVaryings FppShadowVert(FppVertex v)
{
 FppVaryings o=FppVert(v);o.positionCS=TransformWorldToHClip(ApplyShadowBias(o.world,o.normal,_MainLightPosition.xyz));
#if UNITY_REVERSED_Z
 o.positionCS.z=min(o.positionCS.z,o.positionCS.w*UNITY_NEAR_CLIP_VALUE);
#else
 o.positionCS.z=max(o.positionCS.z,o.positionCS.w*UNITY_NEAR_CLIP_VALUE);
#endif
 return o;
}
half4 FppDepth(FppVaryings i):SV_Target{FppClip(i.world);return 0;}
half4 FppNormals(FppVaryings i):SV_Target{FppClip(i.world);return half4(normalize(i.normal),0);}
