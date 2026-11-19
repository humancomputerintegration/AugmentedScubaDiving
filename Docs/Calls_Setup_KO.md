# 통화·전화 내선·무전 주파수 — 연결 원리와 검사표

## 1. 이번에 만든 것과 아직 확인하지 않은 것

이 문서는 `HCI_Diver_Hands_Final`의 통화 담당 C# 6개와 추가 음원 7개를 설명합니다. 제공된 최종 Simulation Scene에는 통화기, 다섯 연락처, 큰 통화 팝업, 손 입력 버튼과 실제 음원 참조가 연결되어 있습니다. 아래 Inspector 연결 계약은 이미 구성된 Scene을 이해하거나 나중에 연결을 점검할 때 사용하는 참고자료입니다.

C# 구문을 tree-sitter로 읽고 기존 공개 메서드와 .meta GUID 보존, 줄별 한국어 설명, 음원 디코딩 결과를 검사했습니다. 이어 실제 최종 통화 C# 6개를 그대로 링크하여 .NET SDK의 C# 9 언어 설정으로 컴파일하고 14개 시나리오·272개 검사를 실행했습니다. AudioSource·Time·SceneManager·UI 등 엔진 API만 좁은 test double로 대체했습니다. **실제 소스의 논리 실행 검증이며 Unity 엔진·Unity 프로젝트 빌드·Quest 실행 검증은 아닙니다.** Quest에서 들리는 크기, 손 추적 버튼 동작, 글자의 선명도, 실제 Scene 재시작은 헤드셋에서 확인해야 합니다.

기록 파일:

- `Call_Logic_Test_Results.json` 및 `Call_Logic_Harness/`: 최종 통화 소스를 복사 구현하지 않고 직접 링크한 C# 논리 실행 결과와 재현 코드. 14개 시나리오 모두 통과했습니다.
- `Original_Vitals_Comment_Validation.json`: InformationManager/BatteryLevel/OxygenBar에 한국어 설명만 추가했으며 주석을 제외한 실행 토큰이 원본과 동일하다는 검사.
- `Call_Code_Static_Validation.json`: C# 6개 문법 파싱, 공개 API 보존, 주석 줄 확인, 원본 .meta 동일 여부.
- `Call_Audio_Manifest.json`: 새 음원 7개 경로·GUID·길이·채널·음량·해시·출처.
- `Call_Audio_Source_Measurements.json`: 원본 음원 11개를 **mono로 내려 읽은 분석값**. 최종 Eliza 보정은 스테레오 채널을 보존하므로 해당 보정의 기준은 `Call_Audio_Manifest.json`의 `source_signal`입니다.
- `Audio_Rebuild/generate_audio.py`: 사용자 제공 Elina 녹음의 WAV 변환, 클릭/확인음, Eliza 음량 사본의 재현 가능한 생성 과정.

## 2. 같은 사람에게 주소가 두 개 있는 이유

학교 교실을 찾는 방법을 생각해 봅시다. “2학년 3반”과 “203호”는 서로 다른 방식의 주소지만 같은 교실을 가리킬 수 있습니다. 여기서 PHONE과 FREQ도 같습니다.

| 주소 체계 | 코드 형식 | 의미 | 예 |
|---|---|---|---|
| FREQ | `int` | 100 이상 1000 미만의 무전 채널 | 실행 중 주소록에 보이는 3자리 값 |
| PHONE | `string` | 실제 전화망이 아닌 게임 속 내선 | `"2001"` |
| SOS | 예약 값 `1001` | 어느 모드에서 입력하든 구조 요청 | 일반 통화보다 먼저 처리 |

`int`는 정수입니다. `314 == 314`처럼 값이 같은지를 비교하기 좋습니다. `string`은 문자들의 순서입니다. 전화 주소를 문자열로 두면 `"02001"`의 앞자리 0도 보존할 수 있습니다. 전화 내선에서 100을 빼서 주파수를 만드는 식의 임의 계산은 없습니다.

주소록의 **같은 배열 위치**가 같은 사람을 가리킵니다.

