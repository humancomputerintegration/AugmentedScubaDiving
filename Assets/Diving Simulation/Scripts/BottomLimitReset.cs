using System.Reflection;
using Unity.XR.CoreUtils;
using UnityEngine;

// A safety net for falling through the world. Put this on a plane below the scene. If the player (the XR Origin)
// drops through the plane, they are put back at the position they started at, and the dive time starts again
// from zero.
//
// The plane only needs to be where you want the catch to happen: it doesn't need a collider or a tag. (If it does
// have a solid collider, the player would land on it instead of falling through, so make it a trigger or remove it.)
public class BottomLimitReset: MonoBehaviour
{
    [Tooltip("The player. Auto-found as the XR Origin.")]
    [SerializeField] Transform player;
    [Tooltip("On: only a fall through the plane's own area counts (it uses the plane's collider or renderer for its size). " +
             "Off: dropping below the plane's height anywhere counts.")]
    [SerializeField] bool onlyWithinPlane = true;

    Vector3 startPosition;
    Quaternion startRotation;
    float previousHeight;
    CharacterController controller;
    Rigidbody body;
    Collider planeCollider;
    Renderer planeRenderer;

    // The dive timer lives in the InformationManager, in private fields. They are reset from here so that script
    // doesn't have to be edited.
    InformationManager info;
    FieldInfo diveDurationField;
    FieldInfo wasUnderwaterField;

    void Start()
    {
        if (!player)
        {
            XROrigin origin = FindFirstObjectByType<XROrigin>();
            if (origin) player = origin.transform;
        }

        startPosition = player.position;
        startRotation = player.rotation;
        previousHeight = startPosition.y;

        controller = player.GetComponent<CharacterController>();
        body = player.GetComponent<Rigidbody>();
        planeCollider = GetComponent<Collider>();
        planeRenderer = GetComponent<Renderer>();

        info = FindFirstObjectByType<InformationManager>();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        diveDurationField = typeof(InformationManager).GetField("diveDuration", flags);
        wasUnderwaterField = typeof(InformationManager).GetField("wasUnderwater", flags);
    }

    void Update()
    {
        float height = player.position.y;
        float planeHeight = transform.position.y;
        bool fellThrough = previousHeight >= planeHeight && height < planeHeight;
        previousHeight = height;

        if (fellThrough && (!onlyWithinPlane || IsOverPlane(player.position))) ResetPlayer();
    }

    bool IsOverPlane(Vector3 position)
    {
        Bounds bounds;
        if (planeCollider) bounds = planeCollider.bounds;
        else if (planeRenderer) bounds = planeRenderer.bounds;
        else return true; 
        return position.x >= bounds.min.x && position.x <= bounds.max.x &&
               position.z >= bounds.min.z && position.z <= bounds.max.z;
    }

    void ResetPlayer()
    {
        if (controller) controller.enabled = false;
        player.SetPositionAndRotation(startPosition, startRotation);
        if (controller) controller.enabled = true;

        if (body && !body.isKinematic) body.linearVelocity = Vector3.zero;
        previousHeight = startPosition.y;

        ResetDiveTime();
    }

    void ResetDiveTime()
    {
        diveDurationField.SetValue(info, 0f);
        wasUnderwaterField.SetValue(info, false);
    }
}