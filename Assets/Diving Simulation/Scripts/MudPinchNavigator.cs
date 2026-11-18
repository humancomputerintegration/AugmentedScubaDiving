/* 보관용 원본: 최종 Simulation에서는 비활성화합니다. 새 UI의 주먹 onset/touch와 중복 실행하지 않습니다. */
using UnityEngine; // 위치, 회전, GameObject와 MonoBehaviour 등 Unity의 기본 기능을 가져옵니다.
using UnityEngine.XR.Hands; // XR Hands의 손 추적 상태와 관절 이름을 사용합니다.
using UnityEngine.XR.Management; // 현재 XR 로더가 제공하는 손 추적 시스템을 찾습니다.
using UnityEngine.XR.Interaction.Toolkit; // 원본 XR 상호작용 이벤트 인자 형식을 사용합니다.
using UnityEngine.XR.Interaction.Toolkit.Interactables; // 원본 XRSimpleInteractable 버튼을 사용합니다.
using UnityEngine.InputSystem; // 원본의 개발용 키보드 입력을 읽습니다.

/*
Only during a mud storm while underwater:
quick RIGHT pinch (thumb + middle finger)  -> highlight the next button
hold the pinch for holdToSelectTime        -> press the highlighted button

Editor test: N = next, Enter = press (tick Force Mud Mode to test without a storm).
*/
public class MudPinchNavigator : MonoBehaviour // 보관하는 원본 mud 전용 pinch UI입니다. 최종 Scene에서는 새 공통 손 UI와 중복되지 않게 끕니다.
{
    [System.Serializable] // 이 묶음의 각 필드를 Unity Inspector와 Scene 파일에 저장할 수 있게 합니다.
    public class NavButton // XR 버튼과 강조 표시를 한 쌍으로 묶습니다.
    {
        public XRSimpleInteractable button;   // the Btn_... root | 원본 XR 선택 이벤트를 가진 버튼입니다.
        public GameObject highlight;          // its Highlight ring | 선택 순서를 표시하는 강조 링입니다.
    }

    public MudStorm mud;                      // Mud Manager | 현재 폭풍 여부를 읽는 원본 관리자입니다.
    public InformationManager iM;             // Information Manager | 수중 여부를 확인할 원본 정보 관리자입니다.

    public GameObject menuRoot;               // RoundMenu | 원본 원형 메뉴입니다.
    public NavButton[] menuButtons;           // the 5 round buttons, left to right | 순서대로 순회할 원형 메뉴 버튼 배열입니다.
    public GameObject callPopup;              // IncomingCallPopup (Part 4) | 수신 전화 팝업입니다.
    public NavButton[] popupButtons;          // Answer, Hang Up | 원본 팝업의 Answer/Hang Up 버튼 배열입니다.

    public float pinchStartDistance = 0.02f;  // metres between thumb and middle tip to start a pinch | 엄지와 중지 끝이 이 거리 이하면 pinch를 시작합니다.
    public float pinchEndDistance = 0.035f;   // ...to end it (bigger = no flicker) | 이 거리 이상 벌어져야 pinch를 끝내 경계 떨림을 줄입니다.
    public float holdToSelectTime = 0.8f;     // hold this long to press | 이 시간 이상 pinch를 유지하면 선택합니다.

    [Header("Editor testing")] // Inspector에 PC 점검 설정 제목을 표시합니다.
    public bool forceMudMode = false; // 테스트에서는 실제 폭풍 없이 이 방식을 켤 수 있습니다.
    public Key testNextKey = Key.N; // 개발용 N 키는 다음 버튼으로 이동합니다.
    public Key testSelectKey = Key.Enter; // 개발용 Enter 키는 현재 버튼을 누릅니다.

    private XRHandSubsystem hands; // 원본이 읽는 손 추적 시스템입니다.
    private bool pinching; // 직전까지 pinch를 유지했는지 기억합니다.
    private float pinchStartTime; // 현재 pinch를 시작한 시각입니다.
    private bool selectFired; // 같은 pinch 유지 중에 이미 선택했는지 기억합니다.
    private NavButton[] current;              // list being navigated right now (null = none) | 현재 순회하는 버튼 배열이며 없으면 null입니다.
    private int index; // 현재 강조하는 버튼의 배열 번호입니다.

    

