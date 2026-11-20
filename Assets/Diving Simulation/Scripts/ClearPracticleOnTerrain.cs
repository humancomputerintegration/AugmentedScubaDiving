using UnityEngine;

public class ClearPracticleOnTerrain : MonoBehaviour
{
    public InformationManager depth_info;
    ParticleSystem particles;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        particles = GetComponent<ParticleSystem>();
        if (!depth_info)
        {
            depth_info = FindFirstObjectByType<InformationManager>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!particles || !depth_info)
        {
            return;
        }

        bool underwater = depth_info.GetDepth() < 0f;

        if (underwater && !particles.isPlaying)
        {
            particles.Play();
        }
        else if (!underwater && particles.isPlaying)
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}