using UnityEngine; // 시간과 위치 계산에 필요한 Unity 기능을 가져온다.
using UnityEngine.UI; // 기존 Button의 클릭 알림을 듣기 위해 사용한다.

public class SoftButtonFeedback : MonoBehaviour // 버튼의 기능을 건드리지 않고 눌림 모양만 잠깐 보여 준다.
{
    public Button button; // 이미 메뉴 열기나 전화 기능이 연결되어 있는 원래 버튼이다.
    public CircleGraphic face; // 살짝 통통한 버튼 얼굴을 그리는 담당자다.
    public RectTransform symbol; // 얼굴과 함께 조금 내려갈 글자나 아이콘이며, 없어도 된다.
    public float pressDuration = 0.07f; // 눌려 들어가는 데 걸리는 시간이다.
    public float releaseDuration = 0.14f; // 원래 모습으로 부드럽게 돌아오는 시간이다.
    private Vector2 restingPosition; // 아이콘이 평소에 놓여 있던 위치다.
    private float elapsed = -1f; // 음수이면 현재 눌림 애니메이션이 없다는 뜻이다.

    private void Awake() // 이 버튼이 처음 준비될 때 한 번 호출된다.
    {
        if (button == null) button = GetComponent<Button>(); // 직접 연결하지 않았으면 같은 물체의 버튼을 찾는다.
        if (face == null) face = GetComponent<CircleGraphic>(); // 같은 물체에서 원을 그리는 담당자를 찾는다.
        if (symbol != null) restingPosition = symbol.anchoredPosition; // 원래 아이콘 위치를 한 번 기억한다.
    }

    private void OnEnable() // 메뉴가 열려 버튼을 사용할 수 있게 되었을 때 호출된다.
    {
        if (button != null) button.onClick.AddListener(PlayPress); // 손 클릭과 마우스 클릭이 이미 모이는 알림을 함께 듣는다.
    }

    private void OnDisable() // 메뉴가 닫히면 눌린 모습이 남지 않도록 정리한다.
    {
        if (button != null) button.onClick.RemoveListener(PlayPress); // 다음에 열 때 같은 알림을 중복 등록하지 않는다.
        elapsed = -1f; // 진행 중이던 눌림 애니메이션을 멈춘다.
        SetAmount(0f); // 얼굴과 아이콘을 평소 모습으로 돌려놓는다.
    }

    private void PlayPress() // 원래 버튼 기능이 실행되었다는 알림만 받는다.
    {
        if (!isActiveAndEnabled) return; // 클릭하면서 이미 닫힌 메뉴에는 애니메이션을 시작하지 않는다.
        elapsed = 0f; // 지금부터 짧게 눌렸다 돌아오는 시간을 센다.
    }

    private void Update() // 눌림을 보여 주는 짧은 시간 동안만 그림을 바꾼다.
    {
        if (elapsed < 0f) return; // 눌리지 않은 버튼은 매 프레임 도형을 다시 만들지 않는다.
        elapsed += Time.unscaledDeltaTime; // 게임 시간을 멈춰도 UI의 눌림은 자연스럽게 끝낸다.
        float downTime = Mathf.Max(0.01f, pressDuration); // 0으로 나누지 않도록 최소 시간을 둔다.
        float upTime = Mathf.Max(0.01f, releaseDuration); // 돌아오는 시간도 같은 방식으로 보호한다.
        float amount = elapsed <= downTime ? elapsed / downTime : 1f - (elapsed - downTime) / upTime; // 먼저 눌리고 그다음 돌아오는 정도다.
        SetAmount(Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(amount))); // 움직임의 시작과 끝을 부드럽게 만든다.
        if (elapsed >= downTime + upTime) elapsed = -1f; // 한 번의 눌림이 끝나면 기다리는 상태로 돌아간다.
    }

    private void SetAmount(float amount) // 입력 영역을 움직이지 않고 눈에 보이는 부분만 조정한다.
    {
        if (face != null) face.SetPressAmount(amount); // 버튼 얼굴의 명암과 안쪽 그림만 바꾼다.
        if (symbol == null) return; // 도형에 직접 그린 전화 아이콘에는 별도 글자가 필요 없다.
        float radius = face != null ? Mathf.Min(face.rectTransform.rect.width, face.rectTransform.rect.height) * 0.5f : 0f; // 얼굴 크기에 맞는 아주 작은 이동량을 구한다.
        symbol.anchoredPosition = restingPosition + Vector2.down * radius * 0.018f * amount; // 버튼과 콜라이더를 그대로 두고 아이콘만 조금 눌러 준다.
    }
}
