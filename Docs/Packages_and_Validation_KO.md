# 손 전용 최종 프로젝트: 패키지·XR 설정·검증 도구

검사 대상은 이 최종 프로젝트 `HCI_Diver_Hands_Final`이며 Unity 버전은 **6000.3.27f2**입니다. 이 문서의 “확인”은 파일 내용과 별도로 설명한 C# 실행 검사를 뜻합니다. 이 환경에서 Unity Editor, Android 빌드, Quest를 실행한 결과는 없습니다.

## 1. 실제로 바꾼 설정

`Packages/manifest.json`에 다음 세 패키지를 **기존 lock 파일에 있던 동일 버전**으로 직접 선언했습니다. `packages-lock.json`의 같은 항목은 `depth: 1`에서 `depth: 0`으로 바꿨습니다.

| 직접 선언한 패키지 | 유지한 버전 | 코드에서 사용하는 이유 |
|---|---:|---|
| com.unity.inputsystem | 1.20.1 | PC 키보드·마우스와 손 Aim의 추적 상태 |
| com.unity.xr.core-utils | 2.6.0 | 원본 XROrigin |
| com.unity.xr.management | 4.6.1 | 현재 로더의 XRHandSubsystem 조회 |

세 패키지는 원본에서도 다른 패키지가 요구하는 **간접 의존성**으로 존재했습니다. 따라서 원본에 없던 패키지를 새 버전으로 업그레이드한 수정이 아닙니다. 우리 코드가 직접 사용하는 의존성을 목록에서 바로 확인할 수 있게 만든 정리입니다. 이미 있던 manifest 패키지의 버전 변경은 **0개**입니다. 최종 manifest에 직접 선언한 모든 버전이 lock의 실제 선택 버전과 일치합니다.

패키지 관리 파일은 “어떤 라이브러리를 설치할지”를 기록합니다. C# 파일의 `using UnityEngine.XR.Hands;`는 그 라이브러리 안의 이름을 짧게 쓰는 선언입니다. `using` 한 줄을 추가한다고 패키지가 설치되지는 않습니다. 프로젝트를 다른 PC로 옮길 때는 **Assets·Packages·ProjectSettings를 함께 가진 프로젝트 루트**를 Unity Hub에 등록해야 합니다.

## 2. 현재 패키지와 Samples 폴더 이름

| 실제 설치 요청 | 버전 |
|---|---:|
| XR Hands | 1.7.3 |
| XR Interaction Toolkit | 3.3.2 |
| OpenXR | 1.16.1 |
| Universal RP | 17.3.0 |
| UGUI | 2.0.0 |

`Assets/Samples/XR Interaction Toolkit/3.2.1`, `3.0.8`, `XR Hands/1.7.0`, `OpenXR Plugin/1.14.3`이라는 폴더 이름은 **그때 가져온 샘플 에셋의 이름**입니다. 현재 설치된 패키지 버전은 manifest와 lock에서 읽어야 합니다. 샘플 폴더 이름을 바꾸거나 다시 가져오면 Scene이 참조하는 GUID/프리팹 구성까지 영향을 줄 수 있으므로, 버전 표기를 일치시키겠다는 이유로 새 샘플을 덮어쓰지 않았습니다.

사용자 C# 폴더 위에는 별도의 `.asmdef`가 없으므로 기본 런타임 어셈블리에 들어갑니다. `Editor` 폴더의 새 진단 도구는 Editor용으로 분리됩니다. 기존 샘플의 8개 `.asmdef`는 샘플의 컴파일 범위와 참조를 정하는 파일이며 수정하지 않았습니다.

## 3. Android OpenXR 설정

`Assets/XR/Settings/OpenXR Package Settings.asset`의 **Android(BuildTargetGroup 7)** 설정이 참조하는 기능 목록을 읽었습니다. `MonoBehaviour.m_Enabled`와 기능의 **`m_enabled`**를 혼동하지 않았습니다. 여기서 실제 기능 토글은 소문자 `m_enabled`입니다.

