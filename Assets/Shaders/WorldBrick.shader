Shader "FPP/World Brick" {
 Properties {_MainTex("Brick",2D)="white"{} _Color("Tint",Color)=(1,1,1,1)}
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
