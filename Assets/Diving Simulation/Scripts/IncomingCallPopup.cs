using UnityEngine;
using UnityEngine.UI;

public class IncomingCallPopup : MonoBehaviour
{
    public SimpleDial dial;              // Spatial > Buttons
    public CallTowerManager ctm;         // Call Tower Manager
    public Transform head;               // Main Camera
    public GameObject popupRoot;         // IncomingCallPopup
    public Text callerText;              // PopupCanvas > CallerText

    public float distance = 0.4f;        // metres in front of the head
    public float drop = 0.02f;           // metres below eye level
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        popupRoot.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        bool ringing = dial.IsRinging();

        if (ringing && !popupRoot.activeSelf)
            Show();
        else if (!ringing && popupRoot.activeSelf)
            popupRoot.SetActive(false);   // answered/hung up elsewhere (e.g. dial pad)
        
    }

    void Show()
    {
        int freq = dial.GetIncomingFrequency();

        // find who is calling from their frequency
        string caller = "Unknown";
        foreach (CrewInfo info in ctm.GetCrewmatesInformation())
            if (info.frequency == freq) caller = info.name;

        callerText.text = "Incoming call\n" + caller + "  ·  Freq " + freq;

        // place it in front of the diver, facing them, and leave it there
        Vector3 lookFlat = head.forward;
        lookFlat.y = 0f;
        lookFlat.Normalize();
        popupRoot.transform.position = head.position + lookFlat * distance + Vector3.down * drop;
        popupRoot.transform.rotation = Quaternion.LookRotation(lookFlat);
        popupRoot.SetActive(true);
    }

    public void Answer()
    {
        dial.AnswerCall();
        popupRoot.SetActive(false);
    }

    // Wired to Btn_HangUp > Select Entered
    public void HangUp()
    {
        dial.HangUp();
        popupRoot.SetActive(false);
    }
}