    // Update is called once per frame
    void Update() // Unity가 실행 중 매 프레임 호출하는 함수입니다.
    {
        if (hands == null) // 손 추적 시스템을 아직 찾지 못한 경우입니다.
        {
            var loader = XRGeneralSettings.Instance?.Manager?.activeLoader; // XR 설정과 관리자가 있을 때만 현재 로더를 읽습니다.
            if (loader != null) hands = loader.GetLoadedSubsystem<XRHandSubsystem>(); // 로더의 손 추적 시스템을 찾습니다.
        }

        // Which buttons should be navigable right now?
        NavButton[] list = GetActiveList(); // 현재 사용할 메뉴 또는 팝업 버튼 목록을 선택합니다.
        if (list != current) // 사용 중인 목록이 바뀌었는지 비교합니다.
        {
            SetHighlight(current, -1);        // clear the old list's highlight | 이전 목록의 강조를 모두 끕니다.
            current = list; // 새 버튼 목록을 현재 목록으로 저장합니다.
            index = 0; // 새 목록은 첫 버튼부터 시작합니다.
        }
        if (current == null || current.Length == 0) return; // 사용할 버튼이 없으면 입력을 처리하지 않습니다.

        SetHighlight(current, index); // 현재 번호의 버튼만 강조합니다.

        if (Keyboard.current != null) // 키보드가 연결되어 있을 때만 시험 키를 읽습니다.
        {
            if (Keyboard.current[testNextKey].wasPressedThisFrame) Next(); // N을 새로 누른 프레임에 다음 버튼으로 갑니다.
            if (Keyboard.current[testSelectKey].wasPressedThisFrame) Press(); // Enter를 새로 누른 프레임에 현재 버튼을 누릅니다.
        }

        HandlePinch(); // 원본의 오른손 엄지+중지 pinch를 판정합니다.
    }
    NavButton[] GetActiveList() // 현재 사용할 버튼 배열을 반환하는 함수입니다.
    {
        bool mudMode = forceMudMode || (mud.storm && iM.GetDepth() < 0f); // 강제 시험 모드 또는 수중 폭풍일 때만 원본 방식을 켭니다.
        if (!mudMode) return null; // 조건이 아니면 선택 목록이 없습니다.

        if (callPopup != null && callPopup.activeSelf) return popupButtons;  // popup has priority | 열린 수신 팝업을 원형 메뉴보다 우선합니다.
        if (menuRoot != null && menuRoot.activeSelf) return menuButtons; // 팝업이 없고 원형 메뉴가 열렸다면 메뉴 버튼을 사용합니다.
        return null; // 사용할 대상을 찾지 못했다는 뜻으로 빈 참조를 돌려줍니다.
    }

    void HandlePinch() // pinch의 시작·유지·해제를 구분하는 원본 함수입니다.
    {
        if (hands == null) return; // 손 추적 시스템이 없으면 판정하지 않습니다.

        XRHand right = hands.rightHand; // 오른손 자료를 선택합니다.
        if (!right.isTracked || // 오른손 추적이 없거나 아래 필요한 관절 중 하나라도 없으면 실패합니다.
            !right.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out Pose thumb) || // 엄지 끝 위치가 유효한지 검사하고 결과를 thumb에 받습니다.
            !right.GetJoint(XRHandJointID.MiddleTip).TryGetPose(out Pose middle)) // 중지 끝 위치가 유효한지 검사하고 결과를 middle에 받습니다.
        {
            pinching = false; // 현재 pinch를 끝났거나 없는 것으로 표시합니다.
            return; // 현재 함수를 바로 끝내고 이번 프레임의 남은 계산을 하지 않습니다.
        }

        float distance = Vector3.Distance(thumb.position, middle.position); // 두 손끝 사이의 실제 거리를 계산합니다.

        if (!pinching && distance <= pinchStartDistance) // 아직 pinch가 아닌데 두 손끝이 충분히 가까워졌습니다.
        {
            pinching = true;                  // pinch started | 새 pinch를 시작했다고 표시합니다.
            pinchStartTime = Time.time; // 얼마나 오래 유지하는지 잴 시작 시각을 저장합니다.
            selectFired = false; // 새 pinch에서는 아직 선택하지 않았습니다.
        }
        else if (pinching && distance >= pinchEndDistance) // 유지하던 pinch가 충분히 벌어졌습니다.
        {
            pinching = false;                 // pinch released | 현재 pinch를 끝났거나 없는 것으로 표시합니다.
            if (!selectFired) Next();         // it was a quick pinch -> move highlight | 오래 유지해 선택하지 않았다면 짧은 pinch이므로 다음 버튼으로 갑니다.
        }

        if (pinching && !selectFired && Time.time - pinchStartTime >= holdToSelectTime) // 유지 중이며 아직 선택하지 않았고 지정 시간을 채웠는지 검사합니다.
        {
            selectFired = true;               // held long enough -> press | 먼저 선택했다고 표시해 같은 pinch에서 중복 실행하지 않습니다.
            Press(); // 현재 강조 버튼의 원본 선택 이벤트를 호출합니다.
        }
    }

    void Next() // 현재 번호를 다음 버튼으로 바꿉니다.
    {
        index = (index + 1) % current.Length; // 나머지 연산 % 덕분에 마지막 다음은 다시 0번입니다.
        SetHighlight(current, index); // 현재 번호의 버튼만 강조합니다.
    }
    
    void Press() // 원본 XR 버튼에 선택 이벤트를 직접 전달합니다.
    {
        XRSimpleInteractable b = current[index].button; // 현재 번호에 해당하는 XR 버튼입니다.
        if (b != null) // 버튼 연결이 있을 때만 호출합니다.
            b.selectEntered.Invoke(new SelectEnterEventArgs());   // same as poking it | 원본은 새 이벤트 인자를 만들어 selectEntered를 직접 실행합니다.
    }

    void SetHighlight(NavButton[] list, int on) // 지정 번호 하나만 강조하는 함수입니다.
    {
        if (list == null) return; // 버튼 목록이 없으면 끝냅니다.
        for (int i = 0; i < list.Length; i++) // 목록의 모든 번호를 차례로 검사합니다.
            if (list[i].highlight != null) // 강조 물체가 연결된 버튼만 처리합니다.
                list[i].highlight.SetActive(i == on); // 현재 번호와 on이 같으면 켜고 다르면 끕니다.
    }



        
    
}
