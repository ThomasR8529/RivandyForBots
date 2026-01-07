// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Distort"
{
	Properties {
		_NoiseTex ("Noise Texture (RG)", 2D) = "white" {}
		_MainTex ("Alpha (A)", 2D) = "white" {}
		_HeatTime  ("Heat Time", range (0,1)) = 0.1
		_HeatForce  ("Heat Force", range (0,0.1)) = 0.008
	}

	SubShader {
		Tags { "Queue" = "Transparent+1" "RenderType" = "Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha
		//AlphaTest Greater .01
		Cull Off Lighting Off ZWrite Off
		Pass {
			Tags { "LightMode" = "UniversalForward" }
			
			HLSLPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma fragmentoption ARB_precision_hint_fastest

				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

				struct appdata_t {
					float4 vertex : POSITION;
					float4 color : COLOR;
					float2 texcoord: TEXCOORD0;
				};

				struct v2f {
					float4 vertex : POSITION;
					float4 uvgrab : TEXCOORD0;
					float2 uvmain : TEXCOORD1;
				};


				float4 _MainTex_ST;
				sampler2D _MainTex;


				float _HeatForce;
				float _HeatTime;
				float4 _NoiseTex_ST;
				sampler2D _NoiseTex;
				SAMPLER(_CameraOpaqueTexture);

				v2f vert (appdata_t v)
				{
					v2f o;
					VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);
					o.vertex = vertexInput.positionCS;

					#if UNITY_UV_STARTS_AT_TOP
					float scale = -1.0;
					#else
					float scale = 1.0;
					#endif

					o.uvgrab.xy = (float2(o.vertex.x, o.vertex.y*scale) + o.vertex.w) * 0.5;
					o.uvgrab.zw = o.vertex.zw;
					o.uvmain = TRANSFORM_TEX( v.texcoord, _MainTex );


					//o.uvmain = TRANSFORM_TEX( v.texcoord, _NoiseTex);
					return o;
				}

				half4 frag( v2f i ) : SV_Target
				{
					//noise effect
					/*half4 offsetColor1 = tex2D(_NoiseTex, i.uvmain + _Time.xz*_HeatTime);
					half4 offsetColor2 = tex2D(_NoiseTex, i.uvmain - _Time.yx*_HeatTime);*/
				


					half4 offsetColor1 = tex2D(_NoiseTex, i.uvmain + _Time.xz*_HeatTime);
				    half4 offsetColor2 = tex2D(_NoiseTex, i.uvmain - _Time.yx*_HeatTime);

					half distortX = ((offsetColor1.r + offsetColor2.r) - 1) * _HeatForce;
					half distorty = ((offsetColor1.g + offsetColor2.g) - 1) * _HeatForce;

					//i.uvgrab.x += ((offsetColor1.r + offsetColor2.r) - 1) * _HeatForce;
					//i.uvgrab.y += ((offsetColor1.g + offsetColor2.g) - 1) * _HeatForce;

					//half2 screenUV = (i.vertex.xy / _ScreenParams.xy)+ float2(distortX, distorty);
					//half4 col = tex2D(_CameraOpaqueTexture, screenUV);


					half2 screenUV = (i.vertex.xy / _ScreenParams.xy)+ float2(distortX, distorty);

					half4 col = tex2D( _CameraOpaqueTexture, screenUV);

					half4 tint = tex2D( _MainTex, i.uvmain);
				


					



					col.a = 1.0f;

					return col * tint;
				}
			ENDHLSL
		}
	}
}
