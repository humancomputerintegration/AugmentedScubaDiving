using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

static class Test
{
    static readonly List<object> results = new();
    static int assertions;
    static void Assert(bool condition,string message) {assertions++;if(!condition)throw new Exception(message);}
    static object Invoke(object target,string method)
    {
        var m=target.GetType().GetMethod(method,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
        if(m==null)throw new Exception("Missing lifecycle "+method);
        return m.Invoke(target,null);
    }
    static AudioClip Clip(string name,float length=2.5f) => new AudioClip {name=name,length=length};
    static T Add<T>(string name) where T:Component,new() => new GameObject(name).AddComponent<T>();
    sealed class Fixture
    {
        public CallTowerManager tower;
        public SimpleDial dial;
        public AudioSource voice,feedback;
        public CrewmateBehavior[] mates;
        public CallDashboardManager dashboard;
        public Fixture(bool fixedChannels=false,int[] channels=null)
        {
            Time.unscaledTime=0;AudioListener.pause=false;SceneManager.LoadRequests.Clear();
            tower=Add<CallTowerManager>("Tower");
            var phone=new GameObject("Call System");
            voice=phone.AddComponent<AudioSource>();
            dial=phone.AddComponent<SimpleDial>();
            feedback=phone.AddComponent<AudioSource>();
            dial.feedbackSource=feedback;dial.ctm=tower;
            dial.ringtone=Clip("Ringtone",25);
            dial.buttonCue=Clip("Click",0.065f);dial.connectedCue=Clip("Connected",0.405f);dial.endedCue=Clip("Ended",0.365f);
            tower.playerTransmitter=dial;tower.emergencyAudio=Clip("Rescue",9.346f);tower.wrongFrequency=Clip("Wrong",2);
            string[] names={"Pedro","Romain","Suzie","Eliza","Elina"};
            mates=names.Select(n=>Add<CrewmateBehavior>(n)).ToArray();
            for(int i=0;i<5;i++){mates[i].ctm=tower;mates[i].seed=i+1;mates[i].callAudioReceiver=Clip(names[i]+" answering");mates[i].callAudioCaller=Clip(names[i]+" calling");}
            tower.crewmates=mates.Select(x=>x.transform).ToArray();
            tower.useFixedFrequencies=fixedChannels;
            if(channels!=null)tower.fixedFrequencies=channels;
            Invoke(tower,"Awake");Invoke(dial,"Awake");
            dashboard=Add<CallDashboardManager>("PhoneUI");dashboard.dial=dial;dashboard.ctm=tower;
            dashboard.numberDisplay=Add<Text>("Number");dashboard.modeDisplay=Add<Text>("Mode");dashboard.feedbackText=Add<Text>("State");
            dashboard.contactLabels=Enumerable.Range(0,5).Select(i=>Add<Text>("Contact"+i)).ToArray();
            dashboard.contactButtons=Enumerable.Range(0,5).Select(i=>Add<Button>("Button"+i)).ToArray();
            Invoke(dashboard,"OnEnable");Invoke(dashboard,"Update");
        }
        public int Frequency(int i)=>tower.crewmateFrequencies[i];
        public void Digits(string digits){foreach(char c in digits)dashboard.PressDigit(c-'0');}
        public string Input=>dashboard.numberDisplay.text.Replace(" ","").TrimEnd('_');
        public IncomingCallPopup Popup()
        {
            var p=Add<IncomingCallPopup>("PopupHost");p.dial=dial;p.ctm=tower;p.head=new GameObject("Head").transform;
            p.popupRoot=new GameObject("PopupScreen");p.callerText=Add<Text>("Caller");p.actionHintText=Add<Text>("Action");
            p.answerButtonRoot=new GameObject("Answer");p.hangUpButtonRoot=new GameObject("End");
            Invoke(p,"Start");Invoke(p,"LateUpdate");return p;
        }
    }
    static void Case(string name,Action body)
    {
        int before=assertions;
        try{body();results.Add(new {name,status="PASS",assertions=assertions-before});}
        catch(Exception e){results.Add(new {name,status="FAIL",assertions=assertions-before,error=(e.InnerException??e).Message});}
    }
    static void Main()
    {
        Case("Five contacts: phone strings and unique radio channels address the same objects",()=>{
            var f=new Fixture();var list=f.tower.GetCrewmatesInformation();
            Assert(list.Length==5,"crew count");Assert(list.Select(c=>c.frequency).Distinct().Count()==5,"unique radio");
            for(int i=0;i<5;i++){
                Assert(list[i].phoneNumber==(2001+i).ToString(),"fixed phone");
                Assert(list[i].frequency>=100&&list[i].frequency<1000,"radio range");
                Assert(f.tower.TryResolvePhone(list[i].phoneNumber,out int freq)&&freq==list[i].frequency,"phone mapping");
                Assert(ReferenceEquals(f.tower.CallCrewmate(freq),f.mates[i].callAudioReceiver),"same actor audio");
                Assert(f.dashboard.contactLabels[i].text.Contains(list[i].name)&&f.dashboard.contactLabels[i].text.Contains(list[i].phoneNumber),"label mapping");
            }
            Assert(!f.tower.TryResolvePhone("1001",out _),"reserved SOS not a contact");
            Assert(!f.tower.TryResolvePhone("9999",out _),"unknown extension");
            f.mates[4].transform.position=new Vector3(9,8,7);
            Assert(f.tower.GetCrewmatesInformation()[4].worldLocation.x==9,"current moving coordinate");
        });
        Case("Independent phone/frequency buffers and both modes reach the same voice",()=>{
            var f=new Fixture();string freq=f.Frequency(0).ToString();
            f.Digits(freq);f.dashboard.SetPhoneMode();f.Digits("2001");Assert(f.Input=="2001","phone buffer");
            f.dashboard.SetFrequencyMode();Assert(f.Input==freq,"preserved freq");
            f.dashboard.SetPhoneMode();f.dashboard.DialAddress();Assert(ReferenceEquals(f.voice.clip,f.mates[0].callAudioReceiver),"phone voice");
            f.dashboard.EndCall();f.dashboard.SetFrequencyMode();Assert(f.Input==freq,"END only clears current mode");
            f.dashboard.DialAddress();Assert(ReferenceEquals(f.voice.clip,f.mates[0].callAudioReceiver),"frequency voice");
            Assert(f.dial.State==SimpleDial.CallState.Connected,"connected actual source");
            f.dashboard.SetPhoneMode();Assert(f.dashboard.feedbackText.text==f.dial.StatusMessage,"mode switch keeps connected status");
        });
        Case("Incoming true means accepted; a busy second incoming cannot overwrite first",()=>{
            var f=new Fixture();
            Assert(f.dial.IncomingCall(f.mates[0].callAudioCaller,f.Frequency(0)),"accepted must be true");
            Assert(f.dial.State==SimpleDial.CallState.Ringing&&f.voice.loop,"ring state and loop");
            Assert(!f.dial.IncomingCall(f.mates[1].callAudioCaller,f.Frequency(1)),"busy reject");
            Assert(f.dial.GetIncomingFrequency()==f.Frequency(0),"first caller preserved");
            f.dial.AnswerCall();Assert(f.dial.State==SimpleDial.CallState.Connected&&!f.voice.loop,"answered not looping");
            Assert(ReferenceEquals(f.voice.clip,f.mates[0].callAudioCaller),"accepted caller voice");
        });
        Case("Natural voice completion ends even while independent feedback still plays",()=>{
            var f=new Fixture();f.dial.QuickDial(f.Frequency(2));Invoke(f.dial,"Update");
            Assert(f.dial.IsAudioPlaying,"voice started");f.feedback.isPlaying=true;f.voice.isPlaying=false;
            Invoke(f.dial,"Update");
            Assert(f.dial.State==SimpleDial.CallState.Ended&&!f.dial.IsBusy,"natural end");
            Assert(f.dial.StatusMessage.StartsWith("CALL ENDED"),"clear ended message");
            Assert(!f.voice.loop&&f.voice.clip==null,"audio cleared");Assert(f.feedback.OneShotCount==2,"one connected and one ended");
        });
        Case("Decline a ringing call, then outgoing voice does not inherit loop",()=>{
            var f=new Fixture();f.dial.IncomingCall(f.mates[0].callAudioCaller,f.Frequency(0));f.dial.HangUp();
            Assert(!f.dial.IsRinging()&&!f.voice.loop&&f.dial.GetIncomingFrequency()==0,"ring cleared");
            f.dial.QuickDial(f.Frequency(1));Assert(f.dial.State==SimpleDial.CallState.Connected&&!f.voice.loop,"fresh outgoing voice");
        });
        Case("SOS preempts connected or ringing calls and schedules restart exactly once",()=>{
            foreach(bool incoming in new[]{false,true}){
                var f=new Fixture();if(incoming)f.dial.IncomingCall(f.mates[0].callAudioCaller,f.Frequency(0));else f.dial.QuickDial(f.Frequency(0));
                f.dashboard.CallEmergency();Assert(f.dial.State==SimpleDial.CallState.Emergency&&f.dial.EmergencyRequested,"SOS priority");
                Assert(ReferenceEquals(f.voice.clip,f.tower.emergencyAudio)&&!f.voice.loop,"rescue clip");
                int plays=f.voice.PlayCount;f.dashboard.CallEmergency();f.dial.HangUp();f.dial.QuickDial(f.Frequency(1));
                Assert(f.voice.PlayCount==plays&&f.tower.StartCoroutineCount==1,"no repeated schedule/play");
                Assert(!f.dial.IncomingCall(f.mates[1].callAudioCaller,f.Frequency(1)),"incoming blocked during rescue");
                var routine=f.tower.RunningCoroutines.Single();
                Assert(Math.Abs(((WaitForSecondsRealtime)routine.Current).Seconds-9.846f)<0.001f,"clip length +0.5");
                Assert(!routine.MoveNext()&&SceneManager.LoadRequests.Count==1,"one actual reload request");
            }
        });
        Case("1001 typed in either mode works while busy and FREQ permits four digits",()=>{
            foreach(bool phone in new[]{false,true}){
                var f=new Fixture();f.dashboard.maxDigits=3;f.dashboard.SetMode(phone);f.Digits("1001");
                Assert(f.Input=="1001","four-digit SOS");f.dial.QuickDial(f.Frequency(1));f.dashboard.DialAddress();
                Assert(f.dial.State==SimpleDial.CallState.Emergency,"SOS before busy guard");
            }
        });
        Case("Empty, unknown and missing audio paths never count as connected",()=>{
            var f=new Fixture();f.dashboard.DialAddress();Assert(f.dial.State==SimpleDial.CallState.Invalid&&!f.dial.IsBusy,"empty invalid");
            f.dashboard.SetPhoneMode();f.Digits("9999");f.dashboard.DialAddress();Assert(f.dial.StatusMessage.Contains("NO CONTACT"),"phone invalid");
            f.dial.QuickDial(99999);Assert(f.dial.State==SimpleDial.CallState.Invalid,"radio invalid");
            f.dial.HangUp();Assert(!f.voice.isPlaying,"END can stop wrong-address audio");
            f.mates[0].callAudioReceiver=null;f.dial.QuickDial(f.Frequency(0));
            Assert(f.dial.State==SimpleDial.CallState.Invalid&&!f.dial.IsBusy,"missing voice invalid");
            Assert(!f.dial.IncomingCall(null,f.Frequency(0)),"missing incoming voice rejected");
        });
        Case("Playback that never starts gives explicit failure, not normal completion",()=>{
            var f=new Fixture();f.voice.AllowPlayback=false;f.dial.QuickDial(f.Frequency(0));
            Time.unscaledTime=0.1f;Invoke(f.dial,"Update");Assert(f.dial.State==SimpleDial.CallState.Connected,"startup grace");
            Time.unscaledTime=3.01f;Invoke(f.dial,"Update");
            Assert(f.dial.State==SimpleDial.CallState.Invalid&&!f.dial.IsBusy,"explicit audio failure");
            Assert(f.dial.StatusMessage.StartsWith("AUDIO DID NOT START"),"not fake normal end");
        });
        Case("Global listener pause is not treated as finished speech",()=>{
            var f=new Fixture();f.dial.QuickDial(f.Frequency(0));Invoke(f.dial,"Update");
            f.voice.isPlaying=false;AudioListener.pause=true;Invoke(f.dial,"Update");
            Assert(f.dial.State==SimpleDial.CallState.Connected,"paused preserved");
            AudioListener.pause=false;Invoke(f.dial,"Update");Assert(f.dial.State==SimpleDial.CallState.Ended,"completed after resume");
        });
        Case("Popup follows real incoming/connected/ended states and a three-second timeout",()=>{
            var f=new Fixture();var p=f.Popup();Assert(!p.popupRoot.activeSelf,"idle hidden");
            f.dial.IncomingCall(f.mates[3].callAudioCaller,f.Frequency(3));Invoke(p,"LateUpdate");
            Assert(p.popupRoot.activeSelf&&p.callerText.text.StartsWith("INCOMING CALL")&&p.answerButtonRoot.activeSelf,"ring visible");
            p.Answer();Invoke(p,"LateUpdate");
            Assert(p.popupRoot.activeSelf&&p.callerText.text.StartsWith("CONNECTED")&&!p.answerButtonRoot.activeSelf&&p.hangUpButtonRoot.activeSelf,"answer visibly connected");
            p.head.position=new Vector3(10,20,30);p.head.forward=new Vector3(1,0,0);Invoke(p,"LateUpdate");
            Assert(Math.Abs(p.popupRoot.transform.position.x-10.65f)<0.001f&&Math.Abs(p.popupRoot.transform.position.y-19.95f)<0.001f,"head-relative formula");
            p.HangUp();Invoke(p,"LateUpdate");Assert(p.popupRoot.activeSelf&&p.callerText.text.StartsWith("CALL ENDED"),"end visible");
            Time.unscaledTime=3.01f;Invoke(p,"LateUpdate");Assert(!p.popupRoot.activeSelf,"ended hides after timeout");
        });
        Case("Crewmate random calls use one coroutine after many accepted and rejected attempts",()=>{
            var f=new Fixture();var m=f.mates[0];Invoke(m,"Start");
            Assert(m.StartCoroutineCount==1&&m.RunningCoroutines.Count==1,"one initial coroutine");
            var r=m.RunningCoroutines.Single();Assert(r.Current is WaitForSeconds,"first wait");
            for(int i=0;i<90;i++){if(i%6==0)f.dial.HangUp();Assert(r.MoveNext(),"loop keeps running");Assert(r.Current is WaitForSeconds,"one scheduled wait");}
            Assert(m.StartCoroutineCount==1&&m.RunningCoroutines.Count==1,"no recursive spawning");
        });
        Case("Invalid fixed frequencies safely fall back; valid fixed frequencies remain",()=>{
            var bad=new Fixture(true,new[]{111,111,1001,222,333});
            Assert(bad.tower.IsReady&&bad.tower.crewmateFrequencies.Distinct().Count()==5,"invalid fixed fallback");
            Assert(bad.tower.crewmateFrequencies.All(x=>x>=100&&x<1000),"fallback within range");
            var good=new Fixture(true,new[]{111,222,333,444,555});
            Assert(good.tower.crewmateFrequencies.SequenceEqual(new[]{111,222,333,444,555}),"valid fixed channels");
        });
        Case("Contact buttons use current mode and old name wrapper remains callable",()=>{
            var f=new Fixture();f.dashboard.SetPhoneMode();f.dashboard.SelectContact(4);
            Assert(f.Input=="2005"&&ReferenceEquals(f.voice.clip,f.mates[4].callAudioReceiver),"Elina contact phone mapping");
            f.dashboard.EndCall();f.dashboard.SetFrequencyMode();f.dashboard.CallCrewmate("pEdRo");
            Assert(ReferenceEquals(f.voice.clip,f.mates[0].callAudioReceiver),"old named wrapper");
            Assert(f.dashboard.contactButtons.All(x=>x.interactable),"five valid buttons");
        });
        var summary=new {scope="Actual six production C# files compiled and executed against narrow Unity API test doubles; NOT Unity Editor/Quest.",total_cases=results.Count,assertions,results};
        Console.WriteLine(JsonSerializer.Serialize(summary,new JsonSerializerOptions{WriteIndented=true}));
        Environment.ExitCode=results.Any(x=>JsonSerializer.Serialize(x).Contains("\"FAIL\""))?1:0;
    }
}
