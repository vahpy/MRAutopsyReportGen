Shader "Unlit/BoxOutline" {
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Color", Color) = (1, 0, 0, 1)
        _OutlineWidth("Outline Width", Range(0, 1)) = 0.1
    }
        SubShader
        {
            Tags { "RenderType" = "Transparent" "Queue" = "Transparent"}
            LOD 100

            Pass
            {
                Blend SrcAlpha OneMinusSrcAlpha
                CGPROGRAM

                #pragma vertex vert
                #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                UNITY_FOG_COORDS(2)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _OutlineWidth;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 col = _Color;
                col.a = 0.0f;
                /*float2 worldScale = float2(unity_ObjectToWorld._11, unity_ObjectToWorld._22);
                float2 i.uv = i.worldPos.xz / worldScale;*/

                // sample the texture
                if (i.uv.x < _OutlineWidth) {
                    col.a = lerp(0,1, 1 - i.uv.x / _OutlineWidth);
                }
                if (i.uv.y < _OutlineWidth) {
                    col.a = max(col.a, lerp(0, 1, 1 - i.uv.y / _OutlineWidth));
                }
                if (i.uv.x > 1 - _OutlineWidth) {
                    col.a = max(col.a, lerp(0, 1, (i.uv.x - (1 - _OutlineWidth)) / _OutlineWidth));
                }
                if (i.uv.y > 1 - _OutlineWidth) {
                    col.a = max(col.a, lerp(0, 1, (i.uv.y - (1 - _OutlineWidth)) / _OutlineWidth));
                }
                col.a = max(0.1f, col.a);

                return col;
            }
            ENDCG
        }
        }
}