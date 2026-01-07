// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'


Shader "Custom/Distortion Bump"
{
	Properties {
		

		_BumpAmt  ("Distortion", range (0,128)) = 10
		_BumpMap ("Normalmap", 2D) = "bump" {}
	}

	SubShader {
		Tags { "Queue" = "Transparent-1" "RenderType" = "Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off
		Cull Off
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

				float _BumpAmt;
				float4 _BumpMap_ST;
				sampler2D _BumpMap;



				float _HeatForce;
				float _HeatTime;

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
					o.uvmain = TRANSFORM_TEX( v.texcoord, _BumpMap );

					return o;
				}

				half4 frag( v2f i ) : SV_Target
				{
					_HeatForce = 0.0059;
					_HeatTime = 0.004;
					
					half2 offsetColor1 =UnpackNormal(tex2D(_BumpMap, i.uvmain + _Time.xz*_HeatTime)).rg;
					half2 offsetColor2 = UnpackNormal(tex2D(_BumpMap, i.uvmain - _Time.yx*_HeatTime)).rg;
					half distortX = ((offsetColor1.r + offsetColor2.r) - 1) * _HeatForce;
					half distorty = ((offsetColor1.g + offsetColor2.g) - 1) * _HeatForce;

					half2 screenUV = (i.vertex.xy / _ScreenParams.xy)+ float2(distortX, distorty);
					half4 col = tex2D(_CameraOpaqueTexture, screenUV);
					col.a = 0;



					/*half2 bump = UnpackNormal(tex2D( _BumpMap, i.uvmain )).rg;
					float2 offset = bump * _BumpAmt * _ScreenParams.xy;
					i.uvgrab.xy = offset * i.uvgrab.z + i.uvgrab.xy;*/
	
					//col = tex2Dproj( _CameraOpaqueTexture, screenUV);





					return col;
				}
			ENDHLSL
		}
	}

	
}