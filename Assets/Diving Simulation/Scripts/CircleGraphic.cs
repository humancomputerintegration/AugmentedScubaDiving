using UnityEngine; // 원의 점 위치와 테두리 색을 계산한다.
using UnityEngine.UI; // Unity UI가 그리는 도형을 확장한다.

public class CircleGraphic : Image // 별도 그림 파일 없이 동그란 메뉴 바탕을 그린다.
{ // 현재 버튼 크기 안에 얼굴과 테두리를 모두 그린다.
    public float rimWidth = 0f; // 0이면 단색 원이고, 양수이면 그만큼 안쪽에 테두리를 만든다.
    public Color rimColor = new Color(0.61483705f, 0.7287318f, 0.71826583f, 1f); // Eliza 원본 Chrome 재질에서 가져온 테두리 색이다.
    [Range(0f, 1f)] public float roundness = 0f; // 0이면 원래 단색 원이고, 조금 높이면 부드러운 볼륨을 색으로 표현한다.
    public bool phoneIcon = false; // 전화 버튼만 글자 대신 이 도형 안에 수화기 그림을 그린다.
    public Color symbolColor = new Color(0.16f, 0.24f, 0.28f, 1f); // 밝은 파스텔 위에서 잘 읽히는 짙은 청회색이다.
    public float PressAmount { get; private set; } // 0은 평소 모습이고, 1은 눌려 들어간 모습이다.

    public void SetPressAmount(float value) // 입력 기능을 바꾸지 않고 버튼 그림만 눌린 모습으로 바꾼다.
    {
        float next = Mathf.Clamp01(value); // 음수나 1보다 큰 눌림 정도는 사용하지 않는다.
        if (Mathf.Abs(PressAmount - next) < 0.0001f) return; // 같은 그림을 불필요하게 다시 만들지 않는다.
        PressAmount = next; // 이번에 그릴 눌림 정도를 기억한다.
        SetVerticesDirty(); // Unity에게 다음 화면에서 도형의 점과 색을 갱신하도록 알린다.
    }

    protected override void OnPopulateMesh(VertexHelper helper) // UI 크기나 색이 바뀔 때 Unity가 도형을 요청한다.
    { // 원 밖으로 도형을 늘리지 않으므로 기존 버튼 크기와 터치 영역을 그대로 유지한다.
        helper.Clear(); // 이전 도형의 점과 삼각형을 지운다.
        Rect rect = GetPixelAdjustedRect(); // 실제로 그릴 사각 영역을 읽는다.
        Vector2 center = rect.center; // 원의 중심이다.
        float radius = Mathf.Min(rect.width, rect.height) * 0.5f; // 짧은 변 안에 들어가는 반지름을 고른다.
        if (radius <= 0f) return; // 크기가 없는 UI에는 삼각형을 만들지 않는다.
        if (roundness > 0f) // 새 모양을 선택한 메뉴 버튼에만 부드러운 볼륨을 적용한다.
        {
            DrawSoftFace(helper, center, radius); // 기존 원 크기 안에서 은은한 명암을 그린다.
            if (phoneIcon) DrawPhone(helper, center + Vector2.down * radius * 0.018f * PressAmount, radius); // 수화기도 눌린 얼굴과 함께 조금 내려간다.
            return; // 선택 테두리처럼 roundness가 0인 원은 아래의 원래 그리기를 사용한다.
        }
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
        if (border <= 0f) // 테두리가 없는 hover 표시 등은 여기서 끝낸다.
        {
            if (phoneIcon) DrawPhone(helper, center, radius); // 볼록함을 0으로 낮춰도 전화 그림은 남겨 둔다.
            return; // 그릴 테두리가 없으므로 계산을 마친다.
        }
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
        if (phoneIcon) DrawPhone(helper, center, radius); // 단색 테두리를 쓰는 경우에도 수화기 그림을 올린다.
    } // UI 도형 만들기를 끝낸다.

