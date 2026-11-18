using UnityEngine; // Transform, Time, MonoBehaviour 같은 Unity 형식을 사용한다.
using UnityEngine.UI; // 원래 프로젝트의 Text UI를 사용한다.

// 이 컴포넌트는 항상 켜진 host에 둔다. popupRoot는 숨길 수 있는 별도 자식이므로 host 자체를 연결하지 않는다.
// 팝업은 실제 SimpleDial 상태만 읽고 수신·연결·종료를 보여 준다. 머리를 따라가므로 세계에 고정된 UI가 아니다.
public class IncomingCallPopup : MonoBehaviour // Unity에 붙일 수 있는 실제 통화 피드백 컴포넌트다.
{ // 머리 부착 통화 피드백의 필드와 동작을 정의한다.
    public SimpleDial dial; // 실제 통화 상태를 소유한 통화기다.
    public CallTowerManager ctm; // 기존 Scene 필드를 보존한다. 주소 이름은 통화기의 메시지에 포함되어 온다.
    public Transform head; // XR Origin 안의 Main Camera를 연결한다.
    public GameObject popupRoot; // 수신·연결·종료 피드백을 담은 별도 화면 오브젝트다.
    public Text callerText; // 최대 3줄의 큰 상태·동료·주파수를 표시한다.
    public GameObject answerButtonRoot; // 수신 대기 때만 보이는 큰 ANSWER 버튼이다.
    public GameObject hangUpButtonRoot; // 일반 수신/통화 중에 보이는 큰 END 버튼이다.
    public Text actionHintText; // 현재 상태에서 할 수 있는 손동작 안내를 표시한다.
    public float distance = 0.65f; // 눈에서 앞쪽으로 팝업을 띄울 거리이며 단위는 Unity 월드 미터다.
    public float drop = 0.05f; // 눈 높이보다 아래로 내릴 거리다.
    public float feedbackSeconds = 3f; // 종료·오류 알림을 보여 주는 실제 초다.
    private int lastRevision = -1; // 마지막으로 표시한 통화 사건 번호다.
    private float hideAt; // 일시 알림이 사라질 실제 시각이다.

    private void Start() // 모든 Awake 초기화 뒤 필수 참조를 검사한다.
    { // 스스로를 꺼서 다시 켤 수 없게 되는 잘못된 연결을 막는다.
        if (dial == null || head == null || popupRoot == null || popupRoot == gameObject) // ||는 한 항목이라도 잘못됐으면 참이다.
        { // 조용히 없는 팝업으로 넘어가지 않고 오류를 알린다.
            Debug.LogError("IncomingCallPopup: assign dial, head and a separate popupRoot.", this); // Inspector에서 연결할 항목을 알려 준다.
            enabled = false; // 이 잘못 연결된 컴포넌트만 멈춘다.
            return; // 나머지 화면 처리를 실행하지 않는다.
        } // 참조 검사 끝이다.
        popupRoot.SetActive(false); // 시작할 때 가짜 수신 팝업을 띄우지 않는다.
    } // 팝업 초기화를 끝낸다.

