// 실제 해초 OBJ의 윗부분만 굽힙니다. 바닥 바위는 높이 가중치가 0이므로 고정됩니다.
// Unity URP 및 single-pass stereo용 인스턴스/눈 정보를 전달합니다. 실제 Quest 검증은 별도로 필요합니다.
Shader "HCI/SeaweedSway"
{
    Properties
    {
        _BaseMap("Original Base Color", 2D) = "white" {}
        _BaseColor("Tint", Color) = (1,1,1,1)
        _Sway("Tip Bend (Model Metres)", Range(0,0.15)) = 0.07
        _Speed("Wave Speed", Range(0,3)) = 0.8
        _FixedBase("Fixed Rock Height", Float) = 0.16
        _PlantHeight("Plant Height Above Rock", Float) = 1.04
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "UnderwaterGrass"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _Sway;
                float _Speed;
                float _FixedBase;
                float _PlantHeight;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half fog : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 pos = input.positionOS.xyz;
                float height = saturate((pos.y - _FixedBase) / max(0.01, _PlantHeight));
                float3 root = TransformObjectToWorld(float3(0,0,0));
                float phase = _Time.y * _Speed + root.x * 0.37 + root.z * 0.23;
                float bend = _Sway * height * height;
                pos.x += sin(phase + pos.y * 1.2) * bend;
                pos.z += cos(phase * 0.83 + pos.y) * bend * 0.6;
                output.positionCS = TransformObjectToHClip(pos);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half3 normal = normalize(input.normalWS);
                Light sun = GetMainLight();
                half3 illumination = SampleSH(normal) + sun.color * (0.25h + 0.75h * abs(dot(normal, sun.direction)));
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                color.rgb = MixFog(color.rgb * max(illumination, 0.25h), input.fog);
                return half4(color.rgb, 1);
            }
            ENDHLSL
        }
    }
}
