/*
역할: 물고기·거북이·수중 동료의 루트를 부드러운 타원 또는 지정 항로로 이동합니다.
이동 루트의 +Z가 정면입니다. 다른 모델 축은 modelEuler 또는 visual 자식에서만 보정합니다.
using은 이름 공간 사용, public은 Inspector 연결, float는 소수, Vector3는 x/y/z 위치입니다.
[]는 배열, foreach는 원소 반복, if는 조건, return은 함수 종료, out은 계산 결과를 받는 인수입니다.
Time.deltaTime은 지난 프레임의 초이므로 속도에 곱하면 이번 프레임의 이동 거리가 됩니다.
Resources 경로는 Assets 안 Resources 폴더 기준이며 확장자를 쓰지 않습니다.
PlayableGraph는 이미 제공된 실제 수영 AnimationClip을 Animator에 전달하는 재생 연결입니다.
{}는 범위, ()는 호출·조건, ;는 문장 끝, =는 대입, ==는 비교, &&는 두 조건 모두 참입니다.
*/
using UnityEngine; // 위치·회전·모델·바닥 검사를 위한 Unity 기본 형식을 사용합니다.
using UnityEngine.Animations; // 기존 수영 클립을 Animator로 보내는 재생 노드를 사용합니다.
using UnityEngine.Playables; // 별도의 AnimatorController 파일 없이 클립 재생 연결을 만듭니다.

[DisallowMultipleComponent] // 같은 루트에 이 이동기를 중복해서 붙이지 못하게 합니다.
public class CreatureSwim : MonoBehaviour // Unity 오브젝트에 붙이는 생물 이동 컴포넌트입니다.
{ // 클래스 범위의 시작입니다.
    public Transform waterSurface; // 수면 높이를 읽을 기존 Water Top입니다.
    public Collider seaFloor; // 다른 생물 대신 실제 TerrainCollider만 연결합니다.
    public Transform visual; // 방향 보정과 작은 몸 흔들림을 적용할 모델 자식입니다.
    public Transform[] waypoints; // 비워 두면 타원, 연결하면 순서대로 도는 고정 월드 항로입니다.
    public Vector2 radii = new Vector2(5f, 3f); // 타원의 x방향·z방향 반지름이며 단위는 미터입니다.
    public float speed = 0.65f; // 초당 진행 거리의 기준값입니다.
    public float turnSpeed = 100f; // 초당 회전할 수 있는 최대 각도입니다.
    public float verticalAmplitude = 0.45f; // 타원 수영 중 높이가 변하는 최대 폭입니다.
    public float phase; // 타원에서 시작할 위치를 라디안으로 지정합니다.
    public float minDepth = 0.5f; // 모델 반경에 더해 수면 아래에 남겨 둘 여유입니다.
    public float bottomClearance = 0.45f; // 모델 반경에 더해 해저 위에 남겨 둘 여유입니다.
    public float bodyRadius = 0.45f; // 몸 전체를 감싸는 보수적인 월드 반경입니다.
    public float bodyRoll = 2f; // 모델 자식만 좌우로 살짝 기울이는 최대 각도입니다.
    public string modelResource = ""; // 선택 사항: 원본 모델 또는 Prefab의 Resources 경로입니다.
    public Vector3 modelEuler; // 불러온 모델 자식에만 적용할 축 보정 각도입니다.
    public float modelScale = 1f; // 불러온 모델의 균일 크기입니다. 이동 루트의 크기는 1을 유지합니다.
    public Material[] modelMaterials; // 선택 사항: 원본 FBX 재질 순서대로 지정한 URP 재질입니다.
    public bool animateEmbeddedSwim; // 실제 FBX의 Swimming_Normal 클립을 재생할 때 켭니다.
    public float animationSpeed = 1f; // 실제 클립의 재생 속도 배수입니다.
    public Transform leftLeg; // 선택 사항: 간단한 잠수부 모델의 왼쪽 다리 회전축입니다.
    public Transform rightLeg; // 선택 사항: 오른쪽 다리 회전축입니다. 물고기에서는 비워 둡니다.
    public float kickAngle = 16f; // 잠수부의 각 다리가 위아래로 움직이는 최대 각도입니다.
    public float kickRate = 2.6f; // 발차기 사인 함수의 초당 진행 각도입니다.
    private Vector3 center; // 시작 때의 루트 위치를 타원의 중심으로 기억합니다.
    private Quaternion visualRotation; // 모델에 이미 적용된 축 보정을 보존합니다.
    private Quaternion leftLegRotation; // 왼쪽 다리의 원래 자세를 보관합니다.
    private Quaternion rightLegRotation; // 오른쪽 다리의 원래 자세를 보관합니다.
    private int waypointIndex; // 현재 향하는 항로점 번호입니다.
    private float angle; // 현재 타원 위치를 나타내는 라디안 각도입니다.
    private float swimTime; // 모델 수영 애니메이션의 누적 시간입니다.
    private bool warned; // 같은 막힌 경로 경고를 매 프레임 반복하지 않습니다.
    private PlayableGraph animationGraph; // 이 생물만의 애니메이션 재생 연결입니다.
    private AnimationClipPlayable swimPlayable; // 기존 실제 클립을 평가하는 노드입니다.
    private AnimationClip swimClip; // 제공된 파일에서 찾은 실제 수영 클립입니다.

