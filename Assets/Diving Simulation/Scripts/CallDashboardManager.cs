using System; // Array.Empty와 대소문자 비교 규칙을 사용한다.
using UnityEngine; // MonoBehaviour, Time, Color 같은 Unity 형식을 사용한다.
using UnityEngine.UI; // 기존 프로젝트의 UGUI Text와 Button을 그대로 사용한다.

// PHONE은 문자열 내선, FREQ는 정수 무전 채널이다. 서로 다른 입력 칸을 보존하지만 주소록에서 같은 동료에 연결한다.
// 이 스크립트는 음성을 직접 재생하지 않고 SimpleDial에 요청한다. Scene의 Button.onClick에는 기존/신규 공개 함수를 연결한다.
public class CallDashboardManager : MonoBehaviour // UI를 Unity 오브젝트에 붙이는 클래스다.
{ // 키패드와 연락처 목록 동작을 정의한다.
    public SimpleDial dial; // 항상 켜져 있는 실제 통화기 컴포넌트다.
    public CallTowerManager ctm; // 두 주소 체계가 같은 사람을 가리키도록 관리하는 주소록이다.
    public Text numberDisplay; // 현재 모드에서 입력 중인 숫자를 크게 표시한다.
    public int maxDigits = 4; // 기존 필드를 유지하며 1001 SOS를 입력할 수 있도록 최소 4자리까지 허용한다.
    public int maxPhoneDigits = 4; // 기본 게임 내선 2001~2005의 입력 길이는 4자리다.
    public Text modeDisplay; // PHONE 또는 FREQ라는 현재 입력 방식의 제목이다.
    public Text feedbackText; // 실제 통화 상태 또는 잘못된 입력 안내를 보여 준다.
    public Text[] contactLabels = new Text[5]; // 동료 이름·주파수·내선을 표시하는 5개 큰 연락처 글자다.
    public Button[] contactButtons = new Button[5]; // 같은 index의 동료 버튼이며 클릭 이벤트는 Scene에서 한 번 연결한다.
    public Color normalColor = new Color(1f, 1f, 1f, 1f); // 미선택 연락처는 Eliza 원본의 흰색이며 마지막 1은 불투명이다.
    public Color selectedColor = new Color(0.5686275f, 0.7843138f, 1f, 1f); // 선택 연락처는 Eliza 원본 InteractionColor의 연한 하늘색이다.
    private bool phoneMode; // false는 FREQ, true는 PHONE 모드다.
    private string frequencyInput = ""; // 무전 키패드에서 입력한 내용만 저장한다.
    private string phoneInput = ""; // 전화 키패드 입력을 따로 저장하여 모드를 바꿔도 지우지 않는다.
    private CrewInfo[] contacts = Array.Empty<CrewInfo>(); // 주소록 준비 전에는 안전한 빈 목록으로 시작한다.
    private float nextRefresh; // 연락처를 매 프레임 새 배열로 조회하지 않도록 다음 조회 시각을 기억한다.
    private int selectedIndex = -1; // -1은 아직 어떤 연락처도 선택하지 않았다는 뜻이다.
    private int lastStatusRevision = -1; // 실제 통화 상태 변경이 있을 때만 안내 문구를 갱신한다.
    public bool IsPhoneMode => phoneMode; // 외부 HUD가 필요하면 모드를 읽기만 할 수 있다.
    private string CurrentInput => phoneMode ? phoneInput : frequencyInput; // ? : 는 현재 모드의 입력 문자열 하나를 고른다.

    private void OnEnable() // 패널이 다시 열려도 이전 입력값을 화면에 복원한다.
    { // 입력과 연락처 표시를 최신 상태로 만든다.
        nextRefresh = 0f; // 다음 Update에서 주소록을 즉시 다시 읽게 한다.
        lastStatusRevision = -1; // 현재 통화 상태를 한 번 다시 표시하게 한다.
        UpdateDisplay(); // 빈 입력 또는 보존된 숫자를 표시한다.
    } // 패널 활성화 처리를 끝낸다.

    private void Update() // 상태와 연락처를 적당한 시점에만 새로 읽는다.
    { // 실제 상태 변경은 즉시, 주소록 표시는 0.5초 간격으로 갱신한다.
        if (Time.unscaledTime >= nextRefresh) // 게임 시간 배율과 무관한 실제 시각을 비교한다.
        { // 연락처 조회 시점이다.
            nextRefresh = Time.unscaledTime + 0.5f; // 다음 조회는 0.5초 후로 예약한다.
            RefreshContacts(); // 주파수가 초기화된 5명 목록을 읽고 그린다.
        } // 연락처 주기 갱신을 끝낸다.
        if (dial != null && lastStatusRevision != dial.StatusRevision) // 새 수신·연결·종료 사건이 있을 때만 문구를 바꾼다.
        { // 통화기의 실제 상태를 그대로 보여 준다.
            lastStatusRevision = dial.StatusRevision; // 방금 표시한 사건 번호를 기억한다.
            ShowMessage(dial.StatusMessage); // 통화기와 화면이 서로 다른 상태를 말하지 않게 한다.
        } // 새 통화 상태 표시를 끝낸다.
    } // 이번 프레임의 UI 갱신을 끝낸다.

