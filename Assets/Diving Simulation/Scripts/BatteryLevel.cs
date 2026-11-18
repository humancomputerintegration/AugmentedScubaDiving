// 원본 동작·상수·공개 API를 보존하고 설명만 추가했다. 실행 토큰 비교로 코드가 같음을 확인한다.
// 문법: = 대입, == 같은지 비교, . 멤버 접근, () 함수 인수, ; 문장 끝, { } 실행 범위, // 뒤는 실행하지 않는 설명이다.
using System.Collections; // using은 형식 이름을 짧게 쓰는 선언이다. 원본의 일반 컬렉션 참조를 보존하며 현재 이 파일에서는 사용하지 않는다.
using System.Collections.Generic; // 원본의 제네릭 컬렉션 참조를 보존한다. 이 파일에서는 현재 사용하지 않는다.
using UnityEngine; // MonoBehaviour, Transform, Mathf 등 Unity 형식을 짧은 이름으로 사용한다.
using UnityEngine.UI; // Slider, Text, Image 같은 기존 UGUI 화면 요소를 사용한다.
using TMPro; // 원본 TextMesh Pro 참조를 보존한다. 아래 화면 글자는 현재 UGUI Text를 사용한다.

[RequireComponent(typeof(Slider))] // 대괄호는 특성 선언이며 이 컴포넌트가 붙는 오브젝트에 Slider가 필요하다고 Unity에 알린다.
public class BatteryLevel : MonoBehaviour // 원래 배터리 Slider UI 클래스다. 최종 Scene에서는 새 HUD가 표시를 담당하여 이 원본 UI는 비활성이다.
{ // BatteryLevel에서 이 실행 범위를 연다.
    private Slider slider; // private은 이 클래스 안에서만 접근한다. slider는 실제 막대 UI 컴포넌트 참조다.
    public InformationManager iM; // public 참조를 Inspector에서 연결한다. iM은 실제 산소·배터리 값을 계산하는 정보 관리자다.

    public Text percentageText; // 반올림한 배터리 백분율을 표시하는 UGUI Text를 연결한다.
    public GameObject LowBatteryWarning; // 배터리가 기준 미만일 때 켤 원본 경고 오브젝트다.
    public AudioSource LowBatteryAlarm; // 낮은 배터리 상태에 진입할 때 재생할 경고음 소스다.
    public float lowBatteryValue = 15f; // 기본 경고 경계는 15퍼센트이며 실제 비교는 엄격한 미만(<)이다.
    public Color lowColor = Color.red; // 낮은 배터리 상태의 막대 색을 빨강으로 시작한다.

    private Image fillImage; // Slider 안에서 채워지는 부분의 Image 컴포넌트를 기억한다.
    private Color normalColor; // 경고가 풀렸을 때 복원할 원래 막대 색을 저장한다.
    private bool isLow; // 직전 프레임이 낮은 배터리였는지 기억한다. bool의 초기값은 false다.

    // Unity 생명주기 함수 또는 원본 동작을 아래 줄별 설명으로 읽는다.
    void Start() // Unity가 처음 활성화한 뒤 한 번 호출하는 준비 함수다. void는 반환값이 없다는 뜻이다.
    { // Start에서 이 실행 범위를 연다.
        slider = GetComponent<Slider>(); // 같은 오브젝트에서 Slider를 한 번 찾아 기억한다. <Slider>는 찾을 컴포넌트 형식이다.
        SetMaxBatteryLevel(iM.GetMaxBatteryLevel()); // 정보 관리자의 최대값을 읽어 원본 Slider를 초기화한다.
        fillImage = slider.fillRect.GetComponent<Image>(); // 채워진 영역의 RectTransform에서 실제 색을 가진 Image를 찾는다.
        normalColor = fillImage.color; // 현재 원래 색을 복사하여 회복 시 되돌릴 수 있게 한다.
        LowBatteryWarning.SetActive(false); // 시작할 때 원본 경고를 숨긴다.

        
    } // Start에서 현재 실행 범위를 닫는다.

