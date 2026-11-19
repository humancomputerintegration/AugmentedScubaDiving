# 검증 결과와 전달 범위

프로젝트: `HCI_Diver_Hands_Final` / 대상 Editor: **6000.3.27f2**.

## 결론

요청한 10개 기능 묶음에 해당하는 C#·UI·Scene 연결·음성·환경 자산을 프로젝트에 반영했습니다. 아래 코드 검사와 파일 검사는 실제로 실행했습니다. **Unity Editor가 없는 환경이므로 Unity 프로젝트 컴파일·Scene 가져오기·Android 빌드·Quest 손 동작·체감 음량·실제 렌더링은 실행하지 못했습니다.** 이 상태를 Quest 시연 검증 완료로 기록하지 않습니다.

검증 중 API 대역(test double)을 쓴 경우를 구분합니다. API 대역은 Unity 함수와 같은 이름·필요한 동작 일부를 제공하여 프로젝트 코드의 분기와 계산을 실행하는 검사 도구입니다. 실제 Unity 엔진의 물리·렌더링·오디오·손 추적을 실행한 것은 아닙니다.

## 실제 실행한 검사

| 검사 | 결과 | 증명하는 범위 | 근거 파일 |
|---|---|---|---|
| 최종 기능 C# 40개, C# 9, Editor/Android 전처리 두 조합의 Roslyn 파싱 | 문법 오류 0, 문법 경고 0 | C# 문장·괄호·선언의 구문 | `Source_Parser_Results.json` |
| 실제 통화 C# 6개를 링크한 .NET 실행 | 14개 시나리오, 272개 단언 통과 | 주소 매핑·통화 상태·종료·SOS·입력 버퍼·수신 시도 논리 | `Call_Logic_Test_Results.json` |
| 실제 손 입력·이동·버튼 C# 4개를 링크한 .NET 실행 | 12개 시나리오, 59개 단언 통과 | 제스처 신호·중립 복귀·한 번의 이동 요청·UI 차단·버튼 제한 | `Hands_Logic_Test_Results.json` |
| 최종 Scene 전체 통합 정적 검사 | 48개 통과, 내부 fileID 누락 0, 중복 asset GUID 0 | 저장된 참조·활성 상태·연결·구성 | `Final_Scene_Static_Validation.json` |
| 최종 통화 Scene 정적 검사 | 238개 검사, 불일치 0 | 다섯 사람과 실제 음원, 버튼 콜백·인수·Collider, 통화기 활성 host | `Call_Scene_Static_Validation.json` |
| 새 월드 프리팹·재질 YAML/GUID 검사 | 43개 대상, 오류 0 | 자산 파일 형식·참조 관계 | `World_Static_Validation.json` |
| 실제 Terrain 높이 기반 여섯 이동 경로 | 경로마다 360각도 × 5위치 검사, 수면·바닥 여유 조건 통과, 확대한 잠수함 경계 겹침 0 | 계산한 경로 표본의 정적 지형 여유 | `World_Placement_Static.json`, `World_Static_Validation.json` |
| 실제 Elina 원본·변환·연결 | 원본 바이트 동일, 샘플 수 보존, WAV 피크 약 -2dBFS, 음원 GUID 각각 1개 | 받은 녹음의 보존·변환·올바른 파일 참조 | `Elina_Recording_Validation.json`, `Call_Audio_Manifest.json` |
| 패키지·Android OpenXR·Player 설정 | manifest/lock 버전 일치, 직접 사용하는 세 간접 패키지를 같은 버전으로 명시 | 파일에 저장된 설치 요청과 XR 설정 | `Package_XR_Audit.json` |

실제 생산 소스 실행 검사는 합계 **26개 시나리오·331개 단언**입니다. Scene 정적 검사와 단언 수를 합쳐 “전체 기능을 장비에서 몇 번 통과했다”고 표현하지 않습니다.

입력 하네스에서는 Inspector로 연결되는 private 필드 세 개에 대해 일반 .NET 컴파일러의 CS0649 경고가 있었고 검사에서는 reflection으로 값을 연결했습니다. 위 Roslyn “문법 경고 0”과 다른 단계의 결과입니다. 어느 쪽도 Unity 어셈블리에 대한 전체 프로젝트 컴파일 결과는 아닙니다.

