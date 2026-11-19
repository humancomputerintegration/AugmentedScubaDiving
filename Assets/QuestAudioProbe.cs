using System; // Android에서 예외가 생겼을 때 오류 내용을 읽기 위해 가져와요.
using System.Collections; // 잠시 기다렸다가 이어서 실행하는 코루틴을 사용해요.
using UnityEngine; // Unity의 오브젝트, 소리, 로그 기능을 사용해요.

public sealed class QuestAudioProbe : MonoBehaviour // 헤드셋의 두 소리 재생 경로를 비교하는 임시 검사예요.
{ // 검사 클래스의 시작이에요.
#if UNITY_ANDROID && !UNITY_EDITOR // Android 앱에만 검사 기능을 넣고, 컴퓨터의 Play에서는 실행하지 않아요.
    private AudioSource unitySource; // Unity 경로로 검사음을 재생할 전용 스피커예요.
    private AudioClip unityClip; // 코드로 만든 짧은 검사음을 보관해요.
    private AndroidJavaObject androidTone; // Android 경로로 검사음을 재생할 객체예요.
    private bool appPaused; // 앱이 잠시 멈춘 상태인지 기억해요.

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // 첫 장면을 읽은 뒤 자동으로 검사를 준비해요.
    private static void Install() // 장면 파일을 수정하지 않고 검사 오브젝트를 만들어요.
    { // 자동 준비 함수의 시작이에요.
        GameObject probe = new GameObject("QuestAudioProbe"); // 검사에만 사용할 새 오브젝트를 만들어요.
        DontDestroyOnLoad(probe); // 검사 중 장면이 바뀌어도 이 오브젝트는 유지해요.
        probe.AddComponent<QuestAudioProbe>(); // 검사 스크립트를 붙여서 Start 함수를 실행하게 해요.
        Debug.Log("[QuestAudioProbe] INSTALLED version=1; package=" + Application.identifier + "; Unity=" + Application.unityVersion); // 검사 파일이 이번 앱에서 실행됐는지 확인할 수 있게 남겨요.
    } // 자동 준비 함수의 끝이에요.