    private void DrawSoftFace(VertexHelper helper, Vector2 center, float radius) // 실제 두께를 늘리지 않고 빛과 그림자로 살짝 통통하게 보이게 한다.
    {
        const int segments = 48; // 기존 원과 같은 수의 조각으로 둥근 가장자리를 만든다.
        AddPoint(helper, center + Vector2.down * radius * 0.018f * PressAmount, FaceColor(0f, 0f)); // 얼굴 가운데도 눌림에 맞춰 조금 내려간다.
        for (int ring = 1; ring <= 4; ring++) // 네 겹의 원으로 명암이 갑자기 끊기지 않게 한다.
        {
            float part = 1f; // 맨 바깥 원은 버튼 반지름 전체를 사용한다.
            if (ring == 1) part = 0.60f; // 첫 원은 반지름의 60% 위치에 그린다.
            if (ring == 2) part = 0.84f; // 두 번째 원을 덧붙여 명암이 서서히 변하게 한다.
            if (ring == 3) part = 0.97f; // 세 번째 원 바깥의 3%만 얇은 테두리로 남긴다.
            for (int step = 0; step <= segments; step++) // 마지막 점을 첫 점과 겹쳐 원을 닫는다.
            {
                float angle = step * Mathf.PI * 2f / segments; // 이번 점이 원에서 차지하는 각도다.
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)); // 각도를 가로·세로 방향으로 바꾼다.
                Vector2 point = center + direction * radius * part; // 원래 버튼 경계를 넘지 않는 위치다.
                if (ring < 4) point += Vector2.down * radius * 0.018f * PressAmount; // 얼굴만 아주 조금 내려가고 바깥 경계는 고정한다.
                Color shade = ring == 4 ? Tint(-0.10f + direction.y * 0.04f) : FaceColor(part, direction.y * part); // 금속 테두리 대신 얼굴과 비슷한 색을 사용한다.
                AddPoint(helper, point, shade); // 위치와 명암이 정해진 점을 도형에 추가한다.
                if (step == 0) continue; // 첫 점에는 아직 연결할 이전 점이 없다.
                int current = 1 + (ring - 1) * (segments + 1) + step; // 방금 넣은 점의 번호다.
                if (ring == 1) helper.AddTriangle(0, current - 1, current); // 맨 안쪽 원은 가운데 점과 연결한다.
                else // 두 번째 원부터는 바로 안쪽 원과 이어 붙인다.
                {
                    int inner = current - (segments + 1); // 같은 각도에 있는 안쪽 점의 번호다.
                    helper.AddTriangle(inner - 1, current - 1, current); // 두 원 사이 사각 조각의 절반을 채운다.
                    helper.AddTriangle(inner - 1, current, inner); // 나머지 절반을 채워 빈틈을 없앤다.
                }
            }
        }
    }

    private Color FaceColor(float distance, float height) // 가운데는 살짝 밝게, 아래쪽 가장자리는 살짝 어둡게 만든다.
    {
        float dome = Mathf.Clamp01(roundness) * (1f - PressAmount * 0.75f); // 누르면 볼록한 명암이 줄어서 납작해 보인다.
        float light = (0.12f * (1f - distance * distance) + 0.11f * height - 0.12f * distance * distance) * dome; // 과장된 반짝임 없이 완만한 명암을 계산한다.
        return Tint(light - PressAmount * 0.06f); // 누른 동안은 아주 조금 어두워져 눌림을 알려 준다.
    }

    private Color Tint(float amount) // 원래 파스텔 색을 기준으로 밝기만 조금 바꾸는 함수다.
    {
        Color target = amount >= 0f ? Color.white : Color.black; // 밝힐 때는 흰색, 어둡힐 때는 검은색 쪽으로 섞는다.
        Color result = Color.Lerp(color, target, Mathf.Abs(amount)); // 0.1이면 목표 색을 10%만 섞는다.
        result.a = color.a; // 명암 때문에 원래 투명도가 바뀌지 않게 한다.
        return result; // 계산한 색을 돌려준다.
    }

    private void DrawPhone(VertexHelper helper, Vector2 center, float radius) // 기기마다 다른 이모지 글꼴에 의존하지 않는 수화기 그림이다.
    {
        const int steps = 16; // 수화기의 굽은 손잡이를 16조각으로 나눈다.
        int first = helper.currentVertCount; // 수화기 점이 시작되는 번호를 기억한다.
        for (int step = 0; step <= steps; step++) // 왼쪽 위부터 아래쪽을 지나 오른쪽 아래까지 그린다.
        {
            float angle = Mathf.Lerp(135f, 315f, (float)step / steps) * Mathf.Deg2Rad; // 기울어진 수화기의 각도다.
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)); // 이번 수화기 조각의 방향이다.
            AddPoint(helper, center + direction * radius * 0.36f, symbolColor); // 손잡이의 안쪽 점을 추가한다.
            AddPoint(helper, center + direction * radius * 0.55f, symbolColor); // 손잡이의 바깥쪽 점을 추가한다.
            if (step == 0) continue; // 첫 쌍에는 아직 이어 붙일 점이 없다.
            int previous = first + (step - 1) * 2; // 바로 앞 조각의 안쪽 점 번호다.
            helper.AddTriangle(previous, previous + 1, previous + 3); // 손잡이 조각의 첫 삼각형이다.
            helper.AddTriangle(previous, previous + 3, previous + 2); // 나머지 삼각형으로 조각을 닫는다.
        }
        DrawReceiverEnd(helper, center, radius, 135f); // 귀 쪽의 두툼한 끝을 그린다.
        DrawReceiverEnd(helper, center, radius, 315f); // 입 쪽에도 같은 모양의 끝을 그린다.
    }

    private void DrawReceiverEnd(VertexHelper helper, Vector2 center, float radius, float degrees) // 수화기 양 끝을 둥근 타원으로 만든다.
    {
        float angle = degrees * Mathf.Deg2Rad; // 도 단위 각도를 삼각함수에 쓰는 라디안으로 바꾼다.
        Vector2 along = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)); // 수화기 중심에서 끝으로 향하는 방향이다.
        Vector2 across = new Vector2(-along.y, along.x); // 그 방향과 직각인 방향이다.
        Vector2 end = center + along * radius * 0.455f; // 손잡이 끝의 가운데 위치를 고른다.
        int first = helper.currentVertCount; // 타원의 가운데 점 번호다.
        AddPoint(helper, end, symbolColor); // 가운데를 채울 기준점을 추가한다.
        for (int step = 0; step <= 16; step++) // 작은 타원을 16개의 삼각형으로 그린다.
        {
            float turn = step * Mathf.PI * 2f / 16f; // 타원 둘레의 각도다.
            Vector2 point = end + along * Mathf.Cos(turn) * radius * 0.19f + across * Mathf.Sin(turn) * radius * 0.13f; // 둥근 수화기 끝의 둘레를 계산한다.
            AddPoint(helper, point, symbolColor); // 계산한 둘레 점을 추가한다.
            if (step > 0) helper.AddTriangle(first, first + step, first + step + 1); // 가운데와 이웃한 점을 연결해 채운다.
        }
    }

    private void AddPoint(VertexHelper helper, Vector2 point, Color shade) // 같은 점 설정을 여러 그리기 함수에서 간단히 사용한다.
    {
        UIVertex vertex = UIVertex.simpleVert; // Unity UI의 기본 점 정보를 준비한다.
        vertex.position = point; // 도형 안에서 이 점을 놓을 위치다.
        vertex.color = shade; // 이 점에 사용할 얼굴색 또는 수화기 색이다.
        vertex.uv0 = new Vector2(0.5f, 0.5f); // 기본 흰 UI 텍스처의 가운데를 사용한다.
        helper.AddVert(vertex); // 완성한 점을 Unity의 UI 도형에 넣는다.
    }
} // CircleGraphic 정의를 끝낸다.
