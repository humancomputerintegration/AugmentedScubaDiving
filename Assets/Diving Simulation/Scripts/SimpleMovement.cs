using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;
using System.Collections;

public class SimpleMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] XROrigin xrOrigin;          // XR Origin to move
    [SerializeField] Transform aimPose;          // optional forward source (e.g., Left/Aim Pose)
    [SerializeField] Transform xrCamera;         // fallback forward source

    [Header("Movement")]
    [SerializeField] float speed = 1.6f;         // m/s for pinch move
    [SerializeField] bool useCharacterController = true;

    [Header("Pinch (XR Hands)")]
    [Tooltip("Distance (m) between IndexTip & ThumbTip to START pinch.")]
    [SerializeField] float pinchStartDistance = 0.035f;
    [Tooltip("Distance (m) to END pinch (hysteresis; > start).")]
    [SerializeField] float pinchEndDistance   = 0.050f;

    [Header("Swim Stroke (Dual-Hand Side Touch)")]
    [Tooltip("Time window in seconds for both hands to touch their side zones.")]
    [SerializeField] float strokeWindow = 0.5f;
    [Tooltip("Meters of forward impulse per valid stroke.")]
    [SerializeField] float strokeDistance = 2.4f;
    [Tooltip("Seconds over which the impulse is applied (smoother than instant teleport).")]
    [SerializeField] float strokeDuration = 0.25f;
    [Tooltip("Cooldown between strokes to prevent rapid re-trigger.")]
    [SerializeField] float strokeCooldown = 0.35f;

    XRHandSubsystem handSubsystem;
    CharacterController cc;
    bool isPinching;

    // Swim stroke bookkeeping
    float lastLeftTime = -999f;
    float lastRightTime = -999f;
    bool strokeOnCooldown = false;
    Coroutine activeStroke;

    void Awake()
    {
        if (!xrOrigin) xrOrigin = GetComponent<XROrigin>();
        if (!xrCamera && xrOrigin) xrCamera = xrOrigin.Camera?.transform;

        cc = xrOrigin ? xrOrigin.GetComponent<CharacterController>() : null;

        var loader = XRGeneralSettings.Instance?.Manager?.activeLoader;
        if (loader != null) handSubsystem = loader.GetLoadedSubsystem<XRHandSubsystem>();
    }

    void Update()
    {
        // Regular pinch-based forward move (left hand)
        HandlePinchForward();
    }

    void HandlePinchForward()
    {
        if (xrOrigin == null || handSubsystem == null) return;

        XRHand left = handSubsystem.leftHand;
        if (!left.isTracked) { isPinching = false; return; }

        var indexTip = left.GetJoint(XRHandJointID.IndexTip);
        var thumbTip = left.GetJoint(XRHandJointID.ThumbTip);
        if (!indexTip.TryGetPose(out var ip) || !thumbTip.TryGetPose(out var tp))
        {
            isPinching = false; return;
        }

        float d = Vector3.Distance(ip.position, tp.position);
        if (!isPinching && d <= pinchStartDistance) isPinching = true;
        else if (isPinching && d >= pinchEndDistance) isPinching = false;

        if (!isPinching) return;

        Transform dirSrc = aimPose ? aimPose : xrCamera;
        if (!dirSrc) return;

        Vector3 fwd = dirSrc.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-6f) return;
        fwd.Normalize();

        Vector3 delta = fwd * speed * Time.deltaTime;

        if (useCharacterController && cc != null && cc.enabled) cc.Move(delta);
        else xrOrigin.transform.position += delta;
    }

    // ===== Swim Stroke API (called by StrokeZone) =====
    public void NotifyStroke(HandSide side)
    {
        float now = Time.time;

        if (side == HandSide.Left) lastLeftTime = now;
        else                       lastRightTime = now;

        // Check if both sides touched within the window
        if (Mathf.Abs(lastLeftTime - lastRightTime) <= strokeWindow)
        {
            TryStartStrokeImpulse();
            // Reset times a bit to avoid re-triggering from lingering colliders
            lastLeftTime  = -999f;
            lastRightTime = -999f;
        }
    }

    void TryStartStrokeImpulse()
    {
        if (strokeOnCooldown) return;

        // Determine forward at the moment of stroke
        Transform dirSrc = aimPose ? aimPose : xrCamera;
        if (!dirSrc) return;

        Vector3 fwd = dirSrc.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-6f) return;
        fwd.Normalize();

        // If a stroke is already moving, stop it to avoid stacking weirdness
        if (activeStroke != null) StopCoroutine(activeStroke);
        activeStroke = StartCoroutine(ApplyForwardImpulse(fwd, strokeDistance, strokeDuration));

        // Begin cooldown
        StartCoroutine(StrokeCooldown());
    }

    IEnumerator ApplyForwardImpulse(Vector3 forward, float distance, float duration)
    {
        // Move smoothly over 'duration' seconds
        float t = 0f;
        Vector3 totalMoved = Vector3.zero;
        while (t < duration)
        {
            float dt = Time.deltaTime;
            t += dt;

            // Even motion; feel free to change to an ease curve if you want (e.g., SmoothStep)
            float moveThisFrame = (distance / duration) * dt;
            Vector3 step = forward * moveThisFrame;

            if (useCharacterController && cc != null && cc.enabled) cc.Move(step);
            else xrOrigin.transform.position += step;

            totalMoved += step;
            yield return null;
        }
        activeStroke = null;
    }

    IEnumerator StrokeCooldown()
    {
        strokeOnCooldown = true;
        yield return new WaitForSeconds(strokeCooldown);
        strokeOnCooldown = false;
    }
}
