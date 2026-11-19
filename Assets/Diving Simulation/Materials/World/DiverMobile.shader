// 이 셰이더는 잠수부의 단색 재질만 그립니다. 손 입력이나 다른 바다 재질에는 영향을 주지 않습니다.
Shader "Scuba/Diver Mobile" // Material Inspector에서 선택할 셰이더의 이름입니다.
{
    Properties // Inspector에 표시하고 각 재질에 저장할 값입니다.
    {
        _BaseColor("Suit Color", Color) = (1, 1, 1, 1) // 기존 옷·피부·장비의 색을 그대로 받습니다.
    }
    SubShader // 현재 프로젝트의 URP 렌더러가 사용할 그리기 방법입니다.
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" } // 불투명한 잠수부를 일반 물체 순서로 그립니다.
        Pass // 한 번의 그리기로 기본 조명과 수중 안개를 적용합니다.
        {
            Name "DiverForward" // Frame Debugger에서 찾을 수 있는 이 패스의 이름입니다.
            Tags { "LightMode"="UniversalForward" } // URP의 전방 조명 패스에 참여합니다.
            Cull Back // 몸의 바깥면을 그리고 뒤쪽 삼각형은 생략합니다.
            ZWrite On // 가까운 잠수부가 뒤쪽 물체를 올바르게 가리도록 깊이를 기록합니다.
            HLSLPROGRAM // 여기부터 GPU가 실행할 코드를 작성합니다.
            #pragma target 3.5 // Quest의 GLES3·Vulkan에서 VR 인스턴스 그리기를 지원하는 수준입니다.
            #pragma vertex DiverVertex // 모델의 점을 화면 좌표로 바꾸는 함수입니다.
            #pragma fragment DiverFragment // 화면의 각 픽셀 색을 정하는 함수입니다.
            #pragma multi_compile_instancing // 여러 물체와 VR의 인스턴스 그리기 변형을 준비합니다.
            #pragma multi_compile_fog // Scene의 안개 설정과 같은 안개 변형을 준비합니다.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" // URP 좌표 변환과 VR 보조 기능을 가져옵니다.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl" // 기존 Scene의 주 조명을 읽습니다.
            CBUFFER_START(UnityPerMaterial) // 각 Material의 값을 URP가 한 묶음으로 읽게 합니다.
                half4 _BaseColor; // 빨강·초록·파랑·불투명도의 네 값을 저장합니다.
            CBUFFER_END // 이 재질의 값 묶음을 닫습니다.
            struct Attributes // 모델 메시에서 한 점마다 받는 입력입니다.
            {
                float4 positionOS : POSITION; // 모델 내부 좌표계에서의 점 위치입니다.
                float3 normalOS : NORMAL; // 이 점에서 표면이 향하는 방향입니다.
                UNITY_VERTEX_INPUT_INSTANCE_ID // 현재 물체와 VR 눈을 구분하는 번호를 받습니다.
            };
            struct Varyings // 점 계산 결과를 픽셀 계산으로 전달합니다.
            {
                float4 positionCS : SV_POSITION; // 화면에 투영한 점의 위치입니다.
                half3 normalWS : TEXCOORD0; // 월드 좌표계로 바꾼 표면 방향입니다.
                half fogFactor : TEXCOORD1; // 카메라에서 멀수록 적용할 안개 정도입니다.
                UNITY_VERTEX_OUTPUT_STEREO // 왼쪽 눈과 오른쪽 눈의 그리기 대상을 전달합니다.
            };
            Varyings DiverVertex(Attributes input) // 모델의 점 하나를 처리합니다.
            {
                UNITY_SETUP_INSTANCE_ID(input); // 좌표를 계산하기 전에 현재 눈과 물체를 설정합니다.
                Varyings output = (Varyings)0; // 전달할 값을 모두 0으로 초기화합니다.
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output); // 이 점을 어느 눈에 그릴지 기록합니다.
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz); // 모델 위치를 현재 눈의 화면 위치로 바꿉니다.
                output.normalWS = TransformObjectToWorldNormal(input.normalOS); // 크기와 회전을 고려해 표면 방향을 바꿉니다.
                output.fogFactor = ComputeFogFactor(output.positionCS.z); // 기존 Scene의 안개 계산을 사용합니다.
                return output; // 화면 위치와 표면 방향을 픽셀 계산으로 보냅니다.
            }
            half4 DiverFragment(Varyings input) : SV_Target // 한 픽셀의 최종 색을 정합니다.
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input); // 현재 픽셀이 어느 눈의 화면인지 복원합니다.
                half3 normal = normalize(input.normalWS); // 표면 방향의 길이를 1로 맞춥니다.
                Light sun = GetMainLight(); // Scene에 이미 있는 주 조명을 가져옵니다.
                half facingLight = saturate(dot(normal, sun.direction)); // 빛을 향하는 면은 밝게, 반대 면은 어둡게 합니다.
                half3 lighting = half3(0.45h, 0.45h, 0.45h) + sun.color * facingLight * 0.55h; // 어두운 면도 알아볼 수 있는 부드러운 명암을 만듭니다.
                half3 suitColor = _BaseColor.rgb * lighting; // 저장된 옷 색에 명암만 곱합니다.
                suitColor = MixFog(suitColor, input.fogFactor); // 다른 물체와 같은 수중 안개에 섞습니다.
                return half4(suitColor, 1.0h); // 투명한 틈이 없는 잠수부 표면을 그립니다.
            }
            ENDHLSL // GPU 코드를 닫습니다.
        }
    }
    FallBack Off // 다른 렌더 파이프라인용 셰이더를 자동으로 섞지 않습니다.
}
