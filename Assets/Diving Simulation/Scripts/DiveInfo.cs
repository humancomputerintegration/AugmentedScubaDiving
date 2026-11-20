using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DiveInfo : MonoBehaviour
{
    public InformationManager iM;
    public Text depthText;
    public Text batteryText;   // InfoPanel > BatteryInfoText
    public Text oxygenText;    // InfoPanel > OxygenInfoText
    public Text diveTimeText;
    private float timeUnderwater = 0f;//in seconds

    // Update is called once per frame
    void Update()
    {
        // GetDepth() is negative underwater (e.g. -4.2 = 4.2 m below surface)
        float depth = iM.GetDepth();

        //depth

        //make the number positive
        float metres = Mathf.Max(0f, -depth);
        depthText.text = "Depth: " + metres.ToString("F1") + " m";

        //time underwater
        if(depth < 0f){
            timeUnderwater += Time.deltaTime;
        }
        else{
            // reset when surfaced (also after respawn)
            timeUnderwater = 0f;
        }
        int minutes = (int)(timeUnderwater / 60);
        int seconds = (int)(timeUnderwater % 60);
        diveTimeText.text = "Dive time: " + minutes.ToString("00") + ":" + seconds.ToString("00");

        if (batteryText != null){
            batteryText.text = "Battery: " + Mathf.RoundToInt(iM.GetBatteryLevel() / iM.GetMaxBatteryLevel() * 100f) + "%";
        }

        if (oxygenText != null){
            oxygenText.text = "Oxygen: " + Mathf.RoundToInt(iM.GetOxygenLevel() / iM.GetMaxOxygenLevel() * 100f) + "%";
        }


        

    }
}
