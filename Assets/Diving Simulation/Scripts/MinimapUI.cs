using UnityEngine;

//Moves each map marker to match its object's real position on the terrain
//The map image is assumed to show exactly the whole terrain, north (+Z) up.

public class MinimapUI : MonoBehaviour
{
    [System.Serializable]
    public class MapMarker
    {
        public Transform target;          // the real object
        public RectTransform icon;        // its marker on the map
        public bool rotateWithTarget;     // turn the icon with the object's heading
    }

    public Terrain terrain;               // used in whole-terrain mode
    public RectTransform mapArea;         // MapImage
    public MapMarker[] markers;

    [Header("Follow mode")]
    public bool followPlayer = true;      // centre the map on the diver
    public Transform player;              // drag in: Main Camera
    public float viewRange = 150f;        // metres shown across the map's width

    private Vector3 worldMin;
    private Vector3 worldSize;

    void Start()
    {
        worldMin = terrain.transform.position;
        worldSize = terrain.terrainData.size;
    }

    void LateUpdate()
    {
        Vector2 mapSize = mapArea.rect.size;
        Vector2 half = mapSize * 0.5f;

        foreach (MapMarker m in markers)
        {
            if (m.target == null || m.icon == null) continue;

            Vector2 pos;

            if (followPlayer && player != null)
            {
                // offset from the diver in metres (x = east, z = north)
                Vector3 offset = m.target.position - player.position;

                // metres -> map units (same scale on both axes so distances stay true)
                float unitsPerMetre = mapSize.x / viewRange;
                pos = new Vector2(offset.x, offset.z) * unitsPerMetre;

                // keep far-away markers pinned to the edge of the map
                pos.x = Mathf.Clamp(pos.x, -half.x, half.x);
                pos.y = Mathf.Clamp(pos.y, -half.y, half.y);
            }
            else
            {
                // whole terrain mode (original behaviour)
                float u = Mathf.Clamp01((m.target.position.x - worldMin.x) / worldSize.x);
                float v = Mathf.Clamp01((m.target.position.z - worldMin.z) / worldSize.z);
                pos = new Vector2((u - 0.5f) * mapSize.x, (v - 0.5f) * mapSize.y);
            }

            m.icon.anchoredPosition = pos;

            if (m.rotateWithTarget)
                m.icon.localEulerAngles = new Vector3(0f, 0f, -m.target.eulerAngles.y);
        }
    }

}
