using System; // Array를 사용하여 같은 값이 든 배열 위치를 찾는다.
using System.Collections; // IEnumerator는 기다렸다가 이어 실행하는 코루틴의 반환 형식이다.
using UnityEngine; // Transform, AudioClip 등 Unity 형식을 사용한다.
using UnityEngine.SceneManagement; // SceneManager로 SOS 뒤 현재 Scene을 다시 불러온다.

// 같은 index는 같은 사람이다: crewmates[i], crewmateFrequencies[i], phoneNumbers[i].
// 전화 주소와 주파수를 서로 계산하지 않고 같은 동료 번호 i를 통해 연결한다.
public class CallTowerManager : MonoBehaviour // MonoBehaviour를 상속하면 Unity 오브젝트에 붙일 수 있다.
{ // 동료 주소록과 통화 연결 담당 클래스다.
    public Transform[] crewmates; // []는 배열이다. Pedro, Romain, Suzie, Eliza, Elina 순서로 연결한다.
    public int[] crewmateFrequencies; // 시작 시 만든 주파수 배열이며 Inspector의 이전 값은 다시 계산된다.
    public bool useFixedFrequencies = false; // false면 실행마다 중복 없는 무전 채널을 뽑는다.
    public int[] fixedFrequencies = { 111, 222, 333, 444, 555 }; // 고정 모드에서만 사용하는 채널 예시다.
    public string[] phoneNumbers = { "2001", "2002", "2003", "2004", "2005" }; // 실제 전화번호가 아닌 게임 내선이다.
    public AudioClip wrongFrequency; // 존재하지 않는 주소에 대한 안내 소리다.
    public AudioClip emergencyAudio; // SOS 접수 시 한 번 재생할 안내 녹음이다.
    public float min_wait = 10f; // 자동 수신 시도의 최소 대기 초다. f는 float 실수 표시다.
    public float max_wait = 120f; // 자동 수신 시도의 최대 대기 초다.
    public int maxAutomaticIncomingCalls = 2; // 앱을 켠 한 세션에서 모든 동료를 합쳐 받을 자동 전화의 최대 횟수다.
    public float minimumIncomingGap = 60f; // 이전 자동 수신이 끝난 뒤 다음 자동 수신까지 최소 60초를 기다린다.
    public SimpleDial playerTransmitter; // 플레이어의 실제 수신·재생 상태를 관리하는 컴포넌트다.
    private const int transmitterFrequencyRangeMin = 100; // const는 실행 중 바꾸지 않는 값이다.
    private const int transmitterFrequencyRangeMax = 1000; // 정수 Random.Range의 상한은 포함하지 않으므로 마지막 값은 999다.
    private const int emergencyFrequency = 1001; // 동료 주파수와 겹치지 않는 예약 SOS 주소다.
    private static int automaticIncomingCount; // static 값은 Scene을 다시 불러도 앱 세션 동안 공유되므로 동료마다 2번씩 세지 않는다.
    private static float nextAutomaticIncomingTime; // 다음 자동 전화가 허용되는 앱의 실제 경과 시각이다.
    private static bool automaticIncomingInProgress; // 어느 동료의 자동 수신이 진행 중인지 전역으로 기억한다.
    private bool ownsAutomaticIncoming; // 현재 Scene의 이 주소록이 진행 중인 자동 수신을 시작했는지 기억한다.
    private bool emergencyStarted; // 여러 번 눌러도 Scene 재시작 예약은 한 번만 만든다.
    public bool IsReady { get; private set; } // 외부는 읽기만 하고 이 클래스만 초기화 완료 여부를 바꾼다.

