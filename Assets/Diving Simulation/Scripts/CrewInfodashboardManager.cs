using UnityEngine;
using UnityEngine.UI;


public class CrewInfodashboardManager : MonoBehaviour
{
    public CallTowerManager ctm;
    public Transform player;

    public Text infoHint;
    public Text nameText;
    public Text locationText;
    public Text frequencyText;

    private string selectedName = "";


    //runs every time the panel is opened
    //clears the previous selection so the panel starts fresh

    void onEnable(){
        selectedName = "";
        nameText.text = "Name: ";
        locationText.text = "Location: ";
        frequencyText.text =  "frequency: ";
        infoHint.gameObject.SetActive(true);

    }

    //remember who was selected and show their info.
    public void ShowCrewmate (string crewName){
        selectedName = crewName;
        RefreshInfo();

    }

    // Update is called once per frame
    void Update()
    {
        if (selectedName != "")
            RefreshInfo();
        
    }

    // once a crew name is selected, refresh the info to the corresponding crew mate
    void RefreshInfo(){

        CrewInfo[] crew = ctm.GetCrewmatesInformation();

        foreach (CrewInfo info in crew){
            if(info.name == selectedName){
                nameText.text = "Name: " + info.name;
                string location = "Location: " + info.worldLocation.ToString("F0");

                //add distance as well
                if (player != null){
                    float distance = Vector3.Distance(player.position, info.worldLocation);
                    location += "\n" + distance.ToString("F0") + " m away";
                }

                locationText.text = location;
                frequencyText.text = "Frequency: " + info.frequency;
                infoHint.gameObject.SetActive(false);
                return;
            }
        }
        Debug.LogWarning("CrewInfoManager: no crewmate named " + selectedName);
        selectedName = "";

    }
}
