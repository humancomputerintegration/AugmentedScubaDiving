using UnityEngine; // Ray, Collider, 시간과 위치 계산에 필요한 기능을 가져온다.
using UnityEngine.InputSystem; // 개발용 마우스 입력을 읽는다.
using UnityEngine.XR.Hands; // 검지가 펴져 있는지 실제 관절로 확인한다.

[DefaultExecutionOrder(-90)] // 손 좌표와 손목 UI를 갱신한 다음, 수영 이동보다 먼저 버튼을 처리한다.
public class HandUIInteractor : MonoBehaviour // 검지로 만지기와 레이저를 가리킨 뒤 손끝을 모았다 펴기를 같은 클릭으로 바꾼다.
{
    public HandsInput hands; // 한 프레임에 한 번 계산한 손 추적 결과를 받아 쓴다.
    public Camera headCamera; // PC 마우스 위치에서 화면 속 레이저를 만드는 데 쓰는 카메라다.
    public LayerMask buttonLayers = 1 << 5; // UI 레이어의 버튼만 검색한다.
    public float rayDistance = 2f; // 손 레이저가 선택할 수 있는 최대 거리는 2미터다.
    public float aimGraceTime = 0.18f; // 손을 모으며 레이저가 살짝 흔들려도 직전 목표를 0.18초만 기억한다.
    public float touchRadius = 0.009f; // 검지 끝을 반지름 9밀리미터의 작은 구로 생각한다.
    public HandPointerLight pointerLight; // 손전등과 선택 레이저를 그리는 담당자다.
    private readonly Collider[] touchHits = new Collider[24]; // 매 프레임 새 배열을 만들지 않고 접촉 결과를 담는다.
    private readonly RaycastHit[] rayHits = new RaycastHit[32]; // 레이저가 만난 물체들을 담을 고정 배열이다.
    private HandUIButton leftHover; // 지난 프레임에 왼손이 가리킨 버튼이다.
    private HandUIButton rightHover; // 지난 프레임에 오른손이 가리킨 버튼이다.
    private HandUIButton mouseHover; // PC 마우스가 가리킨 버튼이다.
    private HandUIButton leftTouch; // 왼손이 계속 누르고 있는 버튼을 기억한다.
    private HandUIButton rightTouch; // 오른손이 계속 누르고 있는 버튼을 기억한다.
    private bool leftWasTracked; // 추적이 다시 시작된 순간을 알아내기 위해 이전 상태를 기억한다.
    private bool rightWasTracked; // 오른손도 같은 방식으로 추적 복구를 구분한다.
    private bool leftUIHold; // UI 선택에 쓴 주먹은 다시 펼칠 때까지 전진에 쓰지 않는다.
    private bool rightUIHold; // 오른손도 같은 선택 잠금을 기억한다.
    private AimMemory leftAimMemory; // 왼손을 모으기 직전의 목표와 레이저를 잠깐 기억한다.
    private AimMemory rightAimMemory; // 오른손은 별도의 목표를 기억한다.
    private SelectionMemory leftPending; // 왼손으로 모아서 선택을 준비한 버튼이다.
    private SelectionMemory rightPending; // 오른손은 손을 펼칠 때 이 버튼을 한 번 실행한다.

    private struct AimMemory // 오래된 버튼을 잘못 누르지 않도록 목표·레이저·시각을 함께 묶는다.
    {
        public HandUIButton button; // 마지막으로 펼친 손이 실제 가리킨 버튼이다.
        public Ray ray; // 그 버튼을 가리킨 순간의 레이저다.
        public float time; // 목표를 확인한 실제 시간이다.
    }

    private struct SelectionMemory // 손을 모은 뒤에는 움직이는 버튼을 기준으로 선택 위치를 기억한다.
    {
        public HandUIButton button; // 손을 펼칠 때 실행할 버튼이다.
        public Vector3 localPoint; // 머리·손목이 움직여도 함께 따라가는 버튼 내부의 선택 위치다.
    }

