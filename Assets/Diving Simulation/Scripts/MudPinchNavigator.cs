using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.InputSystem;

/*
Only during a mud storm while underwater:
quick RIGHT pinch (thumb + middle finger)  -> highlight the next button
hold the pinch for holdToSelectTime        -> press the highlighted button

Editor test: N = next, Enter = press (tick Force Mud Mode to test without a storm).
*/
public class MudPinchNavigator : MonoBehaviour
{
    [System.Serializable]
    public class NavButton
    {
        public XRSimpleInteractable button;   // the Btn_... root
        public GameObject highlight;          // its Highlight ring
    }

    public MudStorm mud;                      // Mud Manager
    public InformationManager iM;             // Information Manager

    public GameObject menuRoot;               // RoundMenu
    public NavButton[] menuButtons;           // the 5 round buttons, left to right
    public GameObject callPopup;              // IncomingCallPopup (Part 4)
    public NavButton[] popupButtons;          // Answer, Hang Up

    public float pinchStartDistance = 0.02f;  // metres between thumb and middle tip to start a pinch
    public float pinchEndDistance = 0.035f;   // ...to end it (bigger = no flicker)
    public float holdToSelectTime = 0.8f;     // hold this long to press

    [Header("Editor testing")]
    public bool forceMudMode = false;
    public Key testNextKey = Key.N;
    public Key testSelectKey = Key.Enter;

    private XRHandSubsystem hands;
    private bool pinching;
    private float pinchStartTime;
    private bool selectFired;
    private NavButton[] current;              // list being navigated right now (null = none)
    private int index;

    

    // Update is called once per frame
    void Update()
    {
        if (hands == null)
        {
            var loader = XRGeneralSettings.Instance?.Manager?.activeLoader;
            if (loader != null) hands = loader.GetLoadedSubsystem<XRHandSubsystem>();
        }

        // Which buttons should be navigable right now?
        NavButton[] list = GetActiveList();
        if (list != current)
        {
            SetHighlight(current, -1);        // clear the old list's highlight
            current = list;
            index = 0;
        }
        if (current == null || current.Length == 0) return;

        SetHighlight(current, index);

        if (Keyboard.current != null)
        {
            if (Keyboard.current[testNextKey].wasPressedThisFrame) Next();
            if (Keyboard.current[testSelectKey].wasPressedThisFrame) Press();
        }

        HandlePinch();
    }
    NavButton[] GetActiveList()
    {
        bool mudMode = forceMudMode || (mud.storm && iM.GetDepth() < 0f);
        if (!mudMode) return null;

        if (callPopup != null && callPopup.activeSelf) return popupButtons;  // popup has priority
        if (menuRoot != null && menuRoot.activeSelf) return menuButtons;
        return null;
    }

    void HandlePinch()
    {
        if (hands == null) return;

        XRHand right = hands.rightHand;
        if (!right.isTracked ||
            !right.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out Pose thumb) ||
            !right.GetJoint(XRHandJointID.MiddleTip).TryGetPose(out Pose middle))
        {
            pinching = false;
            return;
        }

        float distance = Vector3.Distance(thumb.position, middle.position);

        if (!pinching && distance <= pinchStartDistance)
        {
            pinching = true;                  // pinch started
            pinchStartTime = Time.time;
            selectFired = false;
        }
        else if (pinching && distance >= pinchEndDistance)
        {
            pinching = false;                 // pinch released
            if (!selectFired) Next();         // it was a quick pinch -> move highlight
        }

        if (pinching && !selectFired && Time.time - pinchStartTime >= holdToSelectTime)
        {
            selectFired = true;               // held long enough -> press
            Press();
        }
    }

    void Next()
    {
        index = (index + 1) % current.Length;
        SetHighlight(current, index);
    }
    
    void Press()
    {
        XRSimpleInteractable b = current[index].button;
        if (b != null)
            b.selectEntered.Invoke(new SelectEnterEventArgs());   // same as poking it
    }

    void SetHighlight(NavButton[] list, int on)
    {
        if (list == null) return;
        for (int i = 0; i < list.Length; i++)
            if (list[i].highlight != null)
                list[i].highlight.SetActive(i == on);
    }



        
    
}
