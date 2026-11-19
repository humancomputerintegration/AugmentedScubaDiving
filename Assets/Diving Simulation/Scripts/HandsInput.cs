/*
 * 관절 → 이번 프레임의 입력 신호를 만드는 곳입니다. 몸이나 UI는 여기서 움직이지 않습니다.
 * public은 외부 공개, private set은 읽기는 공개하되 쓰기는 이 클래스만 허용한다는 뜻입니다.
 * out은 결과를 돌려주는 추가 출구, &&는 모두 참, ||는 하나라도 참, !는 반대입니다.
 * ?:는 조건에 따라 두 값 중 하나를 고르고, return은 현재 함수를 끝냅니다.
 * Pose는 위치+회전, Vector3는 x/y/z, Quaternion은 3차원 회전을 담습니다.
 * 순서: 이 입력(-100) → HandUIInteractor(-90) → SimpleMovement(0).
 * UI는 이번 프레임의 LeftUIBusy/RightUIBusy를 설정하고, 이동은 그 뒤에 읽습니다.
 */
using System.Collections.Generic; // 손 추적 시스템 목록을 한 번 만들어 계속 재사용합니다.
using UnityEngine; // 위치, 회전, 시간과 MonoBehaviour를 사용합니다.
using UnityEngine.XR; // 실제 XR 장치가 동작 중인지 확인합니다.
using UnityEngine.XR.Hands; // XR Hands 1.7.3의 관절과 추적 상태를 읽습니다.
using UnityEngine.XR.Management; // 현재 로더의 손 추적 시스템을 찾습니다.
using UnityEngine.InputSystem.XR; // 기존 Aim Pose의 위치/회전 추적 유효성도 확인합니다.

[DefaultExecutionOrder(-100)] // UI와 이동보다 먼저 이번 프레임의 입력을 준비합니다.
[DisallowMultipleComponent] // 같은 물체에 입력 관리자가 중복으로 붙는 것을 막습니다.
public class HandsInput : MonoBehaviour // Unity가 매 프레임 실행하는 손 입력 컴포넌트입니다.
{
    [SerializeField] Transform trackingSpace; // 관절 좌표의 기준인 Camera Offset을 연결합니다.
    [SerializeField] Transform head; // Main Camera이며 보조 레이저 방향의 기준입니다.
    [SerializeField] Transform leftAim; // 기존 왼손 Aim Pose를 그대로 사용합니다.
    [SerializeField] Transform rightAim; // 기존 오른손 Aim Pose를 그대로 사용합니다.
    [SerializeField] float closeRatio = 1.15f; // 꽉 쥔 주먹도 보조 선택 동작으로 허용합니다.
    [SerializeField] float pinchStartDistance = 0.035f; // 원본 Eliza처럼 엄지와 검지가 3.5cm 안으로 모이면 입력을 시작합니다.
    [SerializeField] float pinchEndDistance = 0.05f; // 5cm 이상 벌려야 풀리므로 경계에서 클릭이 떨리지 않습니다.
    [SerializeField] float releaseRatio = 1.35f; // 풀 때는 더 큰 기준을 써 경계의 떨림을 줄입니다.
    [SerializeField] float thumbReachRatio = 0.85f; // 손 크기에 비례해 엄지가 펼쳐진 정도를 판단합니다.
    [SerializeField] float verticalThumbDot = 0.65f; // 엄지가 세계의 위/아래를 충분히 향해야 합니다.

