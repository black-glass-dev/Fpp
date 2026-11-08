Shader "Hidden/FPP/Actual Canvas Composite"
{
    Properties { _MainTex("Actual Canvas camera render",2D)="black"{} }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            ZTest Always ZWrite Off Cull Off
            // Camera rendering into transparent black stores premultiplied RGB.
            Blend One OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            fixed4 frag(v2f_img input):SV_Target { return tex2D(_MainTex,input.uv); }
            ENDCG
        }
    }
}
