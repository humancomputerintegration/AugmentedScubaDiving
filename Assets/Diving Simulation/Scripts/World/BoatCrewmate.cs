using UnityEngine; // 배의 위치, 회전, 시간과 Mathf 계산을 사용한다.

[DisallowMultipleComponent] // 같은 배에 이 흔들림을 두 번 붙이지 못하게 한다.
public class BoatCrewmate : CrewmateBehavior // 전화가 되는 동료 기능을 물려받고 배의 움직임을 더한다.
{ // 배 동료의 상태와 행동을 담는 범위다.
    public float bobHeight = 0.06f; // 물결 때문에 위아래로 움직이는 최대 거리는 6cm다.
    public float waveSeconds = 5f; // 배가 한 번 올라갔다 내려오는 데 걸리는 초다.
    public float pitchDegrees = 0.8f; // 뱃머리가 위아래로 기울어지는 최대 각도다.
    public float rollDegrees = 1.2f; // 배가 좌우로 기울어지는 최대 각도다.
    private Vector3 anchorPosition; // Scene에서 정한 배의 원래 자리를 기억한다.
    private Quaternion anchorRotation; // Scene에서 정한 배의 원래 방향을 기억한다.
    private float waveTime; // 배가 활성 상태로 움직인 시간을 초 단위로 쌓는다.

    protected override void Start() // 부모의 초기화에 배 전용 준비를 보탠다.
    { // 초기화는 이 배가 처음 실행될 때 한 번 일어난다.
        anchorPosition = transform.position; // 원래 정박 위치를 저장해 배가 떠밀려 멀어지지 않게 한다.
        anchorRotation = transform.rotation; // 기존 배의 정면을 그대로 보관한다.
        base.Start(); // 부모가 담당하던 주파수, 녹음, 자동 전화 초기화를 그대로 실행한다.
    } // 배의 초기화를 마친다.

    private void Update() // 화면을 그리는 매 프레임마다 작은 물결 움직임을 계산한다.
    { // 배의 위치와 자세를 함께 갱신한다.
        waveTime += Time.deltaTime; // 이번 프레임에 흐른 초만 더해 프레임 수와 속도를 분리한다.
        float angle = waveTime * Mathf.PI * 2f / Mathf.Max(1f, waveSeconds); // 한 주기마다 사인 함수의 각도를 한 바퀴 진행한다.
        float height = Mathf.Sin(angle) * Mathf.Clamp(bobHeight, 0f, 0.2f); // 원래 높이를 중심으로 최대 20cm 이내에서 위아래로 흔든다.
        float pitch = Mathf.Sin(angle * 0.8f) * Mathf.Clamp(pitchDegrees, 0f, 4f); // 뱃머리를 조금 더 느리게 기울인다.
        float roll = Mathf.Sin(angle * 1.1f) * Mathf.Clamp(rollDegrees, 0f, 4f); // 좌우 기울기는 다른 속도로 움직여 딱딱한 왕복을 줄인다.
        transform.position = anchorPosition + Vector3.up * height; // 정박한 가로 위치를 지키면서 높이만 바꾼다.
        transform.rotation = anchorRotation * Quaternion.Euler(pitch, 0f, roll); // 원래 방향에 작은 기울임을 더한다.
    } // 이번 프레임의 배 움직임을 마친다.
} // BoatCrewmate 정의를 마친다.
