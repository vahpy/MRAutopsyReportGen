// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "VolumeRendering/TFHistogramShaderNew"
{
    Properties
    {
        _HistTex("Histogram Texture", 2D) = "white" {}
        _TFTex("Transfer Function Texture", 2D) = "white" {}
    }
        SubShader
    {
        Tags { "Queue" = "Transparent" }
        LOD 100
        Cull Off

        //Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID 	// Line to add
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _HistTex;
            sampler2D _TFTex;
            sampler2D _CPTex;

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float density = i.uv.x;
                float value = i.uv.y;
                float4 col;
                float cpAlpha = tex2D(_CPTex, float2(density, value)).a;
                if (cpAlpha > 0.0f) {
                    col = float4(1, 1, 1, 1);
                }
                else {
                    float histY = tex2D(_HistTex, float2(density, 0.0f)).r;
                    float4 tfCol = tex2D(_TFTex, float2(density, 0.0f));
                    float4 histCol = histY > value ? float4(1.0f, 1.0f, 1.0f, 1.0f) : float4(0.0f, 0.0f, 0.0f, 0.0f);

                    float alpha = tfCol.a;
                    if (value > alpha)
                        tfCol.a = 0.0f;

                    col = histCol * 0.5f + tfCol * 0.7f;
                }
                return col;
            }
             ENDCG
         }
    }
}
