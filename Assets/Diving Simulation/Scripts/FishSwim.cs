using UnityEngine;

public class FishSwim : MonoBehaviour
{
    [Header("Swimming")]
    public float swim_speed = 1.2f;
    public float turn_speed = 60f;
    public float wander_radius = 20f;

    [Header("Water")]
    public float water_height = 14f;
    public float min_height_above_ground = 0.8f;
    public float surface_margin = 1f;

    [Header("Model")]
    [Tooltip("Turns the model so its head points forward. 0 if the head points along +Z, 180 if along -Z.")]
    public float model_turn = 180f;
    public float wiggle_angle = 8f;
    public float wiggle_speed = 6f;

    [Header("Avoiding rocks")]
    public float body_radius = 0.3f;
    public float look_ahead = 1.5f;

    Vector3 start_pos;
    Vector3 target;
    Vector3 heading;
    float wiggle_phase;

    void Start()
    {
        start_pos = transform.position;
        heading = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward;
        wiggle_phase = Random.Range(0f, 10f);
        target = PickNewTarget();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 to_target = target - transform.position;
        if (to_target.magnitude < 1f)
        {
            target = PickNewTarget();
            to_target = target - transform.position;
        }
        float max_turn = turn_speed * Mathf.Deg2Rad * Time.deltaTime;
        heading = Vector3.RotateTowards(heading, to_target.normalized, max_turn, 0f).normalized;


        if (PathBlocked(transform.position, transform.position + heading * look_ahead))
        {
            target = PickNewTarget();
        }

        Vector3 position = transform.position + heading * swim_speed * Time.deltaTime;
        float lowest = GroundHeight(position) + min_height_above_ground;
        float highest = water_height - surface_margin;
        if (position.y < lowest) position.y = lowest;
        if (position.y > highest) position.y = highest;
        transform.position = position;

        // Face where it's swimming, plus a side-to-side wiggle.
        float wiggle = Mathf.Sin(Time.time * wiggle_speed * swim_speed + wiggle_phase) * wiggle_angle;
        transform.rotation = Quaternion.LookRotation(heading) * Quaternion.Euler(0f, model_turn + wiggle, 0f);
    }

    Vector3 PickNewTarget()
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector2 offset = Random.insideUnitCircle * wander_radius;
            Vector3 spot = new Vector3(start_pos.x + offset.x, 0f, start_pos.z + offset.y);

            float lowest = GroundHeight(spot) + min_height_above_ground;
            float highest = water_height - surface_margin;
            if (highest <= lowest)
            {
                continue;
            }

            spot.y = Random.Range(lowest, highest);
            if (!PathBlocked(transform.position, spot))
            {
                return spot;
            }
        }

        return start_pos;
    }

    // True if a rock (anything solid that isn't the sand or this fish) is between the two points.
    bool PathBlocked(Vector3 from, Vector3 to)
    {
        Vector3 line = to - from;
        float distance = line.magnitude;
        if (distance < 0.01f) return false;

        RaycastHit[] hits = Physics.SphereCastAll(from, body_radius, line / distance, distance,
                                                  ~0, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider is TerrainCollider) continue;            // the sand is handled by GroundHeight
            if (hit.collider.transform.IsChildOf(transform)) continue; // this fish itself
            if (hit.distance <= 0f) continue;                          // already inside it: let it swim out
            return true;
        }
        return false;
    }

    float GroundHeight(Vector3 position)
    {
        Terrain terrain = Terrain.activeTerrain;
        if (!terrain) return transform.position.y - 10f;
        return terrain.SampleHeight(position) + terrain.transform.position.y;
    }
}

