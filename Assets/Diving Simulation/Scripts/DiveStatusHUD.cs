using UnityEngine; // 시간과 색깔 계산 기능을 사용한다.
using UnityEngine.UI; // 화면의 막대와 글자를 갱신한다.

public class DiveStatusHUD : MonoBehaviour // 항상 보이는 산소·배터리·수심·잠수 시간을 한 곳에서 표시한다.
{
    public InformationManager information; // 산소, 배터리, 머리 높이의 원본 값은 이 관리자에게서 읽는다.
    public Image oxygenFill; // 파란 산소 막대의 채워진 부분이다.
    public Image batteryFill; // 연두 또는 주황 배터리 막대의 채워진 부분이다.
    public Text oxygenPercent; // 산소 잔량 숫자다.
    public Text batteryPercent; // 배터리 잔량 숫자다.
    public Text depthText; // 배터리 오른쪽에 표시하는 실제 머리 수심이다.
    public Text diveTimeText; // 물속에서만 표시하는 잠수 시간이다.
    public Text lowBatteryWarning; // 낮은 배터리를 글자로도 알려 준다.
    public AudioSource lowBatteryAlarm; // 경고 기준을 넘을 때 한 번 재생하는 소리다.
    public Color oxygenColor = new Color(0.1f, 0.62f, 1f, 1f); // O2의 파란색이다.
    public Color batteryNormal = new Color(0.62f, 0.95f, 0.22f, 1f); // 정상 배터리는 연두색이다.
    public Color batteryLow = new Color(1f, 0.51f, 0.12f, 1f); // 배터리가 15% 이하이면 주황색이다.
    public float lowBatteryPercent = 15f; // 정확히 15%인 경우도 낮은 배터리에 포함한다.
    public float DiveSeconds { get; private set; } // 다른 정보 화면에서도 같은 잠수 시간을 읽는다.
    private bool wasUnderwater; // 방금 잠수를 시작했는지 확인하기 위한 이전 상태다.
    private bool wasLow; // 낮은 배터리 상태로 들어가는 순간을 찾는다.

    private void Update() // 화면을 그릴 때마다 실제 정보에 맞춰 표시한다.
    {
        if (information == null) return; // 정보 관리자가 없으면 잘못된 숫자를 표시하지 않는다.
        float oxygen = Mathf.Clamp01(information.GetOxygenLevel() / Mathf.Max(1f, information.GetMaxOxygenLevel())); // 산소를 0~1 비율로 바꾼다.
        float battery = Mathf.Clamp01(information.GetBatteryLevel() / Mathf.Max(1f, information.GetMaxBatteryLevel())); // 배터리도 0~1 비율로 바꾼다.
        bool low = battery * 100f <= lowBatteryPercent; // 15%와 그 이하를 정확히 같은 경고 구간으로 처리한다.
        if (oxygenFill != null) // 연결된 산소 막대가 있으면 갱신한다.
        {
            oxygenFill.fillAmount = oxygen; // 산소 잔량만큼만 채운다.
            oxygenFill.color = oxygenColor; // 산소 막대는 항상 파란색을 유지한다.
        }
        if (batteryFill != null) // 연결된 배터리 막대가 있으면 갱신한다.
        {
            batteryFill.fillAmount = battery; // 배터리 잔량에 맞춰 길이를 바꾼다.
            batteryFill.color = low ? batteryLow : batteryNormal; // 낮을 때만 주황색으로 바꾼다.
        }
        if (oxygenPercent != null) oxygenPercent.text = Mathf.CeilToInt(oxygen * 100f) + "%"; // 산소 숫자를 보여 준다.
        if (batteryPercent != null) batteryPercent.text = Mathf.CeilToInt(battery * 100f) + "%"; // 배터리 숫자를 보여 준다.
        float signedHeight = information.GetDepth(); // 원본 API는 수면 아래에서 음수가 된다.
        bool underwater = signedHeight < 0f; // 실제 머리가 수면 아래에 있을 때만 잠수 중이다.
        float depth = Mathf.Max(0f, -signedHeight); // 사용자에게 보여 줄 수심은 양수 미터로 바꾼다.
        if (depthText != null) depthText.text = "DEPTH " + depth.ToString("F1") + " m"; // 수면 위에서는 0.0m를 표시한다.
        if (underwater) // 물속에 있는 시간만 센다.
        {
            if (!wasUnderwater) DiveSeconds = 0f; // 새로 잠수를 시작하면 이번 잠수의 시간을 0으로 시작한다.
            DiveSeconds += Time.deltaTime; // 이번 프레임이 지난 시간을 더한다.
        }
        if (diveTimeText != null) // 잠수 시간 표시가 연결되어 있으면 갱신한다.
        {
            diveTimeText.gameObject.SetActive(underwater); // 수면 위에서는 DIVE TIME 글자 자체를 숨긴다.
            int seconds = Mathf.FloorToInt(DiveSeconds); // 소수 초를 버리고 읽기 쉬운 정수 초로 만든다.
            diveTimeText.text = "DIVE TIME: " + (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00"); // 분과 초를 두 자리로 표시한다.
        }
        if (lowBatteryWarning != null) lowBatteryWarning.gameObject.SetActive(low); // 색깔만으로 구분하기 어려운 사람에게도 경고한다.
        if (low && !wasLow && lowBatteryAlarm != null) lowBatteryAlarm.Play(); // 경계에 처음 들어온 순간에만 경고음을 낸다.
        if (!low && wasLow && lowBatteryAlarm != null) lowBatteryAlarm.Stop(); // 배터리가 복구되면 남아 있는 경고음을 멈춘다.
        wasLow = low; // 다음 프레임의 경계 비교에 쓰도록 저장한다.
        wasUnderwater = underwater; // 다음 프레임에 새 잠수인지 구분할 수 있게 한다.
    }
}
