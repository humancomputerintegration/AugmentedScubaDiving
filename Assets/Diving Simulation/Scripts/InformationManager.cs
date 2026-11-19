// 원본 동작·상수·공개 API를 보존하고 설명만 추가했다. 실행 토큰 비교로 코드가 같음을 확인한다.
// 문법: = 대입, == 같은지 비교, . 멤버 접근, () 함수 인수, ; 문장 끝, { } 실행 범위, // 뒤는 실행하지 않는 설명이다.
using System.Collections; // using은 형식 이름을 짧게 쓰는 선언이다. 원본의 일반 컬렉션 참조를 보존하며 현재 이 파일에서는 사용하지 않는다.
using System.Collections.Generic; // 원본의 제네릭 컬렉션 참조를 보존한다. 이 파일에서는 현재 사용하지 않는다.
using UnityEngine; // MonoBehaviour, Transform, Mathf 등 Unity 형식을 짧은 이름으로 사용한다.
using System.Linq; // 원본 LINQ 참조를 보존한다. 현재 아래 코드에서는 사용하지 않는다.
using System; // 원본 C# 기본 형식 참조를 보존한다. 현재 별도로 필요한 사용은 없다.
using UnityEngine.SceneManagement; // 원본 Scene 참조를 보존한다. 이 파일의 ResetPosition은 Scene을 다시 불러오지 않는다.

public class InformationManager : MonoBehaviour // class는 기능을 묶는 설계도, : MonoBehaviour는 Unity의 수명주기 함수를 받는 상속이다.
{ // InformationManager에서 이 실행 범위를 연다.
    [Header("User Information")] // Header는 Inspector에서 플레이어 참조 위에 제목을 붙이며 실행 계산을 바꾸지 않는다.
    public Transform player; // 수심을 잴 대상 Transform이다. 최종 Scene에서는 실제 Main Camera의 높이를 읽도록 연결한다.

    [Header("Water Information")] // Inspector에서 수면 관련 연결을 묶어 보이게 하는 제목이다.
    public Transform waterPlane; // 수면 높이의 기준 Transform이다. 그 position.y와 플레이어 y를 비교한다.

    public Transform cameraRig; // 위치 초기화 때 옮길 XR Origin 루트다. 눈 카메라의 로컬 자세가 아니라 플레이 공간 전체를 옮긴다.

    [Header("Audio")] // Inspector에서 수중 주변음 연결의 제목을 표시한다.
    public AudioSource underwaterAudio; // 물속에서 재생하고 수면 밖에서 멈출 주변음 소스다. 통화 음성 소스와 별개다.

    // 산소 정보: 최대량·현재량·초당 소비·보충률이다.
    private float oxygenLevelMax = 100f; // 산소 최대값을 100으로 둔다. float는 실수이고 f는 이 숫자가 float임을 표시한다.
    private float currentOxygenLevel = 100f; // 현재 산소를 처음 100으로 시작한다. 외부 UI는 getter로 이 값을 읽는다.
    public float oxygenConsumpsionRate = -1.00f; // 물속에서 초당 -1.00만큼 바뀌는 기본 산소 변화율이다. 음수이므로 더하면 감소한다.
    private float oxygenReplenishRate = 3.66f; // 수면 이상에서는 초당 3.66만큼 산소를 보충하는 원본 변화율이다.

    // 배터리 정보: 원본은 수심과 관계없이 소비하며 충전 필드는 아직 계산에 쓰지 않는다.
    private float batteryLevelMax = 100f; // 배터리 최대량을 100으로 둔다.
    private float currentBatteryLevel = 100f; // 현재 배터리는 처음 100이며 이후 Update가 계산한다.
    public float batteryConsumptionRate = -0.6f; // 기본 배터리 변화율은 초당 -0.6이다. Inspector 값이 이 기본값을 덮을 수 있다.
    public float batteryRechargeRate = 2.0f; // 원본 공개 필드를 보존한다. 아래 코드에서는 이 충전율을 사용하지 않아 수면에서 자동 충전하지 않는다.
    
    /*
     * 현재 정보를 읽는 공개 함수들
     */
    public float GetDepth() // 외부가 읽을 높이 차를 float로 반환한다. 이름은 Depth지만 원본 값은 물속에서 음수다.
    { // GetDepth에서 이 실행 범위를 연다.
        return player.position.y - waterPlane.position.y; // 플레이어 y에서 수면 y를 뺀다. 예를 들어 8-13=-5이며 양의 수심 표시는 UI가 부호를 바꿔야 한다.
    } // GetDepth에서 현재 실행 범위를 닫는다.

    public float GetBatteryLevel() // 현재 배터리를 읽는 공개 getter다. 원본 API의 반환 형식은 int가 아니라 float다.
    { // GetBatteryLevel에서 이 실행 범위를 연다.
        return currentBatteryLevel; // return은 현재 배터리 실수를 호출한 쪽으로 돌려준다. 값을 바꾸지는 않는다.
    } // GetBatteryLevel에서 현재 실행 범위를 닫는다.