    public void SetMode(bool usePhone) // Scene에서는 bool 인수를 가진 모드 선택 버튼에도 연결할 수 있다.
    { // 입력값은 지우지 않고 보이는 입력 방식만 바꾼다.
        phoneMode = usePhone; // true면 전화, false면 무전이다.
        UpdateDisplay(); // 선택된 모드의 별도 버퍼를 화면에 보여 준다.
        if (dial != null) dial.PlayButtonClick(); // 실제 버튼을 눌렀다는 짧은 피드백을 준다.
        if (dial != null && dial.IsBusy) ShowMessage(dial.StatusMessage); // 통화 중 모드를 바꾸어도 실제 수신·연결·SOS 안내를 유지한다.
        else ShowMessage(phoneMode ? "GAME EXTENSION 2001–2005\nSOS 1001" : "RADIO FREQ 100–999\nSOS 1001"); // 대기 중에는 두 주소 체계의 의미를 명시한다.
    } // 모드 전환을 끝낸다.

    public void SetFrequencyMode() { SetMode(false); } // 인수 없는 FREQ 버튼용 wrapper이며 기존 UnityEvent 연결이 쉽다.
    public void SetPhoneMode() { SetMode(true); } // 인수 없는 PHONE 버튼용 wrapper다.

    public void PressDigit(int digit) // 기존 숫자 버튼 콜백 이름과 int 인수를 보존한다.
    { // 키패드는 0부터 9까지의 숫자 하나만 받는다.
        if (digit < 0 || digit > 9) return; // 잘못된 Inspector 인수로 여러 글자가 들어가지 않게 한다.
        int limit = phoneMode ? Mathf.Max(4, maxPhoneDigits) : Mathf.Max(4, maxDigits); // 두 모드 모두 예약 SOS 1001을 입력할 수 있다.
        if (CurrentInput.Length >= limit) return; // 허용 길이를 넘으면 기존 입력을 유지한다.
        StoreInput(CurrentInput + digit.ToString()); // 숫자를 문자열로 바꾸어 현재 모드 버퍼 끝에 붙인다.
        if (dial != null) dial.PlayButtonClick(); // 눌렀다는 확인 소리를 낸다.
        UpdateDisplay(); // 새 숫자를 즉시 보여 준다.
    } // 숫자 입력 처리를 끝낸다.

    public void Backspace() // 기존 뒤로 지우기 버튼 API다.
    { // 현재 모드의 마지막 글자 하나만 없앤다.
        if (CurrentInput.Length == 0) return; // 빈 문자열에서 음수 길이를 요청하지 않는다.
        StoreInput(CurrentInput.Substring(0, CurrentInput.Length - 1)); // Substring은 앞에서 마지막 글자 직전까지 복사한다.
        if (dial != null) dial.PlayButtonClick(); // 삭제 입력의 짧은 피드백이다.
        UpdateDisplay(); // 현재 모드의 줄어든 입력을 그린다.
    } // 한 글자 삭제를 끝낸다.

    public void ClearNumber() // 기존 전체 지우기 버튼 API다.
    { // 다른 모드의 입력은 보존한다.
        StoreInput(""); // 현재 모드 버퍼만 빈 문자열로 만든다.
        if (dial != null) dial.PlayButtonClick(); // 지우기 입력을 확인한다.
        UpdateDisplay(); // 입력 대기 표시를 다시 그린다.
    } // 현재 모드 지우기를 끝낸다.

    private void StoreInput(string value) // 두 독립 입력 칸 중 현재 칸에만 쓰는 작은 함수다.
    { // 문자열 내선의 앞자리 0을 임의로 제거하지 않는다.
        if (phoneMode) phoneInput = value; // 전화 모드이면 전화 버퍼에 넣는다.
        else frequencyInput = value; // 그 외에는 무전 버퍼에 넣는다.
    } // 모드별 입력 저장을 끝낸다.

