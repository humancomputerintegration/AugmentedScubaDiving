// 원본 동작·상수·공개 API를 보존하고 설명만 추가했다. 실행 토큰 비교로 코드가 같음을 확인한다.
// 문법: = 대입, == 같은지 비교, . 멤버 접근, () 함수 인수, ; 문장 끝, { } 실행 범위, // 뒤는 실행하지 않는 설명이다.
using System.Collections; // using은 형식 이름을 짧게 쓰는 선언이다. 원본의 일반 컬렉션 참조를 보존하며 현재 이 파일에서는 사용하지 않는다.
using System.Collections.Generic; // 원본의 제네릭 컬렉션 참조를 보존한다. 이 파일에서는 현재 사용하지 않는다.
using UnityEngine; // MonoBehaviour, Transform, Mathf 등 Unity 형식을 짧은 이름으로 사용한다.
using UnityEngine.UI; // Slider, Text, Image 같은 기존 UGUI 화면 요소를 사용한다.

[RequireComponent(typeof(Slider))] // 대괄호는 특성 선언이며 이 컴포넌트가 붙는 오브젝트에 Slider가 필요하다고 Unity에 알린다.
public class OxygenBar : MonoBehaviour // 원래 산소 Slider 표시 클래스다. 최종 Scene의 새 HUD와 구분하여 보존한 학습용 원본이다.
{ // OxygenBar에서 이 실행 범위를 연다.

    private Slider slider; // private은 이 클래스 안에서만 접근한다. slider는 실제 막대 UI 컴포넌트 참조다.
    public InformationManager iM; // public 참조를 Inspector에서 연결한다. iM은 실제 산소·배터리 값을 계산하는 정보 관리자다.

    // Unity 생명주기 함수 또는 원본 동작을 아래 줄별 설명으로 읽는다.
    void Start() // Unity가 처음 활성화한 뒤 한 번 호출하는 준비 함수다. void는 반환값이 없다는 뜻이다.
    { // Start에서 이 실행 범위를 연다.
        slider = GetComponent<Slider>(); // 같은 오브젝트에서 Slider를 한 번 찾아 기억한다. <Slider>는 찾을 컴포넌트 형식이다.
        SetMaxOxygenLevel(iM.GetMaxOxygenLevel()); // 정보 관리자의 최대 산소량을 읽어 Slider의 최대값과 최초 표시값을 설정한다.
    } // Start에서 현재 실행 범위를 닫는다.

    public void SetMaxOxygenLevel (float maxLevel) // 최대 산소 실수를 인수로 받는 공개 함수다. 기존 이름과 형식을 그대로 유지한다.
    { // SetMaxOxygenLevel에서 이 실행 범위를 연다.
        slider.maxValue = maxLevel; // 점(.)으로 Slider의 최대값 칸에 접근하고 전달받은 실수를 대입한다.
        slider.value = maxLevel; // 원본 동작대로 처음에는 최대값을 표시한다. 실제 현재값은 다음 Update에서 반영한다.
    } // SetMaxOxygenLevel에서 현재 실행 범위를 닫는다.

    public void SetOxygenLevel (float level) // 현재 산소 실수를 인수로 받아 표시 막대를 바꾸는 공개 함수다.
    { // SetOxygenLevel에서 이 실행 범위를 연다.
        slider.value = level; // 현재 수치를 Slider 값에 대입하여 막대의 채워진 길이를 바꾼다.
    } // SetOxygenLevel에서 현재 실행 범위를 닫는다.

    private void Update() // 매 프레임 실행하되 다른 스크립트에서 직접 부를 필요가 없어 private으로 둔다.
    { // Update에서 이 실행 범위를 연다.
        SetOxygenLevel(iM.GetOxygenLevel()); // 매 프레임 실제 산소값을 정보 관리자에서 읽어 원본 표시 막대로 전달한다.
    } // Update에서 현재 실행 범위를 닫는다.
} // OxygenBar에서 현재 실행 범위를 닫는다.