    private void Update() // Unity가 화면을 한 번 갱신할 때마다 호출한다.
    {
        ClearHighlight(leftHover); // 이전 왼손 테두리를 지운다.
        ClearHighlight(rightHover); // 이전 오른손 테두리를 지운다.
        ClearHighlight(mouseHover); // 이전 PC 테두리도 지운다.
        Physics.SyncTransforms(); // 이번 프레임에 움직인 손목 UI의 위치를 물리 검색에도 반영한다.
        if (hands != null && hands.UseHandInput && Application.isFocused) // 손 입력이 가능하고 앱을 보고 있을 때만 손 클릭을 처리한다.
        {
            leftHover = ProcessHand(HandSide.Left, ref leftTouch, ref leftWasTracked, ref leftUIHold, ref leftAimMemory, ref leftPending); // 왼손을 검사한다.
            rightHover = ProcessHand(HandSide.Right, ref rightTouch, ref rightWasTracked, ref rightUIHold, ref rightAimMemory, ref rightPending); // 오른손을 검사한다.
        }
        else // PC 모드이거나 앱이 백그라운드로 갔을 때 실행한다.
        {
            leftTouch = rightTouch = leftHover = rightHover = null; // 오래된 접촉과 조준을 모두 잊는다.
            if (hands == null || !hands.UseHandInput) leftUIHold = rightUIHold = false; // PC 모드 전환 때만 잠금을 지우고 앱 포커스 상실 때는 보존한다.
            leftWasTracked = rightWasTracked = false; // 복귀 직후 자동 클릭이 나지 않게 한다.
            leftAimMemory = rightAimMemory = default; // 앱을 떠난 뒤 과거의 목표를 다시 누르지 않는다.
            leftPending = rightPending = default; // 포커스 상실은 펼친 손 동작이 아니므로 예약한 클릭을 취소한다.
            if (pointerLight != null) pointerLight.HidePointers(); // 사라진 손의 레이저가 공중에 남지 않게 한다.
        }
        ProcessMouse(); // 헤드셋 없이도 같은 버튼을 마우스로 검사할 수 있다.
    }

