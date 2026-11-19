/*
 * 이동의 주인은 이 스크립트 하나입니다: 손/키보드 입력 → 속도 합 → 한 번의 CharacterController.Move.
 * Vector3 +는 방향별 속도를 합하고, * Time.deltaTime은 초당 속도를 이번 프레임의 거리로 바꿉니다.
 * if는 조건부 실행, return은 조기 종료, ?:는 두 값 중 선택, &&는 두 조건을 모두 요구합니다.
 * [SerializeField]는 private 필드도 Inspector에 저장하고, get-only 성격의 HandsInput 신호를 읽어 사용합니다.
 * 물에서는 손을 놓으면 초당 2cm씩 천천히 가라앉고, 엄지를 위로 유지하면 머리가 수면 위 35cm까지 올라옵니다.
 * 시선 전진만으로는 수면 15cm 아래까지만 올라옵니다. 얼굴을 내밀고 수면을 유지하려면 오른손 엄지 위가 필요합니다.
 * 물 밖에서는 시선을 위로 들어도 수평으로 걷고, 발밑에 바닥이 없으면 내려와 수면에서 낙하를 멈춥니다.
 * PC: WASD 이동, 오른쪽 마우스를 누르고 시점 회전, E/U 상승, Q/J 하강(물속과 수면에서).
 */
using UnityEngine; // 위치, 회전, CharacterController를 사용합니다.
using Unity.XR.CoreUtils; // 원본 rig의 XROrigin 연결을 보존합니다.
using UnityEngine.InputSystem; // 새 Input System으로 키보드와 마우스를 읽습니다.
using UnityEngine.InputSystem.XR; // PC 개발 중에는 머리의 TrackedPoseDriver만 잠시 멈춥니다.

[DefaultExecutionOrder(0)] // HandsInput(-100), HandUIInteractor(-90)가 끝난 뒤 이동합니다.
[DisallowMultipleComponent] // 같은 물체에서 두 이동 컴포넌트가 동시에 실행되지 않게 합니다.
public class SimpleMovement : MonoBehaviour // 원본 클래스 이름과 script GUID를 유지합니다.
{ // 이동에 필요한 설정, 상태와 함수를 한 클래스에 묶습니다.
    [SerializeField] XROrigin xrOrigin; // 실제로 움직일 원본 XR Origin입니다.
    [SerializeField] Transform xrCamera; // 사용자가 보고 있는 방향인 Main Camera입니다.
    [SerializeField] HandsInput handsInput; // 모든 손 입력을 한 곳에서 받아옵니다.
    [SerializeField] InformationManager informationManager; // 원본 깊이 계산을 그대로 사용합니다.
    [SerializeField] float speed = 2.4f; // 전진과 PC 이동 속도이며 단위는 m/s입니다.
    [SerializeField] float verticalSpeed = 1f; // 엄지 위/아래 명령의 수직 속도입니다.
    [SerializeField] float idleSinkSpeed = 0.02f; // 상승 명령이 없을 때의 느린 침강 속도입니다. 0.02m는 2cm입니다.
    [SerializeField] float surfaceHeadClearance = 0.35f; // 엄지 위를 유지하면 머리를 수면보다 35cm 위까지 올려 얼굴을 물 밖으로 내밉니다.
    [SerializeField] float mouseSensitivity = 0.12f; // 마우스 1픽셀을 몇 도로 바꿀지 정합니다.

    CharacterController cc; // 충돌을 처리하며 rig를 움직일 유일한 컴포넌트입니다.
    TrackedPoseDriver headPoseDriver; // VR에서는 머리 추적, PC에서는 마우스 시점을 사용합니다.
    bool poseDriverWasEnabled; // 이 스크립트가 꺼질 때 원래 머리 추적 설정을 돌려줍니다.
    bool previousHandsMode = true; // 입력 모드가 바뀌는 순간을 구분합니다.
    bool appPaused; // 앱 일시 중단 중에는 움직이지 않습니다.
    float desktopPitch; // PC 카메라의 위/아래 각도를 기억합니다.
    float desktopCameraYaw; // PC 모드에 들어올 때 카메라의 기존 좌우 각도를 보존합니다.
    float fallSpeed; // 물 밖에서 발밑이 비었을 때만 쌓이는 아래 방향 속도입니다.
    bool wasSwimming; // 직전 프레임에 물에서 수영했는지 기억하여 얼굴만 물 밖으로 나와도 수영을 이어 갑니다.

