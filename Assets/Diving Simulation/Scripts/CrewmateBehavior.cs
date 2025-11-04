using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrewmateBehavior : MonoBehaviour
{
    
    public AudioClip callAudioReceiver;
    public AudioClip callAudioCaller;
    private int frequency;
    public int seed;

    //private delays, do not change
    private float min_wait;
    private float max_wait;

    public CallTowerManager ctm;

    // Start is called before the first frame update
    void Start()
    {
        Random.InitState(seed);
        min_wait = ctm.min_wait;
        max_wait = ctm.max_wait;
        StartCoroutine(RandomCall());
    }

    void Update()
    {
        //count frames for calls? StartCoroutine(RandomCall());
    }

    public void SetFrequency(int f)
    {
        frequency = f;
    }

    IEnumerator RandomCall ()
    {
        yield return new WaitForSeconds(Random.Range(min_wait, max_wait));//10-120
        Debug.Log("couroutine");
        if (ctm.playerTransmitter.IncomingCall(callAudioCaller, frequency)) // If call failed, restart
        {
            Debug.Log("Call attempted!");
            StartCoroutine(RandomCall());
        }
        Debug.Log("Call made for: " + frequency);
        yield return new WaitForSeconds(Random.Range(min_wait, max_wait));//10-120
        Debug.Log("recall: " + frequency);
        StartCoroutine(RandomCall());
    }
}