| 배열 index | 동료 | 고정 게임 내선 | 무전 주파수 |
|---:|---|---|---|
| 0 | Pedro | 2001 | 주소록이 배정한 중복 없는 100~999 |
| 1 | Romain | 2002 | 주소록이 배정한 중복 없는 100~999 |
| 2 | Suzie | 2003 | 주소록이 배정한 중복 없는 100~999 |
| 3 | Eliza | 2004 | 주소록이 배정한 중복 없는 100~999 |
| 4 | Elina | 2005 | 주소록이 배정한 중복 없는 100~999 |

`crewmates[3]`, `crewmateFrequencies[3]`, `phoneNumbers[3]`가 모두 Eliza를 뜻합니다. PHONE 2004를 누르면 주소록이 index 3을 찾고, 같은 index의 현재 주파수를 `SimpleDial.QuickDial`로 보냅니다. FREQ 모드에서 그 주파수를 직접 눌러도 같은 함수를 통해 같은 녹음에 연결됩니다.

`useFixedFrequencies = false`일 때 채널은 실행마다 다시 배정됩니다. PHONE 내선은 그대로입니다. SOS가 Scene을 다시 시작하면 새 무전 채널을 다시 확인해야 합니다. 고정 모드를 선택하면 `fixedFrequencies`의 길이·범위·중복을 검사하고, 유효하지 않으면 무작위 채널로 전환하며 Console에 경고합니다.

## 3. 파일마다 맡은 일

| 파일 | 맡은 일 | 직접 하지 않는 일 |
|---|---|---|
| `CrewInfo.cs` | 이름, 현재 위치, 주파수, 내선의 정보 묶음 | 실제 목소리 재생 |
| `CallTowerManager.cs` | 동료 등록, 주소 배정·변환, 응답 녹음 선택, SOS 재시작 예약 | 키패드 글자 그리기 |
| `CrewmateBehavior.cs` | 동료가 기다렸다가 수신 전화를 시도 | 플레이어의 받기 선택 |
| `SimpleDial.cs` | 실제 통화 상태, 벨·목소리·확인음, 받기·종료 | 연락처 버튼 배치 |
| `CallDashboardManager.cs` | 모드별 독립 입력, 연락처 목록, 발신 버튼 | 자체 통화 상태 생성 |
| `IncomingCallPopup.cs` | 실제 상태를 큰 글씨로 표시하고 머리를 따라가기 | 가짜 타이머로 연결 성공 만들기 |

`CallTowerManager.Awake()`는 다른 일반 `Start()`가 실행되기 전에 주소록을 준비합니다. 그래서 어떤 동료의 `Start()`나 화면의 `Start()`가 먼저 실행될지에 기대지 않습니다. 준비 전의 조회는 빈 배열을 반환합니다.

`GetCrewmatesInformation()`는 호출한 순간의 월드 위치를 복사합니다. 동료가 이동하면 다음 조회에서 바뀐 위치를 얻습니다. 이 통화 화면의 주소 목록은 0.5초마다 조회하므로 매 프레임 정보 배열을 새로 만들지 않습니다. 항법 화면은 별도 갱신 주기를 사용할 수 있습니다.

## 4. 왜 통화 상태를 따로 만들었을까?

“소리가 재생 중이다” 하나만으로는 벨소리와 목소리, 잘못된 번호 안내를 구분할 수 없습니다. 그래서 `CallState`라는 이름 목록 중 하나를 실제 상태로 저장합니다.

| 상태 | 회선 사용 중 `IsBusy` | 주 소스 반복 | 화면과 동작 |
|---|---:|---|---|
| Idle | 아니오 | 없음 | 첫 대기 상태 |
| Ringing | 예 | 수신 벨만 반복 | 큰 INCOMING CALL, ANSWER/END |
| Connected | 예 | 목소리는 한 번 | 큰 CONNECTED, END |
| Ended | 아니오 | 없음 | CALL ENDED를 잠시 보여 줌 |
| Invalid | 아니오 | 오류 소리는 한 번 가능 | 잘못된 주소 또는 재생 시작 실패 |
| Emergency | 예 | 구조 안내는 한 번 | SOS 접수 표시, 일반 통화 차단 |

여기서 연결은 과제의 녹음 기반 모의 통화입니다. 상대방과 실시간 네트워크 음성 채팅을 만들거나 마이크를 켜는 기능이 아닙니다.

