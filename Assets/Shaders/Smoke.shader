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
   float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float noise(float2 p){float2 q=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(q),hash(q+float2(1,0)),f.x),lerp(hash(q+float2(0,1)),hash(q+1),f.x),f.y);}
   fixed4 frag(output i):SV_Target {float2 p=(i.uv-.5)*2;float edge=saturate(1-dot(p,p));float billow=noise(i.uv*5)*.65+noise(i.uv*11)*.35;float alpha=edge*edge*smoothstep(.12,.72,billow);return fixed4(i.color.rgb*_Color.rgb,i.color.a*_Color.a*alpha);}

   ENDCG
  }
 }
}
