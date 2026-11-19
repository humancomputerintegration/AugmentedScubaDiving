using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// Only Unity API surfaces are simulated. Production call-system methods are linked unchanged.
namespace UnityEngine
{
    public class Object { public string name; }
    public class GameObject : Object
    {
        private readonly List<Component> components = new();
        public Transform transform { get; }
        public bool activeSelf = true;
        public GameObject(string name = "") { this.name = name; transform = new Transform { owner = this }; components.Add(transform); }
        public T AddComponent<T>() where T : Component, new() { var value = new T { owner = this }; components.Add(value); return value; }
        public T GetComponent<T>() where T : Component => components.OfType<T>().FirstOrDefault();
        public void SetActive(bool value) { activeSelf = value; }
    }
    public class Component : Object
    {
        internal GameObject owner;
        public GameObject gameObject => owner;
        public Transform transform => owner.transform;
        public new string name { get => owner.name; set => owner.name = value; }
        public T GetComponent<T>() where T : Component => owner.GetComponent<T>();
    }
    public class Behaviour : Component { public bool enabled = true; }
    public class MonoBehaviour : Behaviour
    {
        public readonly List<IEnumerator> RunningCoroutines = new();
        public int StartCoroutineCount;
        public Coroutine StartCoroutine(IEnumerator routine) { StartCoroutineCount++; RunningCoroutines.Add(routine); routine.MoveNext(); return new Coroutine(routine); }
    }
    public sealed class Coroutine { public IEnumerator Routine; public Coroutine(IEnumerator routine) { Routine = routine; } }
    public class Transform : Component
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 forward = new Vector3(0,0,1);
        public Vector3 up = new Vector3(0,1,0);
    }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z) { this.x=x;this.y=y;this.z=z; }
        public static Vector3 operator +(Vector3 a,Vector3 b) => new(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator -(Vector3 a,Vector3 b) => new(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator *(Vector3 a,float v) => new(a.x*v,a.y*v,a.z*v);
    }
    public struct Quaternion { public float x,y,z,w; }
    public struct Color
    {
        public float r,g,b,a;
        public Color(float r,float g,float b,float a=1) {this.r=r;this.g=g;this.b=b;this.a=a;}
    }
    [AttributeUsage(AttributeTargets.Class)] public class RequireComponent : Attribute { public RequireComponent(Type t) {} }
    [AttributeUsage(AttributeTargets.Field)] public class RangeAttribute : Attribute { public RangeAttribute(float min,float max) {} }
    public enum AudioDataLoadState { Unloaded, Loading, Loaded, Failed }
    public class AudioClip : Object
    {
        public float length = 2.5f;
        public AudioDataLoadState loadState = AudioDataLoadState.Loaded;
        public bool LoadAudioData() { if(loadState == AudioDataLoadState.Failed) return false;loadState=AudioDataLoadState.Loaded;return true; }
    }
    public class AudioSource : Behaviour
    {
        public AudioClip clip;
        public bool playOnAwake,loop,spatialize,mute,bypassEffects,bypassListenerEffects,bypassReverbZones;
        public float spatialBlend,dopplerLevel,panStereo,pitch,volume;
        public int priority,PlayCount,StopCount,OneShotCount;
        public bool isPlaying;
        public bool AllowPlayback = true;
        public void Play() { PlayCount++;isPlaying=AllowPlayback && clip!=null && clip.loadState!=AudioDataLoadState.Failed; }
        public void Stop() { StopCount++;isPlaying=false; }
        public void PlayOneShot(AudioClip value) { OneShotCount++;isPlaying=value!=null && AllowPlayback; }
    }
    public static class AudioListener { public static bool pause; }
    public static class Time { public static float unscaledTime; }
    public static class Mathf
    {
        public static float Clamp01(float x) => Math.Clamp(x,0,1);
        public static float Max(float a,float b) => Math.Max(a,b);
        public static int Max(int a,int b) => Math.Max(a,b);
        public static float Min(float a,float b) => Math.Min(a,b);
    }
    public static class Random
    {
        private static readonly System.Random generator = new(7);
        public static int Range(int min,int max) => generator.Next(min,max);
    }
    public static class Debug
    {
        public static readonly List<string> Messages = new();
        public static void Log(string s,Object context=null) {Messages.Add(s);}
        public static void LogWarning(string s,Object context=null) {Messages.Add(s);}
        public static void LogError(string s,Object context=null) {Messages.Add(s);}
    }
    public class WaitForSeconds { public float Seconds; public WaitForSeconds(float value) {Seconds=value;} }
    public class WaitForSecondsRealtime { public float Seconds; public WaitForSecondsRealtime(float value) {Seconds=value;} }
}
namespace UnityEngine.UI
{
    public class Text : UnityEngine.Behaviour { public string text; }
    public struct ColorBlock { public UnityEngine.Color normalColor,selectedColor; }
    public class Button : UnityEngine.Behaviour { public bool interactable;public ColorBlock colors; }
}
namespace UnityEngine.SceneManagement
{
    public struct Scene { public int buildIndex;public string name; }
    public static class SceneManager
    {
        public static readonly List<string> LoadRequests = new();
        public static Scene GetActiveScene() => new Scene {buildIndex=0,name="Simulation"};
        public static void LoadScene(int index) { LoadRequests.Add(index.ToString()); }
        public static void LoadScene(string name) { LoadRequests.Add(name); }
    }
}
