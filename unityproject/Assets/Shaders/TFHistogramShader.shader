// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "VolumeRendering/TFHistogramShader"
{
    Properties
    {
        _HistTex("Histogram Texture", 2D) = "white" {}
        _Slider1("Slider 1", range(0.0, 1.0)) = 0.0
        _Slider2("Slider 2", range(0.0, 1.0)) = 0.0
        //_TFTex("Transfer Function Texture", 2D) = "white" {}
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
                //float4 relVert : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _HistTex;
            uniform float _Slider1;
            uniform float _Slider2;

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
                float histY = tex2D(_HistTex, float2(density, 0.0f)).r;
                float4 histCol = histY > i.uv.y ? float4(1.0f, 1.0f, 1.0f, 1.0f) : float4(0.1f, 0.1f, 0.1f, 1.0f);

                if (density<_Slider1 || density > _Slider2) {
                    histCol.rgb = histCol.rgb * 0.4f;
                }

                return histCol;
            }
             ENDCG
         }
    }
}
