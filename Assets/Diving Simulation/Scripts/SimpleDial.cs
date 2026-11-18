using UnityEngine; // AudioSource, AudioClip, MonoBehaviour 같은 Unity 형식을 사용한다.

// 실제 통화 상태는 이 한 컴포넌트가 소유한다. UI는 State/StatusMessage를 읽고 버튼은 공개 메서드를 호출한다.
// ;는 한 문장을 끝내고, { }는 묶음의 범위, ()는 함수에 넘기는 인수, .은 객체의 멤버 접근이다.
[RequireComponent(typeof(AudioSource))] // 이 스크립트가 붙는 오브젝트에는 음성용 AudioSource가 반드시 필요하다.
public class SimpleDial : MonoBehaviour // MonoBehaviour를 상속하여 Unity의 Awake/Update를 사용한다.
{ // 플레이어 통화기의 정의를 연다.
    public enum CallState { Idle, Ringing, Connected, Ended, Invalid, Emergency, AudioTest } // enum은 상태 이름 중 하나만 선택하게 하는 자료형이다.
    public CallTowerManager ctm; // 주파수·내선·동료 음성을 아는 주소록을 Inspector에서 연결한다.
    public AudioClip ringtone; // 받기 전 반복할 벨소리다.
    public AudioSource voiceSource; // 목소리 전용 소스를 Inspector에서 정확히 연결하여 배터리 경고음과 혼동하지 않는다.
    public AudioClip audioTestClip; // SOUND 버튼은 실제 Elina 녹음을 같은 목소리 소스로 재생하여 들리는지 확인한다.
    public AudioSource feedbackSource; // 버튼·연결·종료 소리를 음성과 독립적으로 재생할 별도 AudioSource다.
    public AudioClip buttonCue; // 숫자·모드 버튼을 눌렀다는 짧은 클릭 소리다.
    public AudioClip connectedCue; // 실제 음성 재생을 시작할 때 한 번 내는 연결 확인 소리다.
    public AudioClip endedCue; // 통화가 끝날 때 한 번 내는 종료 확인 소리다.
    [Range(0f, 1f)] public float voiceVolume = 1f; // Range는 Inspector 범위를 0~1로 표시하며 1은 이 소스의 원래 최대 음량이다.
    [Range(0f, 1f)] public float ringVolume = 0.8f; // 벨소리를 적당히 크게 재생하는 소스 음량이다.
    [Range(0f, 1f)] public float feedbackVolume = 0.8f; // 짧은 피드백 소리의 소스 음량이다.
    private AudioSource aS; // 실제 목소리와 수신 벨을 번갈아 재생하는 주 음원이다.
    private AudioClip incomingCall; // 받기를 누르기 전까지 보관할 발신 동료의 녹음이다.
    private int incomingCallerFrequency; // 수신 팝업과 원래 API가 읽을 발신자 주파수다.
    private float voiceStartedAt; // 재생 직후 한 프레임을 종료로 오인하지 않도록 시작 시각을 기억한다.
    private bool waitingForClipData; // 비동기 로딩 시간을 실제 재생 시작 대기 시간과 따로 구분한다.
    private bool voiceHasPlayed; // 실제 재생을 관찰했는지 기억하여 로딩 실패를 정상 통화 종료로 표시하지 않는다.
    private float lastButtonSoundAt = -1f; // 같은 프레임에 겹친 클릭 소리만 줄이는 시각이다.
    public CallState State { get; private set; } = CallState.Idle; // 외부는 상태를 읽고 변경은 이 통화기 안에서만 한다.
    public string StatusMessage { get; private set; } = "READY TO CALL"; // 큰 피드백 UI가 그대로 읽는 실제 상태 문구다.
    public int StatusRevision { get; private set; } // 상태가 바뀔 때 증가하므로 팝업이 새 알림을 알아볼 수 있다.
    public int CurrentFrequency { get; private set; } // 지금 또는 방금 끝난 통화 상대의 무전 주소다.
    public bool EmergencyRequested { get; private set; } // SOS가 접수되면 Scene 재시작 전까지 해제하지 않는다.
    public bool IsBusy => State == CallState.Ringing || State == CallState.Connected || State == CallState.Emergency; // =>는 오른쪽 계산 결과를 읽기 속성으로 돌려준다.
    public bool IsAudioTest => State == CallState.AudioTest; // 소리 확인은 실제 전화가 아니므로 새 수신이나 발신을 막지 않는다.
    public bool IsIncomingCall => State == CallState.Ringing; // 수신 벨이 울리는 상태인지 읽는 속성이다.
    public bool IsAudioPlaying => aS != null && aS.isPlaying; // 실제 음성 재생 여부를 읽되 null 참조는 먼저 피한다.

