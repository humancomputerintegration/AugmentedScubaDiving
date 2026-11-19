using UnityEngine; // 소리 크기를 부드럽게 바꾸기 위한 기능을 가져온다.

public class CallAudioDucking : MonoBehaviour // 통화 중에는 물속 배경음을 줄여 말소리를 잘 들리게 한다.
{ // 통화 배경음 조절 컴포넌트의 범위다.
    public SimpleDial dial; // 수신 대기나 연결 중인지 통화 담당자에게 묻는다.
    public AudioSource ambient; // 기존 잠수 호흡 배경음이다.
    public float normalVolume = 0.16f; // 통화가 없을 때의 배경음 크기다.
    public float callVolume = 0.035f; // 통화 중에는 말소리를 가리지 않게 더 작게 한다.

    private void LateUpdate() // 다른 코드가 재생 여부를 정한 뒤 소리 크기만 조정한다.
    { // 실제 전화와 소리 확인 동안만 배경음을 줄인다.
        if (ambient == null) return; // 배경음 연결이 없으면 끝낸다.
        float target = dial != null && (dial.IsBusy || dial.IsAudioTest) ? callVolume : normalVolume; // 현재 통화 상태에 맞는 목표 크기다.
        ambient.volume = Mathf.MoveTowards(ambient.volume, target, Time.unscaledDeltaTime); // 갑자기 튀지 않게 조금씩 목표로 바꾼다.
        ambient.spatialBlend = 0f; // 카메라와의 거리에 따라 배경음이 들쭉날쭉해지지 않게 한다.
    } // 이번 프레임의 음량 조절을 끝낸다.
} // 통화 배경음 조절 정의를 끝낸다.
