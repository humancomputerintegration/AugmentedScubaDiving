using UnityEngine; // 레이저 선과 조명을 다룬다.
using UnityEngine.UI; // 손전등 상태 글자를 바꾼다.

public class HandPointerLight : MonoBehaviour // 일반 선택 선과 밝은 손전등 표시를 담당한다.
{
    public LineRenderer leftRay; // 왼손 선택 선이다.
    public LineRenderer rightRay; // 오른손 선택 선이다.
    public Transform leftTip; // 왼손 레이저 끝의 작은 빛이다.
    public Transform rightTip; // 오른손 레이저 끝의 작은 빛이다.
    public Light leftLight; // 가까운 물체를 비추는 왼쪽 조명이다.
    public Light rightLight; // 가까운 물체를 비추는 오른쪽 조명이다.
    public Text statusText; // 켜짐과 꺼짐을 글자로도 알려 준다.
    public bool IsOn { get; private set; } // 다른 코드는 현재 상태를 읽고 Toggle로 바꾼다.
    private Color normalColor = new Color(0.2f, 0.85f, 1f, 0.65f); // 기본 선택 선의 색이다.
    private Color lightColor = new Color(0.85f, 1f, 0.62f, 1f); // 손전등을 켰을 때 쓸 밝은 색이다.

    private void Start() // 처음 시작할 때 실행한다.
    {
        IsOn = false; // 처음에는 손전등을 꺼 둔다.
        UpdateLabel(); // 버튼의 글자도 같은 상태로 맞춘다.
        HidePointers(); // 실제 손 좌표가 오기 전에는 빛을 숨긴다.
    }

    public void Toggle() // 손목 버튼이 호출하는 함수다.
    {
        IsOn = !IsOn; // 켜짐과 꺼짐을 서로 바꾼다.
        UpdateLabel(); // 소리 없이도 상태를 알 수 있게 한다.
    }

    private void UpdateLabel() // 상태를 글자로 바꾸는 공통 함수다.
    {
        if (statusText != null) statusText.text = IsOn ? "LIGHT ON" : "LIGHT OFF"; // 버튼 위 설명에 현재 상태를 표시한다.
    }

    public void ShowPointer(HandSide side, bool tracked, Vector3 start, Vector3 end, bool target) // 입력 코드가 계산한 실제 선을 그대로 그린다.
    {
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
            lamp.enabled = tracked && IsOn; // 추적이 끊기거나 손전등을 끄면 조명도 끈다.
            lamp.transform.position = start; // 손에서 빛이 나오게 한다.
            Vector3 direction = end - start; // 선택 선과 조명의 방향을 맞춘다.
            if (direction.sqrMagnitude > 0.0001f) lamp.transform.rotation = Quaternion.LookRotation(direction); // 길이가 없는 방향을 회전으로 바꾸지 않는다.
        }
    }

    public void HidePointers() // 앱 전환이나 추적 중단에 사용하는 함수다.
    {
        ShowPointer(HandSide.Left, false, Vector3.zero, Vector3.zero, false); // 왼손의 표시를 숨긴다.
        ShowPointer(HandSide.Right, false, Vector3.zero, Vector3.zero, false); // 오른손의 표시도 숨긴다.
    }
}
