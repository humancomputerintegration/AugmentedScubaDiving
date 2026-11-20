using System.Collections.Generic;
using UnityEngine;

public class AddDiver : MonoBehaviour
{
    public GameObject diverPrefab;
    public CallTowerManager ct;
    public List<string> diver_name = new List<string> { "Leon", "Justin", "Jiawen", "Wen" };
    public List<AudioClip> callerClips = new List<AudioClip>();
    public List<AudioClip> receiverClips = new List<AudioClip>();
    public Vector2 area_size = new Vector2(60f, 60f);
    public float water_height = 14f;
    public float min_water_depth = 3f;
    public float min_distance = 6f;
    void Awake()
    {
        List<Transform> all_crewmates = new List<Transform>();
        if (ct.crewmates != null)
        {
            all_crewmates.AddRange(ct.crewmates);
        }

        for (int i = 0; i < diver_name.Count; i++)
        {
            Vector3 position = RandomDiverPosition(all_crewmates);
            Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            GameObject diver = Instantiate(diverPrefab, position, rotation);
            diver.name = diver_name[i];

            CrewmateDiver crewmate = diver.GetComponent<CrewmateDiver>();
            if (!crewmate)
            {
                Debug.LogWarning("AddDiver: the diver prefab has no CrewmateDiver on it.");
                Destroy(diver);
                continue;
            }

            crewmate.ctm = ct;
            crewmate.seed = RandomSeed(all_crewmates);
            crewmate.water_height = water_height;
            if (i < callerClips.Count) crewmate.callAudioCaller = callerClips[i];
            if (i < receiverClips.Count) crewmate.callAudioReceiver = receiverClips[i];

            all_crewmates.Add(diver.transform);
        }
        ct.crewmates = all_crewmates.ToArray();
    }


    Vector3 RandomDiverPosition(List<Transform> crewmates)
    {
        Vector3 position = transform.position;

        for (int attempt = 0; attempt < 30; attempt++)
        {
            float x = transform.position.x + Random.Range(-area_size.x / 2f, area_size.x / 2f);
            float z = transform.position.z + Random.Range(-area_size.y / 2f, area_size.y / 2f);
            position = new Vector3(x, 0f, z);
            position.y = GroundHeight(position);

            bool deep_enough = position.y < water_height - min_water_depth;
            if (deep_enough && NoCrewmateNear(position, crewmates))
            {
                return position;
            }
        }

        Debug.LogWarning("AddDiver: couldn't find a free underwater spot.");
        return position;
    }

    bool NoCrewmateNear(Vector3 position, List<Transform> crewmates)
    {
        foreach (Transform crewmate in crewmates)
        {
            if (!crewmate) continue;

            Vector3 offset = crewmate.position - position;
            offset.y = 0f;
            if (offset.magnitude < min_distance)
            {
                return false;
            }
        }
        return true;
    }

    int RandomSeed(List<Transform> crewmates)
    {
        int new_seed = 0;

        for (int attempt = 0; attempt < 100; attempt++)
        {
            new_seed = Random.Range(0, 1000);

            if (!SeedUsed(new_seed, crewmates))
            {
                return new_seed;
            }
        }
        Debug.LogWarning("AddDiver: couldn't find an unused seed.");
        return new_seed;
    }

    bool SeedUsed(int seed, List<Transform> crewmates)
    {
        foreach (Transform crewmate in crewmates)
        {
            if (!crewmate) continue;

            CrewmateBehavior behavior = crewmate.GetComponent<CrewmateBehavior>();
            if (behavior && behavior.seed == seed)
            {
                return true;
            }
        }
        return false;
    }

    float GroundHeight(Vector3 position)
    {
        Terrain terrain = Terrain.activeTerrain;
        if (!terrain) return transform.position.y;
        return terrain.SampleHeight(position) + terrain.transform.position.y;
    }
}