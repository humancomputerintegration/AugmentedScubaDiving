using UnityEngine;

public class SeabedLimit : MonoBehaviour
{
    public Terrain terrain;
    public float clearance = 0.2f;

    Rigidbody rb;


    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
    }

   
    void LateUpdate()
    {
        if (terrain == null) return;

        Vector3 p = transform.position;
        float floor = terrain.SampleHeight(p) + terrain.transform.position.y + clearance;

        if (p.y < floor){
            p.y = floor;
            transform.position = p;

            // stop gravity from building up downward speed while resting on the bottom
            if (rb != null && rb.linearVelocity.y < 0f){
                Vector3 v = rb.linearVelocity;
                v.y = 0f;
                rb.linearVelocity = v;

            }



        }
 

        
    }
}
