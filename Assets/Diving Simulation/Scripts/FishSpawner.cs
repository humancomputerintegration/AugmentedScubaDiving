using UnityEngine;
using UnityEngine.Serialization;

public class FishSpawner : MonoBehaviour
{
    [FormerlySerializedAs("fishPrefabs")] public GameObject[] prefabs;
    [FormerlySerializedAs("fishCount")] public int count = 15;
    [FormerlySerializedAs("areaSize")] public Vector3 area = new Vector3(10f, 3f, 10f);
    [FormerlySerializedAs("fishSize")] public float size = 0.75f;
    [FormerlySerializedAs("swimSpeed")] public float speed = 1.05f;
    [FormerlySerializedAs("wanderRadius")] public float wander = 3f;
    [FormerlySerializedAs("verticalRange")] public float height = 4f;
    [FormerlySerializedAs("maxClimbAngle")] public float climb = 20f;
    [FormerlySerializedAs("straightSwimmerFraction")] public float straight = 0.3f;
    [FormerlySerializedAs("waterSurface")] public Transform surface;
    [FormerlySerializedAs("surfaceMargin")] public float margin = 1f;

    void Start()
    {
        for (int i = 0; i < count; i++)
        {
            GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];

            Vector3 pos = transform.position + new Vector3(
                Random.Range(-0.5f, 0.5f) * area.x,
                Random.Range(-0.5f, 0.5f) * area.y,
                Random.Range(-0.5f, 0.5f) * area.z);

            if (surface != null && pos.y > surface.position.y - margin)
            {
                pos.y = surface.position.y - margin;
            }

            Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            GameObject fish = Instantiate(prefab, pos, rot, transform);
            fish.transform.localScale *= size;

            FishSwim swim = fish.AddComponent<FishSwim>();
            swim.speed = speed;
            swim.wander = wander;
            swim.height = height;
            swim.climb = climb;
            swim.straight = Random.value < straight;
            swim.surface = surface;
            swim.margin = margin;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, area);
    }
}
