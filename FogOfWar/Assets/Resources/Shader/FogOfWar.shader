Shader "Hidden/Scene/FogOfWar"
{
	Properties
	{
		_Color ("Color", Color) = (1,1,1,1)
		_RecordTexture("Record Tex", 2D) = "black" {}
		_FOWVisionRadiusesTexture("Radius Tex", 2D) = "white" {}
		_FOWPositionsTexture("Position Tex", 2D) = "white" {}
		_ActualUnitsCount("_ActualUnitsCount", float) = 0
		_MaxUnits("_MaxUnits", float) = 1024
	}

	CGINCLUDE
		#include "UnityCG.cginc"
		uniform float _FogStrength;

		uniform sampler2D _CDepthTexture;
		uniform float4x4  _CCameraToWorld;
		uniform float4x4 _CCameraInvProjection;
		// x = 1 or -1 (-1 if projection is flipped)
		// y = near plane
		// z = far plane
		// w = 1/far plane
		uniform float4  _CProjectionParams; 
		
		sampler2D _FOWVisionRadiusesTexture;
        sampler2D _FOWPositionsTexture;
        sampler2D _RecordTexture;
        float _ActualUnitsCount;
        float _MaxUnits;
        float4 _StartPos;

        struct appdata
        {
        	float4 positionOS : POSITION;
        };
        
		struct v2f
		{
			float4 positionCS : SV_POSITION;
			float4 screenPos  : TEXCOORD0;
		};

		float4 _Color;

		v2f vert(appdata input)
		{
			v2f o = (v2f)0;
			o.positionCS = UnityObjectToClipPos(input.positionOS);
			o.screenPos = ComputeScreenPos(o.positionCS);
			return o;
		}

		float3  GetWorldPos(v2f i)
		{
			float4 screenPos = i.screenPos;
			float4 screenPosNorm = screenPos / screenPos.w;
			float depth = SAMPLE_DEPTH_TEXTURE(_CDepthTexture, screenPosNorm.xy);

			#ifdef UNITY_REVERSED_Z
				depth = 1.0 - depth;
			#endif

			//transform screen position to world position; 
			float4 projectPos = float4(screenPosNorm.x, screenPosNorm.y, depth, 1.0);
			projectPos.xyz = projectPos.xyz * 2 - 1.0;
			float4 camPos = mul(_CCameraInvProjection, projectPos);
			camPos.xyz /= camPos.w; 
			camPos.z *= -1; 
			camPos.w = 1.0f;
				
			float4 worldPos  = mul(_CCameraToWorld, camPos);

			return worldPos.xyz;
		}

		float4 frag(v2f input) : SV_TARGET0
		{
            float alpha = _FogStrength;
			float2 textureResolution = float2(_MaxUnits, 1);
			float3  pos = GetWorldPos(input);
			pos.y = 0;

			float2 worldUV = (pos.xz - _StartPos.xy)/_StartPos.zw;
			half record = tex2D(_RecordTexture, worldUV).r * 0.5;
			// if (record > 0.1) alpha = 1-record;

			bool fineded = false;
			UNITY_LOOP 
            for (int i = 0; i < _ActualUnitsCount; i++)
            {
                float2 unitPixelCenterPos = float2(i, 0) + 0.5; 
                float3 unitPosition = tex2D(_FOWPositionsTexture, unitPixelCenterPos / textureResolution).rgb * 1024; //_Positions[i].xyz;
                unitPosition.y = 0;
                float visionRadius = tex2D(_FOWVisionRadiusesTexture, unitPixelCenterPos / textureResolution).r * 512;
                float distanceToUnit = distance(unitPosition, pos);
                 
                UNITY_BRANCH
                if (distanceToUnit < visionRadius)
                {
                    if (!fineded)
                    {
                    	fineded = true;
                    	alpha = 0;
                    }

					float2 uv = (unitPosition.xz - _StartPos.xy)/_StartPos.zw;
                    float2 dir = normalize(worldUV - uv);
                    float2 len  = visionRadius.xx / _StartPos.zw;
                    float offset = len.x;
                    uv = uv + dir * offset;
                    float record2 = tex2D(_RecordTexture, uv).r*0.5f;
                    record2 = (1-record2);
                    float ratio = distanceToUnit / visionRadius;
                    alpha = clamp((1-ratio) * alpha + record2 * ratio, 0, record2);
                }
            }
            if (!fineded) alpha = 1-record;
            alpha = clamp(alpha*1.3f, 0, _FogStrength);

            float4  c = 0;
			c.a = alpha;

			return c; 
		}
	ENDCG

	SubShader
	{
		Tags {  "Queue"="Transparent"  "RenderType"="Opaque" }
		LOD 200
		ZTest Always
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
		Cull Off

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
