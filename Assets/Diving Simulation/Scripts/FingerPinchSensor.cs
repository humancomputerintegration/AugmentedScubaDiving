using UnityEngine;

public class FingerPinchSensor : MonoBehaviour
{
    public Collider thumbCollider;
    public float doublePinchWindow = 0.5f;

    public event System.Action DoublePinched;
    private float lastPinchTime = -999f;

    void OnTriggerEnter(Collider other){
        if (other != thumbCollider) return;


        if (Time.time - lastPinchTime <= doublePinchWindow){

            lastPinchTime = -999f;            // reset so a third pinch starts fresh
            DoublePinched?.Invoke();
        }
        else{
            
            lastPinchTime = Time.time;
        }


    }
}