    void Awake() // 필요한 연결과 물리 설정을 한 번 준비합니다.
    { // 기존 플레이어와 입력 컴포넌트를 준비합니다.
        if (!xrOrigin) xrOrigin = GetComponent<XROrigin>(); // 같은 rig의 원본 XROrigin을 찾습니다.
        if (!xrCamera && xrOrigin && xrOrigin.Camera) xrCamera = xrOrigin.Camera.transform; // 원본 카메라를 재사용합니다.
        if (!handsInput) handsInput = GetComponent<HandsInput>(); // 같은 rig의 손 입력을 찾습니다.
        if (xrOrigin) cc = xrOrigin.GetComponent<CharacterController>(); // 기존 CharacterController를 재사용합니다.
        if (!xrOrigin || !xrCamera || !handsInput || !informationManager || !cc) // 연결 없이 transform 이동으로 우회하지 않습니다.
        { // 필수 연결이 빠졌으면 이동을 시작하지 않습니다.
            Debug.LogError("SimpleMovement: connect XR Origin, Camera, HandsInput, InformationManager and CharacterController.", this); // 빠진 참조를 알립니다.
            enabled = false; // 불완전한 이동을 실행하지 않습니다.
            return; // 다음 초기화를 중단합니다.
        } // 이동에 필요한 기본 연결 검사를 마칩니다.
        if (!informationManager.player || !informationManager.waterPlane) // 깊이 계산에 필요한 두 위치도 확인합니다.
        { // 수심 계산에 필요한 참조가 빠진 경우를 처리합니다.
            Debug.LogError("SimpleMovement: InformationManager needs Player and Water Plane.", this); // 정확한 누락 연결을 표시합니다.
            enabled = false; // 잘못된 수중 판정을 막습니다.
            return; // 초기화를 끝냅니다.
        } // 수심 계산 참조 검사를 마칩니다.
        headPoseDriver = xrCamera.GetComponent<TrackedPoseDriver>(); // 카메라의 기존 Input System 추적기를 찾습니다.
        if (headPoseDriver) poseDriverWasEnabled = headPoseDriver.enabled; // 나중에 복구할 원래 상태입니다.
        PrepareBody(); // 중력·중복 collider를 정리하고 CC를 켭니다.
    } // 최초 이동 준비를 마칩니다.

    void PrepareBody() // 기존 물리 컴포넌트를 하나의 이동 방식에 맞춥니다.
    { // Rigidbody와 CharacterController가 동시에 몸을 움직이지 않도록 정리합니다.
        Rigidbody body = xrOrigin.GetComponent<Rigidbody>(); // 원본 rig에 붙은 Rigidbody입니다.
        if (body) // 원본에는 이 컴포넌트가 존재합니다.
        { // 원본 Rigidbody가 있을 때만 설정을 바꿉니다.
            if (!body.isKinematic) // dynamic일 때만 기존 속도를 지울 수 있습니다.
            { // 직접 움직이던 Rigidbody에 남은 속도를 지웁니다.
                body.linearVelocity = Vector3.zero; // 초기의 낙하 속도를 지웁니다.
                body.angularVelocity = Vector3.zero; // 초기의 회전 속도도 지웁니다.
            } // 기존 Rigidbody의 속도 정리를 마칩니다.
            body.useGravity = false; // 침강과 낙하는 아래의 CC 계산으로만 처리하여 두 번 움직이지 않게 합니다.
            body.collisionDetectionMode = CollisionDetectionMode.Discrete; // kinematic 전환과 맞는 충돌 모드입니다.
            body.isKinematic = true; // 힘과 중력이 rig를 따로 움직이지 못하게 합니다.
        } // Rigidbody 설정을 마칩니다.
        CapsuleCollider legacyCapsule = xrOrigin.GetComponent<CapsuleCollider>(); // 원본의 두 번째 몸통 collider입니다.
        cc.enabled = false; // 크기를 바꾸는 동안 기존 CC를 잠시 멈춥니다.
        if (legacyCapsule) // 원본 몸통 치수를 새로 발명하지 않고 재사용합니다.
        { // 원본 몸통 충돌체의 크기를 재사용합니다.
            cc.radius = legacyCapsule.radius; // 원본 몸통 반지름입니다.
            cc.height = Mathf.Max(legacyCapsule.height, cc.radius * 2f); // 높이가 지름보다 작아질 수는 없습니다.
            legacyCapsule.enabled = false; // CC와 같은 몸통을 중복으로 충돌시키지 않습니다.
        } // 중복 몸통 충돌체 정리를 마칩니다.
        cc.stepOffset = 0f; // 수중에서 계단을 자동으로 올라 수직 이동이 생기지 않게 합니다.
        cc.skinWidth = 0.02f; // 충돌 표면과 작은 간격을 둡니다.
        cc.minMoveDistance = 0f; // 작은 손 이동 명령도 버리지 않습니다.
        AlignBodyToHead(); // 몸통을 현재 머리 위치 아래로 맞춥니다.
        cc.enabled = true; // 이제 이동과 충돌은 이 CC가 담당합니다.
    } // 몸통 충돌과 이동 방식 준비를 마칩니다.

