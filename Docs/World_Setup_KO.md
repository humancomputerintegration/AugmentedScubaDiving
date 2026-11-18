# 수중 환경·생물 구성과 검증 범위

## 지금 제공한 것

원본 Eliza 프로젝트에 있던 물고기·거북이를 재사용하고, 실제로 다운로드한 Grab3D CC0 산호·해초·잠수함을 추가했습니다. Elina는 기본 도형으로 만든 잠수부이며 실제 외부 사람 모델을 다운로드했다고 표시하지 않습니다. 이 문서는 월드 자산의 구성 설명입니다. **Unity 6000.3.27f2 및 Quest에서 실행·임포트·시연을 완료했다는 증거가 아닙니다.**

최종 `Simulation.unity`에는 `World_Asset_Manifest.json`의 GUID와 `World_Placement_Static.json`의 지형 기반 좌표를 사용해 월드가 이미 배치되어 있습니다. 새 배치는 물고기 12마리, 거북이 1마리, Elina 1명, 산호 4개, 해초 5개, 잠수함 1개입니다. 아래 연결 계약은 이미 구성된 Scene을 이해하고 점검하기 위한 참고자료입니다. 원본 모델 파일 내용은 보존했습니다.

| 구성 | 실제 원본 / 새 파일 | 동작 |
|---|---|---|
| Royal Gramma | 원본 `RoyalGramma.fbx`를 Resources에 내용 그대로 복사 | 원본의 실제 `Swimming_Normal` 애니메이션 + 타원 이동 |
| Butterfly Fish | 원본 `ButterflyFish.fbx`를 내용 그대로 복사 | 원본 수영 애니메이션 + 타원 이동 |
| Lionfish | 원본 `Lionfish.fbx`를 내용 그대로 복사 | 원본 수영 애니메이션 + 타원 이동 |
| 기존 Fish | 원본 `Ocean Assets/Prefabs/Fish` | 원본 legacy 애니메이션 유지, 모델 정면축 보정, 타원 이동 |
| Turtle | 원본 `Ocean Assets/Prefabs/Turtle` | 부드러운 이동·방향 전환·작은 몸 기울기. 원본에 뼈/지느러미 애니메이션은 없으며 새 지느러미 애니메이션이 있다고 주장하지 않음 |
| Coral Reef | Grab3D 실제 OBJ·MTL·텍스처 | 산호·바위·바닥 장식이 묶인 실제 모델 |
| Seaweed | Grab3D 실제 OBJ·MTL·텍스처 | 바닥 바위는 고정하고 윗잎만 GPU 셰이더로 흔들림 |
| Submarine | Grab3D 실제 OBJ·MTL·텍스처 | 잠수함 1개와 보수적인 전체 경계 BoxCollider |
| Elina | 새 primitive prefab | 머리·마스크·몸·탱크·팔·다리·오리발. 두 다리의 교차 발차기와 작은 타원 이동 |

새 월드 C# 파일은 세 개입니다. 각 유효 코드 줄에 한국어 설명을 넣었습니다.

| 파일 | 왜 필요한가 |
|---|---|
| `Assets/Diving Simulation/Scripts/World/CreatureSwim.cs` | 생물과 Elina의 이동, 모델 축 보정, 실제 FBX 수영 클립 재생, 선택적인 발차기를 한곳에서 담당 |
| `Assets/Diving Simulation/Scripts/World/ReefModel.cs` | 실제 외부 OBJ 전체를 읽고 원본 재질 이름에 맞춰 URP 재질을 연결 |
| `Assets/Diving Simulation/Scripts/World/ClearWaterLook.cs` | 기본 수중 안개색·밀도만 변경. InformationManager와 MudStorm의 실행 규칙에는 접근하지 않음 |

해초를 위해 `Assets/Diving Simulation/Shaders/SeaweedSway.shader` 한 개를 추가했습니다. 전체 해초 모델에는 바위까지 한 메시로 들어 있으므로 오브젝트 전체를 흔들면 바위도 흔들립니다. 이 셰이더는 모델 높이 0.16m 이하를 고정하고 위쪽으로 갈수록 굽힘을 늘립니다. 별도의 CPU 메시 수정 스크립트나 외부 Unity 패키지는 추가하지 않습니다. 상용 파도/유체 시뮬레이션은 포함하지 않습니다.

## 프리팹 연결 계약

