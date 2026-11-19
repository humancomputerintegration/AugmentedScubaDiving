/*
 * 원본 손 collider의 위치 추종을 보존하되 추적 상실 때는 collider를 끕니다.
 * 실제 UI 터치는 HandUIInteractor가 검지 관절로 판정하며, 이 파일은 기존 collider를 안전하게 보관합니다.
 * [RequireComponent]는 필요한 컴포넌트를 표시하고, ?:는 왼손/오른손 상태 중 하나를 고릅니다.
 * if 뒤의 return은 자료가 없는 프레임을 중단하여 마지막 위치에서 계속 접촉하지 않게 합니다.
 */
using UnityEngine; // 위치, collider, Rigidbody를 사용합니다.

[RequireComponent(typeof(Collider))] // 이 물체에는 접촉 모양이 있어야 합니다.
[RequireComponent(typeof(Rigidbody))] // 이 물체에는 움직임을 관리할 Rigidbody가 있어야 합니다.
public class HandColliderFollower : MonoBehaviour // 원본 클래스와 GUID를 유지합니다.
{
    public Transform handTransform; // 원본의 추적되는 손 Transform 연결을 유지합니다.
    public bool smoothFollow = false; // true일 때만 부드러운 보간을 사용합니다.
    public float followSpeed = 20f; // 보간으로 목표 위치에 접근하는 속도입니다.
    [SerializeField] HandsInput handsInput; // 손 추적 유효성은 공통 입력에서 읽습니다.
    [SerializeField] HandSide handSide; // 왼손은 Left, 오른손은 Right로 연결해야 합니다.

    Collider handCollider; // 추적 상실 시 끌 collider입니다.
    bool initiallyEnabled; // 원래 꺼 둔 collider는 이 코드가 임의로 다시 켜지 않습니다.
    bool followedLastFrame; // 재추적 첫 프레임은 과거 위치에서 천천히 이동하지 않고 바로 맞춥니다.

    void Awake() // 원본 컴포넌트를 한 번 찾아 초기화합니다.
    {
        Rigidbody body = GetComponent<Rigidbody>(); // RequireComponent로 존재가 보장됩니다.
        body.useGravity = false; // 손 collider가 중력으로 떨어지지 않게 합니다.
        body.collisionDetectionMode = CollisionDetectionMode.Discrete; // kinematic 방식에 맞는 충돌 설정입니다.
        body.isKinematic = true; // 힘이 아닌 추적 위치가 collider를 움직입니다.
        handCollider = GetComponent<Collider>(); // 실제 접촉 모양을 보관합니다.
        initiallyEnabled = handCollider.enabled; // Scene에서 비활성화한 설정을 존중합니다.
        if (!handsInput) handsInput = GetComponentInParent<HandsInput>(); // 손의 부모 XR rig에서 입력을 찾습니다.
        handCollider.enabled = false; // 첫 유효한 추적 위치를 적용하기 전에는 접촉하지 않습니다.
    }

    void LateUpdate() // 손 Transform이 갱신된 뒤 collider 위치를 맞춥니다.
    {
        bool tracked = handsInput && handsInput.isActiveAndEnabled; // 입력 컴포넌트가 살아 있어야 합니다.
        if (tracked) tracked = handSide == HandSide.Left ? handsInput.LeftTracked : handsInput.RightTracked; // 정확히 이 손의 유효성을 읽습니다.
        if (!tracked || !handTransform || !handTransform.gameObject.activeInHierarchy) // 위치가 없거나 손이 사라진 경우입니다.
        {
            handCollider.enabled = false; // 오래된 손 위치가 버튼이나 trigger를 누르지 못하게 합니다.
            followedLastFrame = false; // 다음 추적은 처음부터 다시 맞춥니다.
            return; // 이 프레임의 추종을 끝냅니다.
        }
        if (smoothFollow && followedLastFrame) // 연속 추적 중일 때만 보간합니다.
        {
            transform.position = Vector3.Lerp(transform.position, handTransform.position, Time.deltaTime * followSpeed); // 위치를 목표 쪽으로 옮깁니다.
            transform.rotation = Quaternion.Slerp(transform.rotation, handTransform.rotation, Time.deltaTime * followSpeed); // 회전도 목표 쪽으로 바꿉니다.
        }
        else // 기본 설정 또는 재추적 첫 프레임입니다.
        {
            transform.SetPositionAndRotation(handTransform.position, handTransform.rotation); // 실제 손 위치로 즉시 맞춥니다.
        }
        followedLastFrame = true; // 다음 프레임에는 연속 추적으로 볼 수 있습니다.
        handCollider.enabled = initiallyEnabled; // 원래 켜져 있던 collider만 유효한 위치에서 켭니다.
    }

    void OnDisable() // 추종 컴포넌트를 끄는 것도 추적 중단으로 취급합니다.
    {
        if (handCollider) handCollider.enabled = false; // 마지막 손 위치의 collider를 남기지 않습니다.
        followedLastFrame = false; // 다시 켤 때 과거 위치에서 보간하지 않습니다.
    }
}