    public void SetMaxBatteryLevel (float maxLevel){ // 공개 함수로 최대값 실수를 받아 Slider 범위를 설정하는 범위를 연다.
        slider.maxValue = maxLevel; // 점(.)으로 Slider의 최대값 칸에 접근하고 전달받은 실수를 대입한다.
        slider.value = maxLevel; // 원본 동작대로 처음에는 최대값을 표시한다. 실제 현재값은 다음 Update에서 반영한다.

    } // SetMaxBatteryLevel에서 현재 실행 범위를 닫는다.

    public void SetBatteryLevel (float level){ // 현재 배터리 실수를 받아 표시 막대에 전달하는 범위를 연다.
        slider.value = level; // 현재 수치를 Slider 값에 대입하여 막대의 채워진 길이를 바꾼다.
    } // SetBatteryLevel에서 현재 실행 범위를 닫는다.



    // Unity 생명주기 함수 또는 원본 동작을 아래 줄별 설명으로 읽는다.
    void Update() // Unity가 매 프레임 호출한다. 함수 호출 순서가 아래 계산·표시 순서를 결정한다.
    { // Update에서 이 실행 범위를 연다.
        SetBatteryLevel(iM.GetBatteryLevel()); // 매 프레임 실제 현재 배터리를 읽어 막대 표시를 바꾼다.
        float batteryPercentage = iM.GetBatteryLevel() / iM.GetMaxBatteryLevel() * 100f; // 현재량을 최대량으로 나누고 100을 곱해 백분율을 구한다. 원본은 분모 0 검사 없이 최대 100을 전제한다.
        percentageText.text =  Mathf.RoundToInt(batteryPercentage)+"%"; // 표시 글자만 가장 가까운 정수로 반올림해 %를 붙인다. 아래 경고 비교는 반올림 전 값으로 한다.
        bool nowLow = batteryPercentage < lowBatteryValue; // 실제 백분율이 경계보다 작을 때만 true다. 실제 14.9%는 화면에 15%로 보여도 경고 대상이다.

        if(nowLow && !isLow){ // &&는 두 조건이 모두 참이라는 뜻이다. 지금 낮고 직전에는 낮지 않았던 진입 순간만 처리한다.
            // 직전 상태와 비교하여 경고 진입 또는 회복 순간만 한 번 처리한다.
            fillImage.color = lowColor; // 낮은 배터리로 들어가면 막대 색을 경고색으로 바꾼다.
            LowBatteryWarning.SetActive(true); // 낮은 배터리 경고 오브젝트를 켠다.
            LowBatteryAlarm.Play(); // 낮은 상태에 진입하는 순간 한 번 재생을 요청한다. 반복 여부는 AudioSource의 loop 설정에 달려 있다.
        } // Update에서 현재 실행 범위를 닫는다.

        if(!nowLow && isLow){ // 지금은 낮지 않고 직전에는 낮았던 회복 순간만 처리한다.
            // 직전 상태와 비교하여 경고 진입 또는 회복 순간만 한 번 처리한다.
            fillImage.color = normalColor; // 회복하면 시작 때 저장했던 막대 색으로 되돌린다.
             LowBatteryWarning.SetActive(false); // 회복 분기에서는 다시 정상 상태가 되었으므로 원본 경고를 숨긴다.
            LowBatteryAlarm.Stop(); // 회복하면 원본 경고음 재생을 멈춘다.
        } // Update에서 현재 실행 범위를 닫는다.

        isLow = nowLow; // 이번 결과를 다음 프레임의 직전 상태로 저장하여 같은 진입 동작을 매 프레임 반복하지 않는다.
    } // Update에서 현재 실행 범위를 닫는다.
} // BatteryLevel에서 현재 실행 범위를 닫는다.
