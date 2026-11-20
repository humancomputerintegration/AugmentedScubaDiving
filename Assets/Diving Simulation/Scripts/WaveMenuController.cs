using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;
using UnityEngine.InputSystem;

//Shows the round button menu when the LEFT hand waves left -> right, close left <- right

public class WaveMenuController : MonoBehaviour
{
    //References
    public Transform head; //Main Camera
    public GameObject menuRoot; //button menu
    public GameObject[] panels; //InfoPanel, Calldashboard, FindCrewPanel, CrewInfoPanel

    //Menuplacement
    public float menuDistance = 0.4f;         // metres in front of the head
    public float menuDrop = 0.12f;            // metres below eye level
    public float autoHideTime = 10f;          // hide after this many seconds (0 = never)


    //left hand gesture - wave
    public float palmOutwardThreshold = 0.6f; // 1 = palm pointing exactly forward
    public float waveDistance = 0.15f;        // metres the hand must travel left -> right
    public float waveMaxTime = 0.8f;          // ...within this many seconds

    public bool hideMenuAfterPress = false; // keep the round menu up

    public float panelHeight = 0.2f;   // how far above the round menu panels appear (metres)

    public CrewArrowNavigator arrowNavigator;   // drag in: Flat > CrewNavigator


    //Testing in Unity
    public Key testKey = Key.M;

    private XRHandSubsystem hands;
    private bool tracking;                    // currently measuring a possible wave
    private float startX;                     // hand's left/right position when the wave started
    private float startTime;
    private float cooldownUntil;              // ignore gestures briefly after one fires
    private float shownAt;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        menuRoot.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        // Find the hand-tracking system (it may not be ready on the first frame)
        if (hands == null)
        {
            var loader = XRGeneralSettings.Instance?.Manager?.activeLoader;
            if (loader != null) hands = loader.GetLoadedSubsystem<XRHandSubsystem>();
        }

        // Keyboard shortcut for testing without a headset
        if (Keyboard.current != null && Keyboard.current[testKey].wasPressedThisFrame)
            ToggleMenu();

        DetectWave();

        // Auto-hide
        if (menuRoot.activeSelf && autoHideTime > 0f && Time.time - shownAt > autoHideTime)
            HideMenu();
        
    }

    //for wave detection: 
    /*
    1. Is the left palm facing outward (same way the diver is looking)?
    2. While it is, did the hand move to the right by waveDistance quickly enough?
    */
    void DetectWave(){
        if (hands == null || Time.time < cooldownUntil) return;

        XRHand left = hands.leftHand;
        if (!left.isTracked || !left.GetJoint(XRHandJointID.Palm).TryGetPose(out Pose palm))
        {
            tracking = false;
            return;
        }

        // Hand poses are relative to the rig's Camera Offset (the head's parent) -> convert to world
        Transform space = head.parent;
        Vector3 palmPos = space.TransformPoint(palm.position);
        Quaternion palmRot = space.rotation * palm.rotation;

        // Palm normal points out of the palm (the hand's local -Y axis)
        Vector3 palmNormal = palmRot * Vector3.down;

        Vector3 lookFlat = head.forward;
        lookFlat.y = 0f;
        lookFlat.Normalize();

        bool palmOutward = Vector3.Dot(palmNormal, lookFlat) > palmOutwardThreshold;
        if (!palmOutward)
        {
            tracking = false;
            return;
        }

        // How far right is the hand, from the diver's point of view?
        float x = Vector3.Dot(palmPos - head.position, head.right);

      // Start a new measurement if we weren't tracking, or the last one took too long
        if (!tracking || Time.time - startTime > waveMaxTime)
        {
            tracking = true;
            startX = x;
            startTime = Time.time;
            return;
        }

        float moved = x - startX;   // + = hand moved right, - = hand moved left

        if (moved >= waveDistance){            // left -> right: show
    
            if (!menuRoot.activeSelf) ShowMenu();
            tracking = false;
            cooldownUntil = Time.time + 0.75f;
        }
        else if (moved <= -waveDistance){      // right -> left: hide
            HideAll();
            tracking = false;
            cooldownUntil = Time.time + 0.75f;
            }
        }
    

    /* Shows the menu in front of the diver, facing them, then leaves it still. */
    public void ShowMenu(){
        Vector3 lookFlat = head.forward;
        lookFlat.y = 0f;
        lookFlat.Normalize();

        menuRoot.transform.position = head.position + lookFlat * menuDistance + Vector3.down * menuDrop;
        menuRoot.transform.rotation = Quaternion.LookRotation(lookFlat);
        menuRoot.SetActive(true);
        shownAt = Time.time;

    }

    public void HideMenu()
    {
        menuRoot.SetActive(false);
    }

    public void HideAll(){
        HideMenu();
        CloseAllPanels();
        if (arrowNavigator != null)
            arrowNavigator.StopNavigation();
    }

    public void ToggleMenu()
    {
        if (menuRoot.activeSelf) HideMenu();
        else ShowMenu();
    }

    //open + close panel for each button
    public void OpenPanel(GameObject panel)
    {
        bool wasOpen = panel.activeSelf;

        CloseAllPanels();

        if (!wasOpen){

            if (menuRoot.activeSelf)
            {
                // opened from the floating menu: place the panel just above it
                panel.transform.position = menuRoot.transform.position + Vector3.up * panelHeight;
                panel.transform.rotation = menuRoot.transform.rotation;
            }
            else
            {
                // opened from the wrist band: place the panel in front of the diver
                Vector3 lookFlat = head.forward;
                lookFlat.y = 0f;
                lookFlat.Normalize();

                panel.transform.position = head.position + lookFlat * menuDistance
                                       + Vector3.up * (panelHeight - menuDrop);
                panel.transform.rotation = Quaternion.LookRotation(lookFlat);
            }
            panel.SetActive(true);
        }
        if (hideMenuAfterPress)
            HideMenu();

        shownAt = Time.time; // a press counts as activity: restart the auto-hide timer
    }

    public void CloseAllPanels()
    {
        foreach (GameObject p in panels)
            if (p != null) p.SetActive(false);
    }


}