    private HandUIButton ProcessHand(HandSide side, ref HandUIButton touching, ref bool wasTracked, ref bool uiHold, ref AimMemory memory, ref SelectionMemory pending) // 한 손의 터치와 모았다 펴기 선택을 처리한다.
    {
        bool tracked = side == HandSide.Left ? hands.LeftTracked : hands.RightTracked; // 지금 검사하는 손의 추적 상태다.
        if (!tracked || !hands.TryGetIndexTip(side, out Vector3 tip)) // 손이 사라지면 펼쳤다고 추측하지 않는다.
        {
            touching = null; // 이전 접촉을 놓는다.
            memory = default; // 이전 조준 위치를 잊는다.
            pending = default; // 추적 상실은 클릭 완료가 아니라 취소다.
            wasTracked = false; // 복구 직후 닫힌 손도 새 클릭으로 취급하지 않는다.
            if (pointerLight != null) pointerLight.ShowPointer(side, false, Vector3.zero, Vector3.zero, false); // 사라진 손의 레이저를 숨긴다.
            return null; // UI 점유는 실제 손이 다시 펼쳐질 때 해제한다.
        }
        bool fistPressed = side == HandSide.Left ? hands.LeftFistPressedThisFrame : hands.RightFistPressedThisFrame; // 엄지검지·만두 손·주먹을 새로 모은 순간이다.
        bool fist = side == HandSide.Left ? hands.LeftFist : hands.RightFist; // 계속 모으고 있는 상태다.
        bool wasUIHold = uiHold; // 이번 프레임에 풀어도 터치가 중복 클릭을 만들지 않도록 기억한다.
        bool hadPending = pending.button != null; // 이미 선택을 준비한 손인지 먼저 기억한다.
        bool indexExtended = IsIndexExtended(side); // 접힌 검지나 엄지척으로는 터치를 시작하지 않는다.
        HandUIButton touch = indexExtended ? FindTouch(tip, side) : null; // 검지가 펴졌을 때만 근접 버튼을 찾는다.
        if (touch == null && touching != null && touching.CanUse(side) && indexExtended) // 접촉 경계의 떨림을 줄인다.
        {
            Collider held = touching.GetComponent<Collider>(); // 직전에 닿은 버튼의 표면이다.
            if (held != null && Vector3.Distance(held.ClosestPoint(tip), tip) <= touchRadius + 0.012f) touch = touching; // 12mm 더 떼기 전에는 같은 접촉으로 유지한다.
        }
        HandUIButton aimed = null; // 지금 가리키는 목표다.
        Vector3 rayEnd = tip; // 레이저가 없으면 손끝이 기본 끝점이다.
        Vector3 rayStart = tip; // 레이저의 시작점이다.
        bool rayValid = hands.TryGetAimRay(side, out Ray ray); // 버튼 검색과 빛 표시가 같은 레이저를 사용한다.
        if (rayValid) // 추적된 레이저가 있을 때만 조준을 검사한다.
        {
            rayStart = ray.origin; // 현재 손의 시작점을 기억한다.
            aimed = FindRayButton(ray, side, false, out rayEnd); // 가장 앞에 있는 버튼을 찾는다.
            if (!fist && aimed != null && !hadPending) // 선택을 준비하기 전에 실제로 가리킨 목표만 기억한다.
            {
                memory.button = aimed; // 선택 테두리가 보인 버튼이다.
                memory.ray = ray; // 모으는 순간 레이저가 흔들릴 때 참고할 방향이다.
                memory.time = Time.unscaledTime; // 0.18초가 지나면 이 기억을 시작 동작에 쓸 수 없다.
            }
        }
        HandUIButton target = touch != null ? touch : aimed; // 손끝 접촉의 시각적 표시를 우선한다.
        if (pending.button != null && !pending.button.CanUse(side)) pending = default; // 숨겨지거나 비활성화된 버튼의 예약은 취소한다.
        if (pending.button != null && !fist) // 모은 손이 풀리기 시작하면 실제 펼침인지 확인한다.
        {
            if (IsOpenForRelease(side)) // 검지와 중지가 펼쳐진 손만 클릭 완료로 인정한다.
            {
                Vector3 selectedPoint = pending.button.transform.TransformPoint(pending.localPoint); // 움직인 머리·손목을 반영한 버튼의 현재 선택 위치다.
                Vector3 toSelection = selectedPoint - rayStart; // 현재 손에서 현재 버튼으로 향하는 방향이다.
                HandUIButton releaseTarget = null; // 범위나 레이저가 유효하지 않으면 클릭을 취소한다.
                Vector3 releaseEnd = selectedPoint; // 선택 표시가 이동한 버튼을 따라가게 한다.
                if (rayValid && toSelection.sqrMagnitude > 0.000001f && toSelection.magnitude <= rayDistance) // 선택한 버튼이 여전히 손의 조작 범위 안에 있어야 한다.
                    releaseTarget = FindRayButton(new Ray(rayStart, toSelection.normalized), side, false, out releaseEnd); // 현재 경로에서 앞면·가림 여부를 다시 검사한다.
                if (wasTracked && releaseTarget == pending.button && releaseTarget.CanUse(side)) // 추적이 이어지고 현재 버튼이 가려지지 않았을 때만 실행한다.
                {
                    releaseTarget.Press(); // 주먹·만두 손을 쥐었다가 펼친 순간 딱 한 번 클릭한다.
                    target = releaseTarget; // 이번 프레임의 선택 결과를 표시한다.
                    rayEnd = releaseEnd; // 클릭된 버튼 위치에 빛의 끝을 둔다.
                }
                pending = default; // 성공과 취소 모두 예약을 지워 반복 실행을 막는다.
                uiHold = false; // 손을 펼쳤으므로 다음 동작부터 새로 시작할 수 있다.
            }
            else if (side == HandSide.Right && hands.RightThumbAxis != 0f) // 모은 손에서 엄지척으로 바꾸는 것은 클릭 완료가 아니다.
            {
                pending = default; // UI 선택 예약을 취소한다.
                uiHold = false; // 오른손은 즉시 수영의 위·아래 명령을 사용할 수 있다.
            }
        }
        if (pending.button == null && !fist) uiHold = false; // 예약이 없는 손을 풀면 이전 UI 점유도 끝낸다.
        if (touch != null && wasTracked && touch != touching && !fist && !hadPending && !wasUIHold) touch.Press(); // 직접 검지로 만지기는 계속 즉시 한 번 누른다.
        if (fistPressed && touching != null) uiHold = true; // 터치 직후 손을 모아도 두 번째 클릭이나 이동으로 쓰지 않는다.
        if (touch == null && touching == null && fistPressed && wasTracked && !hadPending) // 실제로 손끝을 새로 모을 때만 선택을 준비한다.
        {
            HandUIButton selection = aimed; // 현재 레이저 목표를 먼저 선택한다.
            if (selection == null && rayValid && memory.button != null && Time.unscaledTime - memory.time <= aimGraceTime) // 모으는 동안 잠깐 흔들린 조준만 보완한다.
            {
                bool handStayedNear = Vector3.Distance(ray.origin, memory.ray.origin) <= 0.12f; // 손을 다른 곳으로 크게 옮겼다면 이전 목표를 쓰지 않는다.
                HandUIButton stillVisible = FindRayButton(memory.ray, side, false, out Vector3 rememberedEnd); // 새 패널을 관통해 옛 버튼을 누르지 않게 확인한다.
                if (handStayedNear && stillVisible == memory.button) // 같은 버튼이 여전히 가장 앞에 있어야 한다.
                {
                    selection = memory.button; // 손을 모으기 직전의 목표다.
                    rayEnd = rememberedEnd; // 선택 준비한 위치에 빛을 표시한다.
                }
            }
            if (selection != null && selection.CanUse(side)) // 반대 손 전용 또는 숨긴 버튼은 준비하지 않는다.
            {
                pending.button = selection; // 손을 펼칠 때 실행할 버튼을 저장한다.
                pending.localPoint = selection.transform.InverseTransformPoint(rayEnd); // 선택 위치를 버튼 기준으로 저장해 머리·손목 이동을 따라간다.
                uiHold = true; // UI에 쓴 모은 손은 앞으로 이동하는 손으로 중복 사용하지 않는다.
            }
        }
        if (pending.button != null) // 손을 쥔 동안에는 선택 준비한 버튼을 확실히 보여 준다.
        {
            target = pending.button; // 버튼의 강조 테두리를 계속 유지한다.
            rayEnd = pending.button.transform.TransformPoint(pending.localPoint); // 레이저 끝도 움직이는 버튼과 함께 따라간다.
        }
        touching = touch; // 계속 닿아 있는 접촉은 다음 프레임에도 같은 접촉이다.
        wasTracked = true; // 유효한 손을 한 프레임 처리했다는 표시다.
        bool busy = uiHold || touch != null; // 이동 중 레이저가 UI를 스치는 것만으로는 이동을 막지 않는다.
        if (side == HandSide.Left) hands.LeftUIBusy = busy; // 실제 왼손 UI 조작만 전진에서 제외한다.
        else hands.RightUIBusy = busy; // 실제 오른손 UI 조작만 엄지 이동에서 제외한다.
        if (target != null) target.SetHovered(true); // 조준 또는 선택 준비 중인 버튼을 강조한다.
        if (pointerLight != null) pointerLight.ShowPointer(side, true, rayStart, rayEnd, aimed != null || touch != null || pending.button != null); // 준비된 선택도 빛으로 보여 준다.
        return target; // 다음 프레임에 이전 강조를 지울 수 있게 한다.
    }

