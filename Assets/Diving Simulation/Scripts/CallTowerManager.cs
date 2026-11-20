using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.SceneManagement;

public class CallTowerManager : MonoBehaviour
{

    [Header("Crewmate Information")]
    public Transform[] crewmates;

    private int transmitterFrequencyRangeMin = 100;
    private int transmitterFrequencyRangeMax = 1000;

    public int[] crewmateFrequencies;

    //assign frequency to each crew member
    public bool useFixedFrequencies = true;
    public int[] fixedFrequencies = { 111, 222, 333 };


    public AudioClip wrongFrequency;
    public AudioClip emergencyAudio;
    public float min_wait = 10f;
    public float max_wait = 120f;

    private int emergencyFrequency = 1001;

    public SimpleDial playerTransmitter;

    // Start is called before the first frame update
    void Start()
    {
        /*
        if (crewmates.Length > 0)
            InstantiateCrewmateFrequencies();
        */
        if (crewmates.Length == 0) return;
        if (useFixedFrequencies && fixedFrequencies.Length == crewmates.Length)
            AssignFixedFrequencies();
        else
            InstantiateCrewmateFrequencies();

    }


    /*
     * INSTANTIATION FUNCTIONS
     */
    void InstantiateCrewmateFrequencies()
    {
        crewmateFrequencies = new int[crewmates.Length];
        crewmateFrequencies[0] = (int)UnityEngine.Random.Range(transmitterFrequencyRangeMin, transmitterFrequencyRangeMax);
        crewmates[0].GetComponent<CrewmateBehavior>().SetFrequency(crewmateFrequencies[0]);

        for (int c = 1; c < crewmates.Length; c++)
        {
            int newFrequency = 0;
            bool uniqueNumber = false;

            // Check if we are setting to a unique frequency.
            while (!uniqueNumber)
            {
                newFrequency = (int)UnityEngine.Random.Range(transmitterFrequencyRangeMin, transmitterFrequencyRangeMax);

                uniqueNumber = true;
                foreach (int f in crewmateFrequencies)
                {
                    uniqueNumber = uniqueNumber && (newFrequency != f);
                }
            }

            // Once unique, assign.
            crewmateFrequencies[c] = newFrequency;
            crewmates[c].GetComponent<CrewmateBehavior>().SetFrequency(newFrequency);
        }
    }

    void AssignFixedFrequencies(){
        crewmateFrequencies = new int[crewmates.Length];
        for (int c = 0; c < crewmates.Length; c++){
            crewmateFrequencies[c] = fixedFrequencies[c];
            crewmates[c].GetComponent<CrewmateBehavior>().SetFrequency(fixedFrequencies[c]);
        }
    }

    /*
     * INFORMATION FUNCTIONS
     */
    public CrewInfo[] GetCrewmatesInformation()
    {
        CrewInfo[] returnInfo = new CrewInfo[crewmates.Length];
        for(int c = 0; c < crewmates.Length; c++)
        {
            Transform currentCrewmate = crewmates[c];
            returnInfo[c] = new CrewInfo(currentCrewmate.name, currentCrewmate.position, crewmateFrequencies[c]);
        }
        return returnInfo;
    }

    public int GetFrequencyMax()
    {
        return transmitterFrequencyRangeMax;
    }

    public int GetFrequencyMin()
    {
        return transmitterFrequencyRangeMin;
    }

    public int GetEmergencyFrequency()
    {
        return emergencyFrequency;
    }


    /*
     * CALL FUNCTIONS
     */
    public AudioClip CallCrewmate(int frequency)
    {
        if (frequency == emergencyFrequency)
        {
            StartCoroutine(Emergency());
            return emergencyAudio;
        }
        if (crewmateFrequencies.Contains(frequency))
        {
            return crewmates[Array.IndexOf(crewmateFrequencies, frequency)].GetComponent<CrewmateBehavior>().callAudioReceiver;
        }
        return wrongFrequency;
    }

    public bool CallPlayer(AudioClip call, int freq)
    {
        return playerTransmitter.IncomingCall(call, freq);
    }

    /*
    public AudioClip ReceiveRandomCrewmateCall()
    {
        return crewmates[(int)UnityEngine.Random.Range(0, crewmates.Count)].GetComponent<CrewmateBehavior>().callAudioCaller;
    }
    */

    IEnumerator Emergency ()
    {
        yield return new WaitForSeconds(emergencyAudio.length + 0.5f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
