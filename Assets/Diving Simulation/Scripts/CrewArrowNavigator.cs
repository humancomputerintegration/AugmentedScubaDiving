using UnityEngine;
using UnityEngine.UI;

public class CrewArrowNavigator : MonoBehaviour
{
    public CallTowerManager ctm;
    public Transform head; //main camera
    public GameObject arrowGroup;
    public RectTransform arrow;
    public Text arrowLabel;

    public float arriveDistance = 10f;// metres; closer than this = arrived

    private Transform target;// crewmate we're guiding to (null = none)



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        arrowGroup.SetActive(false);
    }
    
    //if click on a crew mate's name, find them and show the navigation arrow
    public void NavigateTo(string crewName){
        foreach (Transform crewmate in ctm.crewmates){
            if (crewmate.name == crewName){
                target = crewmate;
                CancelInvoke("HideArrow");      // cancel a pending "Arrived" hide
                arrowGroup.SetActive(true);
                return;

            }
        }
        Debug.LogWarning("CrewArrowNavigator: no crewmate named " + crewName);

    }

    //if arrived, stop navigation
    public void StopNavigation(){
        target = null;
        HideArrow();
    }

    public void HideArrow(){
        arrowGroup.SetActive(false);
    }

    // Update is called once per frame
    // while guiding, rotate the arrow and update the distance.
    void Update()
    {
        if (target == null) return;

        //need to flat 3d into 2d
        //Flatten both directions onto the horizontal plane, so looking up/down doesn't affect the arrow and boat height is ignored.
        Vector3 toTarget = target.position - head.position;
        toTarget.y = 0f;
        Vector3 lookDir = head.forward;
        lookDir.y = 0f;

        float distance = toTarget.magnitude;

        //arrived
        if (distance <= arriveDistance){

            arrowLabel.text = "Arrived at " + target.name;
            arrow.localEulerAngles = Vector3.zero;
            target = null;
            Invoke("HideArrow", 3f);
            return;
        }

        //Angle from where we look to the target, in degrees (-180..180).Positive = target is to the right.
        float angle = Vector3.SignedAngle(lookDir, toTarget, Vector3.up);

        // UI rotates counter-clockwise for positive Z, so negate it:target on the right -> arrow tilts clockwise (points right).
        arrow.localEulerAngles = new Vector3(0f, 0f, -angle);

        arrowLabel.text = target.name + " · " + distance.ToString("F0") + " m";


    }
}