    private IEnumerator Start() // 정해진 순서대로 소리를 재생하면서 중간에 기다려요.
    { // 검사 순서의 시작이에요.
        yield return new WaitForSecondsRealtime(20f); // 앱을 실행하고 고글을 쓸 시간을 20초 드려요.
        Debug.Log("[QuestAudioProbe] WAIT focused=" + Application.isFocused + "; paused=" + appPaused); // 아직 소리가 시작되지 않았다면 앱 상태를 확인할 수 있게 해요.
        yield return WaitForReady(); // 앱이 앞에 열려 있고 일시 정지가 풀릴 때까지 기다려요.
        CreateUnityTone(); // 기존 음성 파일과 상관없는 새 검사음을 만들어요.
        int listenerCount = 0; // 현재 켜져 있는 AudioListener의 수를 셀 준비를 해요.
        foreach (AudioListener listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None)) // 장면에 있는 소리를 듣는 구성요소를 살펴봐요.
        { // AudioListener 하나를 확인하는 구간이에요.
            if (listener.isActiveAndEnabled) listenerCount++; // 실제로 켜진 구성요소만 하나씩 세요.
        } // AudioListener 확인을 마쳐요.
        Debug.Log("[QuestAudioProbe] BEGIN listeners=" + listenerCount + "; listenerVolume=" + AudioListener.volume + "; listenerPaused=" + AudioListener.pause + "; outputStarted=" + AudioSettings.Mobile.audioOutputStarted); // 현재 상태를 기록할 뿐, 값을 바꾸지는 않아요.
        for (int round = 1; round <= 3; round++) // 같은 비교를 세 번 해서 소리를 놓쳤는지 확인하기 쉽게 해요.
        { // 한 번의 비교를 시작해요.
            yield return WaitForReady(); // Android 소리를 내기 전에 앱 상태를 확인해요.
            PlayAndroidTone(round); // Android 경로로 긴 삐 소리를 한 번 요청해요.
            yield return new WaitForSecondsRealtime(0.6f); // 요청한 검사음의 길이인 0.6초를 기다려요.
            yield return new WaitForSecondsRealtime(2f); // 두 경로의 소리를 구분하도록 2초 쉬어요.
            for (int beep = 1; beep <= 2; beep++) // Unity 경로에서는 짧은 소리를 두 번 요청해요.
            { // Unity 검사음 하나를 재생하는 구간이에요.
                yield return WaitForReady(); // 매번 재생하기 전에 앱 상태를 다시 확인해요.
                unitySource.Play(); // 준비한 Unity 검사음을 재생하도록 요청해요.
                Debug.Log("[QuestAudioProbe] UNITY requested round=" + round + "; beep=" + beep); // 재생 요청을 기록하며, 실제로 들렸다는 뜻은 아니에요.
                yield return new WaitForSecondsRealtime(0.5f); // 검사음이 끝날 때까지 0.5초 기다려요.
                if (beep == 1) yield return new WaitForSecondsRealtime(0.35f); // 첫 소리와 두 번째 소리 사이에 짧은 간격을 둬요.
            } // 두 번의 Unity 검사음 요청을 마쳐요.
            yield return new WaitForSecondsRealtime(4f); // 다음 비교 전에는 4초 쉬어요.
        } // 세 번의 비교를 마쳐요.
        Debug.Log("[QuestAudioProbe] END: sound requests finished; audibility must be checked by the wearer."); // 요청 순서가 끝났고 실제 청취 결과는 사용자가 확인해야 한다고 적어요.
        Destroy(gameObject); // 검사 오브젝트를 지워서 더 이상 소리를 요청하지 않아요.
    } // 검사 순서의 끝이에요.

    private IEnumerator WaitForReady() // 앱이 화면에 돌아올 때까지 기다리는 짧은 함수예요.
    { // 대기 함수의 시작이에요.
        while (!Application.isFocused || appPaused) // 다른 화면을 보고 있거나 앱이 멈춰 있으면 기다려요.
        { // 기다리는 구간이에요.
            yield return null; // 다음 화면 갱신 때 상태를 다시 확인해요.
        } // 앱이 준비되면 반복을 끝내요.
    } // 대기 함수의 끝이에요.

    private void CreateUnityTone() // Unity가 직접 재생할 작은 소리 파일을 메모리에 만들어요.
    { // 검사음을 만드는 함수의 시작이에요.
        const int sampleRate = 48000; // 1초를 48,000개의 작은 소리 값으로 나눠요.
        float[] samples = new float[24000]; // 0.5초에 해당하는 소리 값 24,000개를 담아요.
        for (int i = 0; i < samples.Length; i++) // 소리 값 하나하나를 채워요.
        { // 소리 값 하나를 만드는 구간이에요.
            float fadeIn = Mathf.Min(1f, i / 480f); // 처음 0.01초 동안 소리를 부드럽게 키워요.
            float fadeOut = Mathf.Min(1f, (samples.Length - 1 - i) / 480f); // 마지막 0.01초 동안 소리를 부드럽게 줄여요.
            samples[i] = 0.12f * Mathf.Sin(2f * Mathf.PI * 660f * i / sampleRate) * fadeIn * fadeOut; // 660Hz 높이의 작은 소리를 만들고 양 끝의 갑작스러운 잡음을 줄여요.
        } // 소리 값을 모두 채웠어요.
        unityClip = AudioClip.Create("QuestProbe660Hz", samples.Length, 1, sampleRate, false); // 한 채널로 된 0.5초짜리 검사음을 만들어요.
        bool dataCopied = unityClip.SetData(samples, 0); // 만들어 둔 소리 값을 검사음에 넣어요.
        Debug.Log("[QuestAudioProbe] Unity clip dataCopied=" + dataCopied); // 소리 값을 넣는 작업이 성공했는지 기록해요.
        unitySource = gameObject.AddComponent<AudioSource>(); // 검사에만 쓸 AudioSource를 만들어요.
        unitySource.playOnAwake = false; // 준비만 하고 바로 소리를 내지는 않아요.
        unitySource.clip = unityClip; // 방금 만든 검사음을 연결해요.
        unitySource.spatialBlend = 0f; // 거리와 방향에 따라 작아지지 않는 2D 소리로 설정해요.
        unitySource.outputAudioMixerGroup = null; // 별도의 믹서 그룹을 거치지 않게 해요.
        unitySource.volume = 0.8f; // 이 검사음만의 음량을 80%로 정해요.
        unitySource.loop = false; // 검사음이 저절로 반복되지 않게 해요.
    } // 검사음을 만드는 함수의 끝이에요.

    private void PlayAndroidTone(int round) // Unity AudioSource를 거치지 않는 Android 검사음을 요청해요.
    { // Android 검사 함수의 시작이에요.
        try // Android 소리 객체를 만들거나 호출할 때 생길 수 있는 오류를 확인해요.
        { // Android 기능을 호출하는 구간이에요.
            if (androidTone == null) // 처음 재생할 때만 Android 소리 객체를 만들어요.
            { // 최초 준비 구간이에요.
                androidTone = new AndroidJavaObject("android.media.ToneGenerator", 3, 30); // 3은 STREAM_MUSIC이고, 이 검사음의 음량은 30%예요.
            } // Android 소리 객체 준비를 마쳐요.
            bool accepted = androidTone.Call<bool>("startTone", 5, 600); // 5는 길이를 정할 수 있는 TONE_DTMF_5이며, 600ms 동안 재생을 요청해요.
            int session = androidTone.Call<int>("getAudioSessionId"); // Android가 이 소리에 붙인 세션 번호를 읽어요.
            Debug.Log("[QuestAudioProbe] NATIVE accepted=" + accepted + "; round=" + round + "; session=" + session); // 요청 수락 여부를 기록하며 실제 청취 성공을 뜻하지는 않아요.
        } // Android 기능 호출을 마쳐요.
        catch (Exception error) // Android 경로가 실패하더라도 다음 Unity 검사는 계속해요.
        { // 오류를 기록하는 구간이에요.
            Debug.LogWarning("[QuestAudioProbe] NATIVE failed: " + error.Message); // 실패 이유를 Logcat에서 읽을 수 있게 남겨요.
        } // 오류 기록 구간의 끝이에요.
    } // Android 검사 함수의 끝이에요.

    private void OnApplicationPause(bool paused) // 앱의 일시 정지 상태가 바뀌면 Unity가 호출해요.
    { // 일시 정지 처리의 시작이에요.
        appPaused = paused; // 다음 검사 단계가 이 상태를 확인할 수 있게 기억해요.
        if (paused) StopProbeSounds(); // 앱이 멈출 때는 검사음도 멈춰요.
        Debug.Log("[QuestAudioProbe] PAUSE=" + paused + "; interrupted sounds are not counted as heard."); // 중간에 멈춘 경우 청취 성공으로 판단하면 안 된다고 기록해요.
    } // 일시 정지 처리의 끝이에요.

    private void OnApplicationFocus(bool focused) // 앱이 앞에 보이는 상태가 바뀌면 Unity가 호출해요.
    { // 화면 상태 처리의 시작이에요.
        if (!focused) StopProbeSounds(); // 다른 화면으로 나가면 검사음만 멈춰요.
        Debug.Log("[QuestAudioProbe] FOCUS=" + focused); // 검사가 화면 상태 때문에 기다리는지 확인할 수 있게 해요.
        Debug.Log("[QuestAudioProbe] FOCUS=" + focused + "; each new sound waits for focus."); // 검사 중 다른 화면으로 나간 사실도 로그에서 확인할 수 있게 해요.
    } // 화면 상태 처리의 끝이에요.

    private void StopProbeSounds() // 이 검사에서 만든 소리만 멈춰요.
    { // 검사음 중지 함수의 시작이에요.
        if (unitySource != null) unitySource.Stop(); // Unity 검사음이 있으면 멈춰요.
        if (androidTone == null) return; // Android 소리 객체가 없으면 여기서 끝내요.
        try // Android에 검사음 중지를 요청해요.
        { // 중지 요청의 시작이에요.
            androidTone.Call("stopTone"); // Android 검사음이 재생 중이면 멈춰요.
        } // 중지 요청을 마쳐요.
        catch (Exception error) // 중지 과정에 오류가 생기면 여기로 와요.
        { // 중지 오류를 기록하는 구간이에요.
            Debug.LogWarning("[QuestAudioProbe] Native stop failed: " + error.Message); // 중지 실패 이유를 기록해요.
        } // 중지 오류 기록을 마쳐요.
    } // 검사음 중지 함수의 끝이에요.

    private void OnDestroy() // 검사 오브젝트가 없어질 때 사용한 자원을 정리해요.
    { // 정리 함수의 시작이에요.
        StopProbeSounds(); // 아직 재생 중인 검사음이 있다면 먼저 멈춰요.
        if (unityClip != null) Destroy(unityClip); // 메모리에 만든 검사음도 지워요.
        if (androidTone == null) return; // Android 소리 객체가 없으면 정리를 끝내요.
        try // Android에 재생 자원을 돌려달라고 요청해요.
        { // 자원 정리 요청의 시작이에요.
            androidTone.Call("release"); // Android가 잡고 있던 소리 재생 자원을 돌려줘요.
        } // 자원 정리 요청을 마쳐요.
        catch (Exception error) // 정리 과정에 오류가 생기면 여기로 와요.
        { // 정리 오류를 기록하는 구간이에요.
            Debug.LogWarning("[QuestAudioProbe] Native release failed: " + error.Message); // 정리 실패 이유를 기록해요.
        } // 정리 오류 기록을 마쳐요.
        finally // 성공하거나 실패해도 Java 연결 정리는 시도해요.
        { // Java 연결 정리의 시작이에요.
            androidTone.Dispose(); // C#이 가지고 있던 Java 객체 참조를 정리해요.
            androidTone = null; // 이제 이 객체를 다시 사용하지 않게 해요.
        } // Java 연결 정리를 마쳐요.
    } // 정리 함수의 끝이에요.
#endif // Android 앱에서만 사용하는 검사 코드의 끝이에요.
} // 검사 클래스의 끝이에요.
