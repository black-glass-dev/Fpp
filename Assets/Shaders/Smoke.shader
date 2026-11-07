Shader "FPP/Smoke" {
 Properties { _Color("Tint",Color)=(1,1,1,1) }
 SubShader {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" }
  Blend SrcAlpha OneMinusSrcAlpha
  ZWrite Off Cull Off
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   fixed4 _Color;
   struct data {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
   struct output {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
   output vert(data v){output o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
   fixed4 frag(output i):SV_Target {float2 p=(i.uv-.5)*2;float alpha=saturate(1-dot(p,p));alpha*=alpha*(.75+.25*sin(p.x*13+p.y*9));return fixed4(i.color.rgb*_Color.rgb,i.color.a*_Color.a*alpha);}
   ENDCG
  }
 }
}
