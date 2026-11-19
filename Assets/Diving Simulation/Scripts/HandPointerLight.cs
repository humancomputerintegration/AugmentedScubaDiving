using UnityEngine; // 레이저 선과 조명을 다룬다.
using UnityEngine.UI; // 손전등 상태 글자를 바꾼다.
using UnityEngine.InputSystem; // PC에서는 마우스로 비출 곳을 정한다.

public class HandPointerLight : MonoBehaviour // 일반 선택 선과 밝은 손전등 표시를 담당한다.
{
    public LineRenderer leftRay; // 왼손 선택 선이다.
    public LineRenderer rightRay; // 오른손 선택 선이다.
    public Transform leftTip; // 왼손 레이저 끝의 작은 빛이다.
    public Transform rightTip; // 오른손 레이저 끝의 작은 빛이다.
    public Light leftLight; // 가까운 물체를 비추는 왼쪽 조명이다.
    public Light rightLight; // 가까운 물체를 비추는 오른쪽 조명이다.
    public Text statusText; // 켜짐과 꺼짐을 글자로도 알려 준다.
    public HandsInput hands; // 기존 손 입력 담당자에게 PC 모드인지 확인한다.
    public Camera headCamera; // 두 눈의 가운데에서 빛이 닿는 곳까지 보이는 길을 만든다.
    public float clearRange = 8f; // 머드 속에서 손전등으로 볼 수 있는 거리는 8미터다.
    public bool IsOn { get; private set; } // 다른 코드는 현재 상태를 읽고 Toggle로 바꾼다.
    private Color normalColor = new Color(0.2f, 0.85f, 1f, 0.65f); // 기본 선택 선의 색이다.
    private Color lightColor = new Color(0.85f, 1f, 0.62f, 1f); // 손전등을 켰을 때 쓸 밝은 색이다.
    private bool paused; // 앱이 잠시 멈췄는지 기억한다.
    private int leftFrame = -1; // 왼손 좌표를 마지막으로 받은 화면 번호다.
    private int rightFrame = -1; // 오른손 좌표를 마지막으로 받은 화면 번호다.
    private static readonly int leftStartId = Shader.PropertyToID("_ScubaMudLeftStart"); // 셰이더의 왼손 위치 이름을 한 번만 번호로 바꾼다.
    private static readonly int rightStartId = Shader.PropertyToID("_ScubaMudRightStart"); // 오른손 위치의 번호다.
    private static readonly int leftDirectionId = Shader.PropertyToID("_ScubaMudLeftDirection"); // 왼손 방향의 번호다.
    private static readonly int rightDirectionId = Shader.PropertyToID("_ScubaMudRightDirection"); // 오른손 방향의 번호다.
    private static readonly int viewerId = Shader.PropertyToID("_ScubaMudViewer"); // 빛길을 볼 머리 위치의 번호다.

    private void Start() // 처음 시작할 때 실행한다.
    {
        IsOn = false; // 처음에는 손전등을 꺼 둔다.
        clearRange = Mathf.Clamp(clearRange, 2f, 12f); // 실수로 너무 짧거나 긴 거리를 적지 않게 한다.
        if (leftLight != null) leftLight.range = clearRange; // 실제 조명도 같은 거리까지 닿게 한다.
        if (rightLight != null) rightLight.range = clearRange; // 오른손 조명의 거리도 맞춘다.
        UpdateLabel(); // 버튼의 글자도 같은 상태로 맞춘다.
        HidePointers(); // 실제 손 좌표가 오기 전에는 빛을 숨긴다.
    }

    public void Toggle() // 손목 버튼이 호출하는 함수다.
    {
        IsOn = !IsOn; // 켜짐과 꺼짐을 서로 바꾼다.
        UpdateLabel(); // 소리 없이도 상태를 알 수 있게 한다.
        if (!IsOn) ClearLight(); // 끈 순간에 조명과 머드 속 빛길도 바로 끈다.
    }

    private void UpdateLabel() // 상태를 글자로 바꾸는 공통 함수다.
    {
        if (statusText != null) statusText.text = IsOn ? "LIGHT ON" : "LIGHT OFF"; // 버튼 위 설명에 현재 상태를 표시한다.
    }

