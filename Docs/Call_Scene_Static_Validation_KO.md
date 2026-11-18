# 최종 Scene 통화 연결 정적 검토

최종 Simulation Scene의 통화 관련 직렬화 계약 238개를 검사하여 불일치 0개를 확인했습니다. Scene 파일은 수정하지 않았습니다. 사용자가 추가로 제공한 실제 Elina 녹음을 연결한 상태의 결과입니다.

검사 범위는 Scene YAML, GUID 대상 파일, 공개 콜백과 인수, PrefabInstance의 추가 컴포넌트 연결입니다. Unity가 Scene을 실제로 가져와 실행한 검증, 화면 렌더링, 헤드셋 음량·손 추적 검증은 포함하지 않습니다.

## 통화 시스템

- SimpleDial 1618581351, CallDashboardManager 122658819, IncomingCallPopup 2126120168은 항상 활성인 00_Systems/Call System에 연결되어 있습니다.
- 주 음성 AudioSource 1618581352는 이 오브젝트의 첫 AudioSource이며, feedbackSource 9300000012와 분리되어 있습니다. 두 소스의 초기 2D 설정·자동 재생 꺼짐·반복 꺼짐을 확인했습니다.
- 주소록과 화면이 같은 SimpleDial/CallTowerManager를 참조합니다. 클릭·연결·종료 확인음 3개와 벨소리·잘못된 주소 안내·SOS 음원 참조가 존재합니다.
- 팝업 host와 숨기는 popupRoot는 서로 다른 오브젝트입니다. head와 InformationManager.player는 실제 Main Camera로 이어지는 Transform 1692552786을 사용합니다. XR Origin Hands 프리팹이 상속한 XR Origin의 카메라 객체까지 소스 연결을 대조했습니다.
- 통화 중 주변음을 줄이는 CallAudioDucking은 같은 통화기와 InformationManager의 실제 수중 AudioSource를 참조합니다. 정상 음량 0.16, 통화 중 목표 0.035입니다.

## 다섯 동료의 실제 연결

| 동료 | 게임 내선 | CTM Transform | 추가 통화 컴포넌트 | 발신 응답 파일 | 수신용 파일 |
|---|---|---:|---:|---|---|
| Pedro | 2001 | 264486946 | 873617700 | pedro_asnwer.mp3 | pedro_calling.mp3 |
| Romain | 2002 | 1150354825 | 1639722122 | Romain_answering_howsitgoing.mp3 | Romain_calling_boat.mp3 |
| Suzie | 2003 | 1740762272 | 1226253113 | suzie_answer.mp3 | suzie_calling.mp3 |
| Eliza | 2004 | 9400000083 | 9400000085 | Eliza_answering_level_adjusted.wav | Eliza_calling_level_adjusted.wav |
| Elina | 2005 | 9400000043 | 9400000045 | Elina_answering.wav | Elina_calling.wav |

CTM의 다섯 Transform과 같은 프리팹 인스턴스에 붙은 GameObject를 대조했습니다. 각 원본 프리팹에는 중복 통화 컴포넌트가 없으며, Scene의 m_AddedComponents가 해당 GameObject에 CrewmateBehavior 하나를 추가합니다. Eliza와 Elina는 World Scene 계약의 transform/gameObject/behavior와도 일치합니다.

무전 주파수는 useFixedFrequencies=0으로 실행 시 생성되며, 내선은 2001~2005입니다. 최종 Scene의 동료 대기 구간은 min_wait=10, max_wait=45초입니다.

## 버튼과 손 입력 연결

신규 통화 버튼 25개를 확인했습니다.

| 기능 | 개수 | 콜백 |
|---|---:|---|
| 연락처 | 5 | SelectContact(0~4) |
| 숫자 | 10 | PressDigit(0~9) |
| 입력 모드 | 2 | SetFrequencyMode(), SetPhoneMode() |
| 지우기 | 2 | ClearNumber(), Backspace() |
| 발신/종료 | 2 | DialAddress(), EndCall() |
| 수신 받기/종료 | 2 | IncomingCallPopup.Answer(), HangUp() |
| 원형 메뉴·왼팔 SOS | 2 | CallEmergency() |

각 Button의 UnityEvent 대상·메서드·인수 형식과 runtime 활성 상태를 확인했습니다. 각 버튼에는 같은 UGUI Button을 참조하는 HandUIButton 하나와 활성 BoxCollider가 있으며 UI layer 5입니다. 실제 접촉·조준 선택은 Quest에서 확인해야 합니다.

## 화면 크기의 정적 자료

| 항목 | 글자 크기 | Text Rect 크기 | 예상 내용 |
|---|---:|---|---|
| 연락처 5개 | 29 | 374 × 80 | 이름 + FREQ/EXT 2줄 |
| 큰 통화 상태 | 34 | 740 × 170 | 상태·이름·주파수 최대 3줄 |
| 팝업 행동 안내 | 23 | 740 × 56 | 행동 안내 1~2줄 |
| 전화 패널 상태 | 22 | 384 × 83 | 상태 최대 3줄 |

치수와 필드 연결을 확인했습니다. 실제 Unity 글꼴 생성, 화면 겹침, Quest에서의 가독성은 렌더링·헤드셋 검증이 남아 있습니다.

## 재현 가능한 증거

- Call_Scene_Static_Validation.json: 각 검사와 Scene/오브젝트 ID, 콜백, 오디오 대상.
- Call_Audio_Manifest.json 및 Elina_Recording_Validation.json: 실제 녹음 출처, 원본 보존, 파형·음량·GUID 검사.
- Call_Logic_Test_Results.json: 최종 통화 C# 6개를 직접 링크한 C#9 하네스 14개 시나리오·272개 검사. 엔진 API는 좁은 test double을 사용했습니다.

검토한 Scene SHA-256: `cd63ba065cb0c585cb2708af49e730aed77e17b01c07fa0ce806ce3f647180b7`
