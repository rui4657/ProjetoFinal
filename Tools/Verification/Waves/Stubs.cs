using System;
using System.Collections;
using System.Collections.Generic;
namespace UnityEngine {
 public class HeaderAttribute:Attribute { public HeaderAttribute(string x){} }
 public class MinAttribute:Attribute { public MinAttribute(float x){} }
 public class HideInInspector:Attribute{}
 public class Object {
  public static Dictionary<Type,object> Registry=new Dictionary<Type,object>();
  public static T FindFirstObjectByType<T>() where T:class => Registry.TryGetValue(typeof(T),out object value)?value as T:null;
 }
 public class Coroutine {
  public IEnumerator Enumerator; public bool Stopped; public bool Complete;
  public bool Tick(){if(Stopped||Complete)return false; Complete=!Enumerator.MoveNext(); return !Complete;}
 }
 public class MonoBehaviour:Object {
  public readonly List<Coroutine> Coroutines=new List<Coroutine>();
  public Coroutine StartCoroutine(IEnumerator routine){var c=new Coroutine{Enumerator=routine};Coroutines.Add(c);c.Tick();return c;}
  public void StopCoroutine(Coroutine c){c.Stopped=true;}
 }
 public static class Mathf {
  public static int Max(int a,int b)=>Math.Max(a,b); public static float Max(float a,float b)=>Math.Max(a,b);
  public static int Clamp(int a,int min,int max)=>Math.Max(min,Math.Min(max,a));
  public static float Clamp(float a,float min,float max)=>Math.Max(min,Math.Min(max,a));
  public static float Pow(float a,float b)=>(float)Math.Pow(a,b);
 }
 public static class Time {public static float timeScale=1f;public static float deltaTime=1f;}
 public class WaitForSeconds{public WaitForSeconds(float x){}}
 public static class Debug { public static List<string> Errors=new List<string>(); public static void LogError(object message,Object context=null){Errors.Add(message.ToString());} }
}
public class PlayerHealth {public bool IsDead;}
public class SurvivalSession {
 public static SurvivalSession Instance; public bool IsGameOver; public readonly List<int> Completed=new List<int>();
 public void OnRoundCompleted(int round){Completed.Add(round);}
}
public class ZombieSpawner {
 public bool IsConfigured=true; public int Attempts; public readonly Queue<bool> Outcomes=new Queue<bool>();
 public bool SpawnZombie(int round,RoundManager owner){Attempts++;return Outcomes.Count==0||Outcomes.Dequeue();}
}
