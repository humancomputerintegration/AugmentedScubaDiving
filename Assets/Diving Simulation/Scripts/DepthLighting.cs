using UnityEngine;

public class DepthLighting : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public InformationManager depth_info;
    public Light sun;
    public float max_depth = 14f;

    [Header("Bottom")]
    public Color deep_fog_color = new Color(0.03f, 0.10f, 0.20f);
    public float deep_fog_density = 0.11f;
    public Color deep_ambient_color = new Color(0.15f, 0.22f, 0.32f);
    [Range(0f, 1f)]
    public float deep_sun_strength = 0.3f;

    [Header("Smoothing")]
    public float change_speed = 2f;

    Color shallow_fog_color;
    float shallow_fog_density;
    Color shallow_ambient_color;
    float shallow_sun_intensity;

    float current_amount;
    void Start()
    {
        shallow_fog_color = RenderSettings.fogColor;
        shallow_fog_density = RenderSettings.fogDensity;
        shallow_ambient_color = RenderSettings.ambientLight;
        if (sun) shallow_sun_intensity = sun.intensity;
    }

    // Update is called once per frame
    void Update()
    {
        if (!depth_info)
        {
            return;
        }
        float depth = -depth_info.GetDepth();
        float wanted_amount = Mathf.Clamp01(depth / Mathf.Max(0.1f, max_depth));
        current_amount = Mathf.Lerp(current_amount, wanted_amount, change_speed * Time.deltaTime);

        RenderSettings.fogColor = Color.Lerp(shallow_fog_color, deep_fog_color, current_amount);
        RenderSettings.fogDensity = Mathf.Lerp(shallow_fog_density, deep_fog_density, current_amount);
        RenderSettings.ambientLight = Color.Lerp(shallow_ambient_color, deep_ambient_color, current_amount);
        if (sun)
        {
            float deep_intensity = shallow_sun_intensity * deep_sun_strength;
            sun.intensity = Mathf.Lerp(shallow_sun_intensity, deep_intensity, current_amount);
        }
    }

    void OnDisable()
    {
        RenderSettings.fogColor = shallow_fog_color;
        RenderSettings.fogDensity = shallow_fog_density;
        RenderSettings.ambientLight = shallow_ambient_color;
        if (sun) sun.intensity = shallow_sun_intensity;
    }
}
