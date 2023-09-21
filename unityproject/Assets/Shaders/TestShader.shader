Shader "Custom/TestShader"
{
	Properties
	{

	}
	SubShader
	{
		Tags { "RenderType" = "Transparent" }
		LOD 200
		Blend SrcAlpha OneMinusSrcAlpha
		Pass{
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma vertex vert
		#pragma fragment frag

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0


		struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

	struct v2f
	{
		float2 uv : TEXCOORD0;
		float4 vertex : SV_POSITION;
	};

v2f vert(appdata v)
{
	v2f o;

	o.vertex = UnityObjectToClipPos(v.vertex);
	o.uv = v.uv;
	return o;
}
	fixed4 frag(v2f i) : SV_Target
{
	return float4(1.0f,1.0f,0.0f,0.0f);
	}
	ENDCG
	}
	}
}