    public void DialAddress() // CALL 버튼이 현재 모드의 주소로 연결을 요청한다.
    { // 예약 SOS를 먼저 처리하고 일반 전화와 무전을 구분한다.
        if (dial == null || ctm == null) { ShowMessage("CALL SYSTEM NOT CONNECTED"); return; } // 필수 참조 누락을 명확히 표시한다.
        string address = CurrentInput; // 이번 클릭 시점의 주소를 한 번 읽는다.
        if (address == "1001") { CallEmergency(); return; } // PHONE과 FREQ 양쪽에서 1001은 항상 구조 요청이다.
        if (dial.IsBusy) { ShowMessage("LINE BUSY\nEND THE CURRENT CALL FIRST"); return; } // 현재 수신·통화를 새 발신이 몰래 끊지 않는다.
        if (address.Length == 0) { dial.ReportAddressError("ENTER AN ADDRESS FIRST"); return; } // 빈 입력을 0번 채널로 바꾸지 않는다.
        int frequency; // 두 주소 방식이 최종적으로 찾을 동료 무전 주소다.
        if (phoneMode) // 전화 내선은 정수 계산 대신 주소록에서 문자열로 찾는다.
        { // 같은 동료의 주파수를 얻은 뒤 공통 통화기로 전달한다.
            if (!ctm.TryResolvePhone(address, out frequency)) { dial.ReportAddressError("NO CONTACT\nEXT " + address + "\nCHECK THE CREW LIST"); return; } // out 결과는 성공했을 때만 사용한다.
        } // 전화 내선 변환을 끝낸다.
        else if (!int.TryParse(address, out frequency)) { dial.ReportAddressError("ENTER A VALID RADIO FREQ"); return; } // TryParse는 잘못된 입력에도 예외를 던지지 않는다.
        dial.QuickDial(frequency); // 두 모드가 같은 사람의 같은 실제 통화 상태로 합쳐지는 지점이다.
    } // 현재 주소 발신 처리를 끝낸다.

    public void CallDialed() { DialAddress(); } // 원래 CALL UnityEvent에 저장된 이름을 보존한다.

    public void SelectContact(int index) // 5개 연락처 버튼에는 각각 0, 1, 2, 3, 4를 연결한다.
    { // 선택한 사람의 현재 모드 주소를 채우고 발신을 요청한다.
        RefreshContacts(); // 클릭 순간의 실제 주소록을 읽어 오래된 주파수를 사용하지 않는다.
        if (index < 0 || index >= contacts.Length) return; // 배열 범위를 벗어난 버튼 인수를 막는다.
        selectedIndex = index; // 색으로 강조할 사람을 기록한다.
        CrewInfo contact = contacts[index]; // 선택한 동료의 정보 묶음을 읽는다.
        StoreInput(phoneMode ? contact.phoneNumber : contact.frequency.ToString()); // 현재 모드 버퍼만 선택한 사람의 주소로 채운다.
        UpdateDisplay(); // 발신에 사용한 주소를 사용자가 볼 수 있게 한다.
        RefreshContacts(); // 선택한 버튼의 강조 색을 즉시 갱신한다.
        if (dial != null) dial.PlayButtonClick(); // 연락처 클릭을 짧게 확인한다.
        DialAddress(); // 실제 통화기가 연결 가능 여부와 오디오를 처리한다.
    } // 연락처 선택 발신을 끝낸다.

    public void CallCrewmate(string crewName) // 원래 이름 인수 방식의 Scene 콜백도 그대로 동작한다.
    { // 같은 주소록에서 이름을 찾은 뒤 새 연락처 동작을 재사용한다.
        RefreshContacts(); // 동료 목록을 최신으로 받는다.
        for (int i = 0; i < contacts.Length; i++) // 동료 이름을 하나씩 확인한다.
            if (string.Equals(contacts[i].name, crewName, StringComparison.OrdinalIgnoreCase)) { SelectContact(i); return; } // 대소문자 차이만 무시하고 같은 동료를 선택한다.
        if (dial != null) dial.ReportAddressError("CONTACT NOT FOUND\n" + crewName); // 없는 사람을 통화 성공으로 표시하지 않는다.
    } // 이름으로 발신하는 기존 API 처리를 끝낸다.

    public void CallEmergency() // 전용 SOS 버튼은 입력 모드나 현재 통화와 관계없이 동작한다.
    { // HangUp을 먼저 요구하지 않고 통화기의 우선 처리 분기로 간다.
        if (dial == null || ctm == null) { ShowMessage("SOS SYSTEM NOT CONNECTED"); return; } // 누락된 연결은 숨기지 않는다.
        dial.QuickDial(ctm.GetEmergencyFrequency()); // 일반 통화를 중단하고 1001 구조 안내를 우선한다.
    } // SOS 버튼 처리를 끝낸다.