    private bool IsOpenForRelease(HandSide side) // 엄지척이나 자료 누락을 펼친 손과 혼동하지 않는다.
    {
        if (!IsIndexExtended(side)) return false; // 검지가 펴져 있어야 한다.
        if (!hands.TryGetWorldPose(side, XRHandJointID.Wrist, out Pose wrist)) return false; // 중지 길이의 기준인 손목이다.
        if (!hands.TryGetWorldPose(side, XRHandJointID.MiddleProximal, out Pose middleBase)) return false; // 중지 뿌리 자료가 없으면 추측하지 않는다.
        if (!hands.TryGetWorldPose(side, XRHandJointID.MiddleTip, out Pose middleTip)) return false; // 중지 끝도 실제 추적 자료여야 한다.
        float baseLength = Vector3.Distance(wrist.position, middleBase.position); // 손 크기에 맞춘 기준이다.
        return baseLength > 0.01f && Vector3.Distance(wrist.position, middleTip.position) > baseLength * 1.35f; // 검지와 중지를 펼쳤을 때만 선택을 완료한다.
    }

    private bool IsIndexExtended(HandSide side) // 엄지척과 검지 터치를 구분하는 작은 함수다.
    {
        if (!hands.TryGetWorldPose(side, XRHandJointID.Wrist, out Pose wrist)) return false; // 손목 좌표가 없으면 터치하지 않는다.
        if (!hands.TryGetWorldPose(side, XRHandJointID.IndexProximal, out Pose fingerBase)) return false; // 검지 뿌리 좌표를 확인한다.
        if (!hands.TryGetWorldPose(side, XRHandJointID.IndexTip, out Pose fingerTip)) return false; // 검지 끝 좌표를 확인한다.
        float baseLength = Vector3.Distance(wrist.position, fingerBase.position); // 손 크기에 맞춘 기준 길이를 계산한다.
        return baseLength > 0.01f && Vector3.Distance(wrist.position, fingerTip.position) > baseLength * 1.35f; // 접힌 검지는 버튼에 가까워도 터치로 세지 않는다.
    }

