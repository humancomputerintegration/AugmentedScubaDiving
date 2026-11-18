using System.Collections; // IEnumerator로 대기 가능한 코루틴을 작성한다.
using UnityEngine; // AudioClip, MonoBehaviour, WaitForSeconds를 사용한다.

// 동료마다 통화 음성 두 개와 자기 주파수를 가진다. 반복 코루틴은 하나만 유지하여 수신 시도가 증식하지 않게 한다.
public class CrewmateBehavior : MonoBehaviour // 이동 동료도 상속할 수 있는 기본 통화 행동이다.
{ // 동료 통화 상태와 행동을 정의한다.
    public AudioClip callAudioReceiver; // 플레이어가 이 동료에게 발신했을 때 재생할 응답 녹음이다.
    public AudioClip callAudioCaller; // 이 동료의 수신 전화를 플레이어가 받았을 때 재생할 녹음이다.
    public int seed; // 동료의 대기 시간 난수열을 정하는 시작 정수다.
    public CallTowerManager ctm; // 같은 주소록과 플레이어 통화기를 가리킨다.
    private Coroutine automaticCallRoutine; // 이 동료가 만든 자동 전화 코루틴 하나의 손잡이를 보관한다.
    private int frequency; // CallTowerManager가 배정한 무전 주소다.
    private System.Random random; // 동료별 난수기라서 다른 시스템의 전역 난수를 재설정하지 않는다.
    private float min_wait; // 안전하게 복사한 최소 대기 초다.
    private float max_wait; // 안전하게 복사한 최대 대기 초다.

    protected virtual void Start() // protected는 자식에게 공개, virtual은 자식이 override 후 base.Start()를 호출할 수 있다는 뜻이다.
    { // 원래 동료와 이동 동료가 같은 통화 초기화를 공유한다.
        if (ctm == null || ctm.playerTransmitter == null || callAudioCaller == null) // 필수 참조 중 빠진 것이 있는지 검사한다.
        { // 자동 수신에 필요한 항목이 없으면 원인을 알린다.
            Debug.LogWarning("Crewmate: assign tower, player transmitter and caller audio.", this); // 문제 동료를 Inspector에서 찾을 수 있다.
            return; // 자동 수신만 시작하지 않으며 자식 이동 자체를 임의로 끄지 않는다.
        } // 필수 참조 검사를 끝낸다.
        if (ctm.AutomaticIncomingStopped) return; // 같은 앱에서 이미 두 번 울렸다면 Scene이 다시 열려도 자동 전화를 시작하지 않는다.
        min_wait = Mathf.Max(0.1f, Mathf.Min(ctm.min_wait, ctm.max_wait)); // 설정이 뒤집혔어도 0보다 큰 작은 값을 고른다.
        max_wait = Mathf.Max(min_wait, Mathf.Max(ctm.min_wait, ctm.max_wait)); // 큰 값이 최소 대기보다 작지 않게 한다.
        random = new System.Random(seed ^ frequency); // ^는 비트 XOR이며 seed와 주파수를 섞어 같은 시각에 시도가 몰리는 것을 줄인다.
        automaticCallRoutine = StartCoroutine(RandomCall()); // 처음에 딱 한 번 시작하고 나중에 중지할 수 있게 손잡이를 기억한다.
    } // 동료 통화 초기화를 끝낸다.

    public void SetFrequency(int f) // 기존 API를 보존하여 주소록이 주파수를 전달할 수 있게 한다.
    { // 전달받은 값을 자기 상태로 기록한다.
        frequency = f; // =는 오른쪽 정수를 왼쪽 주파수 칸에 넣는다.
    } // 주파수 저장을 끝낸다.

    private float NextDelay() // 다음 대기 시간을 초 단위 실수로 계산한다.
    { // NextDouble은 0 이상 1 미만이므로 최소에서 최대 사이 값을 만든다.
        return min_wait + (float)random.NextDouble() * (max_wait - min_wait); // (float)는 double 값을 Unity가 쓰는 float로 바꾼다.
    } // 다음 대기 시간 계산을 끝낸다.

    public void StopAutomaticCalls() // 전체 두 번 제한에 도달하면 주소록이 이 함수를 호출한다.
    { // 동료 물체를 끄지 않고 자동 전화 반복만 멈춘다.
        if (automaticCallRoutine == null) return; // 이미 끝났거나 시작하지 않은 경우는 할 일이 없다.
        StopCoroutine(automaticCallRoutine); // 저장한 코루틴 하나만 중단한다.
        automaticCallRoutine = null; // 중단한 손잡이를 다시 사용하지 않도록 비운다.
    } // 자동 전화 중지를 끝낸다.

    private void OnDisable() // Scene이 닫히거나 이 동료가 꺼질 때 호출된다.
    { // 이전 Scene의 대기 중 전화가 남지 않게 한다.
        StopAutomaticCalls(); // 자동 전화만 정리하며 세션 횟수는 주소록에 그대로 둔다.
    } // 동료 비활성화 정리를 끝낸다.

    private IEnumerator RandomCall() // 코루틴 하나가 자동 수신 일정 전체를 담당한다.
    { // 전체 수신 두 번과 종료 뒤 60초 간격은 주소록 한 곳에서 결정한다.
        while (!ctm.IsReady) yield return null; // 주소록이 준비되지 않았다면 다음 프레임까지 기다린다.
        yield return new WaitForSeconds(NextDelay()); // 첫 시도 전에는 기존의 무작위 초기 대기를 유지한다.
        while (!ctm.AutomaticIncomingStopped) // 모든 동료가 공유하는 전체 제한에 도달할 때까지만 반복한다.
        { // 다음 수신 시도를 처리한다.
            bool accepted = ctm.CallPlayer(callAudioCaller, frequency); // 통화 중이거나 60초가 지나지 않았으면 횟수를 쓰지 않고 false를 받는다.
            if (accepted) Debug.Log(name + " automatic call " + ctm.AutomaticIncomingCount + " accepted.", this); // 실제 수신 두 번만 기록하고 보류 시도는 반복 출력하지 않는다.
            if (ctm.AutomaticIncomingStopped) yield break; // 자기 시도가 두 번째였다면 다음 대기를 예약하지 않고 즉시 끝낸다.
            yield return new WaitForSeconds(NextDelay()); // 기존 시도 사이 첫 번째 무작위 대기를 유지한다.
            if (ctm.AutomaticIncomingStopped) yield break; // 기다리는 사이 다른 동료가 두 번째 전화를 걸었어도 끝낸다.
            yield return new WaitForSeconds(NextDelay()); // 기존 시도 사이 두 번째 무작위 대기를 유지한다.
        } // 전체 횟수가 남아 있을 때만 다음 시도로 돌아간다.
        automaticCallRoutine = null; // 정상 종료된 코루틴의 손잡이를 비운다.
    } // 동료 자동 수신 코루틴 정의를 끝낸다.
} // CrewmateBehavior 정의를 끝낸다.