    public float GetMaxBatteryLevel() // 배터리 최대량을 읽는 공개 함수다. 백분율 계산의 분모로 쓸 수 있다.
    { // GetMaxBatteryLevel에서 이 실행 범위를 연다.
        return batteryLevelMax; // 배터리 최대값을 그대로 돌려준다.
    } // GetMaxBatteryLevel에서 현재 실행 범위를 닫는다.

    public float GetOxygenLevel() // 현재 산소를 float로 읽는 공개 함수다.
    { // GetOxygenLevel에서 이 실행 범위를 연다.
        return currentOxygenLevel; // 산소 계산 결과를 읽어 호출한 UI에 돌려준다.
    } // GetOxygenLevel에서 현재 실행 범위를 닫는다.

    public float GetMaxOxygenLevel() // 산소 최대량을 float로 읽는 공개 함수다.
    { // GetMaxOxygenLevel에서 이 실행 범위를 연다.
        return oxygenLevelMax; // 최대 산소값을 호출한 쪽으로 돌려준다.
    } // GetMaxOxygenLevel에서 현재 실행 범위를 닫는다.

    // Unity 생명주기 함수 또는 원본 동작을 아래 줄별 설명으로 읽는다.
    void Start() // Unity가 처음 활성화한 뒤 한 번 호출하는 준비 함수다. void는 반환값이 없다는 뜻이다.
    { // Start에서 이 실행 범위를 연다.
    } // Start에서 현재 실행 범위를 닫는다.

    /*
     * 프레임마다 상태를 계산하는 함수들
     */
    
    private void BatteryUpdate () // 한 프레임 동안 배터리가 얼마나 변했는지 계산하는 내부 함수다.
    { // BatteryUpdate에서 이 실행 범위를 연다.
        currentBatteryLevel = Mathf.Clamp(currentBatteryLevel + batteryConsumptionRate * Time.deltaTime, 0f, batteryLevelMax); // 초당 변화율에 이번 프레임의 초를 곱해 더하고 Clamp로 0부터 최대량 사이에 제한한다. 수심 조건은 없다.

        if (currentBatteryLevel <= 0.1f) // if는 조건부 실행이고 <=는 이하 비교다. 배터리가 0.1 이하이면 다음 한 문장을 실행한다.
            GameOverBattery(); // 괄호는 함수 호출이다. 배터리 고갈 처리로 플레이 위치와 두 자원을 초기화한다.
    } // BatteryUpdate에서 현재 실행 범위를 닫는다.
    

    

    private void OxygenUpdate () // 수면 안팎에 따라 이번 프레임의 산소 변화를 계산한다.
    { // OxygenUpdate에서 이 실행 범위를 연다.
        float factor = 0f; // 이번 계산에 사용할 초당 변화율을 담을 지역 실수 변수다.

        if (GetDepth() < 0f) // GetDepth를 호출한 결과가 0보다 작으면 수면 아래다. 정확히 0이면 이 조건은 거짓이다.
        { // OxygenUpdate에서 이 실행 범위를 연다.
            factor = oxygenConsumpsionRate; // 물속에서는 음수 소비율을 선택한다.
        } else // 물속 분기를 닫고 그렇지 않은 경우, 즉 수면과 수면 위에서 실행할 분기로 넘어간다.
        { // OxygenUpdate에서 이 실행 범위를 연다.
            factor = oxygenReplenishRate; // 수면과 수면 위에서는 양수 보충률을 선택한다.
        } // OxygenUpdate에서 현재 실행 범위를 닫는다.
        float oxygenDifference =  factor * Time.deltaTime; // 초당 변화율에 이번 프레임의 경과 초를 곱하여 이번 프레임에만 더할 양을 구한다.

        currentOxygenLevel = Mathf.Clamp(currentOxygenLevel + oxygenDifference, 0f, oxygenLevelMax); // 현재 산소에 변화량을 더하고 0~최대량 범위로 제한한다.

        if (currentOxygenLevel <= 0.05f) // 산소가 0.05 이하로 내려갔는지 확인한다.
            GameOverOxygen(); // 산소 고갈 처리 함수를 호출한다.
    } // OxygenUpdate에서 현재 실행 범위를 닫는다.

    // Unity 생명주기 함수 또는 원본 동작을 아래 줄별 설명으로 읽는다.
    void Update() // Unity가 매 프레임 호출한다. 함수 호출 순서가 아래 계산·표시 순서를 결정한다.
    { // Update에서 이 실행 범위를 연다.
        OxygenUpdate(); // 매 프레임 산소 계산을 먼저 한다. 여기서 고갈되면 위치·자원이 즉시 초기화될 수 있다.
        BatteryUpdate(); // 그다음 배터리 계산을 한다. 산소 초기화와 같은 프레임에도 이 순서대로 이어진다.
        HandleUnderwaterAudio(); // 마지막으로 현재 높이를 읽어 주변음의 재생·정지를 맞춘다.
    } // Update에서 현재 실행 범위를 닫는다.