    private HandUIButton FindTouch(Vector3 tip, HandSide side) // 검지와 가장 가까운 접촉 버튼을 찾는다.
    {
        int count = Physics.OverlapSphereNonAlloc(tip, touchRadius, touchHits, buttonLayers, QueryTriggerInteraction.Collide); // 버튼의 트리거 영역도 검색한다.
        HandUIButton nearest = null; // 아직 선택한 버튼은 없다.
        float distance = float.PositiveInfinity; // 첫 버튼을 비교할 기준을 충분히 큰 값으로 둔다.
        for (int i = 0; i < count; i++) // 실제로 검색된 물체만 하나씩 검사한다.
        {
            HandUIButton candidate = touchHits[i].GetComponentInParent<HandUIButton>(); // 콜라이더가 속한 버튼을 찾는다.
            if (candidate == null || !candidate.CanUse(side)) continue; // 버튼이 아니거나 이 손으로 누를 수 없으면 건너뛴다.
            if (Vector3.Dot(tip - candidate.transform.position, candidate.transform.forward) > 0.012f) continue; // 패널 뒷면에서 손을 가져와 누르는 실수를 막는다.
            float current = Vector3.SqrMagnitude(touchHits[i].ClosestPoint(tip) - tip); // 제곱 거리만 비교하면 불필요한 제곱근 계산이 없다.
            if (current >= distance) continue; // 이미 찾은 버튼이 더 가까우면 그대로 둔다.
            distance = current; // 더 가까운 거리를 새 기준으로 삼는다.
            nearest = candidate; // 그 거리에 있는 버튼을 저장한다.
        }
        return nearest; // 접촉한 버튼이 없으면 null이 돌아간다.
    }

    private HandUIButton FindRayButton(Ray ray, HandSide side, bool mouse, out Vector3 end) // 레이저의 첫 버튼을 찾는다.
    {
        int count = Physics.RaycastNonAlloc(ray, rayHits, rayDistance, buttonLayers, QueryTriggerInteraction.Collide); // 미리 만든 배열에 레이저 충돌 결과를 담는다.
        float nearestDistance = rayDistance; // 아무것도 없으면 레이저를 최대 길이로 그린다.
        HandUIButton nearest = null; // 처음에는 목표가 없다.
        for (int i = 0; i < count; i++) // 검색 결과의 순서는 보장되지 않으므로 거리를 직접 비교한다.
        {
            if (rayHits[i].distance >= nearestDistance) continue; // 앞에 있는 물체보다 뒤쪽은 선택하지 않는다.
            nearestDistance = rayHits[i].distance; // 버튼이 아닌 패널 바탕도 뒤쪽 클릭을 가로막는다.
            nearest = null; // 앞 물체가 장식이면 뒤 버튼을 선택하지 않게 비운다.
            HandUIButton candidate = rayHits[i].collider.GetComponentInParent<HandUIButton>(); // 실제 버튼인지 확인한다.
            if (candidate == null || !candidate.isActiveAndEnabled) continue; // 패널 바탕을 통과해 뒤쪽 버튼을 누르지 않는다.
            if (mouse ? candidate.button == null || !candidate.button.IsInteractable() : !candidate.CanUse(side)) continue; // 손별 허용과 사용 가능 상태를 확인한다.
            if (Vector3.Dot(ray.direction, candidate.transform.forward) <= 0f) continue; // 앞면에서 가리킬 때만 누를 수 있다.
            nearest = candidate; // 가장 가까운 유효한 버튼을 선택한다.
        }
        end = ray.GetPoint(nearestDistance); // 빛의 끝을 버튼 표면이나 최대 거리에 놓는다.
        return nearest; // 현재 레이저 목표를 알려 준다.
    }

    private void ProcessMouse() // 키보드·마우스로 하는 개발 점검 경로다.
    {
        mouseHover = null; // 지난 마우스 목표를 지운다.
        if (!Application.isFocused || headCamera == null || Mouse.current == null) return; // 사용할 카메라나 마우스가 없으면 끝낸다.
        if (hands != null && hands.UseHandInput) return; // 헤드셋 시연 중에는 PC 마우스가 손 입력에 끼어들지 않는다.
        Ray ray = headCamera.ScreenPointToRay(Mouse.current.position.ReadValue()); // 화면의 커서 위치를 3차원 레이저로 바꾼다.
        mouseHover = FindRayButton(ray, HandSide.Left, true, out Vector3 unusedEnd); // 손과 같은 버튼 검색 함수를 사용한다.
        if (mouseHover == null) return; // 가리키는 버튼이 없으면 클릭하지 않는다.
        mouseHover.SetHovered(true); // PC에서도 선택 테두리를 보여 준다.
        if (Mouse.current.leftButton.wasPressedThisFrame) mouseHover.Press(); // 마우스 버튼을 누른 순간에만 한 번 실행한다.
    }

    private void ClearHighlight(HandUIButton target) // 이미 파괴되거나 꺼진 목표도 안전하게 처리하는 작은 함수다.
    {
        if (target != null) target.SetHovered(false); // 남아 있는 버튼의 강조 테두리를 끈다.
    }
}