    public int AutomaticIncomingCount => automaticIncomingCount; // 지금 앱 세션에서 실제로 표시한 자동 수신 횟수를 읽는다.
    public bool AutomaticIncomingStopped => automaticIncomingCount >= Mathf.Max(0, maxAutomaticIncomingCalls); // 제한 횟수에 도달하면 모든 동료가 같은 중지 상태를 읽는다.

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // 앱 시작이나 새 Editor Play 세션에서만 호출하며 Scene 재시작에서는 호출하지 않는다.
    private static void ResetAutomaticIncomingSession() // Domain Reload를 꺼 놓은 Editor에서도 이전 Play 횟수가 남지 않게 한다.
    { // 새 앱 실행의 자동 수신 기록을 초기화한다.
        automaticIncomingCount = 0; // 실제 수신 횟수는 0번부터 센다.
        nextAutomaticIncomingTime = 0f; // 첫 전화는 동료의 기존 초기 대기 시간이 끝나면 받을 수 있다.
        automaticIncomingInProgress = false; // 새 세션에는 진행 중인 수신이 없다.
    } // 앱 세션 수신 초기화를 끝낸다.

    private void Awake() // 일반 Start보다 먼저 주소록을 준비하여 Start 실행 순서에 의존하지 않는다.
    { // 모든 필수 항목을 확인한 뒤 IsReady를 true로 만든다.
        if (crewmates == null || crewmates.Length == 0 || crewmates.Length > 900) // ||는 조건 중 하나만 참이어도 참이다.
        { // 서로 다른 3자리 주파수 900개 안에서만 동료를 등록할 수 있다.
            Debug.LogError("CallTower: assign 1 to 900 crewmates.", this); // this를 붙이면 Inspector의 문제 컴포넌트로 찾아갈 수 있다.
            return; // 잘못된 주소록의 초기화를 여기서 중단한다.
        } // 동료 개수 검사 끝이다.
        foreach (Transform mate in crewmates) // 배열의 동료를 하나씩 검사한다.
        { // 모든 동료에 위치와 통화 컴포넌트가 있어야 한다.
            if (mate == null || mate.GetComponent<CrewmateBehavior>() == null) // 파생 MovingCrewmate도 기본 형식으로 찾을 수 있다.
            { // 빈 참조는 나중의 오류 대신 초기화 시점에 알린다.
                Debug.LogError("CallTower: every crewmate needs CrewmateBehavior.", this); // 누락된 참조를 고치라는 메시지다.
                return; // 부분적으로만 연결된 주소록을 사용하지 않는다.
            } // 현재 동료 검사가 끝났다.
        } // 전체 동료 검사가 끝났다.
        PreparePhoneNumbers(); // 내선의 길이·중복·예약 번호를 검사한다.
        bool fixedValid = useFixedFrequencies && FixedFrequenciesAreValid(); // &&는 두 조건이 모두 참이어야 참이다.
        if (useFixedFrequencies && !fixedValid) Debug.LogWarning("CallTower: invalid fixed frequencies; using random channels.", this); // !는 참과 거짓을 뒤집는다.
        crewmateFrequencies = new int[crewmates.Length]; // 동료 수와 정확히 같은 길이의 정수 배열을 만든다.
        for (int i = 0; i < crewmates.Length; i++) // 0번부터 마지막 동료까지 i를 1씩 늘린다.
        { // 현재 동료에게 주파수 하나를 배정한다.
            int frequency = fixedValid ? fixedFrequencies[i] : UnityEngine.Random.Range(transmitterFrequencyRangeMin, transmitterFrequencyRangeMax); // ? : 는 조건에 따라 값 하나를 고른다.
            while (Array.IndexOf(crewmateFrequencies, frequency, 0, i) >= 0) // 이미 배정한 앞쪽 i개에 같은 값이 있으면 다시 뽑는다.
                frequency = UnityEngine.Random.Range(transmitterFrequencyRangeMin, transmitterFrequencyRangeMax); // 중복을 없애는 동안에만 반복한다.
            crewmateFrequencies[i] = frequency; // 주소록의 i번째 채널 칸에 기록한다.
            crewmates[i].GetComponent<CrewmateBehavior>().SetFrequency(frequency); // 동료에게도 같은 값을 전달하여 발신자 주소를 일치시킨다.
        } // 모든 동료의 채널 배정이 끝났다.
        IsReady = true; // UI와 동료가 이제 완전한 주소록을 읽을 수 있다.
    } // Awake 초기화가 끝났다.

