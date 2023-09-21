Shader "Unlit/RedBorderShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}
		SubShader
	{
		Tags { "Queue"="Transparent" "RenderType" = "Transparent" }
		LOD 100
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog

			#include "UnityCG.cginc"


			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;

				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			v2f vert(appdata v)
			{
				v2f o;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);

				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = fixed4(1.0f,0.0f,0.0f,0.3f);
			if ((i.uv.x < 0.01f && i.uv.x > 0.0f) || (i.uv.x < 1.0f && i.uv.x > 0.99f) || (i.uv.y < 0.01f && i.uv.y > 0.0f) || (i.uv.y < 1.0f && i.uv.y > 0.99f)) {
				col.a = 1.0f;
			}
			if ((i.uv.x < 1.01f && i.uv.x > 1.0f) || (i.uv.x < 2.0f && i.uv.x > 1.99f) || (i.uv.y < 1.01f && i.uv.y > 1.0f) || (i.uv.y < 2.0f && i.uv.y > 1.99f)) {
				col.a = 1.0f;
			}

			// apply fog
			UNITY_APPLY_FOG(i.fogCoord, col);
			return col;
		}
		ENDCG
	}
	}
}
