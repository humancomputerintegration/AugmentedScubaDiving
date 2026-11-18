/* 원본 동작을 유지하고 각 의미 있는 코드 줄에 한국어 해설을 추가했습니다. */
using UnityEngine; // 위치, 회전, GameObject와 MonoBehaviour 등 Unity의 기본 기능을 가져옵니다.

//Moves each map marker to match its object's real position on the terrain
//The map image is assumed to show exactly the whole terrain, north (+Z) up.

public class MinimapUI : MonoBehaviour // 원본 미니맵의 표식을 실제 물체 위치에 맞춥니다.
{
    [System.Serializable] // 이 묶음의 각 필드를 Unity Inspector와 Scene 파일에 저장할 수 있게 합니다.
    public class MapMarker // 실제 물체 하나와 지도 표식 하나를 묶는 자료형입니다.
    {
        public Transform target;          // the real object | 지도에 표시할 실제 물체입니다.
        public RectTransform icon;        // its marker on the map | 지도 안에서 움직일 표식입니다.
        public bool rotateWithTarget;     // turn the icon with the object's heading | 표식도 실제 물체의 방향을 따라 회전할지 정합니다.
    }

    public Terrain terrain;               // used in whole-terrain mode | 전체 지형 모드의 원점과 크기를 제공하는 Terrain입니다.
    public RectTransform mapArea;         // MapImage | 지도 이미지가 차지하는 UI 사각형입니다.
    public MapMarker[] markers; // 업데이트할 표식 묶음의 배열입니다.

    [Header("Follow mode")] // Inspector에 추적 모드 설정 제목을 표시합니다.
    public bool followPlayer = true;      // centre the map on the diver | true이면 플레이어를 지도 중심에 놓습니다.
    public Transform player;              // drag in: Main Camera | 중심에 놓을 플레이어 Transform입니다.
    public float viewRange = 150f;        // metres shown across the map's width | 지도 가로폭이 실제 몇 미터를 나타내는지 정합니다.

    private Vector3 worldMin; // 전체 지형의 세계 원점을 저장합니다.
    private Vector3 worldSize; // 전체 지형의 x/y/z 크기를 저장합니다.

    void Start() // 첫 Update 전에 한 번 실행하는 원본 초기화 함수입니다.
    {
        worldMin = terrain.transform.position; // Terrain의 세계 위치를 지도 원점으로 기록합니다.
        worldSize = terrain.terrainData.size; // TerrainData에서 실제 지형 크기를 읽습니다.
    }

    void LateUpdate() // 일반 Update가 끝난 뒤 위치 표시를 맞추는 함수입니다.
    {
        Vector2 mapSize = mapArea.rect.size; // 현재 지도 UI의 가로·세로 크기를 읽습니다.
        Vector2 half = mapSize * 0.5f; // 지도 중앙에서 각 가장자리까지의 거리를 계산합니다.

        foreach (MapMarker m in markers) // 등록된 표식을 하나씩 같은 방식으로 처리합니다.
        {
            if (m.target == null || m.icon == null) continue; // 물체나 아이콘 연결이 빠진 표식은 건너뜁니다.

            Vector2 pos; // 계산할 지도 좌표 두 개를 담을 변수를 준비합니다.

            if (followPlayer && player != null) // 플레이어 중심 모드와 연결이 모두 준비된 경우입니다.
            {
                // offset from the diver in metres (x = east, z = north)
                Vector3 offset = m.target.position - player.position; // 플레이어에서 표식 대상까지의 세계 위치 차이입니다.

                // metres -> map units (same scale on both axes so distances stay true)
                float unitsPerMetre = mapSize.x / viewRange; // 실제 1m가 지도에서 몇 UI 단위인지 계산합니다.
                pos = new Vector2(offset.x, offset.z) * unitsPerMetre; // 세계 x/z를 지도의 가로/세로로 바꾸고 같은 배율을 적용합니다.

                // keep far-away markers pinned to the edge of the map
                pos.x = Mathf.Clamp(pos.x, -half.x, half.x); // 가로 범위를 벗어난 표식을 지도 가장자리에 붙입니다.
                pos.y = Mathf.Clamp(pos.y, -half.y, half.y); // 세로 범위를 벗어난 표식도 가장자리에 붙입니다.
            }
            else // 바로 앞 조건이 거짓인 경우의 처리를 시작합니다.
            {
                // whole terrain mode (original behaviour)
                float u = Mathf.Clamp01((m.target.position.x - worldMin.x) / worldSize.x); // 전체 지형에서 대상의 동서 위치를 0~1 비율로 바꿉니다.
                float v = Mathf.Clamp01((m.target.position.z - worldMin.z) / worldSize.z); // 전체 지형에서 대상의 남북 위치를 0~1 비율로 바꿉니다.
                pos = new Vector2((u - 0.5f) * mapSize.x, (v - 0.5f) * mapSize.y); // 0~1 비율을 중앙이 0인 UI 좌표로 변환합니다.
            }

            m.icon.anchoredPosition = pos; // 계산한 위치를 표식의 앵커 기준 좌표에 적용합니다.

            if (m.rotateWithTarget) // 방향을 따라 돌도록 설정한 표식만 회전합니다.
                m.icon.localEulerAngles = new Vector3(0f, 0f, -m.target.eulerAngles.y); // 세계의 좌우 회전각을 지도 평면의 z 회전으로 바꿉니다.
        }
    }

}
