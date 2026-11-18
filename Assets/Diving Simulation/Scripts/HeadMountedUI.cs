using UnityEngine; // 머리 위치와 회전 기능을 사용한다.

public class HeadMountedUI : MonoBehaviour // 화면 위 상태 표시를 머리에 붙인다.
{
    public Transform head; // 실제 Main Camera를 연결한다.
    public Vector3 offset = new Vector3(0f, 0.23f, 0.8f); // 머리 앞 80cm, 위 23cm에 놓는다.

    private void LateUpdate() // 이동과 머리 추적을 갱신한 뒤 UI 위치를 맞춘다.
    {
        if (head == null) return; // 카메라 연결이 없으면 끝낸다.
        transform.position = head.TransformPoint(offset); // 머리 방향에 맞춰 앞·위 위치를 계산한다.
        transform.rotation = head.rotation; // 시선 기준으로 글자가 기울지 않게 한다.
    }
}
