using System.Collections.Generic;
using UnityEngine;

public class AddBoat : MonoBehaviour
{
    public GameObject boatPrefab;
    public CallTowerManager ct;
    public List<string> crewmate_name = new List<string> { "Yudai", "Yun", "Kenny", "Hannah" };
    public List<AudioClip> callerClips = new List<AudioClip>();
    public List<AudioClip> receiverClips = new List<AudioClip>();
    public Vector2 area_size = new Vector2(200f, 200f);
    public float water_height = 13.93564f;
    public float min_distance = 15f;

    public float min_water_depth = 1f;

    void Awake()
    {
        List<Transform> all_crewmates = new List<Transform>();

        //add all the existing crewmates
        if (ct.crewmates != null)
        {
            all_crewmates.AddRange(ct.crewmates);
        }

        for (int i = 0; i < crewmate_name.Count; i++)
        {
            Vector3 position = RandomBoatPosition(all_crewmates);
            Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            GameObject boat = Instantiate(boatPrefab, position, rotation);
            boat.name = crewmate_name[i];
            boat.SetActive(false);

            CrewmateBoat crewmate = boat.AddComponent<CrewmateBoat>();
            crewmate.ctm = ct;
            crewmate.seed = RandomSeed(all_crewmates);
            if (i < callerClips.Count) crewmate.callAudioCaller = callerClips[i];
            if (i < receiverClips.Count) crewmate.callAudioReceiver = receiverClips[i];

            boat.SetActive(true);

            all_crewmates.Add(boat.transform);
        }
        ct.crewmates = all_crewmates.ToArray();
    }

    //Picks a random seed that no crewmate is using yet.
    int RandomSeed(List<Transform> crewmates)
    {
        int new_seed = 0;

        for (int attempt = 0; attempt < 100; attempt++)
        {
            new_seed = Random.Range(0, 1000); //0 to 999

            if (!SeedUsed(new_seed, crewmates))
            {
                return new_seed;
            }
        }

        Debug.LogWarning("AddBoat: couldn't find an unused seed.");
        return new_seed;
    }

    //True if any crewmate in the list already has this seed.
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

    Vector3 RandomBoatPosition(List<Transform> boats)
    {
        Vector3 position = transform.position;

        for (int attempt = 0; attempt < 20; attempt++)
        {
            float x = transform.position.x + Random.Range(-area_size.x / 2f, area_size.x / 2f);
            float z = transform.position.z + Random.Range(-area_size.y / 2f, area_size.y / 2f);
            position = new Vector3(x, water_height, z);

            if (IsDeepEnough(position) && NoBoatNear(position, boats))
            {
                return position;
            }
        }
        Debug.LogWarning("AddBoat: couldn't find a free spot.");
        return position;
    }

    bool NoBoatNear(Vector3 position, List<Transform> boats)
    {
        foreach (Transform boat in boats)
        {
            if (!boat) continue;

            Vector3 offset = boat.position - position;
            offset.y = 0f;
            if (offset.magnitude < min_distance)
            {
                return false;
            }
        }
        return true;
    }

    bool IsDeepEnough(Vector3 position)
    {
        Terrain terrain = Terrain.activeTerrain;
        if (!terrain) return true;
        float ground = terrain.SampleHeight(position) + terrain.transform.position.y;
        return ground < water_height - min_water_depth;
    }
}