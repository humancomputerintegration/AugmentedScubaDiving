# 손 입력과 이동 — 최종 프로젝트 학습 안내

이 문서는 Eliza 기능 ZIP에서 출발한 최종 프로젝트의 손 입력·이동을 설명합니다. 아래 연결과 비활성화 설정은 최종 Simulation Scene에 이미 적용되어 있습니다. 원본 ZIP 추출 폴더는 수정하지 않았습니다. 실제 Unity Editor·Android 빌드·Quest 시연은 아직 확인하지 않았습니다.

## 1. 무엇을 바꿨는가

| 파일 | 처리 | 이유 |
|---|---|---|
| HandsInput.cs + .meta | 추가 | 한 프레임의 손 상태를 UI와 이동이 함께 읽게 한다. |
| SimpleMovement.cs | 원본 클래스/GUID 유지, 내부 정리 | 전진과 상승·하강을 합쳐 CharacterController.Move 한 곳에서 실행한다. |
| HandColliderFollower.cs | 원본 필드/GUID 유지, 추적 중단 처리 추가 | 손 추적이 사라졌을 때 마지막 위치의 collider가 계속 접촉하지 않게 한다. |
| StrokeZone.cs | HandSide와 기존 참조 보존, 한국어 설명 | 공통 Left/Right 이름과 원본 연결을 유지한다. 최종 Scene의 두 StrokeZone은 꺼져 있다. |
| ThumbsUpAscend.cs | 원본 파일 보관 | 최종 Scene의 컴포넌트는 꺼져 있다. 그 안의 독립 rig 이동은 실행하지 않는다. |

기존 SimpleMovement는 왼손 엄지+검지 pinch와 별도 stroke 코루틴으로 움직였다. 새 ZIP Scene에서는 aimPose를 연결했고 y 성분을 0으로 만들어 수평 이동했다. ThumbsUpAscend는 별도의 rig.position 변경으로 상승시켰다. 새 요구에 따라 **왼손 주먹 + 보는 방향의 3차원 전진**, **오른손 엄지 위/아래 + 수중 수직 이동**으로 통일했다.

제공된 Eliza 원본 Scene의 Rigidbody는 gravity=true, dynamic이고, 원래 이동 설정은 useCharacterController=false였다. 새 코드는 기존 Rigidbody의 중력과 동적 이동을 끄고, 중복 CapsuleCollider를 끈다. 원래 있던 CharacterController를 활성화하고 원본 몸통 반지름/높이를 사용한다.

## 2. 확정 연결 계약

### HandsInput

파일 GUID: 43ff927242ab49dba8b60514dd30299a

| Inspector 필드 | 연결 |
|---|---|
| trackingSpace | Camera Offset Transform, Scene fileID 1044027113 |
| head | Main Camera Transform, Scene fileID 1692552786 |
| leftAim | 기존 Left Hand/Aim Pose Transform, Scene fileID 2126120147 |
| rightAim | 기존 Right Hand/Aim Pose Transform; Hands prefab source fileID 8000110866998627907 |
| closeRatio | 1.15 |
| releaseRatio | 1.35 |
| thumbReachRatio | 0.85 |
| verticalThumbDot | 0.65 |

공개 API:

- LeftTracked, RightTracked: 필요한 관절 자료까지 유효한 손.
- LeftFist, RightFist: 네 손가락을 접고 엄지를 모은 상태.
- LeftFistPressedThisFrame, RightFistPressedThisFrame: **펼침→주먹**으로 바뀐 한 프레임.
- RightThumbAxis: 위 +1, 아래 -1, 중립 0.
- UseHandInput: Android 실행 또는 실제 XR 장치 실행이면 true.
- LeftUIBusy, RightUIBusy: HandUIInteractor가 설정하는 이번 프레임의 UI 점유 플래그.
- TryGetWorldPose(HandSide, XRHandJointID, out Pose): 세계 좌표의 관절.
- TryGetIndexTip(HandSide, out Vector3): 직접 터치용 검지 끝.
- TryGetAimRay(HandSide, out Ray): 유효한 원본 XRI aim 또는 머리→손바닥 보조 방향.