    public bool LeftTracked { get; private set; } // 왼손과 필요한 관절들이 이번 프레임에 유효합니다.
    public bool RightTracked { get; private set; } // 오른손과 필요한 관절들이 이번 프레임에 유효합니다.
    public bool LeftFist { get; private set; } // 이름은 기존 연결을 보존하며, 만두 손·엄지검지 모으기·주먹을 모두 뜻합니다.
    public bool RightFist { get; private set; } // 오른손도 모은 손을 선택으로 읽으며 엄지척과는 겹치지 않습니다.
    public bool LeftFistPressedThisFrame { get; private set; } // 왼손을 새로 모아 UI 선택을 준비하는 한 프레임만 참입니다.
    public bool RightFistPressedThisFrame { get; private set; } // 오른손을 새로 모아 UI 선택을 준비하는 한 프레임만 참입니다.
    public float RightThumbAxis { get; private set; } // 위는 +1, 아래는 -1, 나머지는 0입니다.
    public bool UseHandInput { get; private set; } // Quest 또는 실제 XR 장치에서는 손 입력을 사용합니다.
    public bool LeftUIBusy { get; set; } // UI가 왼손을 점유하면 이동에서 제외합니다.
    public bool RightUIBusy { get; set; } // UI가 오른손을 점유하면 엄지 이동에서 제외합니다.

    XRHandSubsystem hands; // 패키지의 손 추적 시스템입니다.
    readonly List<XRHandSubsystem> handSystems = new List<XRHandSubsystem>(); // OpenXR가 직접 등록한 손 시스템도 찾기 위한 목록입니다.
    TrackedPoseDriver leftAimDriver; // 왼손 Aim Pose를 갱신하는 원본 추적기입니다.
    TrackedPoseDriver rightAimDriver; // 오른손 Aim Pose를 갱신하는 원본 추적기입니다.
    bool appPaused; // 시스템 메뉴 등으로 앱이 정지했는지 기억합니다.

    void Awake() // Inspector 연결을 확인하는 초기화입니다.
    {
        if (!head && Camera.main) head = Camera.main.transform; // 명시 연결이 없으면 Main Camera를 찾습니다.
        if (!trackingSpace && head) trackingSpace = head.parent; // 이 프로젝트의 관절 기준은 카메라 부모입니다.
        if (leftAim) leftAimDriver = leftAim.GetComponent<TrackedPoseDriver>(); // 원본 왼손 aim의 추적기를 한 번 찾습니다.
        if (rightAim) rightAimDriver = rightAim.GetComponent<TrackedPoseDriver>(); // 원본 오른손도 같은 방식입니다.
        if (!trackingSpace) // 좌표 기준 없이 잘못된 UI를 누르면 안 됩니다.
        {
            Debug.LogError("HandsInput: assign Camera Offset as Tracking Space.", this); // 빠진 연결을 알려 줍니다.
            enabled = false; // 입력을 중립으로 둔 채 실행을 멈춥니다.
        }
    }

    void Update() // 손 신호를 프레임마다 새로 계산합니다.
    {
        LeftUIBusy = false; // 지난 프레임의 UI 점유를 지웁니다.
        RightUIBusy = false; // HandUIInteractor가 이번 프레임의 값을 다시 씁니다.
        UseHandInput = Application.platform == RuntimePlatform.Android || XRSettings.isDeviceActive; // PC와 XR을 구분합니다.
        if (appPaused || !Application.isFocused) { ClearSignals(); return; } // 앱을 떠나면 마지막 손동작을 유지하지 않습니다.
        hands = FindRunningHands(); // 늦게 시작되거나 OpenXR 기능이 직접 만든 손 시스템도 찾습니다.
        if (hands == null || !hands.running) { ClearSignals(); return; } // 시스템이 멈추면 모든 입력을 멈춥니다.

        bool oldLeftTracked = LeftTracked; // 재추적을 새 클릭으로 착각하지 않도록 이전 유효성을 기억합니다.
        bool oldRightTracked = RightTracked; // 오른손도 같은 규칙입니다.
        bool oldLeftFist = LeftFist; // 직전 프레임의 왼손 주먹 상태입니다.
        bool oldRightFist = RightFist; // 직전 프레임의 오른손 주먹 상태입니다.
        bool rightWasClosed = RightFist || RightThumbAxis != 0f; // 두 형태 모두 네 손가락을 접습니다.
        LeftTracked = ReadGesture(hands.leftHand, oldLeftFist, false, out bool leftFist, out _); // 왼손 엄지 방향은 쓰지 않습니다.
        RightTracked = ReadGesture(hands.rightHand, rightWasClosed, true, out bool rightFist, out float thumb); // 오른손 두 형태를 함께 판정합니다.
        LeftFist = LeftTracked && leftFist; // 관절이 누락되면 전진 입력도 즉시 거짓입니다.
        RightFist = RightTracked && rightFist; // 지난 프레임의 주먹을 남기지 않습니다.
        RightThumbAxis = RightTracked ? thumb : 0f; // 추적 상실 시 수직 입력은 즉시 0입니다.
        LeftFistPressedThisFrame = oldLeftTracked && LeftTracked && !oldLeftFist && LeftFist; // 펼침→모으기 변화에서만 선택 준비 신호를 보냅니다.
        RightFistPressedThisFrame = oldRightTracked && RightTracked && !oldRightFist && RightFist; // 계속 쥐거나 재추적되면 반복하지 않습니다.
    }

