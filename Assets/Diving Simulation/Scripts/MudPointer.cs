using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/*
In a mud storm, show a white dot where the diver is looking/pointer, double pinch (index-thumb) to select

*/

public class MudPointer : MonoBehaviour
{
    public MudStorm mud;                      
    public InformationManager iM;             
    public Transform head;                   
    public Transform dot;                    
    public FingerPinchSensor[] pinchSensors; 

    public float maxDistance = 3f;            // how far the pointer reaches (metres)
    public float restDistance = 0.5f;         // where the dot floats when not on a button
    public float dotSize = 0.02f;             // apparent size (0.02 = 2 cm at 1 m)

    public bool forceMudMode = false;
    public Key testSelectKey = Key.Enter;
    private XRSimpleInteractable target;


    void OnEnable(){

        foreach (FingerPinchSensor s in pinchSensors){
            if (s != null) s.DoublePinched += Select;
        }

    }

    void OnDisable(){
        foreach (FingerPinchSensor s in pinchSensors)
            if (s != null) s.DoublePinched -= Select;
    }

    
    void Start()
    {
        dot.gameObject.SetActive(false);
        
    }

    void Update(){
        bool mudMode = forceMudMode || (mud.storm && iM.GetDepth() < 0f);

        if (dot.gameObject.activeSelf != mudMode)
            dot.gameObject.SetActive(mudMode);
        
        if (!mudMode){
            target = null;
            return;

        }

        //raycasting
        /*
        Find the nearest button along the line of sight. All layers are included because the menu is on the Ignore Raycast layer.

        */
        Ray ray = new Ray(head.position, head.forward);
        target = null;
        float best = maxDistance;

        foreach (RaycastHit hit in Physics.RaycastAll(ray, maxDistance, ~0, QueryTriggerInteraction.Ignore)){

            XRSimpleInteractable button = hit.collider.GetComponentInParent<XRSimpleInteractable>();
            if (button != null && hit.distance < best){

                best = hit.distance;
                target = button;

            }

        }
        float distance = target != null ? best : restDistance;
        dot.position = ray.GetPoint(distance - 0.005f);     // just in front of the button
        dot.localScale = Vector3.one * dotSize * distance;  // same apparent size at any distance

        if (Keyboard.current != null && Keyboard.current[testSelectKey].wasPressedThisFrame){
            Select();
        }
    }
    void Select()
    {
        if (!dot.gameObject.activeSelf || target == null) return;
        target.selectEntered.Invoke(new SelectEnterEventArgs());   // same as poking it

    }
}
