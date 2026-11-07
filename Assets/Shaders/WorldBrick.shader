Shader "FPP/World Brick" {
 Properties {_MainTex("Brick",2D)="white"{} _Color("Tint",Color)=(1,1,1,1)}
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
  Cull Back
  HLSLINCLUDE
  #define FPP_BRICK 1
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
  Pass {
   Tags {"LightMode"="ForwardBase"}
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct input {float4 vertex:POSITION;float3 normal:NORMAL;};
   struct output {float4 pos:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;};
   sampler2D _MainTex;fixed4 _Color;
   output vert(input v){output o;o.pos=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.normal=UnityObjectToWorldNormal(v.normal);return o;}
   fixed4 frag(output i):SV_Target {float3 n=normalize(i.normal);float2 uv=abs(n.x)>.5?i.world.zy:i.world.xy;uv.y*=1.6;fixed3 c=tex2D(_MainTex,uv).rgb*_Color.rgb;return fixed4(c*(.65+.35*saturate(dot(n,normalize(_WorldSpaceLightPos0.xyz)))),1);}
   ENDCG
  }
 }
}
