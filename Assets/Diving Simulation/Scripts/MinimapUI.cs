using UnityEngine; // 위치, 방향, 사각형처럼 지도 계산에 필요한 Unity 기능을 가져옵니다.
using UnityEngine.UI; // 지형 그림의 일부를 보여 주는 RawImage를 사용합니다.

public class MinimapUI : MonoBehaviour // 실제 물체의 위치를 작은 지도에 표시하는 컴포넌트입니다.
{
    [System.Serializable] // 아래 묶음을 Inspector와 Scene 파일에 저장할 수 있게 합니다.
    public class MapMarker // 실제 물체 하나와 지도 표식 하나를 짝지어 보관합니다.
    {
        public Transform target; // 지도에서 찾을 실제 물체입니다.
        public RectTransform icon; // 지도 안에 놓을 표식입니다.
        public bool rotateWithTarget; // 켜면 위를 향하는 표식이 대상의 시선에 맞춰 돌아갑니다.
        public bool hideOutsideMap; // 보트와 보물은 지도 범위 안에 있을 때만 보여 줍니다.
        public RectTransform nameLabel; // 동료의 전체 이름을 보여 주는 작은 글자 영역입니다.
        public Vector2 nameOffset = new Vector2(0f, 9f); // 이름을 표식의 어느 쪽에 놓을지 정합니다.
    }

    public Terrain terrain; // 지도의 원점과 전체 크기를 알려 주는 기존 바닥 지형입니다.
    public RectTransform mapArea; // 지도 표식과 그림을 배치할 기존 UI 영역입니다.
    public MapMarker[] markers; // 동료, 보트, 사용자처럼 지도에 표시할 대상들입니다.
    public RawImage terrainImage; // 실제 지형 높이로 그린 전체 지도의 그림입니다.
    public float edgePadding = 12f; // 테두리에서 이만큼 안쪽에 표식을 두어 잘리지 않게 합니다.

    [Header("Follow mode")] // Inspector에서 플레이어 추적 설정을 따로 보여 줍니다.
    public bool followPlayer = true; // 켜면 사용자가 항상 지도 가운데에 있습니다.
    public Transform player; // 현재 사용자의 위치와 시선을 알려 주는 Main Camera입니다.
    public float viewRange = 80f; // 지도 가로 전체가 실제로 몇 미터인지 정합니다.

    private Vector3 worldMin; // 전체 지도에서 왼쪽 아래에 해당하는 세계 위치입니다.
    private Vector3 worldSize; // 전체 지형의 가로, 높이, 세로 길이입니다.
    private bool terrainReady; // 지형 연결이 올바른지 기억해 잘못된 나눗셈을 막습니다.

    void Start() // 시작할 때 변하지 않는 지형 정보를 한 번 읽습니다.
    {
        if (terrain == null || terrain.terrainData == null) return; // 지형 연결이 없으면 지형 그림 계산을 생략합니다.
        worldMin = terrain.transform.position; // 지형의 세계 원점을 읽습니다.
        worldSize = terrain.terrainData.size; // 지형 그림 한 장이 덮는 실제 길이를 읽습니다.
        terrainReady = worldSize.x > 0f && worldSize.z > 0f; // 가로와 세로 길이가 모두 양수일 때 사용할 수 있습니다.
    }

