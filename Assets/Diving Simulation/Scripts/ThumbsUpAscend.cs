/* 보관용 원본: 최종 Simulation에서는 비활성화합니다. 활성 이동은 HandsInput + SimpleMovement 한 경로입니다. */
using UnityEngine; // 위치, 회전, GameObject와 MonoBehaviour 등 Unity의 기본 기능을 가져옵니다.
using UnityEngine.XR.Hands; // XR Hands의 손 추적 상태와 관절 이름을 사용합니다.
using UnityEngine.XR.Hands.Gestures; // 원본 XRHandPose 손 모양 판정을 사용합니다.
using UnityEngine.InputSystem; // 원본의 개발용 키보드 입력을 읽습니다.

//While the RIGHT hand holds a thumbs-up, the diver rises toward the surface.
// Stops once the diver reaches the surface. Hold U to test in the editor.
public class ThumbsUpAscend : MonoBehaviour // 보관하는 원본 상승 코드입니다. 최종 Scene에서는 꺼 두어 SimpleMovement와 이중 이동하지 않게 합니다.
{
    public XRHandTrackingEvents rightHand;   // the XR Hand Tracking Events set to Right | 오른손 관절 갱신 이벤트 컴포넌트입니다.
    public XRHandPose thumbsUpPose;          // Samples/XR Hands/1.7.0/Gestures/Examples/Hand Poses/Thumbs Up | 원본의 엄지척 손 모양 에셋입니다.
    public Transform rig;                    // XR Origin (XR Rig) | 원본이 별도로 위치를 올리던 XR rig입니다.
    public Transform head;                   // Main Camera | 손 모양 상대 방향의 기준인 카메라입니다.
    public InformationManager iM;            // Information Manager (to know where the surface is) | 수면 아래에 있는지 확인하는 원본 정보 관리자입니다.

    public float ascendSpeed = 1.0f;         // metres per second | 원본 상승 속도이며 단위는 m/s입니다.
    public Key testKey = Key.U;              // hold to rise in the editor | 원본 PC 상승 시험 키는 U입니다.

    private bool thumbsUp; // 마지막 손 모양 검사 결과를 기억합니다.

    void OnEnable() // 컴포넌트를 켰을 때 호출되는 함수입니다.
    {
        if (thumbsUpPose != null && thumbsUpPose.relativeOrientation != null) // 포즈와 상대 방향 설정이 모두 있는지 확인합니다.
            thumbsUpPose.relativeOrientation.targetTransform = head; // 엄지척 상대 방향 검사의 기준을 머리로 지정합니다.

        rightHand.jointsUpdated.AddListener(OnJointsUpdated); // 관절이 갱신될 때 원본 판정 함수를 호출하도록 등록합니다.
    }

    void OnDisable() // 컴포넌트를 껐을 때 연결과 임시 객체를 정리하는 함수입니다.
    {
        rightHand.jointsUpdated.RemoveListener(OnJointsUpdated); // 비활성화되면 등록했던 같은 함수 연결을 제거합니다.
    }


    // Called every time the right hand's joints update: is it a thumbs-up?
    void OnJointsUpdated(XRHandJointsUpdatedEventArgs args) // 갱신된 관절 자료를 전달받는 원본 콜백입니다.
    {
        thumbsUp = thumbsUpPose.CheckConditions(args); // 포즈 에셋의 조건에 맞는지 검사해 참/거짓으로 저장합니다.
    }


    void Update() // Unity가 실행 중 매 프레임 호출하는 함수입니다.
    {
        bool gesture = thumbsUp && rightHand.handIsTracked; // 엄지척 결과와 실제 손 추적을 모두 요구합니다.
        bool testKeyHeld = Keyboard.current != null && Keyboard.current[testKey].isPressed; // 키보드가 있고 U를 누르는 동안 참입니다.

        if (!gesture && !testKeyHeld) return; // 손도 키도 명령하지 않으면 상승하지 않습니다.
        if (iM.GetDepth() >= 0f) return;     // already at the surface | 수면 이상에서는 원본 상승을 멈춥니다.

        rig.position += Vector3.up * ascendSpeed * Time.deltaTime; // 원본의 별도 transform 상승 경로입니다. 최종 Scene에서는 이 컴포넌트를 끕니다.
    }
}