    void Update() // 합쳐진 이동 계산을 한 프레임에 한 번만 수행합니다.
    { // 입력, 수영과 걷기 판단, 충돌 이동을 순서대로 처리합니다.
        if (appPaused || !Application.isFocused) // 시스템 메뉴나 다른 창으로 나가면 이동을 중단합니다.
        { // 일시정지 중에는 지난 낙하 속도를 보관하지 않습니다.
            fallSpeed = 0f; // 재개할 때 지난 낙하 속도를 갑자기 이어받지 않습니다.
            return; // 이 프레임에는 손 입력도 침강도 적용하지 않습니다.
        } // 앱의 일시정지 상태 확인을 마칩니다.
        if (!cc || !cc.enabled || !handsInput || !handsInput.isActiveAndEnabled) // 입력이나 충돌이 꺼졌는지 확인합니다.
        { // 필요한 컴포넌트가 준비되지 않았으면 이번 이동을 건너뜁니다.
            fallSpeed = 0f; // 이동을 다시 켤 때도 중립 속도에서 시작합니다.
            return; // 몸통이나 손 입력 없이 이동하지 않습니다.
        } // 이동에 필요한 컴포넌트 확인을 마칩니다.
        bool handsMode = handsInput.UseHandInput; // 손 추적 상실 때문에 PC 모드로 바꾸지는 않습니다.
        ConfigureHeadMode(handsMode); // XR과 PC의 시점 소유권을 구분합니다.
        if (!handsMode) ReadMouseLook(); // PC에서만 오른쪽 마우스로 시점을 돌립니다.
        AlignBodyToHead(); // 실제 머리의 x/z 이동을 몸통 collider에도 반영합니다.

        float heightAboveWater = informationManager.GetDepth(); // 물속이면 음수, 수면이면 0, 물 밖이면 양수인 머리 높이입니다.
        bool headUnderwater = heightAboveWater < 0f; // 머리가 실제로 물 아래에 있는지 따로 판단합니다.
        float bodyHeight = cc.height * Mathf.Abs(xrOrigin.transform.lossyScale.y); // 몸통 높이를 세계의 미터 단위로 바꿉니다.
        bool bodyTouchesWater = heightAboveWater < bodyHeight; // 몸통 위가 머리에 맞춰져 있으므로 머리 높이에서 몸통 높이를 빼면 발밑 높이입니다.
        bool surfaceSwimming = wasSwimming && !cc.isGrounded && bodyTouchesWater; // 수영하던 몸이 물에 남아 있고 바닥을 딛지 않았으면 얼굴이 나와도 수영을 이어 갑니다.
        bool swimming = heightAboveWater <= 0f || surfaceSwimming; // 물속에서 시작하거나 물속에서 이어 올라온 경우에만 수영합니다.
        Vector3 forwardDirection = xrCamera.forward; // 물속에서는 기존처럼 시선의 위아래 방향도 전진에 포함합니다.
        if (!headUnderwater) forwardDirection = Vector3.ProjectOnPlane(forwardDirection, Vector3.up).normalized; // 얼굴이 물 밖이면 시선 전진은 수평이라서 고개를 들어도 날아가지 않습니다.
        float vertical = 0f; // 매 프레임 중립에서 시작하므로 지난 입력이 남지 않습니다.
        Vector3 velocity = Vector3.zero; // 앞으로 움직일 속도도 매번 새로 만듭니다.
        if (handsMode) // 실제 XR 실행에서는 controller/키보드 이동을 읽지 않습니다.
        { // 기존 손 인식 결과를 읽으며 손 모양 판정 자체는 바꾸지 않습니다.
            if (handsInput.LeftTracked && handsInput.LeftFist && !handsInput.LeftUIBusy) // 만두 손·엄지검지 모으기·주먹으로 전진하며, 실제 UI 선택 중인 손만 잠깐 제외합니다.
                velocity = forwardDirection * speed; // 물속에서는 시선 방향, 물 밖에서는 수평 방향으로 전진합니다.
            if (swimming && handsInput.RightTracked && !handsInput.RightUIBusy) // 물속과 얼굴을 내민 수면에서 유효한 오른손 엄지 입력을 받습니다.
                vertical = handsInput.RightThumbAxis; // 위/아래/중립 신호를 그대로 받습니다.
        } // 이번 프레임의 손 입력 읽기를 마칩니다.
        else // 헤드셋 없이 PC에서 기능을 개발할 때입니다.
        { // 키보드도 같은 수영 허용 조건을 사용합니다.
            velocity = ReadKeyboardMovement(forwardDirection); // WASD도 손과 같은 물속/육지 전진 방향을 사용합니다.
            if (swimming) vertical = ReadKeyboardVertical(); // E/U 상승과 Q/J 하강을 얼굴이 나온 수면에서도 사용할 수 있습니다.
        } // 이번 프레임의 키보드 입력 읽기를 마칩니다.
        if (swimming) // 수영과 물 밖의 낙하를 같은 프레임에 함께 적용하지 않습니다.
        { // 물속 또는 몸이 물에 남은 수면 수영을 계산합니다.
            fallSpeed = 0f; // 물에 들어왔으므로 공중에서 쌓인 낙하 속도를 버립니다.
            if (vertical > 0f && heightAboveWater >= -0.002f) velocity.y = 0f; // 수면 바로 아래부터는 전진의 높이 성분을 없애 엄지 상승을 시선이 방해하지 않게 합니다.
            velocity += Vector3.up * vertical * verticalSpeed; // 수중에서는 왼손 전진과 오른손 위아래 수영을 합칩니다.
            velocity = Vector3.ClampMagnitude(velocity, Mathf.Max(speed, verticalSpeed)); // 대각선으로 이동해도 수영 입력의 최고 속도가 커지지 않게 합니다.
        } // 물에서의 수영 속도 계산을 마칩니다.
        else // 육지·보트를 딛고 있거나 물에 들어오기 전 공중에서 내려오는 중입니다.
        { // 이 상태에서는 엄지 입력으로 공중에 머물 수 없습니다.
            velocity = Vector3.ClampMagnitude(velocity, speed); // 육지의 대각선 걷기도 기본 속도로 제한합니다.
            if (cc.isGrounded) fallSpeed = 0f; // 발밑에 바닥이 있으면 지난 낙하 속도를 버립니다.
            fallSpeed = Mathf.Min(fallSpeed + 9.81f * Time.deltaTime, 10f); // 바닥이 없을 때 중력으로 내려오되 초당 10m를 넘지 않게 합니다.
            velocity.y = -fallSpeed; // 고개 방향과 상관없이 공중에서는 아래 방향 속도만 적용합니다.
        } // 걷기와 낙하 속도 계산을 마칩니다.
        Vector3 delta = velocity * Time.deltaTime; // 초당 속도를 이번 프레임의 이동 거리로 바꿉니다.
        if (swimming && delta.y > 0f) // 엄지뿐 아니라 위를 보며 전진하는 경우도 상승 상한을 지킵니다.
        { // 수영 입력으로 허용 높이보다 계속 떠오르는 일을 막습니다.
            float highestHeadHeight = Mathf.Clamp(surfaceHeadClearance, 0f, 0.5f); // 엄지 위는 기본 35cm까지만 허용하고 잘못 큰 설정도 최대 50cm로 제한합니다.
            if (vertical <= 0f) highestHeadHeight = -0.15f; // 엄지 위 없이 시선으로만 올라올 때는 수면 15cm 아래까지만 허용합니다.
            float remainingRise = Mathf.Max(0f, highestHeadHeight - heightAboveWater); // 이미 상한보다 위라면 상승 거리는 0이며 강제로 아래로 순간이동하지 않습니다.
            delta.y = Mathf.Min(delta.y, remainingRise); // 이번 이동으로 허용된 상한을 넘어 공중으로 날아가거나 수면에 붙지 않게 합니다.
        } // 이번 프레임의 상승 상한 계산을 마칩니다.
        if (swimming && vertical <= 0f) delta.y -= Mathf.Max(0f, idleSinkSpeed) * Time.deltaTime; // 엄지를 놓으면 얼굴이 나온 수면에서도 기존처럼 2cm/s씩 천천히 가라앉습니다.
        if (!swimming && delta.y < 0f) // 물 밖에서 빠르게 내려오다가 수면을 통과하는 프레임입니다.
            delta.y = Mathf.Max(delta.y, -heightAboveWater); // 우선 수면까지만 내려온 뒤 다음 프레임부터 느린 침강을 사용합니다.
        wasSwimming = swimming; // 이번 수영 상태를 기억해 다음 프레임에 얼굴만 나온 상황을 구분합니다.
        cc.Move(delta); // 이 스크립트의 유일한 이동 호출이며 지형 충돌도 여기서 처리합니다.
    } // 이번 프레임의 이동을 마칩니다.