    void LateUpdate() // 사용자가 이동한 다음 지도 그림과 표식을 함께 갱신합니다.
    {
        if (mapArea == null) return; // 지도 영역 연결이 없으면 안전하게 이번 갱신을 끝냅니다.
        Vector2 mapSize = mapArea.rect.size; // 현재 지도 UI의 가로와 세로 크기를 읽습니다.
        if (mapSize.x <= 0f || mapSize.y <= 0f) return; // 아직 UI 크기가 없으면 나누기 계산을 하지 않습니다.
        float range = Mathf.Max(1f, viewRange); // 가로 범위를 최소 1m로 제한해 0으로 나누지 않습니다.
        bool isFollowing = followPlayer && player != null; // 추적 설정과 사용자 연결이 모두 있는지 확인합니다.
        UpdateTerrainImage(mapSize, range, isFollowing); // 표식과 같은 좌표 기준으로 바탕 그림을 먼저 맞춥니다.
        if (markers == null) return; // 등록된 표식 배열이 없으면 그림만 보여 줍니다.
        Vector2 half = mapSize * 0.5f; // 지도 중앙에서 가장자리까지의 거리를 구합니다.
        float limitX = Mathf.Max(0f, half.x - edgePadding); // 가로 표식이 머물 수 있는 가장자리입니다.
        float limitY = Mathf.Max(0f, half.y - edgePadding); // 세로 표식이 머물 수 있는 가장자리입니다.

        foreach (MapMarker marker in markers) // 모든 표식을 하나씩 실제 위치에 맞춥니다.
        {
            if (marker == null || marker.target == null || marker.icon == null) continue; // 연결이 빠진 표식만 건너뜁니다.
            Vector2 position; // 이번 표식의 지도 좌표를 담을 변수를 만듭니다.
            if (isFollowing) // 사용자 중심 지도에서는 사용자와 대상 사이의 거리를 사용합니다.
            {
                Vector3 offset = marker.target.position - player.position; // 대상이 사용자보다 동쪽과 북쪽으로 얼마나 떨어졌는지 구합니다.
                position = new Vector2(offset.x, offset.z) * (mapSize.x / range); // 동쪽은 오른쪽, 북쪽은 위쪽으로 같은 배율을 적용합니다.
            }
            else // 전체 지형 지도에서는 지형의 원점을 기준으로 위치를 구합니다.
            {
                if (!terrainReady) continue; // 지형 크기를 읽지 못했다면 이 표식을 계산하지 않습니다.
                float u = (marker.target.position.x - worldMin.x) / worldSize.x; // 지형 가로에서 대상의 위치를 0~1 비율로 바꿉니다.
                float v = (marker.target.position.z - worldMin.z) / worldSize.z; // 지형 세로에서 대상의 위치를 0~1 비율로 바꿉니다.
                position = new Vector2((u - 0.5f) * mapSize.x, (v - 0.5f) * mapSize.y); // 그림 중앙을 원점으로 하는 UI 좌표로 바꿉니다.
            }

            bool isInside = Mathf.Abs(position.x) <= limitX && Mathf.Abs(position.y) <= limitY; // 실제 위치가 테두리 안에 들어왔는지 확인합니다.
            bool shouldShow = !marker.hideOutsideMap || isInside; // 동료는 멀어도 표시하고 보트와 보물만 범위를 따릅니다.
            if (marker.icon.gameObject.activeSelf != shouldShow) marker.icon.gameObject.SetActive(shouldShow); // 보이는 상태가 바뀔 때만 UI를 켜거나 끕니다.
            if (!shouldShow) continue; // 보이지 않는 표식의 나머지 계산은 생략합니다.
            position.x = Mathf.Clamp(position.x, -limitX, limitX); // 멀리 있는 표식도 가로 테두리 안에서 보이게 합니다.
            position.y = Mathf.Clamp(position.y, -limitY, limitY); // 멀리 있는 표식도 세로 테두리 안에서 보이게 합니다.
            marker.icon.anchoredPosition = position; // 완성한 좌표로 표식을 옮깁니다.
            UpdateNameLabel(marker, position, half, isInside); // 이름도 지도 안에 남게 하고 먼 동료들의 이름 겹침을 막습니다.
            if (!marker.rotateWithTarget) continue; // 동료 이름처럼 회전하지 않을 표식은 여기서 끝냅니다.
            Vector3 forward = marker.target.forward; // 대상이 현재 바라보는 세계 방향을 읽습니다.
            if (forward.x * forward.x + forward.z * forward.z < 0.0001f) continue; // 정수리나 발끝을 보면 마지막 수평 방향을 유지합니다.
            float heading = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg; // 북쪽 0도, 동쪽 90도인 수평 방향을 계산합니다.
            marker.icon.localEulerAngles = new Vector3(0f, 0f, -heading); // 위를 향한 화살표를 실제 시선 쪽으로 돌립니다.
        }
    }

    void UpdateNameLabel(MapMarker marker, Vector2 position, Vector2 half, bool isInside) // 동료 이름이 테두리나 안내 글자에 겹치지 않도록 맞춥니다.
    {
        if (marker.nameLabel == null) return; // 이름 글자가 없는 사용자나 보물 표식은 건너뜁니다.
        if (marker.nameLabel.gameObject.activeSelf != isInside) marker.nameLabel.gameObject.SetActive(isInside); // 가까운 동료만 전체 이름을 표시합니다.
        if (!isInside) return; // 멀리 있는 동료는 가장자리의 점으로 방향만 알려 줍니다.
        Vector2 nameHalf = marker.nameLabel.rect.size * 0.5f; // 이름 사각형의 반쪽 크기를 구합니다.
        float limitX = Mathf.Max(0f, half.x - nameHalf.x - 3f); // 이름이 좌우 테두리 안에 들어갈 범위를 구합니다.
        float limitY = Mathf.Max(0f, half.y - nameHalf.y - 11f); // 위아래 안내 띠 11칸은 이름이 가리지 않게 비웁니다.
        float x = Mathf.Clamp(position.x + marker.nameOffset.x, -limitX, limitX); // 이름 중심의 가로 위치를 지도 안으로 맞춥니다.
        float y = Mathf.Clamp(position.y + marker.nameOffset.y, -limitY, limitY); // 이름 중심의 세로 위치도 안내 띠 안쪽으로 맞춥니다.
        marker.nameLabel.anchoredPosition = new Vector2(x - position.x, y - position.y); // 부모 표식을 기준으로 하는 이름의 좌표로 바꿉니다.
    }

    void UpdateTerrainImage(Vector2 mapSize, float range, bool isFollowing) // 지도 배경도 표식과 함께 이동하도록 맞춥니다.
    {
        if (terrainImage == null || !terrainReady) return; // 그림이나 지형이 없으면 기존 표식 기능은 그대로 남깁니다.
        if (!isFollowing) // 전체 지형 모드에서는 그림 전체를 보여 줍니다.
        {
            terrainImage.uvRect = new Rect(0f, 0f, 1f, 1f); // 그림의 왼쪽 아래부터 오른쪽 위까지 사용합니다.
            return; // 전체 그림을 설정했으므로 추적 모드 계산을 끝냅니다.
        }

        float width = range / worldSize.x; // 현재 가로 범위가 전체 그림에서 차지하는 비율입니다.
        float height = range * (mapSize.y / mapSize.x) / worldSize.z; // 지도 비율에 맞춰 세로 범위도 같은 미터 배율로 구합니다.
        float centerX = (player.position.x - worldMin.x) / worldSize.x; // 사용자 동서 위치를 그림의 가로 비율로 바꿉니다.
        float centerY = (player.position.z - worldMin.z) / worldSize.z; // 사용자 남북 위치를 그림의 세로 비율로 바꿉니다.
        terrainImage.uvRect = new Rect(centerX - width * 0.5f, centerY - height * 0.5f, width, height); // 사용자 주변의 그림 부분만 잘라 보여 줍니다.
    }
}