실행 순서는 HandsInput(-100), HandUIInteractor(-90), SimpleMovement(0)이다. HandsInput은 매 프레임 먼저 UI 점유를 false로 지운다. UI가 현재 검지 터치·주먹 선택·선택 후 주먹 유지 상태를 판정해서 점유를 다시 설정한다. 이동은 그 결과를 읽는다. 열린 손이나 엄지척으로 레이저를 가리키기만 하는 hover는 수영을 막지 않는다. 실제 터치나 버튼 위 주먹, 선택에 사용한 주먹 유지 중에는 해당 손의 이동을 억제한다. UI에 사용한 주먹의 잠금은 추적 상실 때 유지하며 유효한 비주먹 자세를 다시 확인한 뒤 해제한다.

기존 Aim Pose의 TrackedPoseDriver.trackingStateInput.action을 검사해 위치와 회전 비트가 모두 유효할 때만 그 방향을 사용한다. 손 관절은 유효한데 aim이 유효하지 않으면 과거 aim 위치를 쓰지 않고 보조 레이저를 만든다. 표시할 레이저와 버튼 판정에는 반드시 같은 TryGetAimRay 결과를 사용해야 한다.

### SimpleMovement

원본 GUID: 77cf96133e6df48e0b174a90346af4cf

| Inspector 필드 | 연결/기본값 |
|---|---|
| xrOrigin | Scene fileID 2126120146 |
| xrCamera | Scene fileID 1692552786 |
| handsInput | 새 HandsInput 컴포넌트 |
| informationManager | Scene fileID 746461937 |
| speed | 2.4 m/s |
| verticalSpeed | 1 m/s |
| mouseSensitivity | 0.12 degree/pixel |

CharacterController와 Rigidbody는 같은 XR Origin에서, 머리 TrackedPoseDriver는 카메라에서 찾는다. 연결 누락 시 transform 이동으로 우회하지 않고 오류를 표시한 뒤 해당 이동 컴포넌트를 끈다.

기존 aimPose, pinchStartDistance, pinchEndDistance, useCharacterController, stroke 관련 수치는 더 이상 이 이동 클래스에서 사용하지 않는다. Scene YAML에 예전 저장값이 남아 있어도 새 클래스는 그 필드를 읽지 않는다. 사용자는 이 예전 항목을 다시 연결하거나 새로 만들어 넣을 필요가 없다.

### 최종 Scene에서 꺼 둔 기존 컴포넌트

| 항목 | Scene fileID | 이유 |
|---|---:|---|
| ThumbsUpAscend | 2126120165 | 독립 상승 경로를 실행하지 않는다. |
| MudPinchNavigator | 2126120167 | 별도 pinch hold 선택과 새 UI 선택이 중복되지 않게 한다. |
| 오른쪽 StrokeZone | 170676173 | 별도 stroke 입력을 사용하지 않는다. |
| 왼쪽 StrokeZone | 1138812383 | 동일. |
| 기존 몸통 CapsuleCollider | 2126120159 | CharacterController와 중복 몸통 충돌을 피한다. 코드에서도 처리. |
| Rigidbody의 gravity/dynamic | 2126120158 | gravity=false, isKinematic=true. 코드에서도 처리. |

원본 custom leftStroke GO225748326, rightStroke GO300256265는 새 UI가 쓰지 않으며 최종 Scene에서 inactive로 보관한다. 예전 collider/follower도 이 조작 경로에서는 실행되지 않는다. 새 손 버튼은 HandUIInteractor가 직접 검지 관절을 읽어 처리하므로 이 옛 stroke 객체를 다시 켤 필요가 없다.

## 3. 한 프레임을 직접 따라가기

### 왼손 전진과 오른손 상승이 동시에 들어올 때

카메라가 수평으로 +z를 보고 있다고 하자.

1. 왼손 주먹이 감지되고 왼손 UI 점유가 없다.
2. 전진 속도는 (0, 0, 2.4) m/s다.
3. 수중이며 오른손 엄지가 위를 향하고 오른손 UI 점유가 없다.
4. 수직 속도 (0, 1, 0)를 더하면 (0, 1, 2.4)다.
5. 이 길이는 2.6 m/s라서 최고 속도 2.4를 넘는다.
6. 같은 방향을 유지하며 2.4/2.6배로 줄여 약 (0, 0.9231, 2.2154)를 얻는다.
7. 90fps라면 프레임 시간 약 1/90초를 곱한다.
8. 최종 이동량 약 (0, 0.01026, 0.02462)m를 CC.Move에 한 번 준다.
9. 벽이나 바닥이 막고 있다면 CharacterController가 그 이동을 충돌에 맞게 제한한다.