    void AlignBodyToHead() // 룸스케일로 고개를 움직여도 몸통 위치가 멀리 남지 않게 합니다.
    { // 현재 머리 위치에 몸통 충돌체를 맞춥니다.
        Vector3 headLocal = xrOrigin.transform.InverseTransformPoint(xrCamera.position); // 머리의 세계 위치를 rig 기준으로 바꿉니다.
        cc.center = new Vector3(headLocal.x, headLocal.y - cc.height * 0.5f, headLocal.z); // 몸통 위쪽을 머리 높이에 맞춥니다.
    } // 머리와 몸통의 위치 맞추기를 마칩니다.

    void ConfigureHeadMode(bool handsMode) // 같은 카메라를 두 입력이 동시에 회전시키지 않게 합니다.
    { // 헤드셋 추적과 PC 마우스 중 시점을 맡을 입력을 정합니다.
        if (headPoseDriver) headPoseDriver.enabled = handsMode && poseDriverWasEnabled; // 실제 XR에서는 원래 머리 추적을 유지합니다.
        if (handsMode == previousHandsMode) return; // 모드가 유지되면 각도를 다시 초기화하지 않습니다.
        previousHandsMode = handsMode; // 다음 프레임의 비교 기준입니다.
        if (handsMode) return; // XR 머리 각도는 장치가 갱신합니다.
        desktopPitch = Mathf.DeltaAngle(0f, xrCamera.localEulerAngles.x); // 350도를 -10도처럼 이해하기 쉬운 범위로 바꿉니다.
        desktopCameraYaw = xrCamera.localEulerAngles.y; // PC 전환 순간의 좌우 방향을 보존합니다.
    } // 입력 모드에 맞는 시점 준비를 마칩니다.

