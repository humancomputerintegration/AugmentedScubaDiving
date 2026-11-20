using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Slider))]
public class BatteryLevel : MonoBehaviour
{
    private Slider slider;
    public InformationManager iM;

    public Text percentageText;
    public GameObject LowBatteryWarning;
    public AudioSource LowBatteryAlarm;
    public float lowBatteryValue = 15f;
    public Color lowColor = Color.red;

    private Image fillImage;
    private Color normalColor;
    private bool isLow;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        slider = GetComponent<Slider>();
        SetMaxBatteryLevel(iM.GetMaxBatteryLevel());
        fillImage = slider.fillRect.GetComponent<Image>();
        normalColor = fillImage.color;
        LowBatteryWarning.SetActive(false);

        
    }

    public void SetMaxBatteryLevel (float maxLevel){
        slider.maxValue = maxLevel;
        slider.value = maxLevel; // Start with max!

    }

    public void SetBatteryLevel (float level){
        slider.value = level;
    }



    // Update is called once per frame
    void Update()
    {
        SetBatteryLevel(iM.GetBatteryLevel());
        float batteryPercentage = iM.GetBatteryLevel() / iM.GetMaxBatteryLevel() * 100f;
        percentageText.text =  Mathf.RoundToInt(batteryPercentage)+"%";
        bool nowLow = batteryPercentage < lowBatteryValue;

        if(nowLow && !isLow){
            //only play the alarm once when battery drops lower than 15%
            fillImage.color = lowColor;
            LowBatteryWarning.SetActive(true);
            LowBatteryAlarm.Play();
        }

        if(!nowLow && isLow){
            //only play the alarm once when battery drops lower than 15%
            fillImage.color = normalColor;
             LowBatteryWarning.SetActive(false);
            LowBatteryAlarm.Stop();
        }

        isLow = nowLow;
    }
}