모든 신규 프리팹은 `Assets/Diving Simulation/Prefabs/World`에 있습니다. GUID 전체는 `World_Asset_Manifest.json`에 기록했습니다.

| 프리팹 | 권장 크기 | 핵심 연결 |
|---|---:|---|
| `Swimmer_RoyalGramma` | root scale 1, modelScale 0.15 | waterSurface, seaFloor |
| `Swimmer_ButterflyFish` | root scale 1, modelScale 0.15 | waterSurface, seaFloor |
| `Swimmer_Lionfish` | root scale 1, modelScale 0.12 | waterSurface, seaFloor |
| `Swimmer_OriginalFish` | root scale 1, modelScale 0.35 | waterSurface, seaFloor; 모델 보정 Y=-90° |
| `Swimmer_Turtle` | root scale 1, modelScale 0.7 | waterSurface, seaFloor; 원본 자식 회전 유지 |
| `Swimmer_ElinaPrimitiveDiver` | root scale 1 | waterSurface, seaFloor; 같은 root를 Elina 통화·길찾기 대상으로 사용 |
| `Reef_CoralReef` | root scale 약 3.8–4.5 | 해저 높이에 바닥 배치 |
| `Reef_Seaweed` | root scale 약 1.5–1.8 | 해저 높이에 바닥 배치 |
| `Reef_Submarine` | root scale 0.65 | 물속에 배치; 전체 경계 BoxCollider 포함 |

`CreatureSwim.waterSurface`는 기존 Water 부모 Transform, 원본 Scene fileID **1793043608**입니다. 수면 높이는 y=13입니다. `seaFloor`는 실제 TerrainCollider **1994264211**입니다. TerrainData GUID는 **427b32d391da3c14892a9a0366dc86c3**입니다. 임의의 새 바닥이나 전역 레이어 마스크를 대신 사용하지 않습니다.

생물의 root 위치는 **타원의 중심**입니다. 첫 프레임에 `phase`가 가리키는 타원 위로 이동합니다. 반지름 `radii=(5,3)`이면 중심에서 x 방향으로 5m, z 방향으로 3m까지 갑니다. 같은 종 세 마리는 phase를 0, 2.0944, 4.1888로 나누어 시작할 수 있습니다. 3개 새로운 종과 기존 Fish를 각각 3마리 두면 총 12마리입니다.

생물 root의 +Z가 앞으로 갑니다. 메시 축이 다르면 root를 돌리는 대신 `modelEuler` 또는 `visual` 자식에서 보정합니다. 원본 Fish는 꼬리 뼈가 -X 방향에 있으므로 머리 +X를 root +Z로 맞추기 위해 modelEuler Y=-90°를 사용합니다. 새 FBX 3종의 원본 뼈·모델 계층은 그대로 보존합니다. 최종 정면과 꼬리 방향은 Unity Play에서 반드시 눈으로 확인합니다.

프리팹의 루트는 Editor에서 배치할 수 있습니다. **Resources로 불러오는 물고기와 외부 OBJ는 Play의 Start/Awake 때 실제 모델 자식이 생성됩니다.** Edit Mode에서 메시가 아직 보이지 않는 것을 누락 모델로 오인하지 마세요. Play에서 메시가 생성되지 않으면 Console의 Resources 경로 오류를 확인합니다. 새 FBX 내부 mesh/animation fileID를 추측해 직렬화하지 않으려는 구성입니다. Elina primitive 모델은 프리팹 자체에 들어 있어 Edit Mode에서도 구조를 확인할 수 있습니다.

동일한 생물 root에 기존 `FishMovements`와 새 `CreatureSwim`을 동시에 붙이지 않습니다. 이동 명령이 충돌하기 때문입니다. 원본 legacy Fish 프리팹의 Animation은 유지됩니다. 새 물고기는 복사된 FBX 안의 실제 정상 수영 클립을 Animator에 연결하고, 루트 모션은 끕니다. 따라서 몸 자세는 클립이, 이동 위치는 CreatureSwim이 담당합니다.

최종 Scene의 Elina 프리팹 인스턴스에는 같은 root에 `CrewmateBehavior` 하나가 추가되어 있고 CTM·발신 음성·응답 음성·seed가 연결되어 있습니다. CallTowerManager의 Elina 슬롯에도 **이 이동 root**가 등록되어 있습니다. 별도의 정지된 Elina 마커를 등록하면 거리와 화살표가 수영하는 모델을 따라가지 못합니다. 통화 음성의 출처와 연결은 통화 문서를 따릅니다. 월드 프리팹에는 임의의 대화 음성을 넣지 않았습니다.