### 수신 한 번을 순서대로 따라가기

1. 동료가 자신의 `callAudioCaller`와 주파수를 주소록에 전달합니다.
2. 주소록은 `playerTransmitter.IncomingCall(...)`을 호출합니다.
3. 회선이 비어 있고 유효한 동료라면 통화기는 녹음을 보관하고 벨만 반복합니다.
4. 이때 `IncomingCall`은 **true를 반환**합니다. “받기 버튼을 눌렀다”가 아니라 “수신 요청을 받아 벨이 울리는 상태로 들어갔다”는 뜻입니다.
5. 플레이어가 ANSWER를 누르면 벨을 멈추고 `loop = false`인 동료 목소리를 재생합니다.
6. 같은 순간 상태가 Connected로 바뀌고 확인 소리와 큰 CONNECTED 문구가 나타납니다.
7. 플레이어가 END를 누르거나 녹음이 끝나면 Ended가 됩니다. 목소리·반복·남은 수신 정보를 함께 지웁니다.

원본은 수신을 받아 `isBusy = true`로 만든 뒤 `return !isBusy`를 실행했습니다. 따라서 받아 놓고도 false를 반환했습니다. 지금은 수락한 경로에서 true, 거절한 경로에서 false를 직접 반환합니다.

원본 동료 코루틴은 조건에 따라 자신을 여러 번 새로 시작할 수 있었습니다. 지금은 동료마다 코루틴 하나 안에서 `while (true)`로 반복합니다. 첫 시도 전 대기 한 번, 이후 시도 사이 대기 두 번이라는 원본 시간 구조는 유지합니다. 스크립트 기본 각 대기 구간은 10~120초이며 최종 Scene의 Inspector 값은 10~45초입니다. 여러 동료가 각각 시도하므로 전체 플레이어의 수신 간격을 한 동료의 간격으로 단정하면 안 됩니다.

### 자동 종료에서 확인하는 것

목소리 재생이 실제로 관찰된 뒤 `AudioSource.isPlaying`이 false가 되면 정상 종료로 처리합니다. 3초 동안 한 번도 시작되지 않았으면 정상 종료 대신 AUDIO DID NOT START를 표시합니다. 전역 `AudioListener.pause` 중에는 이를 통화 종료로 오해하지 않습니다.

전화 패널이 닫혀도 통화는 계속되어야 하므로 `SimpleDial`과 통화용 AudioSource는 항상 켜진 시스템 오브젝트에 있어야 합니다. 팝업 스크립트도 항상 켜 두고, 숨길 화면 `popupRoot`만 별도 자식으로 연결합니다.

## 5. 오디오가 작아 보이던 문제를 고친 방식

Unity 공식 AudioSource 문서에 따르면 `spatialBlend = 0`은 거리·방향 기반의 3D 감쇠를 적용하지 않는 2D 소리입니다. `volume`은 0~1 범위이며 `PlayOneShot`으로 짧은 확인음을 재생할 수 있습니다. [Unity 6000.3 AudioSource 공식 문서](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/audiosource)

이번 구현은 통화 소스를 2D로 설정하고 통화 명료도를 떨어뜨릴 수 있는 소스/리스너 효과와 잔향을 우회합니다. 기본 `voiceVolume = 1`, `ringVolume = 0.8`, `feedbackVolume = 0.8`입니다. 이 값은 Unity 소스 음량이며 Quest 기기의 하드웨어 음량을 바꾸지는 않습니다.

주 소스는 벨과 목소리를 번갈아 재생합니다. 별도 `feedbackSource`는 클릭·연결·종료 소리를 담당합니다. 확인음 때문에 목소리가 끊기거나 “아직 소리가 난다”는 이유로 통화 종료 판단이 밀리지 않게 분리했습니다.

| 음원 | 출처와 처리 |
|---|---|
| 기존 Pedro/Romain/Suzie 음성 | 원본 파일 보존 |
| 기존 Eliza MP3 2개 | 원본 파일과 메타데이터 보존 |
| Eliza level_adjusted WAV 2개 | 원본을 디코딩한 뒤 일정 음량만 증가; 길이·스테레오 채널 보존 |
| Elina WAV 2개 | 사용자가 제공한 실제 Elina M4A 녹음을 PCM WAV로 변환. 원본은 Docs/Audio_Sources에 보존 |
| Button_Click / Call_Connected / Call_Ended | 외부 샘플 없이 수학적 사인파로 만든 짧은 확인음 |