| Android 기능 | 확인한 값 |
|---|---|
| Hand Tracking | 켜짐 |
| Meta Hand Tracking Aim | 켜짐 |
| Hand Interaction Profile | 켜짐 |
| Hand Common Poses Interaction | 켜짐 |
| Palm Pose Interaction | 켜짐 |
| Meta Quest Support / MetaQuestFeature | 켜짐 |
| 옛 OculusQuestFeature | 꺼짐 |
| Oculus Touch / Touch Plus / Touch Pro / KHR Simple Controller 등 controller profile | 꺼짐 |

새 Meta Quest Support가 켜져 있으므로, 옛 `OculusQuestFeature`가 꺼졌다는 이유로 Quest 지원 누락으로 판정하지 않았습니다. Meta Quest Support 내부의 Quest, Quest 2, Quest Pro, **Quest 3**, Quest 3S 대상도 모두 켜져 있습니다.

Unity의 OpenXR 1.16 공식 문서는 Android에서 Meta Quest Support를 사용하도록 설명하며, Hand Interaction Profile도 Quest에서 지원하는 프로필로 명시합니다. 권장 controller profile 누락 검사는 경고라는 설명도 있습니다. 이 프로젝트는 손 전용 조작을 사용하므로 컨트롤러 조작을 추가하지 않았습니다.

출처: [Unity OpenXR 1.16 — Meta Quest Support](https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.16/manual/features/metaquest.html)

**XR Settings 에셋은 이 검토에서 수정하지 않았습니다.** 해당 설정 파일들은 제공된 Eliza 원본과 바이트가 같습니다. 설정이 파일에 존재한다는 사실과 실제 기기에서 확장이 정상 제공된다는 사실은 다르므로, 최종 Quest 확인은 남아 있습니다.

## 4. XR 시작 및 Android Player 설정

`Assets/XR/XRGeneralSettings.asset`의 Android 설정은 `m_InitManagerOnStart: 1`이며 OpenXR Loader 참조가 연결되어 있습니다. `XRManagerSettings`의 `m_AutomaticLoading/m_AutomaticRunning` 값만 떼어 보고 “XR 자동 시작이 꺼졌다”고 결론 내리지 않았습니다. 공식 `XRGeneralSettings.InitManagerOnStart` 설명은 시작 시 관리자를 활성화·초기화하는 설정입니다.

