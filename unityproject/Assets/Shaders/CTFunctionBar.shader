// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "VolumeRendering/CTFunctionBar"
{
	Properties
	{
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

			sampler2D _TFTex;

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
				float4 col;
				if (i.uv.x < 0.002 || i.uv.x>0.998) col = 1;
				else if (i.uv.y < 0.02 || i.uv.y>0.98) col = 1;
				else  col = tex2D(_TFTex, float2(i.uv.x, 0.0f));
				col.a = 1.0f;
				return col;
			}
			 ENDCG
		 }
	}
}
