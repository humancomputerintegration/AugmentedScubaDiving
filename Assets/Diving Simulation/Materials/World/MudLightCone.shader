// 머드 입자만 그리는 셰이더다. 손전등이 가리키는 작은 빛길 외에는 기존 머드를 유지한다.
Shader "Scuba/Mud Light Cone" // 머드 전용 Material이 사용할 이름이다.
{
    Properties // Material에 저장할 입자 모양과 기본색이다.
    {
        _BaseMap("Mud Particle", 2D) = "white" {} // 원본 머드와 같은 부드러운 입자 그림을 쓴다.
        _BaseColor("Mud Tint", Color) = (1, 1, 1, 1) // ParticleSystem의 원래 갈색에 곱할 색이다.
        _ClearStrength("Light Clarity", Range(0, 1)) = 1 // 빛 한가운데의 머드는 겹친 장수와 관계없이 투명하게 만든다.
    }
    SubShader // 현재 프로젝트의 URP에서 사용하는 그리기 방법이다.
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" } // 원본처럼 반투명 입자 순서로 그린다.
        Pass // 추가 화면 복사 없이 입자를 한 번만 그린다.
        {
            Name "MudForward" // Frame Debugger에서 찾을 이름이다.
            Tags { "LightMode"="UniversalForward" } // URP의 전방 그리기에 참여한다.
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha // 원본 머드 Material과 같은 알파 섞기다.
            ZWrite Off // 투명한 입자가 뒤의 다른 입자를 깊이 값으로 막지 않는다.
            ZTest LEqual // 바위 같은 불투명 물체 뒤의 입자는 앞에 나타나지 않는다.
            Cull Back // 원본 머드와 같은 앞면 그리기를 사용한다.
            HLSLPROGRAM // 여기부터 그래픽 카드에서 실행하는 코드다.
            #pragma target 3.5 // Quest의 GLES3와 Vulkan에서 쓰는 VR 그리기 수준이다.
            #pragma vertex MudVertex // 입자의 점 위치를 계산할 함수다.
            #pragma fragment MudFragment // 입자의 픽셀 색과 투명도를 계산할 함수다.
            #pragma multi_compile_instancing // VR 한 번 그리기에서 두 눈의 인스턴스를 구분한다.
            #pragma multi_compile_fog // 원래 바다 안개 설정을 그대로 지원한다.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" // URP 좌표 변환과 VR 기능을 가져온다.
            TEXTURE2D(_BaseMap); // 원본 입자 그림이다.
            SAMPLER(sampler_BaseMap); // 그림 사이의 색을 부드럽게 읽는 방법이다.
            CBUFFER_START(UnityPerMaterial) // Material마다 따로 저장하는 값들이다.
                float4 _BaseMap_ST; // 입자 그림의 크기와 위치다.
                half4 _BaseColor; // 기본 색과 알파다.
                half _ClearStrength; // 빛길에서 줄일 머드의 양이다.
            CBUFFER_END // Material 값 묶음을 끝낸다.
            float4 _ScubaMudLeftStart; // 왼손의 월드 위치와 켜짐 값이다. Properties에 넣지 않아 C#의 공통 값을 받는다.
            float4 _ScubaMudRightStart; // 오른손의 월드 위치와 켜짐 값이다.
            float4 _ScubaMudLeftDirection; // 왼손 레이저 방향과 빛의 거리다.
            float4 _ScubaMudRightDirection; // 오른손 레이저 방향과 빛의 거리다.
            float4 _ScubaMudViewer; // 두 눈의 가운데 위치다. 두 눈이 같은 공간의 빛길을 보게 한다.
            struct Attributes // ParticleSystem이 보내는 점 하나의 자료다.
            {
                float4 positionOS : POSITION; // 입자의 점 위치다.
                half4 color : COLOR; // 기존 ParticleSystem이 계산한 갈색과 시간별 알파다.
                float2 uv : TEXCOORD0; // 입자 그림에서 읽을 위치다.
                UNITY_VERTEX_INPUT_INSTANCE_ID // 어느 눈과 물체의 점인지 받는다.
            };
            struct Varyings // 점 계산에서 픽셀 계산으로 전달하는 자료다.
            {
                float4 positionCS : SV_POSITION; // 현재 눈의 화면에 투영한 위치다.
                float3 positionWS : TEXCOORD0; // 같은 점의 실제 월드 위치다.
                float2 uv : TEXCOORD1; // 입자 그림에서 읽을 위치다.
                half4 color : COLOR; // 원본 입자 색이다.
                half fogFactor : TEXCOORD2; // 원래 바다 안개를 섞을 정도다.
                UNITY_VERTEX_OUTPUT_STEREO // 그릴 눈을 픽셀 계산에 전달한다.
            };
            Varyings MudVertex(Attributes input) // 입자 점 하나를 화면 좌표로 바꾼다.
            {
                UNITY_SETUP_INSTANCE_ID(input); // 좌표를 바꾸기 전에 현재 눈과 물체를 고른다.
                Varyings output = (Varyings)0; // 전달할 자료를 모두 초기화한다.
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output); // 왼눈과 오른눈의 그리기 대상을 기록한다.
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz); // 원뿔 검사에 필요한 월드 위치다.
                output.positionCS = TransformWorldToHClip(output.positionWS); // 현재 눈의 화면 위치다.
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap); // 원본 입자 그림의 크기와 위치를 적용한다.
                output.color = input.color * _BaseColor; // 원래 입자 색과 알파를 그대로 보존한다.
                output.fogFactor = ComputeFogFactor(output.positionCS.z); // 기존 바다 안개를 계산한다.
                return output; // 계산한 점 자료를 보낸다.
            }
            float ClearInBeam(float3 particlePositionWS, float4 handStart, float4 handDirection) // 입자 픽셀의 월드 위치를 받아 한 손전등이 이곳을 얼마나 투명하게 할지 정한다.
            {
                if (handStart.w < 0.5) return 0.0; // 꺼졌거나 추적되지 않는 손은 복잡한 원뿔 계산도 하지 않는다.
                float3 target = handStart.xyz + handDirection.xyz * handDirection.w; // 실제 손 포인터를 따라 8미터 앞에 있는 빛의 목적지다.
                float3 start = lerp(handStart.xyz, _ScubaMudViewer.xyz, _ScubaMudViewer.w); // 눈앞의 큰 입자도 가리지 않도록 눈에서 목적지까지 길을 낸다.
                float3 toTarget = target - start; // 빛의 목적지로 향하는 화살표다.
                float beamLength = max(length(toTarget), 0.001); // 0으로 나누지 않도록 길이에 작은 최솟값을 둔다.
                float3 axis = toTarget / beamLength; // 길이가 1인 빛길의 방향이다.
                float3 offset = particlePositionWS - start; // 머리에서 이 입자 픽셀로 향하는 화살표다.
                float along = dot(offset, axis); // 빛길을 따라 얼마나 앞에 있는지 잰다.
                float radius = length(offset - axis * along); // 빛길 중심에서 옆으로 얼마나 떨어졌는지 잰다.
                float inner = 0.08 + max(along, 0.0) * 0.2216947; // 안쪽 25도 범위는 가장 잘 보인다. 0.08은 두 눈 가까이의 작은 여유다.
                float outer = 0.12 + max(along, 0.0) * 0.3443276; // 바깥 38도까지 가장자리를 부드럽게 만든다.
                float side = 1.0 - smoothstep(inner, outer, radius); // 원뿔 밖은 0이어서 머드가 그대로 남는다.
                float front = smoothstep(-0.12, 0.02, along); // 머리 뒤쪽의 머드를 지우지 않는다.
                float far = 1.0 - smoothstep(beamLength * 0.75, beamLength, along); // 빛길 끝에서는 서서히 원래 머드로 돌아간다.
                return handStart.w * side * front * far; // 추적된 켜진 손전등의 원뿔 안에서만 0보다 커진다.
            }
            half4 MudFragment(Varyings input) : SV_Target // 입자의 한 픽셀을 그린다.
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input); // 현재 그리는 눈을 복원한다.
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * input.color; // 원본 입자의 그림·색·알파를 곱한다.
                float left = ClearInBeam(input.positionWS, _ScubaMudLeftStart, _ScubaMudLeftDirection); // 왼손이 비추는 정도다.
                float right = ClearInBeam(input.positionWS, _ScubaMudRightStart, _ScubaMudRightDirection); // 오른손이 비추는 정도다.
                float clear = max(left, right); // 두 빛이 겹쳐도 한 번만 투명도를 낮춘다.
                color.a *= 1.0 - clear * _ClearStrength; // 빛길의 머드만 옅어져 그 뒤의 물고기나 바위가 보인다.
                color.rgb = MixFog(color.rgb, input.fogFactor); // 바다 전체의 청록색 안개는 그대로 유지한다.
                return color; // 최종 색과 투명도를 화면에 섞는다.
            }
            ENDHLSL // 그래픽 카드 코드를 끝낸다.
        }
    }
    FallBack Off // 다른 렌더러의 셰이더로 바꾸지 않는다.
}
