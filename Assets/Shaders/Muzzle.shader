Shader "FPP/Muzzle" {
 Properties { _Color("Color", Color) = (1,0.75,0.2,1) }
 SubShader {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" }
  Blend SrcAlpha One
  ZWrite Off Cull Off
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct data { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
   struct output { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
   float4 _Color;
   output vert(data v) { output o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o; }
   fixed4 frag(output i):SV_Target {
    float2 p=(i.uv-.5)*2;
    float center=saturate(1-length(p)*1.5);
    float star=saturate(1-min(abs(p.x)*6+abs(p.y),abs(p.y)*6+abs(p.x)));
    return fixed4(_Color.rgb,saturate(center+star)*_Color.a);
   }
   ENDCG
  }
 }
}