    private bool FixedFrequenciesAreValid() // 잘못된 고정 채널을 유효한 설정으로 사용하지 않는 검사다.
    { // 길이, 범위, 중복을 확인한다.
        if (fixedFrequencies == null || fixedFrequencies.Length != crewmates.Length) return false; // !=는 서로 다르다는 비교다.
        for (int i = 0; i < fixedFrequencies.Length; i++) // 고정 채널을 하나씩 읽는다.
        { // 현재 값이 범위 밖인지 또는 이미 사용됐는지 검사한다.
            int value = fixedFrequencies[i]; // 배열의 현재 값을 짧은 지역 변수로 읽는다.
            if (value < 100 || value >= 1000 || Array.IndexOf(fixedFrequencies, value, 0, i) >= 0) return false; // SOS 주소와 중복 채널을 막는다.
        } // 모든 고정 채널을 확인했다.
        return true; // 모든 검사를 통과했음을 호출한 곳으로 돌려준다.
    } // 고정 채널 검사를 끝낸다.

    private void PreparePhoneNumbers() // 전화 내선은 숫자처럼 보여도 주소 문자열로 다룬다.
    { // 설정이 잘못됐으면 전체 내선을 동료 순서에 맞춰 다시 만든다.
        bool valid = phoneNumbers != null && phoneNumbers.Length == crewmates.Length; // 같은 사람을 가리키려면 배열 길이가 같아야 한다.
        if (valid) // 배열이 있을 때만 원소를 읽는다.
        { // 빈 내선, 비숫자, 중복 내선, SOS 예약 주소를 막는다.
            for (int i = 0; i < phoneNumbers.Length; i++) // 내선 문자열을 하나씩 방문한다.
            { // 이번 주소의 유효성을 검사한다.
                string phone = phoneNumbers[i]; // 주소 문자열을 지역 변수로 읽는다.
                if (string.IsNullOrWhiteSpace(phone) || phone == "1001" || Array.IndexOf(phoneNumbers, phone, 0, i) >= 0) valid = false; // 예약 SOS는 동료 내선으로 사용할 수 없다.
                if (!string.IsNullOrEmpty(phone)) foreach (char digit in phone) if (digit < '0' || digit > '9') valid = false; // 각 문자가 숫자 키패드로 입력 가능한지 본다.
            } // 내선 배열 검사를 끝낸다.
        } // 배열이 있을 때의 검사를 끝낸다.
        if (valid) return; // 정상 설정이면 앞자리 0도 포함하여 원래 주소를 그대로 보존한다.
        phoneNumbers = new string[crewmates.Length]; // 동료 수만큼 새 문자열 칸을 만든다.
        for (int i = 0; i < phoneNumbers.Length; i++) phoneNumbers[i] = (2001 + i).ToString(); // 동료 순서대로 2001, 2002, ... 게임 내선을 배정한다.
    } // 내선 준비가 끝났다.

    public CrewInfo[] GetCrewmatesInformation() // UI는 동료를 직접 찾지 않고 이 API로 현재 정보를 받는다.
    { // 매 조회 시점의 위치를 새 정보 묶음에 복사한다.
        if (!IsReady) return Array.Empty<CrewInfo>(); // 초기화 전에는 빈 배열을 주어 범위 오류를 피한다.
        CrewInfo[] result = new CrewInfo[crewmates.Length]; // 동료 수만큼만 결과 배열을 만든다.
        for (int i = 0; i < crewmates.Length; i++) // 주소록 순서를 유지한다.
            result[i] = new CrewInfo(crewmates[i].name, crewmates[i].position, crewmateFrequencies[i], phoneNumbers[i]); // 움직이는 동료의 현재 위치를 함께 읽는다.
        return result; // 완성된 정보 배열을 호출한 UI에 돌려준다.
    } // 정보 조회를 끝낸다.

