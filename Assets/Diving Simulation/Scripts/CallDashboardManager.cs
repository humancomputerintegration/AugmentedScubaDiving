using UnityEngine;
using UnityEngine.UI;

public class CallDashboardManager : MonoBehaviour
{ 
    public SimpleDial dial;
    public CallTowerManager ctm;
    public Text numberDisplay;
    public int maxDigits = 3;
    private string dialed = "";

    void Start(){
        UpdateDisplay();
    }

    //only allow 3 digits dialed
    public void PressDigit(int digit){
        if (dialed.Length < maxDigits){
            dialed += digit;
            UpdateDisplay();
        }

    }

    //for the back button on dial menu, remove the last digit typed
    public void Backspace(){
        if (dialed.Length > 0){
            dialed = dialed.Substring(0,dialed.Length - 1);
            UpdateDisplay();
        }

    }

    //for the clear button on dial menu, remove the all digits typed to _ _ _ 
    public void ClearNumber(){
        dialed = "";
        UpdateDisplay();


    }

    //for the call button, calls the typed number
    public void CallDialed(){
        if (dialed.Length > 0){
            int frequency = int.Parse(dialed); //string to int
            dial.QuickDial(frequency);
        }

    }

    // call each crew member by selecting the name
    public void CallCrewmate(string crewName){
        CrewInfo[] crew = ctm.GetCrewmatesInformation();

        foreach (CrewInfo info in crew){
            if (info.name == crewName){
                dialed = info.frequency.ToString();
                UpdateDisplay();
                dial.HangUp();
                dial.QuickDial(info.frequency);
                return;
            }
        }
        Debug.LogWarning("CallDashboard: no crewmate named " + crewName);

    }

    //dial emergency number
    public void CallEmergency(){
        dial.HangUp();
        dial.QuickDial(ctm.GetEmergencyFrequency());
    }

    //for the end button, Hangs up (stops ringing or the current call, frees the line) and clear the typed number
    public void EndCall(){
        dial.HangUp();
        ClearNumber();

    }

    public void UpdateDisplay(){
        string text = "";

        for (int i = 0; i < maxDigits; i++){
            if (i < dialed.Length)
                text += dialed[i];
            else
                text += "_";
            if (i < maxDigits - 1)
                text += " ";
        }
        numberDisplay.text = text;
    }
}
