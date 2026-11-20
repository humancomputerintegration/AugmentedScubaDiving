using UnityEngine;
public class Move : MonoBehaviour
{
    [SerializeField] float minRadius = 8f;
    [SerializeField] float maxRadius = 20f;
    [SerializeField] float minSpeed = 1.5f;
    [SerializeField] float maxSpeed = 3f;
    [SerializeField] float minSecondsBetweenChanges = 8f;
    [SerializeField] float maxSecondsBetweenChanges = 20f;
    [SerializeField] float modelYawOffset = 0f;

    System.Random rng;
    Vector3 center;
    Vector3 heading = Vector3.forward;
    float radius, speed, angle, direction, timer;

    void Start()
    {
        rng = new System.Random(GetInstanceID() ^ System.Environment.TickCount);
        Rigidbody body = GetComponent<Rigidbody>();
        if (body) body.isKinematic = true;
        Vector3 facing = transform.rotation * Quaternion.Euler(0f, -modelYawOffset, 0f) * Vector3.forward;
        facing.y = 0f;
        if (facing.sqrMagnitude > 1e-4f) heading = facing.normalized;

        NewCircle();
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f) NewCircle();
        angle += direction * speed / radius * Time.deltaTime;
        Vector3 position = transform.position;
        position.x = center.x + radius * Mathf.Cos(angle);
        position.z = center.z + radius * Mathf.Sin(angle);
        transform.position = position;
        heading = direction * new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle)); //face the way it's moving
        transform.rotation = Quaternion.LookRotation(heading) * Quaternion.Euler(0f, modelYawOffset, 0f);
    }

    void NewCircle()
    {
        direction = rng.Next(2) == 0 ? 1f : -1f;
        radius = RandomBetween(minRadius, maxRadius);
        speed = RandomBetween(minSpeed, maxSpeed);
        timer = RandomBetween(minSecondsBetweenChanges, maxSecondsBetweenChanges);
        angle = Mathf.Atan2(-direction * heading.x, direction * heading.z);
        Vector3 p = transform.position;
        center = new Vector3(p.x - radius * Mathf.Cos(angle), 0f, p.z - radius * Mathf.Sin(angle));
    }

    float RandomBetween(float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }
}