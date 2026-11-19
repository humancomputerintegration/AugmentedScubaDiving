using UnityEngine; // 위치, 회전, 시간과 GameObject를 사용한다.
using UnityEngine.InputSystem; // PC에서 M 키로 메뉴를 점검한다.
using UnityEngine.XR.Hands; // 손바닥 관절 이름을 사용한다.

[DefaultExecutionOrder(-94)] // 손목 UI 다음, 버튼 물리 검색 전에 화면 패널 위치를 맞춘다.
public class WaveMenuController : MonoBehaviour // 스와이프와 팔 메뉴가 같은 패널을 열게 한다.
{
    public HandsInput hands; // 공통 손 입력 담당자다.
    public Transform head; // 머리 앞에 화면을 놓기 위한 카메라다.
    public GameObject menuRoot; // 스와이프로 여닫는 원형 화면 메뉴다.
    public GameObject[] panels; // 정보, 통화, 길찾기, 동료 정보 패널이다.
    public float menuDistance = 0.65f; // 화면 메뉴까지의 거리다.
    public float menuDrop = 0.25f; // 메뉴를 시선보다 아래에 놓는다.
    public float autoHideTime; // 0이면 자동으로 닫지 않는다.
    public float palmOutwardThreshold = 0.6f; // 기존처럼 손바닥이 시선 바깥쪽을 향할 때만 스와이프를 받는다.
    public float waveDistance = 0.15f; // 좌우 15cm 이동을 스와이프로 판단한다.
    public float waveMaxTime = 0.8f; // 너무 느린 손 이동은 스와이프로 세지 않는다.
    public float panelHeight = 0.02f; // 큰 패널 중심의 머리 기준 높이다.
    public bool hideMenuAfterPress; // 패널을 열고 원형 메뉴만 숨길지 정한다.
    public CrewArrowNavigator arrowNavigator; // 전체 닫기 시 길찾기도 정리한다.
    public Key testKey = Key.M; // PC 점검용 키다.
    private bool measuring; // 스와이프 시작점을 기록했는지 기억한다.
    private Vector3 waveStart; // 머리 기준 시작 손바닥 위치다.
    private float waveStartTime; // 시작 시각이다.
    private float nextWaveTime; // 같은 동작이 중복 실행되지 않게 하는 시각이다.
    private float lastOpenTime; // 자동 닫기에서 쓰는 마지막 사용 시각이다.

    private void Start() // Scene을 열고 처음 실행한다.
    {
        HideAll(); // 처음에는 메뉴와 큰 패널을 닫는다.
    }

    private void Update() // 매 프레임 입력을 검사한다.
    {
        PlaceVisibleUI(); // 이번 프레임 접촉 검사 전에 움직인 머리 위치를 반영한다.
        if (Keyboard.current != null && Keyboard.current[testKey].wasPressedThisFrame) // PC 메뉴 키를 누른 순간을 확인한다.
        {
            if (menuRoot != null && menuRoot.activeSelf) HideAll(); // 열려 있으면 닫는다.
            else ShowMenu(); // 닫혀 있으면 연다.
        }
        DetectWave(); // 왼손의 좌우 이동을 검사한다.
        if (autoHideTime > 0f && Time.unscaledTime - lastOpenTime > autoHideTime) HideAll(); // 자동 닫기를 켰을 때만 시간 제한을 적용한다.
    }

    private void LateUpdate() // 이동과 머리 추적이 끝난 뒤 실제로 그릴 위치도 맞춘다.
    {
        PlaceVisibleUI(); // 입력 검사와 렌더링에 같은 배치 계산을 사용한다.
    }

    private void PlaceVisibleUI() // 현재 열린 메뉴와 패널만 갱신한다.
    {
        if (head == null) return; // 카메라 연결이 없으면 멈춘다.
        if (menuRoot != null && menuRoot.activeSelf) Place(menuRoot.transform, -menuDrop, menuDistance); // 원형 메뉴를 시선 아래에 놓는다.
        if (panels == null) return; // 목록이 없으면 끝낸다.
        foreach (GameObject panel in panels) // 패널을 하나씩 살펴본다.
        {
            if (panel != null && panel.activeSelf) Place(panel.transform, panelHeight, 0.78f); // 열린 패널만 머리 앞 78cm에 놓는다.
        }
    }

