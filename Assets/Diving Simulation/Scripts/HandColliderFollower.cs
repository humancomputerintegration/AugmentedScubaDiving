using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

public enum XRHandSide { Left, Right }  // ← add this!

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class XRHandColliderFollower : MonoBehaviour
{
    [Header("Hand Joint Source")]
    public XRHandSide handSide = XRHandSide.Left;
    public XRHandJointID jointId = XRHandJointID.Palm;

    [Header("Follow")]
    public bool smoothFollow = false;
    public float followSpeed = 20f;

    XRHandSubsystem handSubsystem;
    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void OnEnable()
    {
        var loader = XRGeneralSettings.Instance?.Manager?.activeLoader;
        if (loader != null)
            handSubsystem = loader.GetLoadedSubsystem<XRHandSubsystem>();
    }

    void LateUpdate()
    {
        if (handSubsystem == null) return;

        XRHand hand = (handSide == XRHandSide.Left) ? handSubsystem.leftHand : handSubsystem.rightHand;
        if (!hand.isTracked) return;

        var joint = hand.GetJoint(jointId);
        if (!joint.TryGetPose(out Pose p)) return;

        if (smoothFollow)
        {
            transform.position = Vector3.Lerp(transform.position, p.position, Time.deltaTime * followSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, p.rotation, Time.deltaTime * followSpeed);
        }
        else
        {
            transform.SetPositionAndRotation(p.position, p.rotation);
        }
    }
}