    public void ShowPointer(HandSide side, bool tracked, Vector3 start, Vector3 end, bool target) // 입력 코드가 계산한 실제 선을 그대로 그린다.
    {
        tracked = tracked && isActiveAndEnabled && Application.isFocused && !paused && Time.timeScale > 0f; // 추적이 있어도 앱이 멈췄거나 꺼진 표시자는 빛을 내지 않는다.
        if (side == HandSide.Left) leftFrame = Time.frameCount; // 이번 화면에 왼손 좌표를 받았다고 기억한다.
        else rightFrame = Time.frameCount; // 오른손도 따로 기억한다.
        Vector3 direction = end - start; // 기존 선택 레이저가 가리키는 방향을 그대로 사용한다.
        bool validDirection = direction.sqrMagnitude > 0.0001f; // 시작과 끝이 같으면 방향이 없으므로 비추지 않는다.
        LineRenderer line = side == HandSide.Left ? leftRay : rightRay; // 표시할 손의 선을 고른다.
        Transform tip = side == HandSide.Left ? leftTip : rightTip; // 같은 손의 끝점을 고른다.
        Light lamp = side == HandSide.Left ? leftLight : rightLight; // 같은 손의 조명을 고른다.
        Color tint = IsOn ? lightColor : normalColor; // 상태에 따라 선 색깔을 결정한다.
        if (line != null) // 선이 연결되어 있을 때 실행한다.
        {
            line.enabled = tracked; // 추적 중인 손에만 선을 그린다.
            line.useWorldSpace = true; // 좌표를 실제 세계 위치로 해석한다.
            line.positionCount = 2; // 선분은 시작점과 끝점 두 개다.
            line.SetPosition(0, start); // 손의 시작점을 지정한다.
            line.SetPosition(1, end); // 선택된 버튼 표면까지 선을 그린다.
            line.startWidth = IsOn ? 0.006f : 0.0015f; // 손전등을 켜면 선을 더 굵게 만든다.
            line.endWidth = IsOn ? 0.009f : 0.002f; // 끝으로 갈수록 조금 넓게 표현한다.
            line.startColor = line.endColor = tint; // 시작과 끝의 색을 맞춘다.
        }
        if (tip != null) // 작은 끝점 빛을 갱신한다.
        {
            tip.gameObject.SetActive(tracked && (IsOn || target)); // 손전등 또는 버튼 조준 중일 때 빛을 보인다.
            tip.position = end; // 선 끝에 빛을 놓는다.
            tip.localScale = Vector3.one * (IsOn ? 0.022f : 0.009f); // 켰을 때 더 크게 보이게 한다.
        }
        if (lamp != null) // 실제 주변 물체를 밝히는 조명을 갱신한다.
        {
            lamp.enabled = tracked && IsOn && validDirection; // 추적이나 방향이 없으면 조명을 끈다.
            lamp.transform.position = start; // 손에서 빛이 나오게 한다.
            if (validDirection) lamp.transform.rotation = Quaternion.LookRotation(direction); // 길이가 없는 방향을 회전으로 바꾸지 않는다.
        }
        UpdateMudBeam(side, tracked && IsOn && validDirection, start, direction); // 같은 레이저가 닿는 부분의 머드만 옅게 만든다.
    }

    private void UpdateMudBeam(HandSide side, bool visible, Vector3 start, Vector3 direction) // 머드 재질에 손전등 하나의 위치와 방향을 보낸다.
    {
        int startId = side == HandSide.Left ? leftStartId : rightStartId; // 갱신할 손의 위치 번호를 고른다.
        int directionId = side == HandSide.Left ? leftDirectionId : rightDirectionId; // 같은 손의 방향 번호를 고른다.
        Shader.SetGlobalVector(startId, new Vector4(start.x, start.y, start.z, visible ? 1f : 0f)); // 마지막 값이 0이면 셰이더가 이 손전등을 무시한다.
        Vector3 unitDirection = visible ? direction.normalized : Vector3.zero; // 켜진 빛의 방향만 길이가 1인 화살표로 만든다.
        Shader.SetGlobalVector(directionId, new Vector4(unitDirection.x, unitDirection.y, unitDirection.z, clearRange)); // 방향 세 값과 거리 한 값을 함께 보낸다.
        Vector3 eye = headCamera != null ? headCamera.transform.position : start; // 카메라 연결이 없을 때만 손 위치를 기준으로 쓴다.
        Shader.SetGlobalVector(viewerId, new Vector4(eye.x, eye.y, eye.z, headCamera != null ? 1f : 0f)); // 두 눈이 같은 월드 공간의 빛길을 보게 머리 가운데를 사용한다.
    }

