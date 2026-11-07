Shader "FPP/Mobile Scan"
{
    Properties { _MainTex("Scan albedo",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) _TrimBelow("World height trim",Float)=-10000 _ScanGroundBelow("Scan ground trim height",Float)=-10000 _Entry("Front passage cut",Vector)=(0,10000,-10000,-10000) _InteriorRect("Interior removal rectangle",Vector)=(10000,10000,-10000,-10000) _InteriorTop("Interior replacement height",Float)=-10000 _SkirtRect("Scan skirt rectangle",Vector)=(-10000,-10000,10000,10000) }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        // Photogrammetry contains thin/open surfaces with inconsistent facing.
        Cull Off
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;};
            struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 normal:TEXCOORD1;float worldY:TEXCOORD2;float2 worldXZ:TEXCOORD3;};
            sampler2D _MainTex;float4 _MainTex_ST;fixed4 _Color;float _TrimBelow;float _ScanGroundBelow;float4 _SkirtRect;float4 _InteriorRect;float _InteriorTop;float4 _Entry;
            v2f vert(appdata v)
            {
                v2f o;float3 world=mul(unity_ObjectToWorld,v.vertex).xyz;
                o.normal=UnityObjectToWorldNormal(v.normal);
                o.pos=UnityWorldToClipPos(world);o.uv=TRANSFORM_TEX(v.uv,_MainTex);
                o.worldY=world.y;o.worldXZ=world.xz;return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                clip(i.worldY-_TrimBelow);
                if(i.worldY<_ScanGroundBelow)
                    clip(min(min(i.worldXZ.x-_SkirtRect.x,_SkirtRect.z-i.worldXZ.x),min(i.worldXZ.y-_SkirtRect.y,_SkirtRect.w-i.worldXZ.y)));
                if(i.worldY<_InteriorTop && i.worldXZ.x>_InteriorRect.x && i.worldXZ.x<_InteriorRect.z && i.worldXZ.y>_InteriorRect.y && i.worldXZ.y<_InteriorRect.w)clip(-1);
                float doorX=i.worldXZ.x-_Entry.x;
                if(i.worldY<_Entry.w && i.worldXZ.y>_Entry.y && i.worldXZ.y<_Entry.z && min(abs(doorX),min(abs(doorX-3.55),abs(doorX+3.55)))<.88)clip(-1);
                fixed4 c=tex2D(_MainTex,i.uv)*_Color;
                fixed lighting=.6+.4*abs(dot(normalize(i.normal),normalize(_WorldSpaceLightPos0.xyz)));
                return fixed4(c.rgb*lighting,1);
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
