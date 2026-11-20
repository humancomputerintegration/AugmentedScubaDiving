using UnityEngine;

public class CrewmateBoat : CrewmateBehavior
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public float up_down_height = 0.15f;
    public float up_down_duration = 3.5f;
    public float left_right_angle = 5f;
    public float left_right_duration = 2f;
    public float for_back_angle = 3f;
    public float for_back_duration = 5f;

    Vector3 start_pos;
    Quaternion start_rot;
    float phase;


    void Awake()
    {
        start_pos = transform.position;
        start_rot = transform.rotation;
        phase = seed * 2.3f; //random number
    }

    // Update is called once per frame
    void Update()
    {
        float t = Time.time + phase;
        float up_down = Mathf.Sin(t * Mathf.PI * 2f / up_down_duration) * up_down_height;
        float left_right = Mathf.Sin(t * Mathf.PI * 2f / left_right_duration) * left_right_angle;
        float for_back = Mathf.Sin(t * Mathf.PI * 2f / for_back_duration) * for_back_angle;
        transform.position = start_pos + Vector3.up * up_down;
        transform.rotation = start_rot * Quaternion.Euler(for_back, 0f, left_right);
    }
}