카메라가 아래를 보는 전진과 엄지 상승이 동시에 들어오면 y 속도는 서로 상쇄된다. 그러므로 엄지가 위여도 아래를 보는 전진이 더 강하면 실제 합성 이동은 아래로 갈 수 있다. 두 입력을 벡터로 더한 결과다.

### 버튼을 가리키고 주먹을 계속 유지할 때

| 프레임 | 주먹 | 직전 주먹 | 새로 쥠 신호 | 현재 레이저가 버튼을 가리킨다면 |
|---|---:|---:|---:|---|
| A | false | false | false | 아직 UI 클릭 없음; 다른 유효 이동 입력은 별도로 처리 |
| B | true | false | true | 한 번 선택, 해당 손 이동 억제 |
| C | true | true | false | 반복 선택 없음 |
| D | true | true | false | 반복 선택 없음 |
| E | false | true | false | 손을 놓음 |
| F | true | false | true | 다음 선택 한 번 가능 |

추적이 사라지면 추적/주먹/엄지/클릭을 모두 지운다. 다시 보이는 첫 프레임에 이미 주먹이어도 클릭 신호는 false다. 열린 손 상태를 거친 뒤 새로 쥐어야 다음 클릭이 된다.

UI의 같은 손목 금지, 직접 터치 우선, 버튼 범위 판정, 한 번의 콜백 실행은 HandUIInteractor/HandUIButton의 책임이다. HandsInput이 제공하는 것은 추적과 형태 신호다.

## 4. 코드 문법을 읽는 방법

| 표기 | 뜻 | 이 코드에서의 역할 |
|---|---|---|
| class A : MonoBehaviour | Unity 컴포넌트 A를 정의 | Update 같은 수명주기 함수를 사용 |
| public | 다른 클래스가 접근 가능 | UI가 손 신호를 읽음 |
| private set | 이 클래스 내부만 값을 변경 | 외부 UI가 손의 추적 결과를 위조하지 않음 |
| [SerializeField] | private 값도 Unity가 저장 | Scene의 카메라·입력 참조 연결 |
| out Pose pose | 함수가 성공 여부 외에 pose도 전달 | TryGetPose의 결과를 받음 |
| out _ | 해당 결과를 버림 | 왼손의 엄지 수직 방향은 쓰지 않음 |
| && | 두 조건이 모두 참 | 추적됨 + 주먹 + UI 점유 없음 |
| ! | 참/거짓 반전 | !oldFist는 이전에는 주먹이 아니었다는 뜻 |
| ?. | 앞 객체가 null이면 이후 접근 생략 | XR 로더가 아직 없는 첫 프레임 처리 |
| ?: | 조건에 따라 두 값 선택 | 왼손/오른손, 쥠/해제 임계값 선택 |
| & | 정수의 비트를 검사 | aim의 위치비트 1과 회전비트 2 검사 |
| normalized | 방향은 유지하며 길이를 1로 | 손 크기와 방향을 구분 |
| Vector3.Dot | 두 방향이 얼마나 같은지 계산 | 엄지가 위인지 아래인지 판단 |
| TransformPoint | 로컬 위치를 세계 위치로 | 관절 좌표를 Scene UI 좌표로 변환 |
| TransformDirection | 로컬 방향을 세계 방향으로 | 엄지 방향을 실제 수직 방향과 비교 |
| ClampMagnitude | 길이의 상한만 제한 | 두 손을 함께 쓸 때 과속 방지 |
| Time.deltaTime | 이번 프레임이 지속된 초 | fps가 달라도 초당 속도를 유지 |

closeRatio와 releaseRatio를 다르게 둔 이유는 한 경계값 주위의 작은 추적 떨림으로 쥠/해제가 계속 번갈아 생기는 것을 줄이기 위해서다. 이 수치들은 실측으로 확정한 값이 아니라 합리적인 시작값이다. Quest의 사용자 손, 조명, 가림 상황에서 확인하고 필요하면 Inspector에서 조정해야 한다.

## 5. PC 동작

- WASD: 카메라 기준 전후좌우 이동.
- 오른쪽 마우스 버튼을 누른 채 움직임: 시점 회전.
- 왼쪽 마우스 버튼: 커서가 가리키는 큰 UI 버튼을 누른 순간 한 번 선택.
- E 또는 U: 상승.
- Q 또는 J: 하강.
- 수직 키도 엄지와 동일하게 수중일 때만 동작한다.
- 수면 위에서 물에 들어갈 때는 아래를 보고 W를 누른다. Quest에서는 아래를 보고 왼손을 쥔다.
- E/U 또는 오른손 엄지의 수직 상승 명령은 InformationManager 깊이가 0인 수면까지 제한한다. 수면 위에서는 이 수직 명령이 중립이다. W 또는 왼손 주먹의 전진은 보는 3차원 방향을 따르므로 위를 보고 전진하면 그 방향의 수직 성분은 생길 수 있다.
- Quest에서는 PC 키 입력으로 손 추적 상실을 대신하지 않는다.

