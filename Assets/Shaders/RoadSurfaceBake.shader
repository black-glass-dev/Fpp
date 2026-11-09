Shader "Hidden/FPP/Road Surface Bake"
{
 Properties {_Base("Source albedo",2D)="white"{} _Normal("Source normal",2D)="bump"{} _Roughness("Source roughness",2D)="white"{}}
 SubShader {Pass {Cull Off ZWrite Off ZTest Always
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _Base,_Normal,_Roughness;float4 _Rect,_Height,_Tint;float _Mode,_HasNormal,_RoughnessFactor;
 struct A {float4 p:POSITION;float3 n:NORMAL;float4 t:TANGENT;float2 uv:TEXCOORD0;};
 struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float3 n:TEXCOORD1;float3 t:TEXCOORD2;float3 b:TEXCOORD3;};
 V vert(A a){V o;float3 w=mul(unity_ObjectToWorld,a.p).xyz;float2 xy=(w.xz-_Rect.xy)/_Rect.zw*2-1;
 // Bake always uses a private non-reversed depth comparison: high surfaces win.
 o.p=float4(xy,lerp(.95,.05,saturate((w.y-_Height.x)/_Height.y)),1);o.uv=a.uv;o.n=UnityObjectToWorldNormal(a.n);o.t=UnityObjectToWorldDir(a.t.xyz);o.b=cross(o.n,o.t)*a.t.w*unity_WorldTransformParams.w;return o;}
 float4 frag(V i):SV_Target {float3 n=normalize(i.n);clip(n.y-.05);
 if(_Mode<.5)return float4(tex2D(_Base,i.uv).rgb*_Tint.rgb,1-tex2D(_Roughness,i.uv).g*_RoughnessFactor);
 if(_HasNormal>.5){float3 detail=UnpackNormal(tex2D(_Normal,i.uv));n=normalize(normalize(i.t)*detail.x+normalize(i.b)*detail.y+n*detail.z);}
 return float4(float3(n.x,n.z,n.y)*.5+.5,1);}
 ENDHLSL
 }}
}
