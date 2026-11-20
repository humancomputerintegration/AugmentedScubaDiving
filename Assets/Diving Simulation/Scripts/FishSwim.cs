using UnityEngine;

public class FishSwim : MonoBehaviour
{
    public float speed = 1.05f;
    public float turn = 1.5f;
    public float wander = 3f;
    public float height = 4f;
    public float climb = 20f;
    public bool straight = false;
    public Transform surface;
    public float margin = 1f;

    Vector3 home;
    Vector3 target;
    float currentSpeed;

    void Start()
    {
        home = transform.position;
        PickNewTarget();
    }

    void Update()
    {
        Vector3 toTarget = target - transform.position;

        if (toTarget.magnitude < 0.5f)
        {
            PickNewTarget();
            return;
        }

        // model faces backwards, so rotate it 180 degrees
        Quaternion look = Quaternion.LookRotation(toTarget) * Quaternion.Euler(0f, 180f, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, turn * Time.deltaTime);

        // fish visually faces -forward because of the 180 flip
        Vector3 pos = transform.position - transform.forward * currentSpeed * Time.deltaTime;

        if (surface != null && pos.y > surface.position.y - margin)
        {
            pos.y = surface.position.y - margin;
        }

        transform.position = pos;
    }

    void PickNewTarget()
    {
        Vector2 flat = Random.insideUnitCircle * wander;
        Vector3 next = home + new Vector3(flat.x, 0f, flat.y);

        float range = height;
        if (straight)
        {
            range = 0.3f;
        }

        // limit how steep the fish climbs or dives
        float flatDistance = Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z), new Vector3(next.x, 0f, next.z));
        float maxChange = Mathf.Tan(climb * Mathf.Deg2Rad) * flatDistance;
        float change = Mathf.Clamp(home.y + Random.Range(-range, range) - transform.position.y, -maxChange, maxChange);
        next.y = transform.position.y + change;

        if (surface != null && next.y > surface.position.y - margin)
        {
            next.y = surface.position.y - margin;
        }

        target = next;
        currentSpeed = speed * Random.Range(0.6f, 1.4f);
    }
}
