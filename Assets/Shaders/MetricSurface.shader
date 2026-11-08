Shader "FPP/Metric Surface Lit"
{
    Properties { _MainTex("Original detail albedo",2D)="white"{} _BumpMap("Original detail normal",2D)="bump"{} _DetailScale("Tiles per metre",Float)=1 _Smoothness("Smoothness",Range(0,1))=.18 _Metallic("Metallic",Range(0,1))=0 _Color("Tint",Color)=(1,1,1,1) _TrimBelow("World height trim",Float)=-10000 _ScanGroundBelow("Scan ground trim height",Float)=-10000 _Entry("Front passage cut",Vector)=(0,10000,-10000,-10000) _InteriorRect("Interior removal rectangle",Vector)=(10000,10000,-10000,-10000) _InteriorTop("Interior replacement height",Float)=-10000 _SkirtRect("Scan skirt rectangle",Vector)=(-10000,-10000,10000,10000) }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
  Cull Off
  HLSLINCLUDE
  #define FPP_METRIC_DETAIL 1
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
}
