using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class HandColliderFollower : MonoBehaviour
{
    [Header("Tracked Hand Transform")]
    public Transform handTransform;   // The tracked hand transform (from XRHand tracking or hand prefab)

    [Header("Settings")]
    [Tooltip("If true, will smoothly follow instead of instant snap.")]
    public bool smoothFollow = false;

    [Tooltip("Speed factor for smooth following.")]
    public float followSpeed = 20f;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;  // collider moves via script, no physics forces
    }

    void LateUpdate()
    {
        if (!handTransform) return;

        if (smoothFollow)
        {
            transform.position = Vector3.Lerp(transform.position, handTransform.position, Time.deltaTime * followSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, handTransform.rotation, Time.deltaTime * followSpeed);
        }
        else
        {
            transform.position = handTransform.position;
            transform.rotation = handTransform.rotation;
        }
    }
}