    private void LateUpdate() // 손 입력이 모두 계산된 뒤 오래된 빛이 남았는지 검사한다.
    {
        ShowMouseLight(); // PC에서는 마우스 방향으로 같은 효과를 확인한다.
        if (leftFrame != Time.frameCount) ShowPointer(HandSide.Left, false, Vector3.zero, Vector3.zero, false); // 새 왼손 자료가 없으면 지난 빛을 지운다.
        if (rightFrame != Time.frameCount) ShowPointer(HandSide.Right, false, Vector3.zero, Vector3.zero, false); // 오른손도 자료가 끊긴 그 화면부터 지운다.
    }

    private void ShowMouseLight() // 헤드셋이 없는 PC 확인용 빛이며 손 추적 중에는 실행하지 않는다.
    {
        if (!IsOn || hands == null || hands.UseHandInput || headCamera == null || Mouse.current == null) return; // 마우스 전용 모드와 필요한 연결을 확인한다.
        if (!Application.isFocused || paused || Time.timeScale <= 0f) return; // 앱 전환이나 일시 정지 중에는 비추지 않는다.
        Vector2 cursor = Mouse.current.position.ReadValue(); // Game 화면에서 마우스의 위치를 읽는다.
        if (float.IsNaN(cursor.x) || float.IsInfinity(cursor.x) || float.IsNaN(cursor.y) || float.IsInfinity(cursor.y)) return; // 잘못된 무한대 좌표로 레이저를 만들지 않는다.
        if (!headCamera.pixelRect.Contains(cursor)) return; // Game 화면 밖에 있는 커서는 사용하지 않는다.
        Ray ray = headCamera.ScreenPointToRay(cursor); // 마우스 아래의 화면 점을 향하는 레이저다.
        ShowPointer(HandSide.Right, true, ray.origin, ray.GetPoint(clearRange), false); // 기존 손전등 그리기를 그대로 이용한다.
    }

    private void ClearLight() // 꺼진 빛이 머드를 계속 지우지 않도록 즉시 정리한다.
    {
        Shader.SetGlobalVector(leftStartId, Vector4.zero); // 왼손 머드 빛길을 끈다.
        Shader.SetGlobalVector(rightStartId, Vector4.zero); // 오른손 머드 빛길을 끈다.
        Shader.SetGlobalVector(leftDirectionId, Vector4.zero); // 다음 Scene이 이전 왼손 방향을 이어받지 않게 지운다.
        Shader.SetGlobalVector(rightDirectionId, Vector4.zero); // 이전 오른손 방향도 지운다.
        Shader.SetGlobalVector(viewerId, Vector4.zero); // 이전 머리 위치도 남기지 않는다.
        if (leftLight != null) leftLight.enabled = false; // 왼쪽 실제 조명도 끈다.
        if (rightLight != null) rightLight.enabled = false; // 오른쪽 실제 조명도 끈다.
    }

    private void OnApplicationPause(bool value) // 헤드셋 앱이 멈추거나 다시 시작할 때 Unity가 알려 준다.
    {
        paused = value; // 현재 멈춤 상태를 저장한다.
        if (paused) HidePointers(); // 멈추는 즉시 오래된 손 위치의 빛을 지운다.
    }

    private void OnApplicationFocus(bool focused) // 시스템 메뉴 등으로 시선의 대상 앱이 바뀌면 실행한다.
    {
        if (!focused) HidePointers(); // 앱 밖에서는 손전등을 표시하지 않는다.
    }

    private void OnDisable() // 이 담당자나 Scene을 끄는 순간 실행한다.
    {
        HidePointers(); // 다른 Scene에 전역 셰이더 값이 남지 않게 한다.
    }

    public void HidePointers() // 앱 전환이나 추적 중단에 사용하는 함수다.
    {
        ShowPointer(HandSide.Left, false, Vector3.zero, Vector3.zero, false); // 왼손의 표시를 숨긴다.
        ShowPointer(HandSide.Right, false, Vector3.zero, Vector3.zero, false); // 오른손의 표시도 숨긴다.
        ClearLight(); // 조명과 셰이더에 기억된 위치·방향을 함께 정리한다.
    }
}