    public bool TryResolvePhone(string phone, out int frequency) // out은 함수가 결과 주파수를 호출자의 변수에 써 준다는 뜻이다.
    { // PHONE 주소를 같은 사람의 FREQ 주소로 바꾼다.
        frequency = 0; // 실패했을 때 남길 초기값이다.
        if (!IsReady || phone == null) return false; // 주소록 또는 입력이 없으면 연결하지 않는다.
        int index = Array.IndexOf(phoneNumbers, phone.Trim()); // Trim은 양끝 공백만 없애며 앞자리 0은 지우지 않는다.
        if (index < 0) return false; // -1은 해당 내선의 동료가 없다는 뜻이다.
        frequency = crewmateFrequencies[index]; // 같은 index에 있는 무전 주소를 꺼낸다.
        return true; // 실제 등록 동료를 찾았음을 알린다.
    } // 내선 주소 변환을 끝낸다.

    public bool IsKnownFrequency(int frequency) // 연결 성공과 잘못된 주소를 통화기가 구별하도록 돕는다.
    { // IsReady를 먼저 보므로 준비 전의 배열을 읽지 않는다.
        return IsReady && Array.IndexOf(crewmateFrequencies, frequency) >= 0; // 등록 채널일 때만 true다.
    } // 주파수 존재 검사를 끝낸다.

    public string GetCrewmateName(int frequency) // 큰 통화 피드백에 표시할 이름을 얻는다.
    { // 주파수로 주소록의 같은 index를 찾는다.
        int index = IsReady ? Array.IndexOf(crewmateFrequencies, frequency) : -1; // 준비 전이면 찾기 실패로 처리한다.
        return index >= 0 ? crewmates[index].name : "Unknown contact"; // 이름 또는 명확한 실패 표기를 돌려준다.
    } // 이름 조회를 끝낸다.

    public int GetFrequencyMax() { return transmitterFrequencyRangeMax; } // 기존 API 보존: 1000은 포함하지 않는 상한이다.
    public int GetFrequencyMin() { return transmitterFrequencyRangeMin; } // 기존 API 보존: 최소 채널은 100이다.
    public int GetEmergencyFrequency() { return emergencyFrequency; } // 모든 SOS 버튼은 같은 예약 값 1001을 얻는다.

    public AudioClip CallCrewmate(int frequency) // 요청 주소에 해당하는 재생 음성을 돌려준다.
    { // SOS를 먼저 검사하여 일반 채널과 혼동하지 않는다.
        if (frequency == emergencyFrequency) // ==는 두 값이 같은지 묻는 비교다.
        { // 안내가 있어야 종료 시점도 정확히 계산할 수 있다.
            if (emergencyAudio == null) return null; // 음성이 빠졌으면 가짜 구조 완료를 실행하지 않는다.
            if (!emergencyStarted) // 연속으로 눌러도 재시작 예약은 한 번이다.
            { // 먼저 접수를 기록하고 기다리는 동작을 시작한다.
                emergencyStarted = true; // 중복 SOS 시작을 막는다.
                StartCoroutine(Emergency()); // 안내 길이를 기다린 뒤 Scene을 다시 시작한다.
            } // 첫 SOS 접수 처리가 끝났다.
            return emergencyAudio; // 실제 구조 안내 음성을 통화기에 넘긴다.
        } // SOS 분기를 끝낸다.
        int index = IsReady ? Array.IndexOf(crewmateFrequencies, frequency) : -1; // 일반 동료의 주소를 찾는다.
        return index >= 0 ? crewmates[index].GetComponent<CrewmateBehavior>().callAudioReceiver : wrongFrequency; // 동료 응답 또는 잘못된 주소 안내를 고른다.
    } // 발신 음성 선택을 끝낸다.

    public bool CallPlayer(AudioClip call, int freq) // 기존 동료가 사용하는 플레이어 수신 진입점이다.
    { // 참조가 있으면 통화기의 실제 수락 결과를 그대로 전달한다.
        if (AutomaticIncomingStopped || automaticIncomingInProgress || Time.unscaledTime < nextAutomaticIncomingTime) return false; // 횟수·진행 중 통화·최소 간격 중 하나라도 막으면 시도만 보류한다.
        if (playerTransmitter == null || !playerTransmitter.IncomingCall(call, freq)) return false; // 통화 중이거나 잘못된 음원 등으로 거절되면 횟수를 쓰지 않는다.
        automaticIncomingCount++; // 실제로 벨과 수신 화면을 시작한 경우에만 전체 횟수를 1 늘린다.
        automaticIncomingInProgress = true; // 받기 또는 거절 뒤 종료될 때까지 다음 자동 전화를 막는다.
        ownsAutomaticIncoming = true; // 이 주소록이 해당 자동 수신의 끝을 기록하도록 한다.
        if (AutomaticIncomingStopped) StopAutomaticCallers(); // 두 번째 수신을 시작했으면 모든 동료의 자동 전화 코루틴을 끝낸다.
        return true; // 실제 수신을 시작했음을 발신 동료에게 알려 준다.
    } // 수신 전달을 끝낸다.