스테레오 원본 전체 채널의 RMS는 Eliza 응답 -26.351dBFS, 수신용 -27.339dBFS였습니다. 소리가 작다는 요구와 측정값에 따라 별도 사본에 각각 +6.323dB, +7.801dB의 일정 gain을 적용했습니다. 피크를 -2dBFS로 제한했으며 원본 파일을 덮어쓰지 않았습니다. 새 음원 7개를 디코딩한 결과 0dBFS 이상 샘플은 0개였습니다. 이것은 파일 신호 검사이고 헤드셋 청취 평가는 아닙니다.

Elina는 사용자가 추가로 제공한 실제 녹음 두 개를 사용합니다. 원본 M4A를 `Docs/Audio_Sources/Elina_answering_original.m4a`와 `Elina_calling_original.m4a`에 바이트 그대로 보존했습니다. Unity용 파일은 `Audio/Elina/Elina_answering.wav`와 `Elina_calling.wav`이며 48kHz, mono, PCM 16-bit입니다. 내용·속도·음높이·길이를 편집하지 않고 일정 gain만 각각 -1.957dB, -1.637dB 적용하여 피크를 -2dBFS 이하로 맞췄습니다. 출력 길이는 약 4.3307초와 4.3093초, RMS는 -21.009dBFS와 -21.528dBFS입니다. Scene이 참조하던 두 AudioClip GUID를 새 WAV에 그대로 이어받았으므로 Scene을 수정하지 않고 기존 Elina 통화 연결을 유지했습니다.

## 6. Inspector / Scene 연결 계약 — 구성된 Scene을 읽는 참고자료

### 항상 켜진 통화기

`SimpleDial`의 `ctm`은 주소록, `ringtone`은 기존 Ringtone MP3입니다. 같은 오브젝트의 첫 AudioSource가 주 음성 소스입니다. `feedbackSource`는 다른 AudioSource로 연결하고 `buttonCue`, `connectedCue`, `endedCue`는 manifest의 세 확인음으로 연결합니다.

### 큰 전화 패널

`CallDashboardManager`의 `dial/ctm/numberDisplay/modeDisplay/feedbackText`를 연결합니다. `contactLabels`와 `contactButtons`는 각각 5개이며 이름은 1줄, FREQ와 EXT는 다음 1줄입니다.

| 버튼 | 콜백 |
|---|---|
| FREQ | SetFrequencyMode() |
| PHONE | SetPhoneMode() |
| 0~9 | PressDigit(해당 정수) |
| 지우기 한 글자 | Backspace() |
| 전체 지우기 | ClearNumber() |
| CALL | DialAddress() 또는 기존 CallDialed() |
| END | EndCall() |
| SOS | CallEmergency() |
| 연락처 5개 | SelectContact(0)~SelectContact(4) |

연락처 선택은 현재 모드의 주소를 채우고 발신을 요청합니다. 기존 통화 중이면 LINE BUSY를 표시합니다. 먼저 END로 끊으면 새 연락처에 연결됩니다. 모드를 바꾸면 다른 모드에서 입력했던 문자열이 보존되어 나타납니다.

`CallCrewmate(string)`도 유지하여 원래 이름 기반 UnityEvent 연결을 깨뜨리지 않습니다.

### 큰 머리 부착 피드백

`IncomingCallPopup`에는 `dial/head/popupRoot/callerText`를 연결하고 `head`는 Main Camera입니다. `answerButtonRoot`와 `hangUpButtonRoot`는 버튼 전체 오브젝트입니다. `actionHintText`에는 1~2줄 행동 안내를 연결합니다.

`popupRoot`를 이 스크립트의 host 자신으로 연결하면 오류를 알리고 컴포넌트를 멈춥니다. 별도 자식을 연결해야 숨겼다가 다시 보여 줄 수 있습니다. 코드가 매 LateUpdate에 카메라의 앞/위 축으로 위치와 회전을 맞추므로 고개를 돌려도 화면이 몸에서 떨어진 월드 위치에 남지 않습니다.

