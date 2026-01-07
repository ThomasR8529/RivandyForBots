// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Dissolve"
{
    Properties {
        _Color ("Main Color", Color) = (1,1,1,1)
        _Amount ("Amount", Range (0, 1)) = 0.5
        _StartAmount("StartAmount", float) = 0.1
        _Illuminate ("Illuminate", Range (0, 1)) = 0.5
        _Tile("Tile", float) = 1
        _DissColor ("DissColor", Color) = (1,1,1,1)
        _ColorAnimate ("ColorAnimate", vector) = (1,1,1,1)
        _MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
        _DissolveSrc ("DissolveSrc", 2D) = "white" {}

    }

    SubShader {
        Tags { "Queue" = "Transparent+1" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        //AlphaTest Greater .01
        //Cull Off Lighting Off ZWrite Off
        Pass {
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma fragmentoption ARB_precision_hint_fastest

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"


                
                sampler2D _MainTex;
                float4 _MainTex_ST;  
                sampler2D _DissolveSrc;

                float4 _Color;
                half4 _DissColor;
                half _Amount;
                static half3 Color = float3(1,1,1);
                half4 _ColorAnimate;
                half _Illuminate;
                half _Tile;
                half _StartAmount;








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

                    o.uvmain = TRANSFORM_TEX( v.texcoord, _MainTex);
                    return o;
                }

                half4 frag( v2f i ) : SV_Target
                {
                    float4 tex = tex2D(_MainTex, i.uvmain);


                    //noise effect
                    /*half4 offsetColor1 = tex2D(_NoiseTex, i.uvmain + _Time.xz*_HeatTime);
                    half4 offsetColor2 = tex2D(_NoiseTex, i.uvmain - _Time.yx*_HeatTime);
                    half distortX = ((offsetColor1.r + offsetColor2.r) - 1) * _HeatForce;
                    half distorty = ((offsetColor1.g + offsetColor2.g) - 1) * _HeatForce;

                    half2 screenUV = (i.vertex.xy / _ScreenParams.xy)+ float2(distortX, distorty);

                    half4 col = tex2D(_CameraOpaqueTexture, screenUV);
                    col.a = 1.0f;*/



                    
                    float2 t = i.uvmain / _Tile;
                    float ClipTex = tex2D(_DissolveSrc, t).x;
                    float ClipAmount = ClipTex - _Amount;
                    float Clip = 0;
                    if (_Amount > 0)
                    {
                        if (ClipAmount <0)
                        {
                            Clip = 1;
                            //clip(-0.1);
                        }
                         else
                         {
                            if (ClipAmount < _StartAmount)
                            {
                                if (_ColorAnimate.x == 0)
                                    Color.x = _DissColor.x;
                                else
                                    Color.x = ClipAmount/_StartAmount;
                  
                                if (_ColorAnimate.y == 0)
                                    Color.y = _DissColor.y;
                                else
                                    Color.y = ClipAmount/_StartAmount;
                  
                                if (_ColorAnimate.z == 0)
                                    Color.z = _DissColor.z;
                                else
                                    Color.z = ClipAmount/_StartAmount;



                                tex.rgb  = (tex.rgb*_Color.rgb *((Color.x+Color.y+Color.z))*Color*((Color.x+Color.y+Color.z)))/(1 - _Illuminate);
                            }
                        }
                    }        
                    if (Clip == 1)
                    {
                        clip(-0.1);
                    }

                    tex.a = tex.a * _Color.a;

                    return tex;
                }
            ENDHLSL
        }
    }
}