## 최종 Scene 식별

파일: `Assets/Diving Simulation/Scenes/Simulation.unity`

최종 Scene SHA-256:

`cd63ba065cb0c585cb2708af49e730aed77e17b01c07fa0ce806ce3f647180b7`

전체 Scene 보고서와 통화 Scene 보고서는 이 동일한 파일을 가리킵니다. 이전 단계의 패키지·입력 검토에는 그때의 경로와 Scene 해시가 남아 있을 수 있습니다. 이는 제작 과정의 증거이며, 최종 통합 상태는 두 최종 Scene 보고서를 기준으로 확인합니다.

## 실제 장비에 남은 검사

| 미실행 항목 | 필요한 이유 |
|---|---|
| Unity 6000.3.27f2 프로젝트 가져오기·전체 컴파일 | 실제 패키지 복원, Unity API 형식 검사, 샘플 어셈블리, 모델·shader importer 결과를 확인해야 한다. |
| HCI > Validate Hands Demo 실행 | Unity가 실제 로드한 참조·Missing Script·Inspector 구성이 저장된 내용대로 작동하는지 확인해야 한다. |
| PC Play | 실제 Physics raycast·CharacterController 충돌·버튼 렌더링·오디오를 확인해야 한다. |
| Android 빌드·Quest 실행 | 실제 기기의 손 추적 확장·제스처 정확도·팔 UI 위치·도달 거리·양안 표시를 확인해야 한다. |
| Quest 청취·성능 | 파일의 음량 수치로 기기 출력과 주변음 속 명료도를 확정할 수 없고, 자산 수만으로 프레임 속도를 보장할 수 없다. |

검사 행동과 성공 기준은 `Acceptance_Checklist_KO.md`에 있습니다. 실기 칸은 의도적으로 비워 두었습니다.

## 이미지의 의미

- `Asset_Previews/UI_LAYOUT_PREVIEW_NOT_UNITY.png`: 실제 Scene의 저장된 UI 크기·글자·색을 이용한 배치 도식. 표시된 주파수·수심·시간은 예시다.
- `Asset_Previews/BLENDER_ASSET_PREVIEW_NOT_UNITY.png`: 실제 포함 자산을 Blender에서 확인한 이미지.

두 이미지는 실제 Unity Game View 또는 Quest 캡처가 아닙니다. 실제 화면 캡처를 얻지 못했으므로 대체 이미지를 런타임 증거처럼 표시하지 않았습니다.

## 포함 범위와 한계

- 실제 사용한 Elina 녹음은 사용자 제공 M4A 두 개이며 원본도 보존했다. 발신 응답·수신용 WAV와 이전 임시 음성을 동시에 두지 않았다.
- 통화는 녹음 기반 시뮬레이션이고 PHONE은 게임 내 내선이다. 실제 전화망이나 실시간 마이크 통신은 추가하지 않았다.
- 물고기는 원본 4종을 사용하며, 수영 클립이 있는 종은 실제 원본 클립을 사용한다. 거북이는 경로 이동·회전을 하고 원본에 없는 지느러미 리깅은 추가하지 않았다.
- Elina는 단순 도형으로 구성한 수영 다이버다. 복잡한 사람 리깅 모델로 설명하지 않는다.
- 생물은 검증한 수중 구역의 경로를 따른다. 서로를 피하는 군집 AI나 모든 장애물을 탐색하는 길찾기를 추가하지 않았다.
- 프로젝트 기능 C# 40개에는 줄별 한글 설명이 있다. 공급자 패키지·샘플 소스와 선택적인 검사 도구는 별도 자료다.
- 실행에 필요한 전체 프로젝트와 설명 문서를 전달한다. Unity가 새로 만들 캐시와 기존 Visual Studio 잠금 파일은 포함하지 않았다.

## 출처

사용자 제공 원본 프로젝트와 두 Elina 녹음이 구현 기준입니다. 외부 모델의 배포 URL·원본 ZIP 해시·LICENSE는 `World_Asset_Manifest.json`과 `Assets/ThirdParty/Grab3D/SOURCE_AND_LICENSE.json`에 기록했습니다. 패키지/API 근거 URL은 각 기능 문서에 있습니다. 구현 결과의 근거는 이 문서가 연결한 실제 코드와 검사 파일입니다.