## 실제 지형에서 계산한 데모 구역

원본 시작 rig x/z=(364.1,493.4) 아래 Terrain 높이는 약 **17.868m**입니다. 수면 y=13보다 높으므로 시작점에서 아래만 향하면 육지에 막힙니다. +X와 +Z 방향의 물가로 이동해야 합니다.

| 대상 | 권장 위치 / 타원 중심 | 해당 위치의 의미 |
|---|---|---|
| 잠수함 | (401,7,510), scale 0.65 | 원본 모델 바닥이 y=0이므로 배치 y=7은 잠수함의 바닥. 실제 높이는 약 2.67m |
| 거북이 | 중심 (391,9.8,511), radii (5,3) | 잠수함 앞의 독립 경로 |
| Elina | 중심 (407,8.5,511), radii (2.5,1.7) | 잠수함 옆에서 수영하는 연락 대상 |
| Royal Gramma | 중심 (390,10,505), radii (3.5,2) | 비교적 가까운 상층 물고기 |
| Butterfly Fish | 중심 (397,8.7,504), radii (4,2.5) | 잠수함과 겹치지 않도록 앞쪽 경로 |
| Lionfish | 중심 (397,7.7,515), radii (3.5,2) | 뒤쪽 아래 경로 |
| 기존 Fish | 중심 (404,9.5,517), radii (4,2.5) | 잠수함 뒤의 경로 |

세부 산호·해초 바닥 좌표, 몸 반경, 속도, 수직 폭은 `World_Placement_Static.json`에 있습니다. 최종 Scene은 이 지형 기반 배치를 사용합니다. Inspector에서 경로를 다른 값으로 바꾸면 해당 경로를 다시 검증해야 합니다.

실제 Terrain.asset의 1025×1025 heightmap을 읽고 `저장 높이 / 32767 × 50m`로 높이를 복원했습니다. Terrain의 원본 transform은 위치 0, 회전 0, scale 1입니다. 경로마다 360개 각도에서 몸 중심과 좌우·앞뒤 5개 위치를 검사했습니다. 권장 여섯 경로는 모두 수면·해저 여유 범위 안에 있었고, 잠수함의 전체 경계를 몸 반경만큼 확대한 영역과 겹친 표본은 0개였습니다.

이것은 **정적 heightmap 보간 검사**입니다. Unity TerrainCollider의 실제 Raycast 결과나 실기 프레임 속도를 측정한 것이 아닙니다. 런타임 코드도 지정한 바닥 Collider에 직접 5개 광선을 쏘며 몸 반경을 고려해 수면과 바닥 사이를 유지합니다. 너무 얕거나 Terrain 밖이면 경고와 함께 멈춥니다. 다른 생물을 피하는 군집 AI나 복잡한 장애물 우회는 구현하지 않았습니다. 따라서 항로를 바꿀 때 잠수함·바위와의 겹침도 확인해야 합니다.

## 맑은 기본 수색과 MudStorm

최종 활성 Scene에는 `ClearWaterLook` 하나가 연결되어 있습니다. 기본 목표색은 밝은 청록색이며 안개 밀도는 원본 0.08에서 0.012로 낮춥니다. 새 `ClearLagoonWater.mat`은 원본 normal texture를 재사용하는 투명 청록색 URP 재질입니다. 최종 Scene의 실제 물 표면 Renderer에 이 재질이 연결되어 있습니다.

이 스크립트가 변경하는 값은 `RenderSettings.fogColor`, `fogDensity`, `fogMode` 세 개뿐입니다. `RenderSettings.fog`의 켜짐/꺼짐은 원본 InformationManager가 계속 결정합니다. MudStorm의 storm·mudGO·카메라 cullingMask·손 추적·UI에는 접근하지 않습니다. 기본 수색이 맑아진 뒤에도 폭풍 중 Mud UI가 요구대로 남는지 회귀 확인해야 합니다.

`ClearWaterLook`를 끄면 자신이 바꾼 세 값만 되돌립니다. 이 파일을 여러 오브젝트에 중복해서 붙이지 않습니다. 물 재질의 투명도와 안개 밀도는 화면 가독성을 목표로 선택한 설정값이며 실제 Quest 양쪽 눈의 보기 품질을 확인한 결과는 아닙니다.

