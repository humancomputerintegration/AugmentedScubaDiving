using UnityEngine; // Unity 컴포넌트의 기본 기능을 사용한다.
using UnityEngine.UI; // 상세 정보의 글자를 바꾼다.

public class DiveInfo : MonoBehaviour // 원형 INFO 버튼으로 여는 상세 정보 화면을 갱신한다.
{
    public InformationManager iM; // 원본 정보 관리자다.
    public DiveStatusHUD status; // 항상 작동 중인 상단 표시의 잠수 시간을 함께 사용한다.
    public Text depthText; // 머리 기준 수심 글자다.
    public Text batteryText; // 배터리 잔량 글자다.
    public Text oxygenText; // 산소 잔량 글자다.
    public Text diveTimeText; // 이번 잠수 시간 글자다.

    private void Update() // 정보가 변할 때마다 화면에도 반영한다.
    {
        if (iM == null) return; // 정보 연결이 없으면 끝낸다.
        if (depthText != null) depthText.text = "HEAD DEPTH  " + Mathf.Max(0f, -iM.GetDepth()).ToString("F1") + " m"; // 음수 높이를 양수 수심으로 바꾼다.
        if (batteryText != null) batteryText.text = "BATTERY  " + iM.GetBatteryLevel().ToString("F0") + "%"; // 같은 배터리 원본 값을 쓴다.
        if (oxygenText != null) oxygenText.text = "O2  " + iM.GetOxygenLevel().ToString("F0") + "%"; // 같은 산소 원본 값을 쓴다.
        if (diveTimeText == null || status == null) return; // 시간 표시에 필요한 연결을 확인한다.
        int seconds = Mathf.FloorToInt(status.DiveSeconds); // 독립 타이머를 만들지 않고 상단 시간과 공유한다.
        diveTimeText.text = iM.GetDepth() < 0f ? "DIVE TIME: " + (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00") : "AT SURFACE"; // 수면에서는 현재 상태를 보여 준다.
    }
}