    private void Awake() // UI 버튼을 누를 수 있기 전에 소스를 준비한다.
    { // 통화 소리와 짧은 확인 소리를 분리한다.
        if (voiceSource == null) voiceSource = gameObject.AddComponent<AudioSource>(); // 명시한 소스가 없으면 다른 경고음을 빌리지 않고 전용 소스를 만든다.
        aS = voiceSource; // 이후 모든 목소리와 벨은 이 명시적인 한 소스만 사용한다.
        ConfigureSource(aS, voiceVolume); // 거리·방향에 감쇠하지 않는 헤드셋용 2D 소스로 설정한다.
        if (feedbackSource == null || feedbackSource == aS) feedbackSource = gameObject.AddComponent<AudioSource>(); // 연결이 빠졌거나 같으면 독립 피드백 소스를 하나 만든다.
        ConfigureSource(feedbackSource, feedbackVolume); // 확인 소리도 2D로 만들되 음성과 별개로 재생한다.
    } // 음원 초기화를 끝낸다.

    private void Start() // 모든 Awake가 끝난 뒤 첫 통화에서 쓸 짧은 녹음을 미리 준비한다.
    { // 파일을 바꾸지 않고 음성 데이터를 메모리에 올리는 요청만 한다.
        PrepareClip(ringtone); // 첫 수신 벨을 준비한다.
        PrepareClip(buttonCue); // 첫 버튼 소리를 준비한다.
        PrepareClip(connectedCue); // 연결 확인음을 준비한다.
        PrepareClip(endedCue); // 종료 확인음을 준비한다.
        PrepareClip(audioTestClip); // 소리 확인용 실제 녹음도 준비한다.
        if (ctm == null) return; // 주소록이 없으면 주소록 안의 파일은 읽을 수 없다.
        PrepareClip(ctm.wrongFrequency); // 잘못된 주소 안내를 준비한다.
        PrepareClip(ctm.emergencyAudio); // 구조 안내를 준비한다.
        if (ctm.crewmates == null) return; // 동료 배열이 없으면 반복을 생략한다.
        foreach (Transform mate in ctm.crewmates) // 등록한 다섯 동료를 한 명씩 방문한다.
        { // 그 동료의 수신·발신 녹음을 함께 준비한다.
            if (mate == null) continue; // 비어 있는 동료 칸은 건너뛴다.
            CrewmateBehavior crew = mate.GetComponent<CrewmateBehavior>(); // 실제 동료가 가진 두 녹음을 찾는다.
            if (crew == null) continue; // 동료 통화 컴포넌트가 없으면 여기서 멈추지 않는다.
            PrepareClip(crew.callAudioCaller); // 동료가 플레이어에게 거는 목소리를 준비한다.
            PrepareClip(crew.callAudioReceiver); // 플레이어가 동료에게 걸 때 듣는 목소리를 준비한다.
        } // 전체 동료의 사전 준비가 끝났다.
    } // 첫 통화 준비가 끝났다.