    public void EndCall() // 기존 END 버튼 API다.
    { // 통화를 끝내고 현재 입력만 비운다.
        if (dial != null) dial.HangUp(); // SOS가 이미 접수됐다면 통화기가 구조 상태를 보존한다.
        ClearNumber(); // 다른 주소 방식에 입력한 숫자는 지우지 않는다.
    } // 종료 버튼 처리를 끝낸다.

    private void RefreshContacts() // 목록 정보는 주소록 API를 통해서만 읽는다.
    { // 새 주파수와 같은 사람의 고정 내선을 나란히 표시한다.
        contacts = ctm != null ? ctm.GetCrewmatesInformation() : Array.Empty<CrewInfo>(); // 주소록이 없으면 빈 목록을 사용한다.
        int labelCount = contactLabels != null ? contactLabels.Length : 0; // null 배열도 안전하게 처리한다.
        for (int i = 0; i < labelCount; i++) // Inspector에 연결된 글자를 하나씩 갱신한다.
        { // 한 슬롯당 이름과 두 주소의 2줄만 사용한다.
            if (contactLabels[i] == null) continue; // 빈 Text 참조는 건너뛴다.
            contactLabels[i].text = i < contacts.Length ? contacts[i].name + "\nFREQ " + contacts[i].frequency + "   EXT " + contacts[i].phoneNumber : "WAITING FOR CONTACT"; // 같은 사람이 두 주소를 갖는다는 점을 보여 준다.
        } // 연락처 글자 갱신을 끝낸다.
        int buttonCount = contactButtons != null ? contactButtons.Length : 0; // 연결된 버튼 수를 안전하게 얻는다.
        for (int i = 0; i < buttonCount; i++) // 연락처 버튼의 사용 가능 상태와 강조 색을 갱신한다.
        { // 빈 슬롯이나 SOS 중에는 연락처 버튼을 사용할 수 없다.
            Button button = contactButtons[i]; // 현재 버튼 참조를 지역 변수로 읽는다.
            if (button == null) continue; // 누락된 슬롯은 오류 없이 건너뛴다.
            button.interactable = i < contacts.Length && dial != null && !dial.EmergencyRequested; // 구조 요청은 일반 발신보다 우선한다.
            ColorBlock colors = button.colors; // ColorBlock은 값 묶음이므로 복사해서 바꾼 뒤 다시 넣는다.
            colors.normalColor = i == selectedIndex ? selectedColor : normalColor; // 선택한 사람만 다른 바탕색으로 표시한다.
            colors.selectedColor = selectedColor; // 키보드/접근성 선택 표시도 같은 강조색을 사용한다.
            button.colors = colors; // 수정한 값 묶음을 실제 버튼에 돌려준다.
        } // 연락처 버튼 갱신을 끝낸다.
    } // 주소록 UI 새로고침을 끝낸다.

    public void UpdateDisplay() // 원래 공개 화면 갱신 함수 이름을 유지한다.
    { // FREQ는 기본 3자리, PHONE은 기본 4자리 빈칸을 보여 준다.
        string value = CurrentInput; // 현재 모드에 해당하는 문자열을 읽는다.
        int cells = Mathf.Max(phoneMode ? 4 : 3, value.Length); // SOS 1001을 입력하면 FREQ에서도 4칸으로 늘어난다.
        string text = ""; // 표시할 글자를 모을 빈 문자열이다.
        for (int i = 0; i < cells; i++) // 입력한 자리 또는 아직 비어 있는 자리를 하나씩 붙인다.
        { // 숫자 사이에는 공백을 두어 손목 UI에서도 읽기 쉽게 한다.
            text += i < value.Length ? value[i].ToString() : "_"; // 입력한 숫자가 없으면 밑줄을 표시한다.
            if (i < cells - 1) text += " "; // 마지막 뒤에는 불필요한 공백을 붙이지 않는다.
        } // 숫자 표시 문자열을 완성했다.
        if (numberDisplay != null) numberDisplay.text = text; // 연결된 큰 숫자 Text에 값을 넣는다.
        if (modeDisplay != null) modeDisplay.text = phoneMode ? "PHONE · GAME EXTENSION" : "FREQ · RADIO CHANNEL"; // 실제 전화망이 아니라 게임 내선임을 명시한다.
    } // 입력 화면 갱신을 끝낸다.

    private void ShowMessage(string message) // 선택적 안내 Text에 문자열을 넣는 작은 함수다.
    { // Text가 연결되지 않아도 통화 로직 자체를 깨뜨리지 않는다.
        if (feedbackText != null) feedbackText.text = message; // 실제 상태 또는 입력 안내를 화면에 쓴다.
    } // 안내 표시를 끝낸다.
} // CallDashboardManager 정의를 끝낸다.