## 6. 남은 실제 검증 — 아직 통과 표시하지 않음

제작 환경에서는 Unity Editor·Unity Hub·Quest를 실행하지 않았다. 별도로 확보한 .NET 10.0.404/Roslyn으로 전체 custom C# 40개의 문법을 두 전처리 조합에서 검사했고, 실제 입력·이동·버튼 소스 4개를 테스트용 Unity API 대역과 함께 실행하여 12개 사례/59개 단언을 통과했다. 이 결과는 실제 Unity 컴파일·물리 충돌·Quest 추적 검증과 다르다. 자세한 범위는 [Packages_and_Validation_KO.md](Packages_and_Validation_KO.md)를 참고한다. 아래 항목은 실제 Editor와 Quest에서 수행해야 한다.

- [ ] Unity 6000.3.27f2에서 전체 프로젝트의 패키지 복원이 완료되고 Console compile error가 0개다.
- [ ] Scene의 SimpleMovement와 HandsInput 필드 연결이 모두 유효하다.
- [ ] PC에서 아무 키도 누르지 않으면 중력에 의한 낙하/자동 전진이 없다.
- [ ] PC WASD/RMB 시점/좌클릭 UI가 함께 동작한다.
- [ ] 컨트롤러를 사용하지 않은 Quest 실행에서 양손 모델과 추적이 나타난다.
- [ ] 왼손을 펼치면 정지, 쥐면 보는 방향으로 전진한다.
- [ ] 수중 오른손 엄지 위/아래/중립에 따라 상승/하강/정지한다.
- [ ] 왼손 전진+오른손 상승/하강을 동시에 수행할 수 있다.
- [ ] 같은 조합을 10초 유지해도 최고 속도 제한을 넘지 않는다.
- [ ] 벽/바닥을 향해 이동했을 때 관통이 없는지 확인한다.
- [ ] 각 손을 가리거나 시야 밖으로 치우면 그 손의 명령이 즉시 중립이 된다.
- [ ] 손을 다시 보이게 했을 때 UI가 자동으로 새 클릭하지 않는다.
- [ ] 레이저가 실제 버튼 위에 있을 때 새 주먹으로 한 번만 선택한다.
- [ ] 주먹을 3초 유지해도 콜백이 반복되지 않는다.
- [ ] 열린 손으로 버튼을 가리켜 hover한 뒤 주먹을 쥐어도 전진이 동시에 발생하지 않는다.
- [ ] 엄지척이 UI 주먹으로 오인되지 않는다.
- [ ] 한 손이 자기 손목 UI를 누르지 못하며 반대 손만 누른다.
- [ ] 앱을 잠시 중단/재개하거나 헤드셋 시스템 메뉴를 열어도 이전 이동/클릭이 남지 않는다.
- [ ] 수면에서 상승이 멈추며 물 밖에서는 엄지 수직 조작이 실행되지 않는다.

## 7. 공식 API 확인 출처

- XR Hands 1.7.3, XRHandJoint.TryGetPose: https://docs.unity3d.com/Packages/com.unity.xr.hands@1.7/api/UnityEngine.XR.Hands.XRHandJoint.html
- XR Hands 1.7.3, XRHandSubsystem: https://docs.unity3d.com/Packages/com.unity.xr.hands@1.7/api/UnityEngine.XR.Hands.XRHandSubsystem.html
- XR Hands 1.7.3, XRHandJointID: https://docs.unity3d.com/Packages/com.unity.xr.hands@1.7/api/UnityEngine.XR.Hands.XRHandJointID.html
- Input System 1.20.1, TrackedPoseDriver.trackingStateInput: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/api/UnityEngine.InputSystem.XR.TrackedPoseDriver.html
- Unity 6000.3, CharacterController.Move: https://docs.unity3d.com/6000.3/Documentation/ScriptReference/CharacterController.Move.html

문서에서 API의 존재·입력 의미를 확인했다. 이는 최종 프로젝트가 실제 Unity에서 컴파일됐거나 Quest에서 실행됐다는 증거와는 별개다.
