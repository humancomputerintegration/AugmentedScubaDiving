# HCI Diver — 손 조작 통합 프로젝트

## 먼저 확인할 상태

이 폴더는 받은 **Eliza 기능 버전 ZIP을 바탕으로 수정한 전체 Unity 프로젝트**입니다. Unity **6000.3.27f2**를 사용합니다. `Assets`, `Packages`, `ProjectSettings`가 포함되어 있고, 주 Scene에 기능·오브젝트·Inspector 참조를 연결했습니다. 스크립트를 하나씩 복사하거나 새 Scene을 조립할 필요는 없습니다.

**코드·파일·Scene 연결 검사는 완료했지만, 이 작업 환경에는 Unity와 Quest가 없어 실제 Unity 가져오기·컴파일·빌드·헤드셋 실행은 완료하지 못했습니다.** 따라서 아래 문서의 장비 검사 칸은 비워 두었습니다. 전달 ZIP을 열어 Console과 손 동작을 확인해야 시연 준비가 끝납니다.

최근 받은 `Elina_answering(1).m4a`와 `Elina_Calling(1).m4a`의 **실제 목소리**를 포함했습니다. 원본 두 파일은 `Docs/Audio_Sources`에 보존하고, Unity 재생용 WAV를 Elina의 발신 응답·수신 통화에 각각 연결했습니다.

## 1. 새 프로젝트로 여는 순서

1. ZIP을 완전히 풉니다. 예를 들어 `C:/UnityProjects/HCI_Diver_Hands_Final`처럼 새 폴더를 사용합니다.
2. Unity Hub에서 디스크의 기존 프로젝트를 추가하는 **Add / Add project from disk**를 선택합니다.
3. **Assets·Packages·ProjectSettings 세 폴더가 바로 보이는 `HCI_Diver_Hands_Final` 폴더**를 선택합니다. ZIP 바깥 폴더나 Assets만 선택하지 않습니다.
4. Editor 버전은 **6000.3.27f2**로 선택합니다. `ProjectSettings/ProjectVersion.txt`에도 이 버전을 기록했습니다.
5. 첫 가져오기와 Package Manager의 패키지 복원이 끝날 때까지 기다립니다. 새로운 프로젝트의 Library는 이때 생성됩니다.
6. `Assets/Diving Simulation/Scenes/Simulation.unity`를 엽니다. 이 Scene은 빌드 목록에도 등록되어 있습니다.
7. **Console의 빨간 오류를 먼저 확인**합니다. 오류가 있다면 첫 오류부터 해결한 뒤 Play로 넘어갑니다. XR namespace 오류가 다시 보이면 `Docs/Packages_and_Validation_KO.md`의 패키지 버전·연결을 확인합니다.
8. 상단 메뉴 **HCI > Validate Hands Demo**를 실행합니다. 이 메뉴는 설정과 참조를 읽어 검사하며 프로젝트를 자동 변경하지 않습니다. Console 결과를 확인한 뒤 Play합니다.

`.vs`, 기존 `Library`, `Temp`, 편집기 캐시와 잠금 파일은 ZIP에 넣지 않았습니다. 예전에 보였던 Visual Studio 인덱싱 파일은 프로젝트 구성요소가 아닙니다.

### Quest에 실행하는 구성

이 프로젝트의 저장된 XR 설정은 **Quest에 Android APK를 설치하는 방식**입니다. Android OpenXR와 손 추적 기능이 연결되어 있고, Standalone XR Loader는 등록되어 있지 않습니다. 따라서 PC의 Play 버튼과 Quest Link만으로 손 시연이 준비됐다고 간주하면 안 됩니다. PC Play는 우선 키보드·마우스 검사용입니다.

Unity에서 **File > Build Profiles**를 열어 Android 프로필을 선택합니다. 프로필이 없으면 **Add Build Profile > Android > Add Build Profile**, 이어 **Switch Profile**로 활성화합니다. Android가 없으면 해당 Editor의 Android 지원 모듈을 설치해야 합니다. APK 실행을 위해 Export Project와 Build App Bundle을 끄고, 연결되어 인식된 Quest를 Run Device로 선택한 뒤 **Build and Run**으로 빌드·설치합니다. 사용자 PC의 Android 도구 설치와 Quest 연결·빌드 성공 여부는 아직 확인하지 못했습니다.