    XRHandSubsystem FindRunningHands() // 로더와 Unity 전체 등록 목록에서 실제 실행 중인 손 추적기를 찾습니다.
    {
        var loader = XRGeneralSettings.Instance?.Manager?.activeLoader; // 원본 Eliza가 사용한 로더 연결을 먼저 확인합니다.
        XRHandSubsystem loaded = loader != null ? loader.GetLoadedSubsystem<XRHandSubsystem>() : null; // 로더에 등록된 손 시스템입니다.
        if (loaded != null && loaded.running) return loaded; // 원본 경로가 정상이라면 그대로 사용합니다.
        if (hands != null && hands.running) return hands; // 한 번 찾은 실행 중 시스템은 매번 다시 검색하지 않습니다.
        SubsystemManager.GetSubsystems(handSystems); // XR Hands 공식 예제처럼 Unity가 알고 있는 모든 손 시스템을 받습니다.
        for (int i = 0; i < handSystems.Count; i++) // 아직 시작되지 않은 시스템은 건너뜁니다.
        {
            if (handSystems[i] != null && handSystems[i].running) return handSystems[i]; // OpenXR 기능이 따로 등록한 실행 중 시스템도 허용합니다.
        }
        return null; // 아직 준비되지 않았으면 이번 프레임은 안전하게 중립으로 둡니다.
    }