    public void NotifyAutomaticIncomingEnded() // 통화기가 벨 거절·목소리 종료·SOS 중단을 알릴 때 호출한다.
    { // 직접 건 전화는 이 주소록의 자동 수신 소유 표시가 없으므로 간격을 바꾸지 않는다.
        if (!ownsAutomaticIncoming) return; // 진행 중인 자동 수신을 시작한 주소록만 종료를 기록한다.
        ownsAutomaticIncoming = false; // 같은 종료를 여러 번 처리하지 않게 자기 표시를 지운다.
        automaticIncomingInProgress = false; // 이제 최소 대기 시간이 지나면 다음 자동 전화가 가능하다.
        nextAutomaticIncomingTime = Time.unscaledTime + Mathf.Max(0f, minimumIncomingGap); // 종료 시각부터 60초 뒤를 다음 허용 시각으로 기록한다.
    } // 자동 수신 종료 기록을 끝낸다.

    private void StopAutomaticCallers() // 어느 동료가 두 번째로 걸었는지와 관계없이 모든 자동 전화를 중지한다.
    { // 주소록의 각 동료에게 자신의 코루틴을 끝내도록 요청한다.
        if (crewmates == null) return; // 동료 배열이 없는 잘못된 연결에서는 반복하지 않는다.
        foreach (Transform mate in crewmates) // 다섯 동료를 차례로 방문한다.
        { // 통화 외의 수영이나 이동 컴포넌트는 끄지 않는다.
            if (mate == null) continue; // 비어 있는 칸은 건너뛴다.
            CrewmateBehavior caller = mate.GetComponent<CrewmateBehavior>(); // 자동 전화 담당 컴포넌트만 찾는다.
            if (caller != null) caller.StopAutomaticCalls(); // 해당 코루틴만 멈추며 녹음과 수동 발신은 보존한다.
        } // 모든 동료에게 중지를 전달했다.
    } // 자동 전화 일괄 중지를 끝낸다.

    private void OnDisable() // SOS 등으로 현재 Scene이 닫힐 때 진행 중이던 자동 수신의 간격을 정리한다.
    { // 세션 횟수는 그대로 남겨 Scene 재시작으로 자동 전화 두 번이 다시 생기지 않게 한다.
        NotifyAutomaticIncomingEnded(); // 진행 중인 자동 수신만 끝내고 다음 허용 시각을 기록한다.
        StopAutomaticCallers(); // 현재 Scene의 자동 전화 코루틴도 정리한다.
    } // 현재 Scene 통화 관리자 정리를 끝낸다.

    private IEnumerator Emergency() // IEnumerator를 반환하므로 yield로 중간에 기다릴 수 있다.
    { // 안내 뒤 0.5초 여유를 두고 초기 수면 시작 위치로 돌아간다.
        yield return new WaitForSecondsRealtime(emergencyAudio.length + 0.5f); // 오디오에 맞춰 게임 시간 배율과 무관한 실제 초를 기다린다.
        Scene current = SceneManager.GetActiveScene(); // 현재 켜진 Scene 정보를 얻는다.
        if (current.buildIndex >= 0) SceneManager.LoadScene(current.buildIndex); // Build 목록에 있으면 번호로 같은 Scene을 불러온다.
        else SceneManager.LoadScene(current.name); // 번호가 없으면 이름으로 요청한다. 실제 빌드에는 Scene 등록이 필요하다.
    } // 구조 후 재시작 코루틴을 끝낸다.
} // CallTowerManager 정의를 끝낸다.