    void ReadMouseLook() // 왼쪽 클릭은 UI용으로 남기고 오른쪽 버튼만 시점 회전에 사용합니다.
    { // PC에서 오른쪽 마우스를 누른 동안의 회전을 처리합니다.
        Mouse mouse = Mouse.current; // 연결된 마우스가 없다면 null입니다.
        if (mouse == null || !mouse.rightButton.isPressed) return; // 오른쪽 버튼을 누를 때만 시점이 움직입니다.
        Vector2 pixels = mouse.delta.ReadValue(); // 이번 프레임의 마우스 이동 픽셀 수입니다.
        xrOrigin.transform.Rotate(Vector3.up, pixels.x * mouseSensitivity, Space.World); // rig를 세계의 위축 기준으로 좌우 회전합니다.
        desktopPitch = Mathf.Clamp(desktopPitch - pixels.y * mouseSensitivity, -80f, 80f); // 위아래 시점이 뒤집히지 않게 제한합니다.
        xrCamera.localRotation = Quaternion.Euler(desktopPitch, desktopCameraYaw, 0f); // 카메라의 위아래 각도를 적용합니다.
    } // 이번 프레임의 마우스 시점 변경을 마칩니다.

    Vector3 ReadKeyboardMovement(Vector3 forwardDirection) // 물속/육지에 맞게 고른 전진 방향으로 PC 이동 속도를 만듭니다.
    { // WASD 입력을 이동 방향과 속도로 바꿉니다.
        Keyboard keys = Keyboard.current; // 키보드가 없는 Quest에서는 읽지 않습니다.
        if (keys == null) return Vector3.zero; // 장치가 없으면 중립입니다.
        float forward = (keys.wKey.isPressed ? 1f : 0f) - (keys.sKey.isPressed ? 1f : 0f); // W는 전진, S는 후진입니다.
        float side = (keys.dKey.isPressed ? 1f : 0f) - (keys.aKey.isPressed ? 1f : 0f); // D는 오른쪽, A는 왼쪽입니다.
        Vector3 right = Vector3.ProjectOnPlane(xrCamera.right, Vector3.up).normalized; // 좌우 이동에는 기울어진 수직 성분을 넣지 않습니다.
        return (forwardDirection * forward + right * side) * speed; // 최종 최고 속도 제한은 Update에서 한 번 적용합니다.
    } // 키보드의 전후좌우 속도 계산을 마칩니다.