출처: [Unity XR Management 4.6 — XRGeneralSettings](https://docs.unity3d.com/Packages/com.unity.xr.management@4.6/api/UnityEngine.XR.Management.XRGeneralSettings.html)

| Player 설정 | 파일에서 확인한 상태 |
|---|---|
| Android 최소 SDK | 29 |
| Android 목표 SDK | 0, 자동 선택 |
| Android 아키텍처 | ARM64 |
| Android scripting backend | IL2CPP |
| Android graphics API | Vulkan, 자동 API 선택 꺼짐 |
| Active Input Handling | Input System |
| Android OpenXR Loader | 연결됨 |
| Standalone XR Loader | 현재 설정에 등록되지 않음 |

PC 개발용 키보드·마우스 경로는 유지했습니다. 이 설정만으로 Quest Link가 구성되어 있다고 주장하지 않습니다. 사용자의 Unity 설치에 Android Build Support 및 SDK/NDK/JDK 모듈이 실제로 있는지는 이 파일 검사로 알 수 없습니다.

## 5. 최종 Scene의 손 전용 rig 확인

최종 Scene의 원본 XR Origin 프리팹 인스턴스에 다음 override가 들어간 것을 실제 YAML에서 재확인했습니다.

| 원본 컴포넌트/객체 | source fileID | 최종 override |
|---|---:|---|
| XRInputModalityManager | 6468442521412095314 | enabled 0 |
| Left Hand 루트 | 6355494128053973299 | active 1 |
| Right Hand 루트 | 1800725127586568702 | active 1 |
| Left Controller 루트 | 657184243640684908 | active 0 |
| Right Controller 루트 | 657184242222895507 | active 0 |
| CharacterController | 6873022368256633593 | enabled 1 |

Modality Manager를 끄는 목적은 컨트롤러가 다시 나타났을 때 원본 자동 전환 경로가 controller 루트를 다시 켜는 일을 막는 것입니다. 손 루트는 명시적으로 켭니다. 원본 XRI 손 추적 패키지/컴포넌트 자체를 삭제하지 않았습니다.

이동의 주인은 `SimpleMovement` 하나입니다. 오른손 엄지와 왼손 전진은 한 속도 벡터로 합친 후 한 번의 `CharacterController.Move`로 전달합니다. 원본 ThumbsUpAscend·StrokeZone·MudPinchNavigator는 최종 Scene에서 비활성화됩니다. Rigidbody 중력과 중복 CapsuleCollider도 이동 경로에서 제외됩니다.

왼손 전진은 **현재 보는 3차원 방향**을 보존합니다. 수면 제한은 오른손 엄지 상승 명령에 적용합니다. 그러므로 왼손을 쥐고 위를 보면 시야 방향 전진 자체의 수직 성분은 발생할 수 있습니다. 코드·문서·수동 시연에서 이 구분을 유지합니다.

## 6. 추가한 읽기 전용 Unity 진단 메뉴

파일:

- `Assets/Diving Simulation/Scripts/Editor/HandsDemoValidator.cs`
- script GUID: `9afdc09c7f4c48bca1b754f761d31474`
- Unity 메뉴: **HCI > Validate Hands Demo**

사용 순서:

1. 최종 프로젝트 루트를 Unity Hub에 등록하고 Unity 6000.3.27f2로 엽니다.
2. 패키지 설치/임포트와 C# 컴파일이 끝날 때까지 기다립니다.
3. `Assets/Diving Simulation/Scenes/Simulation.unity`를 엽니다.
4. Play를 누르기 전에 **HCI > Validate Hands Demo**를 누릅니다.
5. 결과창의 오류 수와 Console의 구체적인 객체/필드 이름을 확인합니다.

도구가 검사하는 구성은 다음과 같습니다.

- Simulation Scene과 `00_Systems`, `02_Hand_UI` 그룹 존재.
- 활성 HandsInput·SimpleMovement·HandUIInteractor·CallTowerManager·SimpleDial·CallDashboardManager가 각각 하나인지.
- 입력·카메라·깊이·통화·음원 참조가 비지 않았는지.
- UI와 이동이 같은 HandsInput을 사용하는지.
- `입력 -100 → UI -90 → 이동 0` 순서. 메타 설정값이 기본 0이면 코드의 DefaultExecutionOrder도 읽습니다.
- CC 활성, Rigidbody 중력 꺼짐/kinematic, 중복 CapsuleCollider 꺼짐.
- 실제 주소록 5명과 연락처 버튼/문자 5개, 중복 동료 참조, 각 동료의 수신·발신 음원.
- 손 버튼의 Button·Collider·UI 레이어·Inspector 콜백.
- 왼팔 버튼은 오른손, 오른손목 버튼은 왼손이라는 제한.
- controller 루트, 자동 modality 전환, 옛 이동, 중복 native UI 입력이 활성인지.
- Unity가 실제로 로드한 Missing Script 개수. 활성 객체는 오류, inactive 보관 객체는 경고.

이 메뉴는 Scene이나 에셋을 저장·수정하지 않습니다. Console에 결과를 출력하고 결과창만 띄웁니다. C# 컴파일 오류가 있으면 사용자 Editor 메뉴도 생기지 않으므로, **이 메뉴가 패키지 설치 오류나 컴파일 오류를 먼저 해결해 주는 도구는 아닙니다.**

Unity 공식 API 확인: [Missing Script 개수 조회](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/GameObjectUtility.GetMonoBehavioursWithMissingScriptCount.html), [실행 순서 설정 조회](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoImporter.GetExecutionOrder.html), [DefaultExecutionOrder](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/DefaultExecutionOrder.html).

## 7. 이 환경에서 실제 실행한 C# 검사

### A. 전체 custom C# 문법 검사

실제 최종 C# 파일 **40개**를 Roslyn으로 읽고 C# 9 문법을 사용하여 Editor/Android 전처리 두 조합으로 분석했습니다.

- 문법 오류: **0**
- 문법 경고: **0**
- 파일별 SHA-256과 검사 시각: `Source_Parser_Results.json`

이 검사는 괄호·문장·선언 등 **C# 문법**을 검사합니다. Unity DLL에 대한 형식 검사, 패키지 설치, shader 컴파일, Scene 임포트는 수행하지 않습니다.

### B. 실제 손 입력·이동·버튼 소스 실행

최종 프로젝트의 아래 **원본 파일 경로를 그대로 Compile Include로 연결**했습니다. 테스트용으로 생산 코드 복사본을 따로 고치지 않았습니다.

- HandsInput.cs
- SimpleMovement.cs
- StrokeZone.cs
- HandUIButton.cs

.NET 10.0.404에서 테스트용 Unity API 대역과 함께 컴파일·실행하여 **12개 사례, 59개 단언**이 통과했습니다.

| 검사 | 확인한 논리 |
|---|---|
| 펼침→주먹 | onset은 한 프레임, 계속 쥐면 반복 없음 |
| 엄지 방향 | 위/아래/옆을 구분하고 주먹과 엄지척은 중복되지 않음 |
| 관절·추적 상실 | 손 신호를 지우며 재추적 자체는 새 클릭이 아님 |
| 앱 정지·포커스 상실 | 신호가 중립으로 돌아옴 |
| Aim 유효성 | 위치와 회전 둘 다 유효해야 사용, 오래된 Aim은 사용하지 않음 |
| 물리 초기 설정 | CC 켜짐, 중력/중복 capsule 꺼짐 |
| 동시 이동 | 한 번의 Move, 속도 상한, 수직·전진 벡터 합 |
| 중립·UI 점유 | 다음 이동 계산에서 즉시 정지 |
| 수면 | 엄지 상승은 물속에서만, 수면까지 남은 거리로 제한 |
| XR 입력 소유권 | XR 중에는 PC 키보드·마우스 이동 무시 |
| PC 경로 | 키보드 개발 입력 유지, 없는 입력은 중립 |
| 손목·버튼 | 반대 손만 허용, 비활성 버튼 차단, cooldown과 재진입 중복 차단 |

빌드 시 Inspector가 나중에 주입하는 private 필드 3개에 대해 .NET 컴파일러의 CS0649 경고가 나왔습니다. 테스트에서는 reflection으로 연결했습니다. Unity의 실제 serialization이 동작했다는 증거로 사용하지 않습니다.

API 대역의 `CharacterController.Move`는 전달된 변위와 호출 횟수만 기록합니다. **실제 지형 충돌, XR 관절 노이즈, 렌더링, Physics raycast, 프레임 시간, Quest 추적은 검증하지 않습니다.** 따라서 이 결과는 “Unity 빌드 성공”이나 “Quest 시연 성공”과 합쳐 표현하지 않습니다.

근거: 이 Docs 폴더의 `Hands_Logic_Test_Results.json`입니다. 개발 중 실행한 입력 하네스의 소스와 빌드 산출물은 이 패키지에 넣지 않았습니다. JSON에는 연결했던 실제 생산 소스의 파일명·SHA-256, 각 검사 결과, 대역 사용 범위가 남아 있습니다. 전체 통화 시스템 검사는 다른 별도 하네스의 결과이며 여기 59개에 합산하지 않았습니다.

## 8. 사용자 환경에서 남은 실제 확인

- [ ] Unity 6000.3.27f2에서 전체 프로젝트 임포트가 끝나고 Console 컴파일 오류가 0개다.
- [ ] HCI > Validate Hands Demo를 실행해 활성 Scene의 구성 오류가 0개다.
- [ ] PC에서 WASD, 오른쪽 마우스 시점, E/Q 또는 U/J 수중 이동, 모든 큰 버튼 클릭이 된다.
- [ ] Quest를 컨트롤러 없이 시작했을 때 양손과 손목/팔 UI가 보인다.
- [ ] 왼손을 쥐면 보는 방향으로 전진하고 펼치면 멈춘다.
- [ ] 물속에서 오른손 엄지 위/아래가 상승/하강이며 중립·추적 상실 때 즉시 멈춘다.
- [ ] 전진과 엄지를 동시에 사용해도 이동이 이중으로 더해지지 않고 속도가 제한된다.
- [ ] 손목 버튼은 반대 손 검지로만 누를 수 있다.
- [ ] 레이저가 현재 버튼 위에 있을 때 주먹을 새로 쥐면 한 번 선택된다. 계속 쥐거나 추적을 잃었다 되찾았다고 반복 선택되지 않는다.
- [ ] UI를 쥔 상태에서 손을 잠깐 가려도 앞으로 튀지 않는다.
- [ ] 손을 가리거나 시스템 메뉴를 열었다 돌아왔을 때 PC용 가짜 손목 위치가 나타나지 않는다.
- [ ] Terrain·바위·물체 앞에서 실제 CC 충돌과 손 UI 거리/크기·가독성을 확인한다.
- [ ] Android Build And Run 뒤 정상 통화·수신·종료·SOS 음성이 헤드셋에서 들린다.

이 체크는 모두 아직 실기 확인 전입니다. 성공했다는 증거는 사용자 PC의 Console/실제 Game View, Android 빌드 로그와 Quest 시연으로 남겨야 합니다.

## 9. 함께 제공하는 근거 파일의 읽는 법

| 파일 | 범위 |
|---|---|
| `Source_Parser_Results.json` | 최종 custom C# 40개, Editor/Android 전처리 두 조합의 문법 검사와 소스 해시 |
| `Hands_Logic_Test_Results.json` | 실제 입력·이동·버튼 소스 4개를 API 대역과 실행한 12개 사례/59개 단언 |
| `Package_XR_Audit.json` | 패키지·Android OpenXR·Player 설정과 해당 검사 시점 Scene override 스냅샷 |
| `Hands_Input_Static_Validation.json` | 앞서 수행한 입력 소스/공식 API/연결 계약의 정적 검토 기록 |
| `Legacy_Annotation_Validation.json` | 11개 기존 스크립트에 한국어 주석만 추가할 때 실행 텍스트를 보존했는지 검사한 기록 |

이 JSON의 제작 환경 경로와 검사 시점 해시는 추적용 기록입니다. 사용자 PC에 그 경로를 만들 필요는 없습니다. 앞선 정적 검토 기록을 뒤의 .NET 검사와 같은 실행 결과로 합산하지 않습니다. 최종 Scene 전체 구조는 같은 Docs 폴더의 `Final_Scene_Static_Validation.json`을 함께 확인합니다.

Elina의 최종 두 통화 음성은 사용자가 제공한 실제 녹음으로 교체되어 있습니다. 합성 placeholder가 최종 Elina 음성이라는 안내는 적용되지 않습니다. 원본·변환 출처는 `Audio_Sources/README_KO.md`와 `Elina_Recording_Validation.json`을 참고합니다. 실제 헤드셋에서 들리는 음량·명료도는 아직 Quest 확인 대상입니다.