    bool ReadGesture(XRHand hand, bool wasClosed, bool allowThumb, out bool fist, out float thumbAxis) // 손끝 모으기와 엄지 수영을 나누어 읽습니다.
    {
        fist = false; // 자료가 없으면 이전의 전진·클릭을 이어 쓰지 않습니다.
        thumbAxis = 0f; // 엄지 이동도 중립에서 새로 계산합니다.
        if (!hand.isTracked) return false; // 실제 손을 추적하고 있을 때만 읽습니다.
        if (!hand.GetJoint(XRHandJointID.Wrist).TryGetPose(out Pose wrist)) return false; // 손목은 주먹과 손 크기의 기준입니다.
        if (!hand.GetJoint(XRHandJointID.Palm).TryGetPose(out Pose palm)) return false; // 손바닥은 레이저와 엄지의 기준입니다.
        if (!hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose indexTip)) return false; // 모으는 두 손끝 중 검지를 읽습니다.
        if (!hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out Pose thumbTip)) return false; // 다른 손끝인 엄지를 읽습니다.
        float handWidth = 0.07f; // 손가락 뿌리가 잠깐 가려져도 두 손끝으로 원본 pinch를 읽을 수 있습니다.
        if (hand.GetJoint(XRHandJointID.IndexProximal).TryGetPose(out Pose indexBase) && hand.GetJoint(XRHandJointID.LittleProximal).TryGetPose(out Pose littleBase)) // 폭을 구할 수 있을 때만 보정합니다.
        {
            float measuredWidth = Vector3.Distance(indexBase.position, littleBase.position); // 검지와 새끼손가락 뿌리 사이의 폭입니다.
            if (measuredWidth >= 0.03f) handWidth = measuredWidth; // 0에 가까운 잘못된 관절 간격은 기준으로 쓰지 않습니다.
        }
        float sizeScale = Mathf.Clamp(handWidth / 0.07f, 0.8f, 1.25f); // 작은 손과 큰 손을 보정하되 허용 거리가 과하게 커지지 않게 합니다.
        float pinchLimit = (wasClosed ? pinchEndDistance : pinchStartDistance) * sizeScale; // 시작보다 해제 간격을 넓혀 입력을 안정시킵니다.
        bool pinched = Vector3.Distance(indexTip.position, thumbTip.position) <= pinchLimit; // 사진의 만두 손은 엄지와 검지가 가까우므로 이 경로로 잡힙니다.
        if (pinched) // 나머지 세 손가락을 손바닥 끝까지 꽉 접을 필요는 없습니다.
        {
            fist = true; // 기존 UI와 이동 코드는 이 하나의 신호를 함께 사용합니다.
            return true; // 엄지와 검지를 모은 동작은 엄지척으로 다시 해석하지 않습니다.
        }
        float ratio = wasClosed ? releaseRatio : closeRatio; // 손끝을 모으지 않은 꽉 쥔 주먹도 보조로 허용합니다.
        bool indexValid = FingerClosed(hand, wrist.position, XRHandJointID.IndexProximal, XRHandJointID.IndexTip, ratio, out bool index); // 검지 접힘 자료입니다.
        bool middleValid = FingerClosed(hand, wrist.position, XRHandJointID.MiddleProximal, XRHandJointID.MiddleTip, ratio, out bool middle); // 중지 접힘 자료입니다.
        bool ringValid = FingerClosed(hand, wrist.position, XRHandJointID.RingProximal, XRHandJointID.RingTip, ratio, out bool ring); // 약지 접힘 자료입니다.
        bool littleValid = FingerClosed(hand, wrist.position, XRHandJointID.LittleProximal, XRHandJointID.LittleTip, ratio, out bool little); // 새끼손가락 접힘 자료입니다.
        bool fingersClosed = indexValid && middleValid && ringValid && littleValid && index && middle && ring && little; // 주먹 경로만 네 손가락 모두를 요구합니다.
        if (!fingersClosed) return true; // 손은 유효하므로 레이저를 유지하고, 이동·선택은 중립으로 둡니다.
        if (!allowThumb) // 왼손은 원본 전진 동작이므로 엄지 펼침까지 강요하지 않습니다.
        {
            fist = true; // 왼손 주먹은 엄지의 각도와 관계없이 앞으로 이동할 수 있습니다.
            return true; // 수직 명령은 오른손만 담당합니다.
        }
        if (!hand.GetJoint(XRHandJointID.ThumbProximal).TryGetPose(out Pose thumbBase)) return true; // 엄지 관절이 없으면 상승·하강을 추측하지 않습니다.
        if (!hand.GetJoint(XRHandJointID.ThumbDistal).TryGetPose(out Pose thumbEnd)) return true; // 끝마디 자료가 없을 때도 손끝 pinch는 위에서 이미 처리했습니다.
        Vector3 lowerThumb = thumbEnd.position - thumbBase.position; // 엄지 첫 마디의 방향입니다.
        Vector3 upperThumb = thumbTip.position - thumbEnd.position; // 엄지 끝마디의 방향입니다.
        if (lowerThumb.sqrMagnitude < 0.000001f || upperThumb.sqrMagnitude < 0.000001f) return true; // 길이가 거의 없는 관절은 수영 명령으로 쓰지 않습니다.
        bool thumbStraight = Vector3.Dot(lowerThumb.normalized, upperThumb.normalized) > 0.6f; // 엄지의 두 마디가 같은 방향으로 펴져 있는지 봅니다.
        bool thumbExtended = thumbStraight && Vector3.Distance(thumbTip.position, palm.position) > handWidth * thumbReachRatio; // 손바닥 가까이 접힌 엄지를 구분합니다.
        fist = !thumbExtended; // 오른손의 펼친 엄지는 UI 선택 동작으로 쓰지 않습니다.
        if (!thumbExtended) return true; // 주먹이면 수직 이동은 0으로 남습니다.
        Vector3 worldThumb = trackingSpace.TransformDirection(upperThumb.normalized); // 추적 공간의 엄지 방향을 세계 방향으로 바꿉니다.
        float vertical = Vector3.Dot(worldThumb, Vector3.up); // +1은 위쪽, -1은 아래쪽입니다.
        if (vertical >= verticalThumbDot) thumbAxis = 1f; // 오른쪽 엄지척을 위로 향하면 상승합니다.
        else if (vertical <= -verticalThumbDot) thumbAxis = -1f; // 아래로 향하면 하강합니다.
        return true; // 손의 추적 유효성과 동작 판정을 마칩니다.
    }

    bool FingerClosed(XRHand hand, Vector3 wrist, XRHandJointID baseID, XRHandJointID tipID, float ratio, out bool closed) // 한 손가락을 검사합니다.
    {
        closed = false; // 자료가 없으면 접혔다고 추측하지 않습니다.
        if (!hand.GetJoint(baseID).TryGetPose(out Pose fingerBase)) return false; // 손가락 뿌리입니다.
        if (!hand.GetJoint(tipID).TryGetPose(out Pose fingerTip)) return false; // 같은 손가락의 끝입니다.
        float baseDistance = Vector3.Distance(wrist, fingerBase.position); // 손목에서 뿌리까지 길이입니다.
        if (baseDistance < 0.01f) return false; // 비정상적으로 짧은 길이는 거절합니다.
        closed = Vector3.Distance(wrist, fingerTip.position) <= baseDistance * ratio; // 끝이 손목 가까이 돌아오면 접힌 것입니다.
        return true; // 접힘 여부와 별도로 자료가 유효함을 알립니다.
    }

    public bool TryGetWorldPose(HandSide side, XRHandJointID jointID, out Pose worldPose) // UI와 collider가 같은 변환을 사용합니다.
    {
        worldPose = default; // 실패한 경우 이 기본값은 사용하면 안 됩니다.
        bool tracked = side == HandSide.Left ? LeftTracked : RightTracked; // 요청한 쪽의 유효성을 고릅니다.
        if (!tracked || hands == null || !hands.running || !trackingSpace) return false; // 오래된 좌표는 반환하지 않습니다.
        XRHand hand = side == HandSide.Left ? hands.leftHand : hands.rightHand; // 해당 손을 고릅니다.
        if (!hand.isTracked || !hand.GetJoint(jointID).TryGetPose(out Pose localPose)) return false; // 관절 유효성을 다시 확인합니다.
        worldPose = new Pose(trackingSpace.TransformPoint(localPose.position), trackingSpace.rotation * localPose.rotation); // 세계 위치와 회전입니다.
        return true; // 호출자는 true일 때만 결과를 사용합니다.
    }

    public bool TryGetIndexTip(HandSide side, out Vector3 position) // 직접 터치는 검지 끝의 위치를 사용합니다.
    {
        bool valid = TryGetWorldPose(side, XRHandJointID.IndexTip, out Pose pose); // 공통 좌표 변환을 사용합니다.
        position = pose.position; // 위치만 꺼냅니다.
        return valid; // false라면 UI는 이 손의 터치를 중단합니다.
    }

    public bool TryGetAimRay(HandSide side, out Ray ray) // 레이저의 시작점과 방향을 제공합니다.
    {
        ray = default; // 실패한 레이저는 사용하지 않습니다.
        if (!TryGetWorldPose(side, XRHandJointID.Palm, out Pose palm)) return false; // 유효한 손만 레이저를 만듭니다.
        Transform aim = side == HandSide.Left ? leftAim : rightAim; // 원본 XRI가 갱신하는 해당 손의 Aim Pose입니다.
        TrackedPoseDriver driver = side == HandSide.Left ? leftAimDriver : rightAimDriver; // aim의 추적 상태를 읽을 컴포넌트입니다.
        if (AimIsTracked(aim, driver)) // 손은 보여도 aim만 잃었으면 오래된 방향을 쓰지 않습니다.
        {
            ray = new Ray(aim.position, aim.forward); // controller 입력 없이 손의 aim 방향을 사용합니다.
            return true; // 정식 aim 포즈를 사용할 수 있습니다.
        }
        if (!head) return false; // 보조 방향 계산에는 머리 위치가 필요합니다.
        Vector3 direction = palm.position - head.position; // aim이 없으면 머리에서 손을 향하는 방향입니다.
        if (direction.sqrMagnitude < 0.0025f) return false; // 손이 머리에 너무 가까우면 방향이 불안정합니다.
        ray = new Ray(palm.position, direction.normalized); // 손바닥에서 바깥쪽으로 나갑니다.
        return true; // 보조 레이저를 사용할 수 있습니다.
    }

    bool AimIsTracked(Transform aim, TrackedPoseDriver driver) // 기존 XRI aim의 실제 유효성을 확인합니다.
    {
        if (!aim || !aim.gameObject.activeInHierarchy || !driver || !driver.isActiveAndEnabled) return false; // 꺼진 추적기는 거절합니다.
        var action = driver.trackingStateInput.action; // InputActionProperty 자체 대신 내부 action의 존재를 검사합니다.
        if (action == null || !action.enabled) return false; // 연결되지 않았거나 비활성인 입력은 추적 자료가 아닙니다.
        int trackingState = action.ReadValue<int>(); // 1은 위치, 2는 회전이 유효하다는 비트입니다.
        return (trackingState & 3) == 3; // &는 비트 검사이며 두 비트가 모두 있어야 완전한 aim입니다.
    }

    void ClearSignals() // 모든 중단 경로가 같은 중립 상태를 만듭니다.
    {
        LeftTracked = false; // 왼손 과거 좌표를 사용할 수 없게 합니다.
        RightTracked = false; // 오른손도 초기화합니다.
        LeftFist = false; // 전진 명령을 지웁니다.
        RightFist = false; // UI 주먹을 지웁니다.
        LeftFistPressedThisFrame = false; // 선택 준비 신호를 지웁니다.
        RightFistPressedThisFrame = false; // 재추적 자체는 클릭이 아닙니다.
        RightThumbAxis = 0f; // 상승과 하강을 중단합니다.
        LeftUIBusy = false; // 오래된 UI 점유를 지웁니다.
        RightUIBusy = false; // 다음 프레임에 다시 판정합니다.
    }

    void OnApplicationPause(bool paused) // 운영체제가 앱을 멈추거나 재개할 때 호출합니다.
    {
        appPaused = paused; // 다음 Update도 중단 여부를 알게 합니다.
        if (paused) ClearSignals(); // 멈추는 순간 입력을 비웁니다.
    }

    void OnApplicationFocus(bool focused) // 앱 포커스가 바뀔 때 호출합니다.
    {
        if (!focused) ClearSignals(); // 시스템 UI를 보는 동안 입력을 유지하지 않습니다.
    }

    void OnDisable() // 컴포넌트가 비활성화될 때 호출합니다.
    {
        ClearSignals(); // 외부 스크립트에도 중립값만 보이게 합니다.
    }
}
