Shader "Hidden/Scene/RecordFogInfo"
{
	Properties
	{
		_RecordTexture ("Albedo (RGB)", 2D) = "black" {}
		_RecordVisionRadiusesTexture("Radius Tex", 2D) = "white" {}
		_RecordPositionsTexture("Position Tex", 2D) = "white" {}
		_ActualUnitsCount("_ActualUnitsCount", float) = 0
		_MaxUnits("_MaxUnits", float) = 1024
	}

	CGINCLUDE
		#include "UnityCG.cginc"

		sampler2D _RecordVisionRadiusesTexture;
        sampler2D _RecordPositionsTexture;
        sampler2D _RecordTexture;
        float _ActualUnitsCount;
        float _MaxUnits;
       	float4 _StartPos;

        struct appdata
        {
        	float2 uv : TEXCOORD0;
        	float4 positionOS : POSITION;
        };
        
		struct v2f
		{
			float2 uv : TEXCOORD0;
			float4 positionCS : SV_POSITION;
		};

		float blur(float2 uv,  float alpha)
		{
			float eps = 0.001f;
			float record = alpha;
			#if defined(PERFORMANCE_HIGH_AA)
				record += tex2D(_RecordTexture, uv + float2(0, 1)*eps).r;
				record += tex2D(_RecordTexture, uv + float2(0, -1)*eps).r;
				record += tex2D(_RecordTexture, uv + float2(1, 0)*eps).r;
				record += tex2D(_RecordTexture, uv + float2(1, -1)*eps).r;
				record += tex2D(_RecordTexture, uv + float2(1, 1)*eps).r;
				record += tex2D(_RecordTexture, uv + float2(-1, 0)*eps).r;
				record += tex2D(_RecordTexture, uv + float2(-1, 1)*eps).r;
				record += tex2D(_RecordTexture, uv + float2(-1, -1)*eps).r;
				return record * 0.11112f;	// record / 9.0f;			
			#else
				record += tex2D(_RecordTexture, uv + float2(0, 1)*eps).r;
				record += tex2D(_RecordTexture, uv + float2(0, -1)*eps).r;	
				record += tex2D(_RecordTexture, uv + float2(1, 0)*eps).r;
				record += tex2D(_RecordTexture, uv + float2(-1, 0)*eps).r;
				return record * 0.2f; // record / 5f;
			#endif
			return record;
		}

		v2f vert(appdata input)
		{
			v2f o = (v2f)0;
			o.uv = input.uv;
			o.positionCS = UnityObjectToClipPos(input.positionOS);
			return o;
		}

		float4 frag(v2f input) : SV_TARGET0
		{
			float2 textureResolution = float2(_MaxUnits, 1);
			half record = tex2D(_RecordTexture, input.uv).r;
			float alpha = 1 - record;

			if (alpha < 0.1) 
			{
				return (1-alpha).xxxx;
			}

			UNITY_LOOP 
            for (int i = 0; i < _ActualUnitsCount && alpha > 0; ++i)
            {
                float2 unitPixelCenterPos = float2(i, 0) + 0.5;
                float2 unitPosition = tex2D(_RecordPositionsTexture, unitPixelCenterPos / textureResolution).rb;
                float visionRadius = tex2D(_RecordVisionRadiusesTexture, unitPixelCenterPos / textureResolution).r * 100;
                float2 ddxy = input.uv * float2(_StartPos.zw) - unitPosition * float2(_StartPos.zw);
                float distanceToUnit = ddxy.x*ddxy.x + ddxy.y*ddxy.y;

                if (distanceToUnit < visionRadius*visionRadius)
                {
                    alpha = 0;
                }
            }
            alpha = 1-alpha;

            alpha = blur(input.uv, alpha);

			return alpha.xxxx; 
		}
	ENDCG

	SubShader
	{
		Tags {  "Queue"="Transparent"  "RenderType"="Opaque" }
		LOD 200
		Cull Off
		ZTest Always
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma vertex vert
			#pragma fragment frag

			ENDCG
		}
	}
	FallBack "Diffuse"
}
