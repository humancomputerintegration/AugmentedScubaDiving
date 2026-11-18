using UnityEngine; // 동료의 위치와 거리를 계산한다.
using UnityEngine.UI; // 동료 정보 화면의 글자를 바꾼다.

public class CrewInfodashboardManager : MonoBehaviour // 선택한 동료의 실제 위치와 두 연락 주소를 보여 준다.
{
    public CallTowerManager ctm; // 통화와 같은 동료 목록을 사용하는 주소록이다.
    public Transform player; // 거리를 계산할 실제 머리 카메라다.
    public Text infoHint; // 아직 이름을 고르지 않았을 때의 안내다.
    public Text nameText; // 선택한 사람의 이름이다.
    public Text locationText; // 실제 세계 좌표와 거리다.
    public Text frequencyText; // 무전 주파수와 게임 내선이다.
    private string selectedName = ""; // 아직 아무도 고르지 않았으면 빈 문자열이다.

    private void OnEnable() // Unity는 대문자 O로 시작한 이 정확한 이름을 호출한다.
    {
        selectedName = ""; // 화면을 다시 열면 선택 대기 상태로 시작한다.
        if (nameText != null) nameText.text = "NAME"; // 이전 사람 이름을 지운다.
        if (locationText != null) locationText.text = ""; // 이전 좌표를 지운다.
        if (frequencyText != null) frequencyText.text = ""; // 이전 주소를 지운다.
        if (infoHint != null) infoHint.gameObject.SetActive(true); // 이름을 고르라는 안내를 켠다.
    }

    public void ShowCrewmate(string crewName) // 각 이름 버튼에서 전달한 이름을 받는다.
    {
        selectedName = crewName; // 다음 프레임에도 같은 사람을 표시하도록 저장한다.
        RefreshInfo(); // 누른 순간 바로 정보를 표시한다.
    }

    private void Update() // 움직이는 Elina의 정보도 계속 갱신한다.
    {
        if (selectedName.Length > 0) RefreshInfo(); // 선택된 사람이 있을 때만 목록을 읽는다.
    }

    private void RefreshInfo() // 선택한 이름을 실제 주소록에서 찾는다.
    {
        if (ctm == null) return; // 주소록이 연결되지 않았으면 끝낸다.
        foreach (CrewInfo info in ctm.GetCrewmatesInformation()) // 현재 다섯 사람의 정보를 하나씩 비교한다.
        {
            if (info.name != selectedName) continue; // 선택하지 않은 사람은 건너뛴다.
            if (nameText != null) nameText.text = info.name; // 선택한 사람의 이름을 크게 표시한다.
            string location = "POSITION\n" + info.worldLocation.ToString("F1"); // 실제 좌표를 소수 한 자리까지 표시한다.
            if (player != null) location += "\n" + Vector3.Distance(player.position, info.worldLocation).ToString("F1") + " m away"; // 실제 머리부터 동료까지의 거리를 덧붙인다.
            if (locationText != null) locationText.text = location; // 화면에 위치와 거리를 표시한다.
            if (frequencyText != null) frequencyText.text = "FREQ " + info.frequency + "\nPHONE " + info.phoneNumber; // 통화 창과 같은 두 연락 주소를 표시한다.
            if (infoHint != null) infoHint.gameObject.SetActive(false); // 선택 대기 안내는 숨긴다.
            return; // 찾았으므로 다른 사람을 더 검사할 필요가 없다.
        }
        selectedName = ""; // 없는 이름이면 매 프레임 같은 조회를 반복하지 않는다.
        if (infoHint != null) infoHint.gameObject.SetActive(true); // 다시 선택할 수 있도록 안내를 보여 준다.
    }
}
