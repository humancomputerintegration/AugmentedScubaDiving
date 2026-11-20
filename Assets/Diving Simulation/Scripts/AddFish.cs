using UnityEngine;
using System.Collections.Generic;

public class AddFish : MonoBehaviour
{
    [System.Serializable]
    public class FishType
    {
        public GameObject prefab;
        public int count = 10;
        public float swim_speed = 1.2f;
        public float model_turn = 180f;
    }

    public List<FishType> fish_types = new List<FishType>();
    public Vector2 area_size = new Vector2(60f, 60f);   // x = width (X), y = length (Z)
    public float water_height = 14f;
    public float min_water_depth = 3f;                  // only spots at least this deep
    [Range(0f, 0.5f)] public float size_variation = 0.2f;  // 0.2 = 80% to 120% of the normal size
    [Range(0f, 0.5f)] public float speed_variation = 0.2f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (FishType fish_type in fish_types)
        {
            if (!fish_type.prefab)
            {
                continue;
            }

            for (int i = 0; i < fish_type.count; i++)
            {
                Vector3 position = RandomFishPosition();
                GameObject fish = Instantiate(fish_type.prefab, position, Quaternion.identity, transform);
                fish.transform.localScale *= Random.Range(1f - size_variation, 1f + size_variation);
                FishSwim swim = fish.AddComponent<FishSwim>();
                swim.swim_speed = fish_type.swim_speed * Random.Range(1f - speed_variation, 1f + speed_variation);
                swim.model_turn = fish_type.model_turn;
                swim.water_height = water_height;
            }
        }

    }

    // Update is called once per frame
    // void Update()
    // {

    // }
    Vector3 RandomFishPosition()
    {
        Vector3 position = transform.position;

        for (int attempt = 0; attempt < 20; attempt++)
        {
            float x = transform.position.x + Random.Range(-area_size.x / 2f, area_size.x / 2f);
            float z = transform.position.z + Random.Range(-area_size.y / 2f, area_size.y / 2f);
            float ground = GroundHeight(new Vector3(x, 0f, z));

            if (ground < water_height - min_water_depth)
            {
                // Somewhere between just above the sand and just below the surface.
                float y = Random.Range(ground + 1f, water_height - 1f);
                return new Vector3(x, y, z);
            }
        }

        Debug.LogWarning("AddFish: couldn't find deep enough water.");
        return position;
    }

    float GroundHeight(Vector3 position)
    {
        Terrain terrain = Terrain.activeTerrain;
        if (!terrain) return transform.position.y;
        return terrain.SampleHeight(position) + terrain.transform.position.y;
    }
}