    float ReadKeyboardVertical() // 손 엄지와 같은 -1/0/+1 수직 입력을 만듭니다.
    { // 상승과 하강 키를 하나의 수직 입력으로 합칩니다.
        Keyboard keys = Keyboard.current; // 연결된 키보드를 확인합니다.
        if (keys == null) return 0f; // 키보드가 없으면 중립입니다.
        bool up = keys.eKey.isPressed || keys.uKey.isPressed; // E와 원본의 U 테스트 키가 상승입니다.
        bool down = keys.qKey.isPressed || keys.jKey.isPressed; // Q 또는 J는 하강입니다.
        return (up ? 1f : 0f) - (down ? 1f : 0f); // 위와 아래를 동시에 누르면 서로 상쇄됩니다.
    } // 키보드의 수직 입력 계산을 마칩니다.

    public void NotifyStroke(HandSide side) // 원본 StrokeZone.cs의 메서드 연결을 보존합니다.
    { // 원본에서 호출하던 함수 이름을 유지합니다.
        // 이번 손 전용 조작에서는 옆구리 stroke를 사용하지 않습니다. Scene의 두 StrokeZone도 비활성화합니다.
        // 여기서 별도의 Move를 호출하지 않습니다. 전진·엄지 수영·느린 침강은 Update 한 곳에서 계산합니다.
    } // 사용하지 않는 원본 stroke 연결 함수를 마칩니다.

    void OnApplicationPause(bool paused) // 시스템 메뉴로 앱이 정지할 때 호출합니다.
    { // 앱의 일시정지 여부를 기록합니다.
        appPaused = paused; // 재개 전까지 Update의 이동을 막습니다.
        fallSpeed = 0f; // 시스템 메뉴를 다녀와도 지난 낙하 속도를 이어받지 않습니다.
    } // 앱 일시정지 상태 처리를 마칩니다.

    void OnDisable() // 이 이동 스크립트를 껐을 때 호출합니다.
    { // 이동이 꺼질 때 머리 추적 설정을 돌려줍니다.
        fallSpeed = 0f; // 컴포넌트를 다시 켤 때 중립 속도에서 시작합니다.
        if (headPoseDriver) headPoseDriver.enabled = poseDriverWasEnabled; // 원래 머리 추적 설정을 돌려줍니다.
    } // 이동 컴포넌트 종료 처리를 마칩니다.
} // SimpleMovement 클래스 정의를 마칩니다.