## 외부 자산의 출처와 재배포 자료

직접 배포처에서 실제 받은 패키지만 포함했습니다.

| 자산 | 제작자 원본 페이지 | 라이선스 |
|---|---|---|
| Coral Reef | https://grab3d.com/models/nature/coral-reef/ | CC0 1.0 Universal |
| Seaweed | https://grab3d.com/models/nature/seaweed/ | CC0 1.0 Universal |
| Submarine | https://grab3d.com/models/vehicles/submarine/ | CC0 1.0 Universal |

공식 라이선스 설명은 https://grab3d.com/license/ 입니다. 원본 ZIP에서 나온 `LICENSE.txt`를 각 모델 폴더에 그대로 보존했습니다. 원본 제작자는 절차적 Blender 생성 모델이며 생성 스크립트에 AI 코딩 보조를 사용했다고 명시합니다. 이 공개 내용도 LICENSE에 남아 있습니다.

원본 OBJ·MTL·JPG 내용은 그대로 복사했습니다. Unity용 meta, URP 재질, 프리팹, 해초 굽힘 셰이더만 추가했습니다. 원본 metallic/roughness 이미지도 보존했지만 현재 URP 재질은 별도 채널 패킹 없이 일정한 metallic/smoothness 값을 사용합니다. Unity texture importer의 최대 크기는 1024로 지정했습니다. 실제 Quest 메모리/성능 측정값은 아닙니다.

다운로드 URL, ZIP SHA-256, 파일 크기, 보존된 LICENSE 경로는 `Assets/ThirdParty/Grab3D/SOURCE_AND_LICENSE.json`과 `World_Asset_Manifest.json`에 있습니다. 기존 fish 3종은 사용자가 준 프로젝트의 자산을 재사용한 것이며 새 Grab3D 자산의 CC0 라이선스를 기존 물고기에 임의로 적용하지 않습니다.

## 소스 검사와 실제 실행 검사를 구분하기

완료한 것은 C# 구문 파서 확인, 새 prefab/material YAML·GUID 참조 확인, 실제 FBX의 뼈·수영 클립 목록 확인, 외부 ZIP/라이선스 확인, 지형 표본 계산입니다. Blender 스튜디오 이미지를 제공할 경우 파일명과 이미지 안에 **NOT UNITY OR QUEST CAPTURE**를 표시합니다. 이는 자산 외형 점검용이며 Unity 화면이나 기능 통과 증거가 아닙니다.

Unity에서 추가로 확인할 항목은 다음과 같습니다.

- [ ] Console 컴파일·임포트 오류 없이 세 새 C#와 SeaweedSway 셰이더가 로드된다.
- [ ] Play에서 각 Resources 모델이 실제로 나타나고 재질이 분홍색이 아니다.
- [ ] 물고기 머리가 진행 방향을 향하며, 실제 꼬리/지느러미 수영 클립이 반복된다.
- [ ] 거북이가 제자리 정지 상태가 아닌 부드러운 경로로 움직인다.
- [ ] 해초의 바위는 고정되고 잎 윗부분만 흔들린다.
- [ ] 잠수함 1개가 보이고, Elina의 두 다리가 번갈아 움직인다.
- [ ] Elina 통화·거리·길찾기가 수영하는 동일 root를 가리킨다.
- [ ] 생물과 Elina가 해저·수면·잠수함을 뚫지 않는다.
- [ ] 플레이어가 잠수함과 해저에 충돌하며 손을 놓으면 불필요한 이동이 없다.
- [ ] 맑은 기본 수역에서 STATUS/RADIO/CREW를 읽을 수 있고 MudStorm 규칙도 유지된다.
- [ ] Quest 양쪽 눈에서 해초·투명 수면·생물 재질이 정상이며 목표 프레임 속도를 유지한다.

참고한 공식 Unity API: [Collider.Raycast](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Collider.Raycast.html), [Quaternion.LookRotation](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Quaternion.LookRotation.html), [AnimationClipPlayable.Create](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/animations/animationclipplayable/create), [URP custom shader lighting](https://docs.unity.com/en-us/engine/6000.3/manual/materials-and-shaders/shaders/writing-custom-shaders-urp/use-built-in-shader-methods/lighting), [Single-pass instanced custom shaders](https://docs.unity.com/en-us/engine/6000.3/manual/xr/graphics/stereo-rendering/single-pass-instancing).