    private void DetectWave() // 왼손의 좌우 이동을 메뉴 명령으로 바꾼다.
    {
        if (hands == null || head == null || !hands.UseHandInput || hands.LeftFist || hands.LeftUIBusy) // 이동용 주먹이나 버튼 조작 중에는 스와이프를 만들지 않는다.
        {
            measuring = false; // 중단된 손동작의 시작점을 버린다.
            return; // 이번 검사를 끝낸다.
        }
        if (!hands.TryGetWorldPose(HandSide.Left, XRHandJointID.Palm, out Pose palm)) // 실제 추적되는 손바닥 좌표가 필요하다.
        {
            measuring = false; // 추적 복구 때 큰 위치 차이가 스와이프로 오인되지 않게 한다.
            return; // 손을 다시 찾을 때까지 기다린다.
        }
        Vector3 lookFlat = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized; // 수평 시선 방향을 계산한다.
        if (Vector3.Dot(palm.rotation * Vector3.down, lookFlat) <= palmOutwardThreshold) // 손바닥의 바깥쪽 방향을 기존 방식으로 확인한다.
        {
            measuring = false; // 다른 자세에서의 일상적인 손 이동은 버린다.
            return; // 올바른 손바닥 방향이 될 때까지 기다린다.
        }
        if (Time.unscaledTime < nextWaveTime) return; // 직전 동작 직후의 중복 판정을 막는다.
        Vector3 local = head.InverseTransformPoint(palm.position); // 좌우를 현재 사용자의 시선 기준으로 계산한다.
        if (!measuring || Time.unscaledTime - waveStartTime > waveMaxTime) // 새로운 동작이거나 제한 시간이 끝났을 때다.
        {
            waveStart = local; // 시작 위치를 기록한다.
            waveStartTime = Time.unscaledTime; // 시작 시각을 기록한다.
            measuring = true; // 다음 프레임부터 거리를 비교한다.
            return; // 시작 프레임을 곧바로 스와이프로 세지 않는다.
        }
        Vector3 delta = local - waveStart; // 시작점 이후의 이동량이다.
        if (Mathf.Abs(delta.x) < waveDistance || Mathf.Abs(delta.x) < Mathf.Abs(delta.y) * 1.3f) return; // 충분히 크고 수직보다 좌우가 뚜렷한 동작만 받는다.
        if (delta.x > 0f) ShowMenu(); // 오른쪽 스와이프는 화면 메뉴를 연다.
        else HideAll(); // 왼쪽 스와이프는 메뉴와 패널을 닫는다.
        measuring = false; // 같은 시작점으로 다시 실행하지 않는다.
        nextWaveTime = Time.unscaledTime + 0.65f; // 한 번의 손동작이 끝날 여유를 준다.
    }

    private void Place(Transform target, float height, float distance) // 머리 앞에 놓는 계산을 한 곳에 모은다.
    {
        target.position = head.TransformPoint(new Vector3(0f, height, distance)); // 머리 위치와 방향을 모두 반영한다.
        target.rotation = head.rotation; // Canvas 앞면을 사용자 쪽으로 향하게 한다.
    }

    public void ShowMenu() // 스와이프와 PC 키가 함께 부르는 열기 함수다.
    {
        if (menuRoot == null) return; // 연결이 없으면 멈춘다.
        menuRoot.SetActive(true); // 원형 화면 메뉴를 켠다.
        lastOpenTime = Time.unscaledTime; // 마지막 사용 시각을 갱신한다.
    }

    public void HideMenu() // 원형 메뉴만 닫아야 할 때 쓴다.
    {
        if (menuRoot != null) menuRoot.SetActive(false); // 큰 패널이나 통화 상태는 건드리지 않는다.
    }

    public void OpenPanel(GameObject panel) // 화면 메뉴와 팔 메뉴의 동일 버튼이 호출한다.
    {
        if (panel == null || panels == null) return; // 대상이 없으면 끝낸다.
        bool wasOpen = panel.activeSelf; // 닫기 전에 이미 열려 있었는지 기억한다.
        foreach (GameObject item in panels) // 겹치지 않게 기존 큰 패널을 닫는다.
        {
            if (item != null) item.SetActive(false); // 빈 연결도 안전하게 처리한다.
        }
        if (!wasOpen) // 이전에 닫혀 있던 패널만 새로 연다.
        {
            if (head != null) Place(panel.transform, panelHeight, 0.78f); // 숨겨진 메뉴의 옛 위치를 사용하지 않는다.
            panel.SetActive(true); // 같은 버튼을 다시 누른 경우 이 줄을 건너뛰어 정상적으로 닫힌다.
        }
        if (hideMenuAfterPress) HideMenu(); // 옵션이 켜진 경우 원형 메뉴를 숨긴다.
        lastOpenTime = Time.unscaledTime; // 패널 조작도 메뉴 사용으로 기록한다.
    }

    public void HideAll() // 왼쪽 스와이프와 전체 닫기에서 부르는 함수다.
    {
        HideMenu(); // 원형 화면 메뉴를 숨긴다.
        if (panels != null) foreach (GameObject item in panels) // 등록된 큰 패널을 살펴본다.
        {
            if (item != null) item.SetActive(false); // 손목 메뉴는 이 목록 밖이므로 계속 사용할 수 있다.
        }
        if (arrowNavigator != null) arrowNavigator.StopNavigation(); // 이전 길찾기도 종료한다.
    }
}
