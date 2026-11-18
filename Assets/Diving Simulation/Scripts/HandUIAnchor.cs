using UnityEngine; // 손과 머리의 위치·회전 계산에 필요한 기능을 사용한다.
using UnityEngine.XR.Hands; // 손목과 손바닥 관절 이름을 사용한다.

[DefaultExecutionOrder(-95)] // 손 추적 다음, 버튼 접촉 검사 전에 UI를 옮긴다.
public class HandUIAnchor : MonoBehaviour // 손목·팔·손등 위에 UI를 붙이는 역할이다.
{
    public enum AnchorPlace { Forearm, Wrist, BackOfHand } // UI를 붙이는 세 위치를 구분한다.
    public HandsInput hands; // 손 추적 담당자를 연결한다.
    public Transform head; // 글자가 사용자를 향하도록 머리 위치를 참고한다.
    public Transform content; // 보이거나 숨길 자식 UI다. 이 스크립트 자신을 연결하지 않는다.
    public HandSide side; // 왼손인지 오른손인지 고른다.
    public AnchorPlace place; // 팔·손목·손등 중 붙일 위치를 고른다.
    public Vector3 desktopOffset; // PC에서는 실제 손 대신 머리 앞 점검용 위치를 쓴다.

    private void Update() // 매 프레임 손의 새 위치를 따라간다.
    {
        if (hands == null || head == null || content == null) return; // 필요한 연결이 없으면 계산을 멈춘다.
        if (!hands.UseHandInput) // 키보드·마우스로 점검할 때다.
        {
            content.gameObject.SetActive(true); // PC에서도 손목 버튼을 볼 수 있다.
            content.position = head.TransformPoint(desktopOffset); // 머리 앞 좌우의 정해 둔 위치에 놓는다.
            content.rotation = head.rotation; // 카메라를 향해 글자를 세운다.
            return; // PC 모드에는 실제 손 좌표가 필요 없다.
        }
        bool valid = hands.TryGetWorldPose(side, XRHandJointID.Wrist, out Pose wrist); // 유효한 손목 좌표를 읽는다.
        valid = hands.TryGetWorldPose(side, XRHandJointID.Palm, out Pose palm) && valid; // 손바닥 좌표도 읽는다.
        content.gameObject.SetActive(valid); // 추적이 끊기면 UI가 공중에 남지 않도록 숨긴다.
        if (!valid) return; // 유효하지 않은 좌표는 사용하지 않는다.
        Vector3 alongHand = (palm.position - wrist.position).normalized; // 손목에서 손바닥으로 향하는 방향이다.
        Vector3 back = wrist.rotation * Vector3.up; // 손목의 위쪽 축을 손등 방향으로 사용한다.
        float along = place == AnchorPlace.Forearm ? -0.09f : place == AnchorPlace.BackOfHand ? 0.06f : -0.015f; // 팔 메뉴는 팔꿈치 쪽으로 조금 옮긴다.
        float lift = place == AnchorPlace.Forearm ? 0.04f : 0.03f; // 손 표면과 겹치지 않도록 띄우는 거리다.
        content.position = wrist.position + alongHand * along + back * lift; // 기준 위치에 앞뒤 거리와 띄우는 거리를 더한다.
        Vector3 facing = content.position - head.position; // Canvas 앞면은 -Z이므로 머리에서 UI로 향하는 +Z를 만든다.
        if (facing.sqrMagnitude > 0.0001f) content.rotation = Quaternion.LookRotation(facing, head.up); // 손이 돌아가도 글자는 읽을 수 있게 세운다.
    }
}
