# Blender 자산 미리보기 — 실제 Unity/Quest 캡처 아님

`BLENDER_ASSET_PREVIEW_NOT_UNITY.png`는 Blender 4.3.2에서 실제 프로젝트 FBX와 실제로 받은 Grab3D OBJ를 불러와 렌더한 자산 외형 확인 이미지입니다. 게임 Scene의 스크린샷이 아니며, 통화·UI·손 추적·MudStorm·프레임 속도 등 Unity 기능이 통과했다는 증거가 아닙니다.

원본 FBX 물고기 세 종에는 실제 Swimming_Normal 클립과 뼈가 있습니다. 프리뷰는 그 클립의 한 시점입니다. Blender가 원본 FBX 재질을 Alpha=0으로 가져와 첫 렌더에서 물고기가 보이지 않는 문제가 있었으며, 제공 Unity 프리팹에 명시적으로 연결한 불투명 URP 재질과 같은 의도로 프리뷰 재질의 Alpha를 1로 맞췄습니다. 원본 FBX 파일 내용은 수정하지 않았습니다.

이미지의 스튜디오 배경·조명·전시용 크기와 위치는 최종 Unity Scene과 다릅니다. 거북이는 원본 정적 모델이며 실제 지느러미 애니메이션이 추가됐다고 표시하지 않습니다. Elina primitive 잠수부는 이 자산 모음 이미지에 포함하지 않았습니다.

외부 모델의 제작자와 CC0 LICENSE는 각 모델 폴더 및 World_Asset_Manifest.json에 보존되어 있습니다. 실제 Unity와 Quest 캡처는 사용자의 Editor/HMD 실행 후 별도로 기록해야 합니다.

## UI 배치 도식

`UI_LAYOUT_PREVIEW_NOT_UNITY.png`와 같은 이름의 SVG는 최종 Scene에 저장된 RectTransform과 글자 크기, 색, 버튼 배치를 읽어서 그린 2D 배치 점검 그림입니다. 연락처 주파수와 수심·시간은 읽기 쉬운 예시 값입니다. 실제 실행 시 FREQ는 주소록이 배정합니다. UI를 Unity에서 렌더한 캡처가 아니므로 양안 가독성·손 도달 범위·실제 클릭·shader 동작의 증거로 사용하지 않습니다. PNG와 SVG 자체에도 이 구분을 표시했습니다.
