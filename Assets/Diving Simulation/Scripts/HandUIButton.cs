using UnityEngine; // 위치, 시간, GameObject처럼 Unity가 제공하는 기본 기능을 가져온다.
using UnityEngine.UI; // 화면에 그리는 Button과 Image를 사용한다.

public class HandUIButton : MonoBehaviour // 한 버튼이 언제, 어느 손에 눌릴 수 있는지 담당한다.
{
    public enum AllowedHand { Either, Left, Right } // 양손 허용, 왼손만 허용, 오른손만 허용을 구분한다.
    public AllowedHand allowedHand = AllowedHand.Either; // 일반 화면 버튼은 어느 손으로도 누를 수 있다.
    public Button button; // Inspector에 연결한 원래 Unity 버튼의 onClick을 실행한다.
    public Image highlight; // 버튼 위에 얹은 밝은 테두리로 현재 가리키는 대상을 알려 준다.
    public float pressCooldown = 0.25f; // 두 손이 같은 순간 닿아도 이 시간 안에는 한 번만 실행한다.
    private float nextPressTime; // 다음 클릭을 허용하는 시각을 저장한다.

    public bool CanUse(HandSide side) // 전달받은 손이 이 버튼을 사용할 수 있는지 계산한다.
    {
        if (!isActiveAndEnabled || button == null || !button.IsInteractable()) return false; // 숨긴 버튼이나 비활성 버튼은 누르지 않는다.
        if (allowedHand == AllowedHand.Left && side != HandSide.Left) return false; // 오른손목 버튼은 왼손으로만 누른다.
        if (allowedHand == AllowedHand.Right && side != HandSide.Right) return false; // 왼팔 버튼은 오른손으로만 누른다.
        return true; // 위 조건을 모두 통과했으므로 사용할 수 있다.
    }

    public bool Press() // 모든 입력을 이 한 곳에 모아 실제 기능이 중복 실행되지 않게 한다.
    {
        if (!isActiveAndEnabled || button == null || !button.IsInteractable()) return false; // 실행 직전에도 버튼 상태를 다시 확인한다.
        if (Time.unscaledTime < nextPressTime) return false; // 잠깐의 접촉 떨림으로 들어온 추가 클릭을 버린다.
        nextPressTime = Time.unscaledTime + pressCooldown; // 기능을 실행하기 전에 다음 허용 시각을 기록한다.
        button.onClick.Invoke(); // Inspector에 연결된 통화, 메뉴 열기 등의 기능을 한 번 호출한다.
        return true; // 이번 입력이 실제로 실행되었다고 알려 준다.
    }

    public void SetHovered(bool value) // 입력 담당자가 현재 이 버튼을 가리키는지 알려 주는 함수다.
    {
        if (highlight != null) highlight.enabled = value; // 테두리만 바꾸므로 연락처의 선택 색깔은 유지된다.
    }

    private void OnDisable() // 메뉴를 닫아서 버튼이 꺼질 때 Unity가 부르는 함수다.
    {
        SetHovered(false); // 다시 열 때 이전 선택 표시가 남지 않게 한다.
    }
}