수신/통화/SOS 중에는 보이고, 종료/오류는 기본 3초 표시합니다. ANSWER 버튼은 `IncomingCallPopup.Answer()`, END는 `IncomingCallPopup.HangUp()`에 연결합니다. 받기 직후 화면을 숨기지 않으므로 CONNECTED를 확인할 수 있습니다.

## 7. Quest에서 반드시 확인할 검사표

아래는 실행했다고 주장하는 완료표가 아니라 실제 장비에서 채워야 할 수용 검사표입니다.

- [ ] 연락처 목록에 Pedro, Romain, Suzie, Eliza, Elina가 5줄로 읽히며 각 이름·FREQ·EXT가 잘리지 않는다.
- [ ] PHONE 2001과 Pedro 행의 FREQ로 각각 발신하면 동일한 Pedro 응답이 재생된다.
- [ ] 다섯 명 모두 전화 내선과 무전 주파수 양쪽에서 같은 사람의 음성을 재생한다.
- [ ] FREQ에 일부 숫자를 넣고 PHONE으로 바꾸어 다른 숫자를 넣은 뒤 돌아와도 각 입력이 따로 보존된다.
- [ ] 잘못된 내선/주파수와 빈 입력은 연결 성공 대신 명확한 오류를 표시한다.
- [ ] 수신이 오면 고개를 움직여도 팝업이 머리를 따라가고 반복 벨이 들린다.
- [ ] 손으로 ANSWER를 누르면 반복 벨이 즉시 멈추고 목소리가 한 번 재생되며 CONNECTED와 짧은 확인음이 함께 나타난다.
- [ ] 손으로 END를 누르면 목소리가 멈추고 CALL ENDED가 표시되며 다음 통화를 할 수 있다.
- [ ] 녹음을 끝까지 들으면 자동 종료되며 CALL ENDED를 확인할 수 있다.
- [ ] 수신 벨을 END로 끊은 뒤 새 발신을 해도 목소리가 반복 재생되지 않는다.
- [ ] 전화 패널을 닫아도 수신 벨·목소리·큰 상태 피드백이 필요한 동안 계속 동작한다.
- [ ] 동료가 동시에 수신을 시도해도 현재 통화가 바뀌거나 코루틴이 계속 증식하지 않는다.
- [ ] 일반 수신/통화 중에도 SOS가 우선하여 1001 접수 안내를 재생한다.
- [ ] SOS를 여러 번 눌러도 안내가 재시작되거나 Scene 재시작이 여러 번 예약되지 않는다.
- [ ] SOS 접수 뒤 END나 새 동료 발신이 구조 상태를 취소하지 않는다.
- [ ] SOS 안내가 끝난 뒤 0.5초 후 초기 Scene으로 돌아가며 수면 시작 위치와 새 주소록을 확인한다.
- [ ] Eliza와 Elina의 실제 녹음이 명료하게 들리고, 두 동료의 발신 응답·수신 음성이 올바르게 구분된다.
- [ ] 주변 수중 소리와 함께 들어도 목소리·벨·클릭을 구분할 수 있다.
- [ ] 모든 기능을 컨트롤러 없이 손으로 완료한다. 버튼 중복 실행이 없는지도 확인한다.

## 8. 코드 읽는 순서

먼저 `CrewInfo`의 네 칸과 두 생성자를 읽고, `CallTowerManager`에서 같은 index가 같은 사람을 뜻하는 부분을 읽습니다. 다음으로 `SimpleDial`의 상태 표와 QuickDial → IncomingCall → AnswerCall → FinishCall 순서를 읽으면 실제 행동을 따라갈 수 있습니다.

그다음 `CallDashboardManager`의 두 입력 버퍼와 DialAddress를 읽습니다. 마지막으로 팝업이 상태를 읽어 화면만 바꾸는 과정과 동료가 대기 후 전화를 시도하는 코루틴을 읽습니다. C# 각 의미 있는 줄의 한글 주석에는 그 줄의 동작과 필요한 문법 요소를 함께 설명했습니다.