    private void LateUpdate() // 이번 프레임의 머리 이동과 통화 상태 변경 뒤에 화면을 맞춘다.
    { // 새 사건은 즉시 표시하고 통화가 계속되는 동안에는 유지한다.
        if (lastRevision != dial.StatusRevision) // 실제 통화 상태가 바뀌었는지 비교한다.
        { // 새 수신·연결·종료 알림을 표시한다.
            lastRevision = dial.StatusRevision; // 이미 표시한 사건 번호를 기록한다.
            bool visible = dial.State != SimpleDial.CallState.Idle; // 처음의 대기 상태만 알림 없이 시작한다.
            popupRoot.SetActive(visible); // 수신을 받았을 때도 연결 확인이 계속 보이게 한다.
            hideAt = Time.unscaledTime + Mathf.Max(0.5f, feedbackSeconds); // 종료·오류 알림은 최소 0.5초 이상 읽을 수 있게 한다.
            if (callerText != null) callerText.text = dial.StatusMessage; // 가짜 문구가 아니라 실제 통화기 상태를 그대로 보여 준다.
            UpdateActions(); // 상태에 맞는 받기/종료 버튼과 안내를 맞춘다.
        } // 새 사건 표시를 끝낸다.
        if (!dial.IsBusy && !dial.IsAudioTest && Time.unscaledTime >= hideAt) popupRoot.SetActive(false); // 일반 통화가 끝난 후 알림 시간을 채웠을 때만 숨긴다.
        if (!popupRoot.activeSelf) return; // 숨겨진 화면은 위치 계산이 필요 없다.
        popupRoot.transform.position = head.position + head.forward * Mathf.Max(0.2f, distance) - head.up * drop; // 머리의 앞·위 축을 사용하여 고개를 돌려도 붙어 있게 한다.
        popupRoot.transform.rotation = head.rotation; // 글자 면도 머리 방향을 따라가며 한 번 놓인 월드 위치에 남지 않는다.
    } // 실제 통화 상태와 머리 위치 갱신을 끝낸다.

    private void UpdateActions() // 지금 눌러도 되는 큰 버튼과 안내를 고른다.
    { // 수신·연결·SOS는 서로 다른 행동 가능 상태다.
        bool ringing = dial.IsRinging(); // 기존 API로 실제 수신 대기 여부를 읽는다.
        if (answerButtonRoot != null) answerButtonRoot.SetActive(ringing); // 받기 버튼은 수신 대기 중일 때만 보인다.
        if (hangUpButtonRoot != null) hangUpButtonRoot.SetActive((dial.IsBusy || dial.IsAudioTest) && !dial.EmergencyRequested); // 구조 접수 뒤 일반 종료 버튼은 숨긴다.
        if (actionHintText == null) return; // 안내 Text가 선택적으로 빠져도 버튼 상태는 이미 맞췄다.
        if (ringing) actionHintText.text = "TOUCH OR AIM + CLOSE, THEN OPEN"; // 목소리가 아직 재생되지 않았음을 행동 안내로 알린다.
        else if (dial.IsAudioTest) actionHintText.text = "LISTEN TO ELINA · END TO STOP"; // 같은 목소리 소스로 소리가 들리는지 확인하는 중이다.
        else if (dial.EmergencyRequested) actionHintText.text = "RESCUE ACCEPTED · PLEASE WAIT"; // 실제 구조 예약이 끝날 때까지 기다리게 한다.
        else if (dial.State == SimpleDial.CallState.Connected) actionHintText.text = "VOICE CONNECTED · END TO HANG UP"; // 받기에 성공했다는 사실과 종료 방법을 알린다.
        else if (dial.State == SimpleDial.CallState.Ended) actionHintText.text = "LINE FREE · READY FOR ANOTHER CALL"; // 자동/수동 종료 뒤 회선이 비었음을 알린다.
        else actionHintText.text = "CHECK THE ADDRESS OR CREW LIST"; // 잘못된 번호 등 입력 오류를 고칠 방향을 알려 준다.
    } // 상태별 버튼과 안내 선택을 끝낸다.

    public void Answer() // 기존 받기 UnityEvent에 저장된 API를 보존한다.
    { // 직접 팝업을 숨기지 않고 통화기의 실제 연결 상태를 기다린다.
        if (dial != null) dial.AnswerCall(); // 다음 LateUpdate에서 CONNECTED가 큰 글씨로 표시된다.
    } // 받기 버튼 전달을 끝낸다.

    public void HangUp() // 기존 종료 UnityEvent에 저장된 API를 보존한다.
    { // 종료 뒤 3초 피드백은 실제 상태 변경을 읽어 표시한다.
        if (dial != null) dial.HangUp(); // 실제 벨·목소리·반복 플래그를 함께 정리한다.
    } // 종료 버튼 전달을 끝낸다.
} // IncomingCallPopup 정의를 끝낸다.