    private void PrepareClip(AudioClip clip) // 같은 녹음이 여러 곳에 연결돼 있어도 한 번 로딩하면 다시 읽지 않는다.
    { // Unloaded일 때만 데이터를 요청한다.
        if (clip != null && clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData(); // 이미 로딩 중이거나 준비된 파일은 그대로 둔다.
    } // 음성 사전 준비 함수가 끝났다.

    private void ConfigureSource(AudioSource source, float volume) // 두 소스에 같은 명확한 2D 설정을 적용하는 작은 함수다.
    { // 위치 때문에 고글 통화 소리가 작아지지 않게 한다.
        source.enabled = true; // 전용 소스가 꺼져 있다면 켜서 실제 재생할 수 있게 한다.
        source.Stop(); // 이전 자동 재생이 있으면 정지한다.
        source.outputAudioMixerGroup = null; // 전화는 별도 음소거 믹서로 보내지 않고 기본 출력으로 보낸다.
        source.playOnAwake = false; // Scene 시작만으로 통화나 클릭이 재생되지 않게 한다.
        source.loop = false; // 기본은 한 번 재생이며 수신 벨에서만 true로 바꾼다.
        source.spatialBlend = 0f; // 0은 2D: 청취자와의 거리·방향으로 음량을 줄이지 않는다.
        source.spatialize = false; // 추가 3D 공간화 플러그인 처리를 사용하지 않는다.
        source.dopplerLevel = 0f; // 이동 속도에 따른 통화 음높이 변화를 막는다.
        source.panStereo = 0f; // 왼쪽 또는 오른쪽으로 치우치지 않게 중앙으로 둔다.
        source.pitch = 1f; // 원래 녹음 속도와 길이대로 재생한다.
        source.mute = false; // 소스 자체 음소거 때문에 상태만 연결되는 것을 막는다.
        source.bypassEffects = true; // 이 소스의 필터가 전화 명료도를 떨어뜨리지 않게 한다.
        source.bypassListenerEffects = true; // 전역 수중 필터가 통화 명료도를 낮추지 않게 한다.
        source.bypassReverbZones = true; // 환경 잔향이 통화에 더해지지 않게 한다.
        source.volume = Mathf.Clamp01(volume); // 코드로 설정해도 음량을 0~1로 제한한다.
        source.priority = 0; // 작은 수가 높은 우선순위여서 통화 음성이 먼저 잘리지 않게 한다.
    } // 공통 음원 설정을 끝낸다.

    public void QuickDial(int freq) // 기존 무전 발신 API를 그대로 유지한다.
    { // SOS는 일반 통화 중인지 검사하기 전에 우선 처리한다.
        if (ctm == null) { ReportAddressError("CALL TOWER NOT CONNECTED"); return; } // 필수 참조가 없으면 가짜 연결을 표시하지 않는다.
        if (freq == ctm.GetEmergencyFrequency()) // 1001은 어느 입력 모드에서 왔든 SOS다.
        { // 한 번 접수한 구조 요청은 일반 통화로 덮어쓰지 않는다.
            if (EmergencyRequested) return; // 반복 클릭이 구조 안내를 처음부터 다시 재생하지 않게 한다.
            if (ctm.emergencyAudio == null) { ReportAddressError("SOS AUDIO NOT CONNECTED"); return; } // 누락된 오디오로 구조 시간을 예약하지 않는다.
            StopMainAudio(); // 수신 벨이나 기존 목소리를 즉시 멈춰 SOS가 우선 들리게 한다.
            EmergencyRequested = true; // 새 수신과 일반 발신을 차단하는 접수 상태를 기록한다.
            CurrentFrequency = freq; // 큰 상태 UI가 구조 주소를 가리키게 한다.
            AudioClip rescue = ctm.CallCrewmate(freq); // 주소록이 구조 안내와 Scene 재시작을 한 번만 예약한다.
            PlayMainAudio(rescue, false, voiceVolume); // 구조 녹음은 반복 없이 한 번 재생한다.
            SetStatus(CallState.Emergency, "SOS 1001 ACCEPTED\nRESCUE IN PROGRESS\nRETURNING TO SURFACE"); // 소리가 들리지 않아도 구조 접수를 알 수 있게 한다.
            PlayFeedback(connectedCue); // 짧은 연결 소리를 별도 소스에서 한 번 재생한다.
            return; // 일반 동료 발신 부분으로 내려가지 않는다.
        } // SOS 우선 처리가 끝났다.
        if (IsBusy || EmergencyRequested) return; // 다른 통화나 수신을 일반 발신이 몰래 끊지 않게 한다.
        AudioClip clip = ctm.CallCrewmate(freq); // 주소록에 등록된 응답 녹음 또는 잘못된 주소 안내를 받는다.
        CurrentFrequency = freq; // 입력한 무전 주소를 기록한다.
        if (!ctm.IsKnownFrequency(freq)) // 잘못된 번호 안내를 실제 연결로 표시하지 않는다.
        { // 오류 음성과 시각 피드백을 함께 제공한다.
            PlayMainAudio(clip, false, voiceVolume); // 연결 실패 안내가 있으면 한 번 재생한다.
            SetStatus(CallState.Invalid, "NO CONTACT\nFREQ " + freq + "\nCHECK THE CREW LIST"); // +는 문자열을 이어 큰 메시지를 만든다.
            return; // 성공 상태로 넘어가지 않는다.
        } // 등록 주소 검사 끝이다.
        if (clip == null) { ReportAddressError("CONTACT AUDIO NOT CONNECTED"); return; } // 누락된 녹음을 연결 성공으로 속이지 않는다.
        PlayMainAudio(clip, false, voiceVolume); // 해당 동료의 발신 응답 음성을 한 번 재생한다.
        SetStatus(CallState.Connected, ContactMessage("CONNECTED")); // 실제 재생 요청과 동시에 큰 연결 상태를 표시한다.
        PlayFeedback(connectedCue); // 벨 대신 연결 확인 소리가 한 번 난다.
    } // 발신 처리를 끝낸다.

    public bool IncomingCall(AudioClip speaker, int freq) // true는 수신을 실제로 받아 벨이 울리게 했다는 뜻이다.
    { // 통화 중에는 기존 소리·상태를 보존한다.
        if (IsBusy || EmergencyRequested || speaker == null || ctm == null || !ctm.IsKnownFrequency(freq)) return false; // 받을 수 없는 조건이면 false다.
        incomingCall = speaker; // 아직 목소리를 틀지 않고 받아야 할 녹음을 보관한다.
        incomingCallerFrequency = freq; // 원래 팝업 API에도 발신자 주소를 전달한다.
        CurrentFrequency = freq; // 공통 피드백에도 같은 발신자를 표시한다.
        PlayMainAudio(ringtone, true, ringVolume); // 벨소리만 수신 대기 동안 반복한다.
        SetStatus(CallState.Ringing, ContactMessage("INCOMING CALL")); // 벨과 함께 큰 수신 상태를 표시한다.
        return true; // 수락 후 busy가 됐더라도 실제 수락 결과는 true다.
    } // 수신 요청 처리를 끝낸다.

    public void AnswerCall() // 기존 받기 버튼이 호출하는 API다.
    { // 수신 중일 때만 대기 녹음으로 전환한다.
        if (State != CallState.Ringing || incomingCall == null) return; // !=는 다르다는 비교이며 두 번 받기를 막는다.
        AudioClip voice = incomingCall; // 지우기 전에 재생할 목소리 참조를 지역 변수로 보관한다.
        incomingCall = null; // 같은 수신 녹음을 다시 받는 일을 막는다.
        PlayMainAudio(voice, false, voiceVolume); // 벨을 멈추고 loop=false인 실제 발신자 목소리를 재생한다.
        SetStatus(CallState.Connected, ContactMessage("CONNECTED")); // 팝업을 바로 숨기지 않고 받기 성공을 크게 보여 준다.
        PlayFeedback(connectedCue); // 수신을 받았다는 명확한 짧은 소리를 한 번 낸다.
    } // 받기 처리를 끝낸다.

    public void HangUp() // 기존 종료 버튼이 호출하는 API다.
    { // SOS 접수 뒤에는 종료 버튼으로 구조 예약을 취소하지 않는다.
        if (IsAudioTest) { FinishAudioTest(); return; } // 소리 확인 중 END를 누르면 테스트만 멈춘다.
        if (EmergencyRequested || (!IsBusy && !IsAudioPlaying)) return; // 실제 통화 또는 재생 중인 오류 안내만 종료하며 구조 요청은 보존한다.
        FinishCall(); // 소리·반복·대기 음성과 상태를 함께 정리한다.
    } // 수동 종료 처리를 끝낸다.

    private void Update() // 매 프레임 실제 녹음 재생이 끝났는지 확인한다.
    { // 오디오 시작 직후의 한 프레임을 자동 종료로 오인하지 않는다.
        if ((State != CallState.Connected && !IsAudioTest) || AudioListener.pause) return; // 통화와 소리 확인만 검사하며 전역 일시정지는 종료로 오인하지 않는다.
        if (waitingForClipData && aS.clip != null && aS.clip.loadState == AudioDataLoadState.Loaded) // 파일 준비가 끝난 첫 프레임인지 확인한다.
        { // 로딩 완료 직후 오디오 장치가 재생을 시작할 짧은 여유를 새로 준다.
            waitingForClipData = false; // 이제 파일 로딩은 끝났음을 기록한다.
            voiceStartedAt = Time.unscaledTime; // 실제 재생 시작 대기는 이 순간부터 센다.
        } // 로딩 완료 시각 처리가 끝났다.
        if (aS.isPlaying) // 실제 음성 소스가 재생을 시작했는지 확인한다.
        { // 재생 요청과 실제 시작을 로그에서 구분할 수 있다.
            if (!voiceHasPlayed) // 이번 재생을 처음 확인한 프레임에서만 실행한다.
            { // 재생 요청만 받은 상태와 실제로 재생 중인 상태를 구분한다.
                Debug.Log("[CallAudio] STARTED: " + (aS.clip != null ? aS.clip.name : "unknown clip"), this); // 소스가 실제 재생 중일 때만 STARTED를 기록한다.
                if (IsAudioTest) SetStatus(CallState.AudioTest, "SOUND TEST\nPLAYING ELINA..."); // 실제 재생을 관찰한 뒤에만 재생 중이라고 표시한다.
            } // 첫 실제 재생 알림이 끝났다.
            voiceHasPlayed = true; // 이번 음성이 재생된 사실을 기억한다.
            return; // 재생 중에는 종료로 바꾸지 않는다.
        } // 실제 재생 관찰을 끝낸다.
        if (voiceHasPlayed) // 시작을 확인했던 녹음이 멈춘 경우다.
        { // 소리 확인과 실제 통화의 종료 문구를 구분한다.
            if (IsAudioTest) FinishAudioTest(); // 테스트는 전화 종료로 표시하지 않는다.
            else FinishCall(); // 실제 통화는 정상 종료로 표시한다.
        } // 자연 종료 처리가 끝났다.
        else if (Time.unscaledTime - voiceStartedAt > (waitingForClipData ? 10f : 3f)) // 로딩 중에는 최대 10초, 준비된 녹음은 3초까지 실제 시작을 기다린다.
        { // 로딩·오디오 재생 실패를 사용자가 볼 수 있게 한다.
            Debug.LogWarning("[CallAudio] DID NOT START. Check clip loading and headset audio output.", this); // 실제 시작 실패만 기록하며 들리는지까지 추측하지 않는다.
            StopMainAudio(); // 남은 소스·수신 참조를 정리한다.
            SetStatus(CallState.Invalid, "AUDIO DID NOT START\nCHECK AUDIO SETTINGS"); // 실제로 재생되지 않은 상황을 분명히 표시한다.
            PlayFeedback(endedCue); // 별도 소스가 가능하다면 짧은 실패 피드백을 낸다.
        } // 음성 시작 실패 처리를 끝낸다.
    } // 자동 종료 검사를 끝낸다.

    private void FinishCall() // 수동 종료와 녹음 완료가 같은 정리 과정을 공유한다.
    { // 남은 벨 반복이나 수신 참조가 다음 통화로 새지 않게 한다.
        StopMainAudio(); // 주 소스를 정지하고 loop를 반드시 false로 만든다.
        SetStatus(CallState.Ended, ContactMessage("CALL ENDED")); // 방금 통화한 동료와 종료를 큰 글씨로 표시한다.
        PlayFeedback(endedCue); // 종료 확인 소리는 별도 소스에서 한 번만 재생한다.
    } // 통화 종료 정리를 끝낸다.

    private void StopMainAudio() // 주 음성을 멈추는 공통 함수다.
    { // 정지 후에도 남는 loop 플래그를 별도로 지운다.
        aS.Stop(); // 실제 재생 중인 벨이나 목소리를 정지한다.
        aS.loop = false; // 다음 목소리가 벨처럼 반복되는 문제를 막는다.
        aS.clip = null; // 이전 녹음 참조를 비운다.
        waitingForClipData = false; // 취소한 파일의 로딩 대기 상태도 지운다.
        incomingCall = null; // 아직 받지 않은 수신 녹음도 비운다.
        incomingCallerFrequency = 0; // 원래 수신 전용 API에는 더 이상 발신자가 없음을 표시한다.
    } // 주 음원 정리를 끝낸다.

    private void PlayMainAudio(AudioClip clip, bool loop, float volume) // clip은 음성, loop는 반복 여부, volume은 0~1 음량이다.
    { // 다른 소리를 시작하기 전에 기존 재생을 멈춘다.
        CheckAudioOutput(); // 재생 요청 때만 청취자 설정을 확인하여 소리가 안 날 때 원인을 로그로 좁힌다.
        aS.enabled = true; // 요청 순간에도 전용 재생 소스를 켠다.
        aS.mute = false; // 전화 소스만 음소거를 해제하며 헤드셋의 실제 볼륨은 바꾸지 않는다.
        aS.Stop(); // 주 소스에 동시에 두 목소리가 겹치지 않게 한다.
        aS.loop = loop; // 수신 벨에서만 true를 전달한다.
        aS.volume = Mathf.Clamp01(volume); // 벨과 음성의 개별 소스 음량을 적용한다.
        aS.clip = clip; // 이번에 재생할 파일을 연결한다.
        voiceStartedAt = Time.unscaledTime; // 실제 초 기준으로 재생 시작 시점을 기억한다.
        voiceHasPlayed = false; // 이번 음성의 실제 재생 관찰 기록을 새로 시작한다.
        waitingForClipData = false; // 이전 녹음의 로딩 대기 여부가 다음 재생에 남지 않게 한다.
        if (clip != null) // 파일이 있을 때만 오디오 데이터를 준비하고 재생한다.
        { // 원본 MP3의 preload 설정이 꺼져 있어도 첫 통화 전에 데이터를 요청한다.
            if (clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData(); // 파일을 수정하지 않고 필요한 음성 데이터를 메모리에 올린다.
            waitingForClipData = clip.loadState == AudioDataLoadState.Loading; // 지금 로딩 중이면 최대 10초 대기를 사용한다.
            aS.Play(); // Unity는 로딩 중인 녹음이 준비되면 재생하며 실제 시작 여부는 Update에서 관찰한다.
            Debug.Log("[CallAudio] REQUEST: " + clip.name + "; load=" + clip.loadState + "; listenerVolume=" + AudioListener.volume + "; paused=" + AudioListener.pause, this); // 입력은 도착했는지와 음원 준비 상태를 한 번 기록한다.
        } // 주 음성의 데이터 준비와 재생 요청을 끝낸다.
    } // 주 음성 재생 설정을 끝낸다.

    private void CheckAudioOutput() // 음성을 들을 청취자가 하나인지 확인하고 문제만 경고한다.
    { // 화면 밖 헤드셋 볼륨을 임의로 바꾸지는 않는다.
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None); // 현재 Scene의 청취자 컴포넌트를 모은다.
        int activeListeners = 0; // 실제 켜진 청취자를 셀 정수다.
        foreach (AudioListener listener in listeners) // 발견한 청취자를 하나씩 확인한다.
            if (listener.isActiveAndEnabled) activeListeners++; // 물체와 컴포넌트가 모두 켜져 있어야 실제 청취자로 센다.
        if (activeListeners != 1) Debug.LogWarning("[CallAudio] Active AudioListeners=" + activeListeners + "; exactly one is required.", this); // 0개 또는 여러 개인 경우만 알린다.
        if (AudioListener.volume <= 0f) Debug.LogWarning("[CallAudio] Global listener volume is zero.", this); // 전역 음량이 0이면 소스가 재생돼도 들리지 않는다.
        if (AudioListener.pause) Debug.LogWarning("[CallAudio] Global listener is paused.", this); // 일시정지가 설정돼 있으면 이유를 로그에 남긴다.
    } // 오디오 출력 설정 검사가 끝났다.

    public void TestCallAudio() // SOUND 버튼은 번호를 걸지 않고 실제 녹음을 재생한다.
    { // 통화 중에는 테스트가 상대 목소리를 덮어쓰지 않는다.
        if (IsBusy || EmergencyRequested) return; // 수신 벨·통화·구조 안내가 우선이다.
        if (IsAudioTest) { FinishAudioTest(); return; } // 다시 누르면 재생 중인 소리 확인만 멈춘다.
        if (audioTestClip == null) { ReportAddressError("SOUND TEST NOT CONNECTED"); return; } // 녹음이 없으면 조용히 성공한 척하지 않는다.
        PlayMainAudio(audioTestClip, false, voiceVolume); // 실제 통화와 같은 소스로 Elina 녹음을 한 번 재생한다.
        SetStatus(CallState.AudioTest, "SOUND TEST\nPREPARING ELINA..."); // 통화 연결과 소리 확인을 구분해 표시한다.
    } // 소리 확인 버튼 처리가 끝났다.

    private void FinishAudioTest() // 테스트 녹음 완료와 테스트 중 END가 같은 정리를 사용한다.
    { // 실제 전화 상태나 구조 예약을 새로 만들지 않는다.
        StopMainAudio(); // 테스트 소리와 남아 있는 재생 참조를 정리한다.
        SetStatus(CallState.Idle, "SOUND TEST FINISHED\nREADY TO CALL"); // 화면에는 완료를 표시하고 실제 회선은 빈 상태로 둔다.
    } // 소리 확인 종료가 끝났다.

    private string ContactMessage(string title) // 공통 상태 메시지를 최대 3줄로 만든다.
    { // 상태·이름·주소를 서로 다른 줄에 놓아 크게 읽히게 한다.
        string crewName = ctm != null ? ctm.GetCrewmateName(CurrentFrequency) : "Unknown contact"; // 주소록이 없을 때도 오류 없이 이름을 만든다.
        return title + "\n" + crewName + "\nFREQ " + CurrentFrequency; // \n은 화면에서 실제 줄바꿈을 뜻한다.
    } // 통화 메시지 만들기를 끝낸다.

    private void SetStatus(CallState next, string message) // 실제 상태가 바뀌는 곳을 한 함수로 모은다.
    { // 상태와 안내가 서로 어긋나지 않게 함께 기록한다.
        bool conversationEnded = (State == CallState.Ringing || State == CallState.Connected) && next != CallState.Ringing && next != CallState.Connected; // 벨에서 목소리로 바뀌는 받기는 아직 종료가 아니다.
        if (conversationEnded && ctm != null) ctm.NotifyAutomaticIncomingEnded(); // 자동 수신이었다면 종료 시점부터 다음 전화의 최소 간격을 센다.
        State = next; // 다음 상태를 저장한다.
        StatusMessage = message; // 그 상태의 화면 안내를 저장한다.
        StatusRevision++; // ++는 1 증가이며 팝업에 새 사건이 생겼음을 알린다.
    } // 상태 기록을 끝낸다.

    public void ReportAddressError(string message) // PHONE 변환 실패도 같은 큰 오류 피드백으로 알린다.
    { // 통화 중 입력 오류로 실제 연결 상태를 덮어쓰지 않는다.
        if (IsBusy || EmergencyRequested) return; // 수신·연결·SOS는 입력 오류보다 우선한다.
        if (IsAudioTest) StopMainAudio(); // 새 입력 오류를 표시하면 이전 테스트 소리도 끝낸다.
        SetStatus(CallState.Invalid, message); // 비어 있는 회선에서만 오류 알림 상태로 바꾼다.
        PlayFeedback(endedCue); // 조용히 실패하지 않고 짧은 피드백을 준다.
    } // 주소 오류 안내를 끝낸다.

    private void PlayFeedback(AudioClip clip) // 음성 소스의 isPlaying 검사에 영향을 주지 않는 짧은 효과음이다.
    { // PlayOneShot은 주 목소리를 교체하지 않는다.
        if (clip != null && feedbackSource != null) feedbackSource.PlayOneShot(clip); // 지정된 파일이 있을 때 별도 소스에서 한 번 재생한다.
    } // 짧은 피드백 재생을 끝낸다.

    public void PlayButtonClick() // 키패드·모드·연락처 버튼이 공통으로 사용할 작은 확인 소리다.
    { // 같은 순간의 중복 효과음만 막으며 숫자 입력 자체는 여기서 처리하지 않는다.
        if (Time.unscaledTime - lastButtonSoundAt < 0.08f) return; // 0.08초 안에 겹친 클릭 소리는 생략한다.
        lastButtonSoundAt = Time.unscaledTime; // 마지막 클릭 소리 시각을 기록한다.
        PlayFeedback(buttonCue); // 짧은 버튼 클릭 파일을 재생한다.
    } // 버튼 소리 처리를 끝낸다.

    public bool IsRinging() { return State == CallState.Ringing; } // 원래 팝업이 쓰던 메서드 이름을 보존한다.
    public int GetIncomingFrequency() { return incomingCallerFrequency; } // 원래 수신 주파수 조회 API를 보존한다.
} // SimpleDial 정의를 끝낸다.
