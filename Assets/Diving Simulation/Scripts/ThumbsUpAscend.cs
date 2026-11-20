using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.Gestures;
using UnityEngine.InputSystem;

//While the RIGHT hand holds a thumbs-up, the diver rises toward the surface.
// Stops once the diver reaches the surface. Hold U to test in the editor.
public class ThumbsUpAscend : MonoBehaviour
{
    public XRHandTrackingEvents rightHand;   // the XR Hand Tracking Events set to Right
    public XRHandPose thumbsUpPose;          // Samples/XR Hands/1.7.0/Gestures/Examples/Hand Poses/Thumbs Up
    public XRHandPose thumbsDownPose;        // .../Hand Poses/Thumbs Down
    public Transform rig;                    // XR Origin (XR Rig)
    public Transform head;                   // Main Camera
    public InformationManager iM;            // Information Manager (to know where the surface is)

    public float ascendSpeed = 1.0f;         // metres per second
    public float descendSpeed = 1.0f;        // metres per second
    public Key testKey = Key.U;              // hold to rise in the editor
    public Key testDownKey = Key.J;          // hold to sink in the editor

    private bool thumbsUp;
    private bool thumbsDown;
    private Rigidbody rb;

    void OnEnable()
    {
        if (thumbsUpPose != null && thumbsUpPose.relativeOrientation != null)
            thumbsUpPose.relativeOrientation.targetTransform = head;
        if (thumbsDownPose != null && thumbsDownPose.relativeOrientation != null)
            thumbsDownPose.relativeOrientation.targetTransform = head;

        rb = rig != null ? rig.GetComponent<Rigidbody>() : null;
        rightHand.jointsUpdated.AddListener(OnJointsUpdated);
    }

    void OnDisable()
    {
        rightHand.jointsUpdated.RemoveListener(OnJointsUpdated);
    }


    // Called every time the right hand's joints update: is it a thumbs-up?
    void OnJointsUpdated(XRHandJointsUpdatedEventArgs args)
    {
        thumbsUp = thumbsUpPose != null && thumbsUpPose.CheckConditions(args);
        thumbsDown = thumbsDownPose != null && thumbsDownPose.CheckConditions(args);

    }


    void Update()
    {
        bool tracked = rightHand.handIsTracked;
        bool keyUp = Keyboard.current != null && Keyboard.current[testKey].isPressed;
        bool keyDown = Keyboard.current != null && Keyboard.current[testDownKey].isPressed;

        bool goUp = (thumbsUp && tracked) || keyUp;
        bool goDown = (thumbsDown && tracked) || keyDown;

        if (goUp == goDown) return;           // neither, or both at once: do nothing
        if (iM.GetDepth() >= 0f) return;      // gestures only work underwater

        float speed = goUp ? ascendSpeed : -descendSpeed;
        rig.position += Vector3.up * speed * Time.deltaTime;

        // stop gravity's built-up fall speed from fighting the gesture
        if (rb != null)
        {
            Vector3 v = rb.linearVelocity;
            v.y = 0f;
            rb.linearVelocity = v;
        }
    }
}
