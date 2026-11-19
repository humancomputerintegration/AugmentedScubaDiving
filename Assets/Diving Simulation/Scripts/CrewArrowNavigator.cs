using UnityEngine; // 위치, 회전, 거리처럼 Unity가 제공하는 기능을 사용한다.
using UnityEngine.UI; // 화면에 글자를 보여 주는 Text를 사용한다.

public class CrewArrowNavigator : MonoBehaviour // 동료를 선택하면 그 동료의 현재 위치까지 안내한다.
{ // 안내에 필요한 정보와 동작을 이 클래스 안에 넣는다.
    public CallTowerManager ctm; // 전화에 등록된 동료 목록을 가져온다.
    public Transform head; // 실제 사용자의 머리 카메라 위치와 바라보는 방향이다.
    public GameObject arrowGroup; // 화살표와 안내 글자를 함께 켜고 끄는 오브젝트이다.
    public RectTransform arrow; // 화면에서 좌우로 돌릴 화살표이다.
    public Text arrowLabel; // 동료 이름, 거리, 위아래 방향을 보여 주는 글자이다.
    public float arriveDistance = 3f; // 동료와의 실제 공간 거리가 3m 이내이면 도착으로 판단한다.

    private Transform target; // 지금 찾아가는 동료의 위치를 기억한다.
    private bool isNavigating; // 이동 안내 중인지, 도착 문구만 잠시 보여 주는 중인지 구분한다.
    private Vector3 lastLookDirection = Vector3.forward; // 정확히 위나 아래를 볼 때 사용할 마지막 수평 시선이다.

    void Start() // 장면을 시작할 때 한 번 실행한다.
    { // 처음에는 목적지를 정하지 않은 상태이다.
        HideArrow(); // 목적지를 고르기 전에는 안내를 숨긴다.
    } // 시작 준비를 마친다.

    public void NavigateTo(string crewName) // 기존 동료 버튼이 이름을 전달하면 안내를 시작한다.
    { // 전달받은 이름과 같은 동료를 찾는다.
        if (ctm == null || ctm.crewmates == null) // 동료 목록이 연결되지 않았다면 사용할 수 없다.
        { // 잘못된 연결 때문에 실행이 멈추지 않도록 확인한다.
            Debug.LogWarning("CrewArrowNavigator: crewmate list is missing.", this); // Inspector에서 확인할 연결을 알려 준다.
            return; // 목록이 없으므로 이번 안내 요청을 여기서 마친다.
        } // 동료 목록 확인을 마친다.
        foreach (Transform crewmate in ctm.crewmates) // 등록된 동료를 한 명씩 살펴본다.
        { // 이번 순서의 동료를 확인한다.
            if (crewmate != null && crewmate.name == crewName) // 실제로 존재하고 이름도 같으면 목적지로 정한다.
            { // 원하는 동료를 찾았을 때 실행한다.
                target = crewmate; // 좌표를 복사하지 않고 동료 자체를 기억해서 움직이는 동료도 따라간다.
                isNavigating = true; // 이제부터 매 프레임 거리를 다시 계산한다.
                CancelInvoke(nameof(HideArrow)); // 이전 도착 안내 때문에 예약된 숨기기 동작을 취소한다.
                if (arrowGroup != null) arrowGroup.SetActive(true); // 연결된 안내 화면을 보여 준다.
                return; // 동료를 찾았으므로 나머지 목록은 더 살펴보지 않는다.
            } // 이름이 같은 동료를 처리했다.
        } // 동료 목록을 끝까지 살펴봤다.
        Debug.LogWarning("CrewArrowNavigator: no crewmate named " + crewName, this); // 목록에 없는 이름이면 확인할 수 있도록 알려 준다.
    } // 목적지 선택을 마친다.

    public void StopNavigation() // 기존 닫기 버튼에서도 안내를 중지할 수 있다.
    { // 현재 목적지와 예약된 동작을 정리한다.
        target = null; // 더 이상 찾아갈 동료를 기억하지 않는다.
        isNavigating = false; // 매 프레임 안내를 갱신하지 않는다.
        CancelInvoke(nameof(HideArrow)); // 이전 도착 문구의 숨기기 예약도 없앤다.
        HideArrow(); // 화면에서 안내를 바로 숨긴다.
    } // 안내 중지를 마친다.

    public void HideArrow() // 기존 연결과 도착 후 예약 호출에서 그대로 사용하는 함수이다.
    { // 화면 표시만 끄는 짧은 동작이다.
        if (arrowGroup != null) arrowGroup.SetActive(false); // 연결된 안내 오브젝트가 있을 때만 끈다.
    } // 화면 숨기기를 마친다.