    private void Start() // 배치와 참조가 준비된 뒤 한 번 초기화합니다.
    { // 초기화 범위의 시작입니다.
        if (waterSurface == null || seaFloor == null || !seaFloor.enabled || seaFloor.isTrigger || speed <= 0f) // 이동 전에 수면·바닥·속도를 검증합니다.
        { // 잘못된 연결 처리 범위입니다.
            Debug.LogError("CreatureSwim: 수면, 활성 TerrainCollider, 양수 속도를 연결하세요.", this); // Inspector에서 고칠 대상을 알려 줍니다.
            enabled = false; // 안전한 수역을 알 수 없으므로 이동하지 않습니다.
            return; // 초기화를 중단합니다.
        } // 잘못된 연결 처리를 마칩니다.
        if (waypoints != null && waypoints.Length == 1) { Debug.LogError("CreatureSwim: 항로는 비우거나 두 점 이상 연결하세요.", this); enabled = false; return; } // 한 점짜리 항로는 거절합니다.
        if (waypoints != null) foreach (Transform point in waypoints) // 연결된 모든 항로점을 검사합니다.
        { // 항로점 검사 범위입니다.
            if (point == null || point.IsChildOf(transform)) { Debug.LogError("CreatureSwim: 항로점은 이동 루트 밖의 고정 Transform이어야 합니다.", this); enabled = false; return; } // 함께 움직이는 목표를 추격하는 오류를 막습니다.
        } // 항로점 검사를 마칩니다.
        radii = new Vector2(Mathf.Max(0.5f, radii.x), Mathf.Max(0.5f, radii.y)); // 반지름이 0이 되어 경로 계산이 무너지지 않게 합니다.
        bodyRadius = Mathf.Max(0.05f, bodyRadius); // 몸을 감싸는 반경은 양수로 유지합니다.
        center = transform.position; // Scene에 배치한 루트 위치를 경로 중심으로 사용합니다.
        angle = Mathf.Repeat(phase, Mathf.PI * 2f); // 시작 각도를 한 바퀴 범위로 정리합니다.
        CreateVisual(); // 이미 연결된 모델을 쓰거나 지정 Resources 모델을 생성합니다.
        if (visual != null) visualRotation = visual.localRotation; // 메시 축 보정을 몸 흔들림과 분리해 보관합니다.
        if (leftLeg != null) leftLegRotation = leftLeg.localRotation; // 왼쪽 다리의 시작 자세를 저장합니다.
        if (rightLeg != null) rightLegRotation = rightLeg.localRotation; // 오른쪽 다리의 시작 자세를 저장합니다.
        Vector3 start = HasWaypoints() ? transform.position : center + Orbit(angle); // 항로 모드와 타원 모드의 시작점을 고릅니다.
        if (!WaterPoint(start, out Vector3 safe)) { WarnBlocked(); enabled = false; return; } // 시작 위치의 바닥·수면 여유가 없으면 시작하지 않습니다.
        transform.position = safe; // 첫 렌더 프레임 전에 실제 수역 안에 생물을 둡니다.
        Vector3 forward = HasWaypoints() ? waypoints[0].position - safe : Tangent(angle); // 초기 정면을 진행 방향에 맞춥니다.
        if (forward.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(forward, Vector3.up); // 0벡터를 LookRotation에 넣지 않습니다.
        CreateAnimation(); // 실제 제공된 수영 클립이 있을 때만 재생 연결을 만듭니다.
    } // 초기화를 마칩니다.

    private void CreateVisual() // 모델 임포터의 불안정한 내부 fileID 대신 Resources의 실제 모델을 사용합니다.
    { // 모델 준비 범위의 시작입니다.
        if (visual != null || string.IsNullOrEmpty(modelResource)) return; // 이미 배치된 모델이나 모델 생성이 필요 없는 동료는 그대로 둡니다.
        GameObject prefab = Resources.Load<GameObject>(modelResource); // 지정한 기존 모델 에셋을 읽습니다.
        if (prefab == null) { Debug.LogError("CreatureSwim: Resources 모델을 찾지 못했습니다: " + modelResource, this); return; } // 없는 모델을 만들었다고 숨기지 않습니다.
        visual = new GameObject("Model Orientation").transform; // 이동과 메시 축 보정을 분리할 자식을 만듭니다.
        visual.SetParent(transform, false); // 루트에 상대적인 원점에서 시작합니다.
        visual.localRotation = Quaternion.Euler(modelEuler); // 모델 보정은 이동 루트가 아닌 이 자식에만 적용합니다.
        visual.localScale = Vector3.one * Mathf.Max(0.001f, modelScale); // 모델 크기만 조정하고 이동 속도 단위는 유지합니다.
        GameObject model = Instantiate(prefab, visual, false); // 원본의 뼈·재질·메시 계층을 그대로 복사합니다.
        model.name = prefab.name; // Hierarchy에서 실제 종을 알아볼 수 있게 합니다.
        foreach (Transform child in model.GetComponentsInChildren<Transform>(true)) child.gameObject.isStatic = false; // 움직이는 생물 복사본의 static 표시를 해제합니다.
        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true)) // 해당 모델의 렌더러만 조사합니다.
        { // 재질 연결 범위입니다.
            if (modelMaterials != null && modelMaterials.Length > 0 && renderer.sharedMaterials.Length == modelMaterials.Length) renderer.sharedMaterials = modelMaterials; // 실제 슬롯 수가 맞을 때만 준비한 URP 재질을 적용합니다.
        } // 재질 연결을 마칩니다.
    } // 모델 준비를 마칩니다.

    private void CreateAnimation() // 실제 원본 애니메이션을 재사용합니다.
    { // 애니메이션 준비 범위입니다.
        if (!animateEmbeddedSwim || visual == null || string.IsNullOrEmpty(modelResource)) return; // 정적 거북이나 기존 legacy fish에는 새 재생기를 만들지 않습니다.
        foreach (AnimationClip clip in Resources.LoadAll<AnimationClip>(modelResource)) // 해당 FBX 안의 실제 클립 목록을 읽습니다.
        { // 클립 검색 범위입니다.
            if (clip.name.Contains("Swimming_Normal") && !clip.name.StartsWith("__preview__")) { swimClip = clip; break; } // 공격·죽음 클립 대신 정상 수영을 선택합니다.
        } // 클립 검색을 마칩니다.
        Animator animator = visual.GetComponentInChildren<Animator>(); // 원본 FBX의 뼈를 구동할 Animator를 찾습니다.
        if (swimClip == null || animator == null) { Debug.LogWarning("CreatureSwim: 실제 Swimming_Normal 클립/Animator를 찾지 못했습니다. 이동만 실행합니다.", this); return; } // 재생하지 못한 애니메이션을 성공으로 주장하지 않습니다.
        animator.applyRootMotion = false; // 클립이 이동 루트를 끌고 가지 않게 합니다.
        animationGraph = PlayableGraph.Create(name + " Swimming"); // 이 모델의 재생 연결을 생성합니다.
        animationGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual); // 실제 이동 프레임과 함께 클립 시간을 직접 평가합니다.
        swimPlayable = AnimationClipPlayable.Create(animationGraph, swimClip); // 원본 클립을 재생 노드에 연결합니다.
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(animationGraph, "Swim", animator); // 계산한 자세를 해당 Animator에 보냅니다.
        output.SetSourcePlayable(swimPlayable); // 클립에서 Animator까지 연결을 완성합니다.
        swimTime = Mathf.Repeat(phase, 1f) * swimClip.length; // 각 생물이 같은 꼬리 동작을 동시에 하지 않게 시작 시점을 나눕니다.
        animationGraph.Play(); // 준비된 연결을 활성화합니다.
    } // 애니메이션 준비를 마칩니다.

    private bool HasWaypoints() // 고정 항로를 사용하는지 한 곳에서 판단합니다.
    { // 항로 선택 범위입니다.
        return waypoints != null && waypoints.Length >= 2; // 두 점 이상 있을 때만 항로 모드입니다.
    } // 항로 선택을 마칩니다.
    private Vector3 Orbit(float t) // 경로 중심에서 떨어진 타원 위치를 계산합니다.
    { // 타원 계산 범위입니다.
        return new Vector3(radii.x * Mathf.Cos(t), verticalAmplitude * Mathf.Sin(t * 2f), radii.y * Mathf.Sin(t)); // 완만한 높이 변화도 한 바퀴 안에서 반복합니다.
    } // 타원 계산을 마칩니다.
    private Vector3 Tangent(float t) // 타원의 진행 방향과 순간 길이 비율을 계산합니다.
    { // 접선 계산 범위입니다.
        return new Vector3(-radii.x * Mathf.Sin(t), 2f * verticalAmplitude * Mathf.Cos(t * 2f), radii.y * Mathf.Cos(t)); // Orbit을 각도에 대해 미분한 방향입니다.
    } // 접선 계산을 마칩니다.

    private void Update() // 매 프레임 조금씩 이동하고 실제 수영 클립을 평가합니다.
    { // 프레임 처리 범위입니다.
        float step = Mathf.Min(Time.deltaTime, 0.1f); // 매우 긴 프레임 뒤 큰 거리를 한 번에 뛰지 않게 합니다.
        if (step <= 0f) return; // 일시정지 중에는 움직임과 몸 흔들림을 진행하지 않습니다.
        float nextAngle = Mathf.Repeat(angle + speed * step / Mathf.Max(0.01f, Tangent(angle).magnitude), Mathf.PI * 2f); // 타원 구간의 실제 길이를 고려해 각도를 진행합니다.
        Vector3 destination = HasWaypoints() ? waypoints[waypointIndex].position : center + Orbit(nextAngle); // 현재 이동 모드에 맞는 목표를 구합니다.
        if (!WaterPoint(destination, out destination)) { WarnBlocked(); return; } // 목표가 육지·너무 얕은 곳이면 이동을 멈춥니다.
        Vector3 proposed = Vector3.MoveTowards(transform.position, destination, speed * step); // 목표를 지나치지 않는 한 프레임 이동량입니다.
        if (!WaterPoint(proposed, out Vector3 safe)) { WarnBlocked(); return; } // 목표뿐 아니라 이번에 통과할 실제 위치도 검사합니다.
        if (Mathf.Abs(safe.y - proposed.y) > speed * step + 0.01f) { WarnBlocked(); return; } // 급한 지형에서 순간적으로 위로 튀어넘지 않습니다.
        Vector3 movement = safe - transform.position; // 실제 적용할 이동 방향을 계산합니다.
        if (movement.sqrMagnitude > 0.0000001f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(movement, Vector3.up), Mathf.Max(1f, turnSpeed) * step); // 갑작스러운 회전 대신 제한된 속도로 정면을 맞춥니다.
        transform.position = safe; // 검사한 위치만 실제 루트에 적용합니다.
        angle = nextAngle; // 이동에 성공한 경우에만 경로 시계를 진행합니다.
        if (HasWaypoints() && Vector3.Distance(safe, destination) < 0.15f) waypointIndex = (waypointIndex + 1) % waypoints.Length; // 마지막 점 다음에는 첫 점으로 돌아갑니다.
        warned = false; // 안전한 이동이 다시 가능해졌으므로 다음 새로운 문제는 알릴 수 있습니다.
        swimTime += step * Mathf.Max(0f, animationSpeed); // 실제 모델 자세와 몸 흔들림의 시간을 진행합니다.
        if (visual != null) visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(swimTime * 1.6f + phase) * bodyRoll) * visualRotation; // 이동 루트의 정면축을 기준으로 기울인 뒤 원래 메시 축 보정을 보존합니다.
        float kick = Mathf.Sin(swimTime * kickRate + phase) * kickAngle; // 시간에 따라 부드럽게 바뀌는 발차기 각도입니다.
        if (leftLeg != null) leftLeg.localRotation = leftLegRotation * Quaternion.Euler(kick, 0f, 0f); // 왼쪽 다리를 원래 자세 주위로 움직입니다.
        if (rightLeg != null) rightLeg.localRotation = rightLegRotation * Quaternion.Euler(-kick, 0f, 0f); // 오른쪽 다리는 반대 방향으로 움직입니다.
        if (animationGraph.IsValid() && swimClip != null && swimClip.length > 0f) // 재생할 실제 클립과 연결이 있을 때만 평가합니다.
        { // 애니메이션 평가 범위입니다.
            swimPlayable.SetTime(Mathf.Repeat(swimTime, swimClip.length)); // 클립 끝을 넘으면 실제 수영 동작의 처음으로 돌아갑니다.
            animationGraph.Evaluate(0f); // 지정한 시점의 자세를 적용하고 루트 이동은 그대로 둡니다.
        } // 애니메이션 평가를 마칩니다.
    } // 프레임 처리를 마칩니다.

    private bool WaterPoint(Vector3 point, out Vector3 safe) // 몸 반경을 고려해 수면과 실제 해저 사이의 위치를 구합니다.
    { // 수역 검사 범위입니다.
        safe = point; // 실패하더라도 out 인수를 초기화합니다.
        if (waterSurface == null || seaFloor == null || !seaFloor.enabled || !seaFloor.gameObject.activeInHierarchy) return false; // 실행 중 연결이 사라지면 이동하지 않습니다.
        float floor = float.NegativeInfinity; // 다섯 지점 중 가장 높은 바닥을 찾기 위한 시작값입니다.
        for (int i = 0; i < 5; i++) // 몸 중심과 앞뒤·좌우 반경 위치를 검사합니다.
        { // 바닥 표본 검사 범위입니다.
            Vector3 offset = i == 1 ? Vector3.right : i == 2 ? Vector3.left : i == 3 ? Vector3.forward : i == 4 ? Vector3.back : Vector3.zero; // 이번 표본의 수평 방향입니다.
            Vector3 origin = point + offset * bodyRadius; // 모델 몸이 차지하는 범위의 한 지점입니다.
            origin.y = Mathf.Max(waterSurface.position.y, seaFloor.bounds.max.y) + 10f; // 지정된 바닥 Collider보다 위에서 광선을 시작합니다.
            float distance = origin.y - seaFloor.bounds.min.y + 10f; // 지정 바닥의 전체 높이를 포함하는 광선 길이입니다.
            if (!seaFloor.Raycast(new Ray(origin, Vector3.down), out RaycastHit hit, distance)) return false; // 해당 바닥 밖이면 임의의 바닥 높이를 만들지 않습니다.
            floor = Mathf.Max(floor, hit.point.y); // 몸 아래에서 가장 높은 바닥 높이를 보관합니다.
        } // 바닥 표본 검사를 마칩니다.
        float lower = floor + Mathf.Max(0f, bottomClearance) + bodyRadius; // 몸 아랫부분까지 해저 위에 남겨 둡니다.
        float upper = waterSurface.position.y - Mathf.Max(0f, minDepth) - bodyRadius; // 몸 윗부분까지 수면 아래에 남겨 둡니다.
        if (lower >= upper) return false; // 수면과 바닥 사이에 몸이 들어갈 공간이 없으면 거절합니다.
        safe.y = Mathf.Clamp(point.y, lower, upper); // 유효한 수역 안에서만 높이를 제한합니다.
        return true; // 실제 바닥과 수면 사이에 안전한 위치가 있습니다.
    } // 수역 검사를 마칩니다.
    private void WarnBlocked() // 막힌 경로를 한 번만 알려 줍니다.
    { // 경고 처리 범위입니다.
        if (!warned) Debug.LogWarning("CreatureSwim: 경로의 바닥·수면 여유를 확보하세요. 장애물 우회 AI는 포함하지 않습니다.", this); // 실제로 하지 않는 경로 탐색을 제공한다고 말하지 않습니다.
        warned = true; // 같은 문제의 Console 도배를 막습니다.
    } // 경고 처리를 마칩니다.
    private void OnDestroy() // 생물 또는 Scene이 제거될 때 재생 연결을 정리합니다.
    { // 자원 정리 범위입니다.
        if (animationGraph.IsValid()) animationGraph.Destroy(); // 이 생물이 만든 PlayableGraph만 제거합니다.
    } // 자원 정리를 마칩니다.
} // CreatureSwim 클래스 정의를 마칩니다.
