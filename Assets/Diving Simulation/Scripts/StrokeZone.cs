/*
 * HandSide는 공통 손 입력과 UI가 함께 사용하는 왼손/오른손 이름입니다.
 * 원본 StrokeZone 연결은 보존하지만 이번 Scene에서는 두 StrokeZone 컴포넌트를 꺼 둡니다.
 * SimpleMovement.NotifyStroke는 호환 연결만 남겼으며 이동을 발생시키지 않습니다.
 */
using UnityEngine; // 원본 trigger와 컴포넌트를 사용합니다.

public enum HandSide // 0/1 숫자 대신 읽기 쉬운 손 이름을 정의합니다.
{
    Left, // 첫 값 0은 왼손입니다.
    Right // 다음 값 1은 오른손입니다.
}

public class StrokeZone : MonoBehaviour // 원본 script GUID와 Scene 필드 연결을 보존합니다.
{
    public HandSide handSide; // 이 영역이 어느 손에 속하는지 표시합니다.
    public Collider targetHandCollider; // 원본 손 collider 참조입니다.
    public SimpleMovement movement; // 원본 이동 컴포넌트 참조입니다.

    void OnTriggerEnter(Collider other) // 원본 연결 호환용이며 최종 Scene에서는 비활성화되어 있습니다.
    {
        if (!movement || !targetHandCollider) return; // 연결이 빠졌다면 아무것도 호출하지 않습니다.
        if (other == targetHandCollider) movement.NotifyStroke(handSide); // 호환 메서드는 추가 이동을 만들지 않습니다.
    }
}