    void Update() // 실행 중에는 매 프레임 호출된다.
    { // 머리 방향과 움직이는 동료의 위치를 최신 값으로 읽는다.
        if (head == null) // 머리 카메라가 없으면 거리를 계산할 기준이 없다.
        { // 연결이 사라졌을 때도 오류 없이 멈춘다.
            if (isNavigating) StopNavigation(); // 안내 중이었다면 화면을 닫는다.
            return; // 카메라가 연결될 때까지 기다린다.
        } // 머리 카메라 확인을 마친다.
        Vector3 lookDirection = head.forward; // 지금 머리가 바라보는 방향을 가져온다.
        lookDirection.y = 0f; // 좌우 화살표를 계산할 때만 시선의 높이를 없앤다.
        if (lookDirection.sqrMagnitude > 0.0001f) lastLookDirection = lookDirection.normalized; // 수평 방향이 충분히 있으면 기억해 둔다.
        if (!isNavigating) return; // 목적지가 없거나 도착 문구만 보여 주는 중이면 계산을 끝낸다.
        if (target == null) // 안내하던 동료가 장면에서 사라졌는지 확인한다.
        { // 없어진 동료의 위치를 읽지 않는다.
            StopNavigation(); // 목적지와 화면을 정리한다.
            return; // 이번 프레임 안내를 끝낸다.
        } // 목적지 확인을 마친다.

        Vector3 toTarget = target.position - head.position; // 머리에서 동료까지의 좌우, 앞뒤, 높이 차이를 모두 구한다.
        float distance = toTarget.magnitude; // 높이 차이까지 포함한 실제 3차원 거리를 먼저 구한다.
        if (distance <= Mathf.Max(0f, arriveDistance)) // 설정값이 음수여도 잘못 도착 처리하지 않도록 0 이상으로 제한한다.
        { // 동료 가까이 실제로 도착했을 때 실행한다.
            if (arrowLabel != null) arrowLabel.text = "Arrived at " + target.name; // 도착한 동료 이름을 보여 준다.
            if (arrow != null) arrow.localEulerAngles = Vector3.zero; // 도착하면 화살표를 똑바로 세운다.
            target = null; // 이번 목적지 안내는 끝났으므로 목적지를 비운다.
            isNavigating = false; // 도착 문구를 다음 프레임에 덮어쓰지 않는다.
            Invoke(nameof(HideArrow), 3f); // 도착 문구를 3초 보여 준 다음 숨긴다.
            return; // 도착했으므로 방향을 더 계산하지 않는다.
        } // 도착 여부 확인을 마친다.

        float heightDifference = toTarget.y; // 동료가 머리보다 위에 있으면 양수, 아래에 있으면 음수이다.
        Vector3 flatDirection = toTarget; // 좌우 화살표용 복사본을 만든다.
        flatDirection.y = 0f; // 복사본에서만 높이를 없애 실제 거리 값은 그대로 유지한다.
        float angle = 0f; // 동료가 바로 위나 아래라면 화살표를 정면으로 둔다.
        if (flatDirection.sqrMagnitude > 0.0001f) angle = Vector3.SignedAngle(lastLookDirection, flatDirection, Vector3.up); // 수평 거리가 있을 때만 좌우 각도를 구한다.
        if (arrow != null) arrow.localEulerAngles = new Vector3(0f, 0f, -angle); // 오른쪽 동료를 향할 때 화면 화살표도 오른쪽으로 돌린다.

        string verticalGuide = "LEVEL"; // 높이 차이가 1m 이내이면 거의 같은 높이로 안내한다.
        if (heightDifference > 1f) verticalGuide = "UP " + heightDifference.ToString("F0") + "m"; // 동료가 위에 있으면 얼마나 올라갈지 보여 준다.
        if (heightDifference < -1f) verticalGuide = "DOWN " + Mathf.Abs(heightDifference).ToString("F0") + "m"; // 동료가 아래에 있으면 얼마나 내려갈지 보여 준다.
        if (arrowLabel != null) arrowLabel.text = target.name + " " + distance.ToString("F0") + "m · " + verticalGuide; // 기존 한 줄 글자 영역에 이름, 실제 거리, 위아래 안내를 간단히 보여 준다.
    } // 이번 프레임의 안내 갱신을 마친다.
} // 동료 안내 클래스가 끝난다.