메뉴 순서 참고: [Unity 6000.3 공식 Android 빌드 문서](https://docs.unity.com/en-us/engine/6000.3/manual/platform-specific/android/building-and-delivering/build-process).

## 2. 조작 방법

### Quest에서 손으로

| 하고 싶은 일 | 동작 |
|---|---|
| 보고 있는 방향으로 이동 | **왼손을 주먹으로 쥐고 유지**합니다. 고개 방향이 실제 이동 방향입니다. 손을 펴면 전진 명령이 멈춥니다. |
| 위로 수영 / 아래로 수영 | **물속에서 오른손 엄지척을 위 / 아래**로 향하게 합니다. 나머지 손가락은 접습니다. |
| 전진하면서 동시에 상승·하강 | 왼손 주먹과 오른손 엄지 방향을 동시에 사용합니다. |
| 가까운 버튼 선택 | **검지를 펴서 버튼에 닿게** 합니다. 다시 누르려면 손가락을 뗐다가 닿게 합니다. |
| 떨어진 버튼 선택 | 손 레이저로 버튼을 가리키고 **손을 편 상태에서 주먹으로 한 번 쥡니다.** 계속 쥐고 있어도 반복 클릭하지 않습니다. |
| 화면 원형 메뉴 열기 / 닫기 | 기존처럼 왼손 손바닥을 바깥쪽으로 향하고 **오른쪽 / 왼쪽으로 스와이프**합니다. |
| 왼팔 원형 메뉴 | 왼팔 위의 INFO·SOS·CALL·FIND·CREW를 **오른손**으로 누릅니다. 화면 메뉴와 같은 기능입니다. |
| 손전등 | 오른손목의 손전등 버튼을 **왼손**으로 누릅니다. LIGHT ON/OFF를 확인합니다. |
| 전화 걸기 | CALL을 열고 연락처를 누르거나, FREQ/PHONE을 골라 숫자를 넣고 CALL을 누릅니다. |
| 전화 받기 / 끊기 | 큰 통화 팝업의 ANSWER / END를 누릅니다. 상태 글자와 확인음도 함께 확인합니다. |

오른손의 엄지척은 검지 터치와 다른 자세로 처리합니다. UI를 실제로 누르는 손은 같은 순간 이동 명령에 쓰지 않습니다. 손 추적이 끊기면 해당 입력을 중립으로 되돌리며, 손이 다시 보였다는 이유만으로 버튼을 자동 클릭하지 않습니다.

### PC에서 키보드·마우스로

| 입력 | 동작 |
|---|---|
| W / A / S / D | 시야 기준 전진 / 왼쪽 / 후진 / 오른쪽 |
| 오른쪽 마우스 버튼을 누른 채 이동 | 시점 회전 |
| 왼쪽 마우스 클릭 | 화면과 개발용 손목 UI의 버튼 선택 |
| E 또는 U / Q 또는 J | **물속에서만** 상승 / 하강 |
| M | 화면 원형 메뉴 열기·닫기 |

실제 XR 실행에서는 손 입력을 사용합니다. 손이 잠시 안 보인다는 이유로 키보드 모드로 전환하지 않습니다. PC 조작 검사는 손 추적 검사를 대신하지 않습니다.

## 3. 처음 물속으로 들어가는 길

원본 시작점은 **육지 쪽**입니다. 바로 아래로만 가려 하면 지형에 막힙니다. Scene 좌표 기준 **+X·+Z 방향의 바다 쪽**으로 먼저 이동합니다. FIND 메뉴에서 Elina를 선택하면 잠수함 옆에서 수영하는 같은 Elina를 향하는 안내를 확인할 수 있습니다.

왼손 주먹을 유지하며 바다 쪽을 보고, 필요하면 시선을 아래로 향해 물속으로 들어갑니다. 머리가 수면 아래에 들어간 뒤 오른손 엄지 위·아래 명령을 점검합니다. 수면 밖에서는 엄지만으로 잠수하지 않습니다. 이는 요청한 “물속에서 엄지로 상승·하강” 조건입니다.

산호·해초·물고기·거북이·잠수함·Elina는 시작점 앞쪽 수중 구역에 배치되어 있습니다. 일부 모델은 **Play 때 Resources에서 생성**되므로 Edit Mode에서 빈 프리팹처럼 보일 수 있습니다.

## 4. 전화 주소와 Elina 음성

| 연락처 | PHONE 내선 | FREQ |
|---|---|---|
| Pedro | 2001 | 현재 연락처 행에 표시된 3자리 |
| Romain | 2002 | 현재 연락처 행에 표시된 3자리 |
| Suzie | 2003 | 현재 연락처 행에 표시된 3자리 |
| Eliza | 2004 | 현재 연락처 행에 표시된 3자리 |
| Elina | 2005 | 현재 연락처 행에 표시된 3자리 |

FREQ는 실행할 때 중복 없이 100~999에서 배정합니다. PHONE은 게임 안의 고정 내선이며 실제 전화망으로 발신하지 않습니다. 연락처 행을 누르면 해당 인물에게 바로 발신합니다. **1001은 두 모드 공통 SOS**입니다. SOS는 구조 안내 뒤 Scene을 다시 시작하므로 검사 순서에서 마지막에 둡니다.

Elina에게 발신했을 때는 `Elina_answering.wav`, Elina의 수신 전화를 받았을 때는 `Elina_calling.wav`를 재생합니다. 통화음은 거리 때문에 작아지지 않도록 2D로 구성하고, 통화 중 수중 주변음을 낮췄습니다. 실제 Quest의 출력 음량과 청취 명료도는 장비에서 확인해야 합니다.

## 5. Hierarchy와 파일 읽는 순서

| Hierarchy 루트 | 들어 있는 것 |
|---|---|
| `00_Systems` | 정보·통화·UI 입력·포인터·상단 HUD 관리 |
| `01_XR_Hands_Rig` | 실제 머리·손 추적, 한 곳에서 계산하는 이동 |
| `02_Hand_UI` | 화면 메뉴, 왼팔 메뉴, 오른손목 손전등, 통화·정보 패널 |
| `03_Reef_and_Marine_Life` | 새 수중 생물, 산호·해초·잠수함, 수영하는 Elina |
| `04_Environment` | 원본 지형·수면·조명·배경 |
| `05_Crew_Boats` | 원본 동료 보트 |
| `99_Original_References_Inactive` | 교체 전 UI·생물 등 참조 보존용 비활성 원본 |

프로젝트 기능 C# **40개**의 의미 있는 코드 줄에 한글 설명을 넣었습니다. 코드는 `Assets/Diving Simulation/Scripts`에 있습니다. Unity 패키지·샘플·외부 자산의 공급자 코드는 별도이며, 프로젝트 기능 코드처럼 다시 작성하지 않았습니다.

1. `Docs/Acceptance_Checklist_KO.md` — 발표 전 실제 손 동작 검사표.
2. `Docs/Code_Reading_Guide_KO.md` — 구성요소와 실행 순서, 숫자로 따라가는 원리.
3. `Docs/Hands_Input_KO.md` — 손 판정·이동·추적 상실·UI 충돌 방지.
4. `Docs/Calls_Setup_KO.md` — 상태 전환·두 주소 체계·실제 음성 연결.
5. `Docs/World_Setup_KO.md` — 생물·환경·외부 자산 출처와 표현 범위.
6. `Docs/Validation_Summary_KO.md` — 실제로 끝낸 검사와 아직 하지 못한 검사.

`Docs/Asset_Previews`의 이미지는 **UI 배치 도식과 Blender 자산 미리보기**입니다. 실제 Unity Game View나 Quest 화면 캡처가 아닙니다.

## 6. 시연 완료 판정

`Acceptance_Checklist_KO.md`의 실제 장비 칸을 하나씩 확인합니다. 실패하면 번호·정확한 행동·Console 첫 오류·화면 또는 짧은 영상을 함께 기록합니다. 그 항목은 통과로 바꾸지 않고 수정 후 같은 동작을 다시 검사합니다. **기능 구현 및 정적 검사 완료와 Quest 시연 통과는 서로 다른 상태입니다.**
