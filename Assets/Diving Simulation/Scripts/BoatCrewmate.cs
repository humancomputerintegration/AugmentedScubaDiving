using UnityEngine;

public class BoatCrewmate : CrewmateBehavior
{
    public Transform surface;
    public float sink = 0f;
    public float speed = 0.5f;
    public float turn = 0.3f;
    public float wander = 10f;
    public float bob = 0.15f;
    public float rock = 3f;
    public float facing = 0f;

    Vector3 home;
    Vector3 target;
    float waveOffset;

    protected override void Start()
    {
        base.Start();
        home = transform.position;
        waveOffset = Random.Range(0f, 10f);
        PickNewTarget();
    }

    void Update()
    {
        Vector3 toTarget = target - transform.position;
        toTarget.y = 0f;

        if (toTarget.magnitude < 1f)
        {
            PickNewTarget();
            return;
        }

        // slowly turn toward the target
        Quaternion look = Quaternion.LookRotation(toTarget) * Quaternion.Euler(0f, facing, 0f);
        float yaw = Quaternion.Slerp(Quaternion.Euler(0f, transform.eulerAngles.y, 0f), look, turn * Time.deltaTime).eulerAngles.y;

        // move forward along the direction the boat is heading
        Vector3 heading = Quaternion.Euler(0f, yaw - facing, 0f) * Vector3.forward;
        Vector3 pos = transform.position + heading * speed * Time.deltaTime;

        // float on the water surface and bob up and down
        float waterY = home.y;
        if (surface != null)
        {
            waterY = surface.position.y;
        }
        float t = Time.time + waveOffset;
        pos.y = waterY - sink + Mathf.Sin(t * 1.2f) * bob;
        transform.position = pos;

        // gentle rocking side to side and front to back
        float roll = Mathf.Sin(t * 0.9f) * rock;
        float pitch = Mathf.Sin(t * 0.7f) * rock * 0.5f;
        transform.rotation = Quaternion.Euler(pitch, yaw, roll);
    }

    void PickNewTarget()
    {
        Vector2 flat = Random.insideUnitCircle * wander;
        target = home + new Vector3(flat.x, 0f, flat.y);
    }
}
