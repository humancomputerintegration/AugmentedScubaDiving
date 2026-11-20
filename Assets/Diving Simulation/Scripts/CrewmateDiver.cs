using UnityEngine;

public class CrewmateDiver : CrewmateBehavior
{
    [Header("Walking")]
    public float walk_speed = 0.8f;
    public float turn_speed = 90f;
    public float arrive_distance = 0.5f;

    [Header("Where to walk")]
    public float wander_radius = 15f;
    public float water_height = 14f;
    public float min_water_depth = 2f;

    [Header("Changing depth")]
    public float vertical_speed = 0.4f;
    public float min_height_above_ground = 1f;
    public float surface_margin = 1.5f;
    [Range(0f, 1f)]
    public float chance_on_ground = 0.3f;

    [Header("Stopping")]
    public float pause_min = 2f;
    public float pause_max = 5f;

    [Header("Avoiding rocks")]
    public float body_radius = 0.5f;           // how wide the diver is, for checking if it fits past rocks
    public float chest_height = 1f;            // checks start this far above the feet, so bumps in the sand don't count
    public float look_ahead = 1f;              // while walking, checks this far in front for rocks
    public LayerMask obstacle_layers = ~0;     // which layers count as obstacles (everything by default)


    Vector3 start_pos;
    Vector3 target;
    bool walking;
    float pause_timer;
    Animator animator;

    void Awake()
    {
        start_pos = transform.position;

        //The Animator is on the Diver model inside this object, not on this object itself.
        animator = GetComponentInChildren<Animator>();
        if (!animator) Debug.LogWarning("CrewmateDiver: no Animator found on " + name + " or its children.");
        walking = false;
        pause_timer = Random.Range(pause_min, pause_max);
    }

    void Update()
    {
        if (!walking)
        {
            SetWalkingAnimation(false);

            pause_timer -= Time.deltaTime;
            if (pause_timer <= 0f)
            {
                target = PickNewTarget();
                walking = true;
            }
            return;
        }

        Vector3 direction = target - transform.position;
        float height_left = direction.y;
        direction.y = 0f;

        bool arrived_plane = direction.magnitude < arrive_distance;
        bool arrived_height = Mathf.Abs(height_left) < 0.1f;

        if (arrived_plane && arrived_height)
        {
            walking = false;
            pause_timer = Random.Range(pause_min, pause_max);
            SetWalkingAnimation(false);
            return;
        }

        Vector3 position = transform.position;

        //Horizontal
        if (!arrived_plane)
        {
            Quaternion wanted = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, wanted, turn_speed * Time.deltaTime);
            float facing = Vector3.Dot(transform.forward, direction.normalized);
            float speed = walk_speed * Mathf.Clamp01(facing);
            position += transform.forward * speed * Time.deltaTime;
        }

        //Vertical
        position.y = Mathf.MoveTowards(position.y, target.y, vertical_speed * Time.deltaTime);

        //Never go into the sand, and never come out of the water.
        float ground = GroundHeight(position);
        float ceiling = water_height - surface_margin;
        if (position.y < ground) position.y = ground;
        Vector3 step = position - transform.position;
        if (step.sqrMagnitude > 0.000001f)
        {
            Vector3 ahead = transform.position + step.normalized * look_ahead;
            if (PathBlocked(transform.position, ahead))
            {
                walking = false;
                pause_timer = Random.Range(0.5f, 1.5f);
                SetWalkingAnimation(false);
                return;
            }
        }
        if (position.y > ceiling) position.y = ceiling;

        transform.position = position;

        SetWalkingAnimation(true);
    }

    // A random spot near the start that is underwater, either on the sand or floating in the water.
    // Tries 10 times, then goes back to the start.
    Vector3 PickNewTarget()
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector2 offset = Random.insideUnitCircle * wander_radius;
            Vector3 spot = new Vector3(start_pos.x + offset.x, 0f, start_pos.z + offset.y);
            float ground = GroundHeight(spot);

            if (ground < water_height - min_water_depth)
            {
                spot.y = PickHeight(ground);
                if (!PathBlocked(transform.position, spot))
                {
                    return spot;
                }
            }
        }

        return start_pos;
    }

    // On the sand, or somewhere between just above the sand and just below the surface.
    float PickHeight(float ground)
    {
        if (Random.value < chance_on_ground) return ground;

        float lowest = ground + min_height_above_ground;
        float highest = water_height - surface_margin;
        if (highest <= lowest) return ground;
        return Random.Range(lowest, highest);
    }

    float GroundHeight(Vector3 position)
    {
        Terrain terrain = Terrain.activeTerrain;
        if (!terrain) return transform.position.y;
        return terrain.SampleHeight(position) + terrain.transform.position.y;
    }

    void SetWalkingAnimation(bool is_walking)
    {
        if (!animator) return;
        animator.SetBool("isWalking", is_walking);
        animator.SetBool("isRunning", false);
    }

    bool PathBlocked(Vector3 from, Vector3 to)
    {
        // Check at chest height, so small bumps in the sand don't count.
        Vector3 start = from + Vector3.up * chest_height;
        Vector3 end = to + Vector3.up * chest_height;
        Vector3 line = end - start;
        float distance = line.magnitude;
        if (distance < 0.01f) return false;

        // A "thick ray" as wide as the diver, so it also notices rocks it would brush past.
        RaycastHit[] hits = Physics.SphereCastAll(start, body_radius, line / distance, distance,
                                                  obstacle_layers, QueryTriggerInteraction.Ignore);

        foreach (RaycastHit hit in hits)
        {
            if (IsObstacle(hit)) return true;
        }
        return false;
    }

    bool IsObstacle(RaycastHit hit)
    {
        // The sand is handled by GroundHeight, so it's not an obstacle.
        if (hit.collider is TerrainCollider) return false;

        // This diver's own body.
        if (hit.collider.transform.IsChildOf(transform)) return false;

        // Already touching it when the check started (for example, stuck inside a rock):
        // ignore it, so the diver can still walk out.
        if (hit.distance <= 0f) return false;

        return true;
    }
}