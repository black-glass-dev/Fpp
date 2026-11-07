Shader "FPP/Layered Ground" {
 Properties {
  _MainTex("Grass albedo",2D)="white"{} _SoilTex("Soil albedo",2D)="white"{}
  _BumpMap("Grass normal",2D)="bump"{} _SoilNormal("Soil normal",2D)="bump"{}
  _Road("Compacted road",Range(0,1))=0 _DetailScale("Tiles per metre",Float)=.55
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
  Cull Back
  HLSLINCLUDE
  #define FPP_GROUND 1
  #include "FppUrpWorld.hlsl"
  ENDHLSL
  Pass {
   Name "UniversalForward" Tags {"LightMode"="UniversalForward"}
   HLSLPROGRAM
   #pragma target 3.5
   #pragma target 4.5 _FORWARD_PLUS
   #pragma vertex FppVert
   #pragma fragment FppFragment
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile _ _ADDITIONAL_LIGHTS
   #pragma multi_compile _ _FORWARD_PLUS
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
   #pragma multi_compile_fog
   ENDHLSL
  }
  Pass {
   Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"} ZWrite On ColorMask 0
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex FppShadowVert
   #pragma fragment FppDepth
   ENDHLSL
  }
  Pass {
   Name "DepthOnly" Tags {"LightMode"="DepthOnly"} ZWrite On ColorMask 0
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex FppVert
   #pragma fragment FppDepth
   ENDHLSL
  }
  Pass {
   Name "DepthNormals" Tags {"LightMode"="DepthNormals"} ZWrite On
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex FppVert
   #pragma fragment FppNormals
   ENDHLSL
  }
 }
 SubShader {
  Tags {"RenderType"="Opaque"}
  LOD 250
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows
  #pragma target 3.0
  sampler2D _MainTex,_SoilTex,_BumpMap,_SoilNormal;
  half _Road,_DetailScale;
  struct Input {float3 worldPos;};
  float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
  float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
  void surf(Input IN,inout SurfaceOutputStandard o){
   float2 p=IN.worldPos.xz;float2 uv=p*_DetailScale;
   half macro=noise(p*.18)*.65+noise(p*.57)*.35;
   half lane=1-smoothstep(4.1,5.0,abs(p.x));
   half yard=(1-smoothstep(15.8,17.5,abs(p.x)))*(1-smoothstep(4.6,6,abs(p.y)));
   half road=_Road*max(lane,yard);
   half soil=lerp(smoothstep(.38,.72,macro)*.78,1,road);
   fixed3 grass=tex2D(_MainTex,uv).rgb;
   fixed3 dirt=tex2D(_SoilTex,uv*1.13+float2(.32,.71)).rgb;
   half ruts=pow(saturate(1-abs(abs(p.x)-2.05)*2.6),3)*road;
   half variation=lerp(.78,1.16,noise(p*.075+17));
   o.Albedo=lerp(grass,dirt,soil)*variation*(1-ruts*.17);
   o.Normal=normalize(lerp(UnpackNormal(tex2D(_BumpMap,uv)),UnpackNormal(tex2D(_SoilNormal,uv*1.13+float2(.32,.71))),soil));
   o.Normal.xy*=.65;o.Normal=normalize(o.Normal);
   o.Metallic=0;o.Smoothness=lerp(.08,.16,ruts);o.Occlusion=.93;
  }
  ENDCG
 }
 FallBack "Diffuse"
}
