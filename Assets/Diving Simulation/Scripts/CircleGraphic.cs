using UnityEngine; // 원의 점 위치와 테두리 색을 계산한다.
using UnityEngine.UI; // Unity UI가 그리는 도형을 확장한다.

public class CircleGraphic : Image // 별도 그림 파일 없이 동그란 메뉴 바탕을 그린다.
{ // 현재 버튼 크기 안에 얼굴과 테두리를 모두 그린다.
    public float rimWidth = 0f; // 0이면 단색 원이고, 양수이면 그만큼 안쪽에 테두리를 만든다.
    public Color rimColor = new Color(0.61483705f, 0.7287318f, 0.71826583f, 1f); // Eliza 원본 Chrome 재질에서 가져온 테두리 색이다.

    protected override void OnPopulateMesh(VertexHelper helper) // UI 크기나 색이 바뀔 때 Unity가 도형을 요청한다.
    { // 원 밖으로 도형을 늘리지 않으므로 기존 버튼 크기와 터치 영역을 그대로 유지한다.
        helper.Clear(); // 이전 도형의 점과 삼각형을 지운다.
        Rect rect = GetPixelAdjustedRect(); // 실제로 그릴 사각 영역을 읽는다.
        Vector2 center = rect.center; // 원의 중심이다.
        float radius = Mathf.Min(rect.width, rect.height) * 0.5f; // 짧은 변 안에 들어가는 반지름을 고른다.
        if (radius <= 0f) return; // 크기가 없는 UI에는 삼각형을 만들지 않는다.
        float border = Mathf.Clamp(rimWidth, 0f, radius); // 테두리가 원의 크기보다 두껍거나 음수가 되지 않게 한다.
        float faceRadius = radius - border; // 전체 반지름에서 테두리를 빼면 가운데 얼굴의 반지름이다.
        UIVertex vertex = UIVertex.simpleVert; // 기본 UI 점 하나를 준비한다.
        vertex.color = color; // Inspector에서 정한 얼굴 색을 점에 넣는다.
        vertex.position = center; // 첫 점은 중심에 놓는다.
        vertex.uv0 = new Vector2(0.5f, 0.5f); // 기본 흰 UI 텍스처의 가운데를 사용한다.
        helper.AddVert(vertex); // 중심점을 추가한다.
        const int segments = 48; // 48개의 작은 삼각형으로 작은 원을 부드럽게 보이게 한다.
        for (int i = 0; i <= segments; i++) // 마지막에는 시작 각도를 다시 넣어 원을 닫는다.
        { // 가운데 얼굴을 부채꼴 삼각형으로 채운다.
            float angle = i * Mathf.PI * 2f / segments; // 0부터 한 바퀴까지 균등한 각도를 만든다.
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)); // 각도를 원 위의 방향으로 바꾼다.
            vertex.position = center + direction * faceRadius; // 얼굴의 둘레에 점을 놓는다.
            helper.AddVert(vertex); // 계산한 점을 도형에 추가한다.
            if (i > 0) helper.AddTriangle(0, i, i + 1); // 중심과 이웃한 두 점을 삼각형 하나로 잇는다.
        } // 가운데 얼굴의 도형을 완성했다.
        if (border <= 0f) return; // 테두리가 없는 hover 표시 등은 여기서 끝낸다.
        int firstRimVertex = segments + 2; // 중심 1개와 둘레 49개 다음부터 테두리 점이 시작된다.
        vertex.color = rimColor; // 이제부터 만드는 점에는 원본 Eliza 테두리 색을 사용한다.
        for (int i = 0; i <= segments; i++) // 안쪽 원과 바깥쪽 원을 같은 각도로 나란히 만든다.
        { // 이웃한 두 쌍의 점을 연결하면 얇은 테두리 조각이 된다.
            float angle = i * Mathf.PI * 2f / segments; // 얼굴과 같은 각도 간격을 사용해 틈을 없앤다.
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)); // 이번 조각이 향할 방향이다.
            vertex.position = center + direction * faceRadius; // 먼저 테두리 안쪽 점을 추가한다.
            helper.AddVert(vertex); // 이 점은 바로 앞에서 만든 얼굴의 가장자리와 맞닿는다.
            vertex.position = center + direction * radius; // 다음에는 기존 버튼 반지름 안의 바깥 점을 추가한다.
            helper.AddVert(vertex); // 외곽 크기를 늘리지 않고 원래 경계에 점을 놓는다.
            if (i == 0) continue; // 첫 번째 점 쌍에는 아직 연결할 이전 쌍이 없다.
            int previous = firstRimVertex + (i - 1) * 2; // 바로 이전 각도의 안쪽 점 번호를 계산한다.
            helper.AddTriangle(previous, previous + 1, previous + 3); // 테두리 사각 조각의 첫 삼각형이다.
            helper.AddTriangle(previous, previous + 3, previous + 2); // 나머지 삼각형까지 채워 조각을 완성한다.
        } // 모든 테두리 조각을 닫아 원을 완성했다.
    } // UI 도형 만들기를 끝낸다.
} // CircleGraphic 정의를 끝낸다.