    void FixedUpdate() // Unity의 고정 시간 간격 콜백에서 전역 안개 여부를 맞춘다. 매 렌더 프레임과 동일 횟수라고 보장되지 않는다.
    { // FixedUpdate에서 이 실행 범위를 연다.
        if (GetDepth() < 0f) // GetDepth를 호출한 결과가 0보다 작으면 수면 아래다. 정확히 0이면 이 조건은 거짓이다.
        { // FixedUpdate에서 이 실행 범위를 연다.
            RenderSettings.fog = true; // 물속이면 전역 안개를 켠다. 이 줄은 mud 오브젝트를 끄는 기능이 아니다.
        } // FixedUpdate에서 현재 실행 범위를 닫는다.
        else // 수면 아래가 아닐 때 실행할 반대 분기다.
        { // FixedUpdate에서 이 실행 범위를 연다.
            RenderSettings.fog = false; // 수면 이상이면 전역 안개를 끈다.
        } // FixedUpdate에서 현재 실행 범위를 닫는다.
    } // FixedUpdate에서 현재 실행 범위를 닫는다.

    void HandleUnderwaterAudio() // 플레이어가 물속에 있는지에 따라 주변음 소스만 관리한다.
    { // HandleUnderwaterAudio에서 이 실행 범위를 연다.
        if (underwaterAudio == null) return; // AudioSource 참조가 없으면 이 함수만 즉시 끝내어 null 오류를 막는다.

        if (GetDepth() < 0f) // GetDepth를 호출한 결과가 0보다 작으면 수면 아래다. 정확히 0이면 이 조건은 거짓이다.
        { // HandleUnderwaterAudio에서 이 실행 범위를 연다.
            if (!underwaterAudio.isPlaying) // !는 논리 부정이다. 소리가 아직 재생 중이 아닐 때만 아래 Play를 실행한다.
                underwaterAudio.Play(); // 주변음을 재생한다. 매 프레임 처음으로 되돌리지 않도록 위의 isPlaying 검사 안에 있다.
        } // HandleUnderwaterAudio에서 현재 실행 범위를 닫는다.
        else // 수면 아래가 아닐 때 실행할 반대 분기다.
        { // HandleUnderwaterAudio에서 이 실행 범위를 연다.
            if (underwaterAudio.isPlaying) // 현재 주변음이 재생 중일 때만 아래 Stop을 실행한다.
                underwaterAudio.Stop(); // 수면 이상에서는 주변음 재생을 멈춘다.
        } // HandleUnderwaterAudio에서 현재 실행 범위를 닫는다.
    } // HandleUnderwaterAudio에서 현재 실행 범위를 닫는다.

    void GameOverOxygen() // 산소 고갈 사건의 로그와 공통 초기화 호출을 담당한다.
    { // GameOverOxygen에서 이 실행 범위를 연다.
        print("dead -- oxygen"); // Console에 산소 고갈 이유를 남긴다. 화면 UI 문구나 추가 데미지 계산은 아니다.
        ResetPosition(); // 공통 초기화 함수로 위치·회전·배터리·산소를 되돌린다.

    } // GameOverOxygen에서 현재 실행 범위를 닫는다.

    void GameOverBattery() // 배터리 고갈 사건의 로그와 공통 초기화 호출을 담당한다.
    { // GameOverBattery에서 이 실행 범위를 연다.
        print("dead -- battery"); // Console에 배터리 고갈 이유를 남긴다.
        ResetPosition(); // 공통 초기화 함수로 위치·회전·배터리·산소를 되돌린다.

    } // GameOverBattery에서 현재 실행 범위를 닫는다.

    void ResetPosition() // Scene 재로딩 없이 XR 루트와 자원 값만 원본 시작 상태로 대입한다.
    { // ResetPosition에서 이 실행 범위를 연다.
            cameraRig.position = new Vector3(364.1f,21.42f,493.3f); // new Vector3는 x,y,z 세 실수를 묶는다. XR Origin의 월드 위치를 원본 고정 좌표로 옮긴다.
            cameraRig.rotation = Quaternion.identity; // 루트 회전을 회전 없음인 identity로 되돌린다. 사용자의 실제 머리 로컬 자세를 강제로 0으로 만들지는 않는다.
            currentBatteryLevel = 100f; // 배터리 현재량을 원본 값 100으로 되돌린다.
            currentOxygenLevel = 100f; // 산소 현재량도 원본 값 100으로 되돌린다.

    } // ResetPosition에서 현재 실행 범위를 닫는다.

} // InformationManager에서 현재 실행 범위를 닫는다.
