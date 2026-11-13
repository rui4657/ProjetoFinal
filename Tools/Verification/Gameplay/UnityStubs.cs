using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
namespace UnityEngine {
 [AttributeUsage(AttributeTargets.All,AllowMultiple=true)] public class HeaderAttribute:Attribute {public HeaderAttribute(string s){}}
 [AttributeUsage(AttributeTargets.All,AllowMultiple=true)] public class MinAttribute:Attribute {public MinAttribute(float x){}}
 public class RangeAttribute:Attribute {public RangeAttribute(float a,float b){}}
 public class DisallowMultipleComponent:Attribute{} public class SerializeField:Attribute{} public class HideInInspector:Attribute{}
 public class DefaultExecutionOrder:Attribute {public DefaultExecutionOrder(int n){}}
 public class RequireComponent:Attribute {public RequireComponent(Type t){}}
 public class RuntimeInitializeOnLoadMethodAttribute:Attribute {public RuntimeInitializeOnLoadMethodAttribute(){} public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t){}}
 public enum RuntimeInitializeLoadType{BeforeSceneLoad,AfterSceneLoad,SubsystemRegistration}
 public class Object {
  public static Dictionary<Type,Object> Registry=new Dictionary<Type,Object>();
  public static readonly List<Object> All=new List<Object>();
  private static readonly List<(Object value,float at)> Scheduled=new List<(Object,float)>();
  public string name;public bool Destroyed;public Object(){All.Add(this);}
  public static implicit operator bool(Object o)=>!ReferenceEquals(o,null)&&!o.Destroyed;
  public static bool operator==(Object a,Object b){bool an=ReferenceEquals(a,null)||a.Destroyed,bn=ReferenceEquals(b,null)||b.Destroyed;return an?bn:!bn&&ReferenceEquals(a,b);}
  public static bool operator!=(Object a,Object b)=>!(a==b);
  public override bool Equals(object o)=>ReferenceEquals(this,o);public override int GetHashCode()=>System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
  public static T FindFirstObjectByType<T>() where T:Object=>Registry.TryGetValue(typeof(T),out Object value)&&value?value as T:All.OfType<T>().FirstOrDefault(x=>x);
  public static T[] FindObjectsByType<T>(FindObjectsSortMode mode) where T:Object=>All.OfType<T>().Where(x=>x).ToArray();
  public static T[] FindObjectsByType<T>(FindObjectsInactive inactive,FindObjectsSortMode mode) where T:Object=>FindObjectsByType<T>(mode);
  public static T FindObjectOfType<T>() where T:Object=>FindFirstObjectByType<T>();
  public static T[] FindObjectsOfType<T>() where T:Object=>FindObjectsByType<T>(FindObjectsSortMode.None);
  public static T Instantiate<T>(T o,Vector3 p,Quaternion r) where T:Object=>o;
  public static T Instantiate<T>(T o) where T:Object=>o;public static T Instantiate<T>(T o,Transform parent) where T:Object=>o;
  public static void Destroy(Object o,float delay=0){if(!o)return;if(delay>0){Scheduled.Add((o,Time.time+delay));return;}o.Destroyed=true;if(o is GameObject go){foreach(var c in go.Components.ToArray())Destroy(c);foreach(var child in go.transform.Children.ToArray())Destroy(child.gameObject);}var callback=o.GetType().GetMethod("OnDestroy",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);callback?.Invoke(o,null);}
  public static void ProcessScheduledDestruction(){foreach(var pending in Scheduled.ToArray()){if(Time.time<pending.at)continue;Scheduled.Remove(pending);Destroy(pending.value);}}
  public static void ResetSimulation(){Registry.Clear();All.Clear();Scheduled.Clear();AI.NavMesh.NextSampleResult=true;AI.NavMesh.NextPathResult=true;Physics.NextOccupants=new Collider[0];AI.NavMesh.NextTriangulation=new AI.NavMeshTriangulation{vertices=new Vector3[0],indices=new int[0]};}
  public static void DontDestroyOnLoad(Object o){}
 }
 public enum FindObjectsSortMode{None,InstanceID}public enum FindObjectsInactive{Include,Exclude}
 public class Component:Object {
  public GameObject gameObject;public Transform transform;
  public T GetComponent<T>() where T:class=>gameObject?.GetComponent<T>();
  public T GetComponentInChildren<T>(bool inactive=false) where T:class=>gameObject?.GetComponentInChildren<T>(inactive);
  public T GetComponentInParent<T>() where T:class{for(Transform t=transform;t;t=t.parent){T found=t.gameObject?.GetComponent<T>();if(found!=null)return found;}return null;}
  public T[] GetComponentsInChildren<T>(bool inactive=false) where T:class=>gameObject?.GetComponentsInChildren<T>(inactive)??new T[0];
  public T[] GetComponents<T>() where T:class=>gameObject?.Components.OfType<T>().ToArray()??new T[0];
  public bool TryGetComponent<T>(out T value) where T:class{value=GetComponent<T>();return value!=null;}
  public bool CompareTag(string tag)=>gameObject!=null&&gameObject.tag==tag;
 }
 public class Behaviour:Component {public bool enabled=true;public bool isActiveAndEnabled=>enabled&&(gameObject==null||gameObject.activeInHierarchy);}
 public class MonoBehaviour:Behaviour {public readonly List<Coroutine> Coroutines=new List<Coroutine>();public Coroutine StartCoroutine(IEnumerator e){var c=new Coroutine{Enumerator=e};Coroutines.Add(c);c.Tick();return c;}public void StopCoroutine(Coroutine c){c.Stopped=true;}public void StopAllCoroutines(){foreach(var c in Coroutines)c.Stopped=true;}}
 public class Coroutine:Object {public IEnumerator Enumerator;public bool Stopped,Complete;public bool Tick(){if(Stopped||Complete)return false;Complete=!Enumerator.MoveNext();return !Complete;}} public class WaitForSeconds {public WaitForSeconds(float s){}}public class WaitForSecondsRealtime {public WaitForSecondsRealtime(float s){}}
 public enum PrimitiveType{Cube,Cylinder,Sphere}
 public class GameObject:Object {
  public static GameObject CreatePrimitive(PrimitiveType type){var go=new GameObject(type.ToString());go.AddComponent<MeshFilter>();go.AddComponent<MeshRenderer>();go.AddComponent<BoxCollider>();return go;}
  public readonly List<Component> Components=new List<Component>();
  public GameObject(string n,params Type[] types){name=n;transform=types.Contains(typeof(RectTransform))?new RectTransform():new Transform();AttachForTest(transform);foreach(Type type in types)if(type!=typeof(Transform)&&type!=typeof(RectTransform))AddComponent(type);}
  public bool activeSelf=true;public bool activeInHierarchy=>activeSelf&&!Destroyed;public int layer;public string tag="Untagged";public Transform transform;public SceneManagement.Scene scene;
  public void SetActive(bool value){activeSelf=value;}
  public T AttachForTest<T>(T component) where T:Component{component.gameObject=this;component.transform=transform;component.name=name;if(!All.Contains(component))All.Add(component);if(component is UI.Graphic graphic)graphic.rectTransform=transform as RectTransform;Components.Add(component);return component;}
  public T AddComponent<T>() where T:Component=>(T)AddComponent(typeof(T));
  public Component AddComponent(Type type){var component=(Component)Activator.CreateInstance(type);AttachForTest(component);var awake=type.GetMethod("Awake",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);awake?.Invoke(component,null);return component;}
  public T GetComponent<T>() where T:class=>Components.OfType<T>().FirstOrDefault(x=>x is Object o?o:true);
  public T GetComponentInChildren<T>(bool inactive=false) where T:class=>GetComponentsInChildren<T>(inactive).FirstOrDefault();
  public T[] GetComponentsInChildren<T>(bool inactive=false) where T:class {var found=new List<T>();if(inactive||activeInHierarchy)found.AddRange(Components.OfType<T>());foreach(var child in transform.Children)found.AddRange(child.gameObject.GetComponentsInChildren<T>(inactive));return found.ToArray();}
  public static GameObject Find(string n)=>All.OfType<GameObject>().FirstOrDefault(x=>x&&x.name==n);
  public static GameObject FindGameObjectWithTag(string tag)=>All.OfType<GameObject>().FirstOrDefault(x=>x&&x.tag==tag);
 }
 public class Transform:Component {
  public readonly List<Transform> Children=new List<Transform>();public Vector3 position,localPosition,localScale=Vector3.one,eulerAngles,localEulerAngles;public Quaternion rotation,localRotation;
  public Vector3 forward=Vector3.forward,up=Vector3.up,right=Vector3.right;public Vector3 lossyScale=>localScale;public Transform parent;public int childCount=>Children.Count;
  public int GetSiblingIndex()=>parent?.Children.IndexOf(this)??0;public void SetSiblingIndex(int index){}public void SetAsFirstSibling(){}public void Rotate(Vector3 value){eulerAngles+=value;}public void Rotate(Vector3 axis,float angle,Space space=Space.Self){eulerAngles+=axis*angle;}
  public bool IsChildOf(Transform value){for(Transform t=this;t;t=t.parent)if(ReferenceEquals(t,value))return true;return false;}
  public void SetParent(Transform value,bool keep=true){parent?.Children.Remove(this);parent=value;value?.Children.Add(this);}public Transform GetChild(int index)=>Children[index];
  public Transform Find(string n)=>Children.FirstOrDefault(c=>c.name==n);public Vector3 TransformDirection(Vector3 value)=>value;public Vector3 InverseTransformPoint(Vector3 value)=>value-position;public Vector3 TransformPoint(Vector3 value)=>value+position;public Vector3 TransformVector(Vector3 value)=>value;public Vector3 InverseTransformVector(Vector3 value)=>value;
 }
 public class RectTransform:Transform {public Vector2 anchorMin,anchorMax,pivot,anchoredPosition,sizeDelta,offsetMin,offsetMax;public Rect rect;public void SetSizeWithCurrentAnchors(Axis a,float v){}public enum Axis{Horizontal,Vertical}}
 public struct Rect {public float x,y,width,height,xMin,yMin,xMax,yMax;}
 public struct Vector2 {
  public float x,y;public Vector2(float a,float b){x=a;y=b;}public static Vector2 zero=>new Vector2();public static Vector2 one=>new Vector2(1,1);public float sqrMagnitude=>x*x+y*y;public float magnitude=>(float)Math.Sqrt(sqrMagnitude);
  public static Vector2 operator*(Vector2 a,float b)=>new Vector2(a.x*b,a.y*b);public static Vector2 ClampMagnitude(Vector2 a,float max)=>a.magnitude>max?a*(max/a.magnitude):a;public static Vector2 Lerp(Vector2 a,Vector2 b,float t)=>new Vector2(a.x+(b.x-a.x)*t,a.y+(b.y-a.y)*t);
 }
 public struct Vector4 {public float x,y,z,w;public Vector4(float a,float b,float c,float d){x=a;y=b;z=c;w=d;}public static Vector4 zero=>new Vector4();}
 public struct Vector3 {
  public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public static Vector3 zero=>new Vector3();public static Vector3 one=>new Vector3(1,1,1);public static Vector3 up=>new Vector3(0,1,0);public static Vector3 down=>new Vector3(0,-1,0);public static Vector3 forward=>new Vector3(0,0,1);public static Vector3 right=>new Vector3(1,0,0);
  public float sqrMagnitude=>x*x+y*y+z*z;public float magnitude=>(float)Math.Sqrt(sqrMagnitude);public Vector3 normalized=>magnitude>.000001f?this/magnitude:zero;
  public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator-(Vector3 a)=>a*-1;
  public static Vector3 operator*(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);public static Vector3 operator*(float b,Vector3 a)=>a*b;public static Vector3 operator/(Vector3 a,float b)=>new Vector3(a.x/b,a.y/b,a.z/b);
  public static bool operator==(Vector3 a,Vector3 b)=>(a-b).sqrMagnitude<.0000000001f;public static bool operator!=(Vector3 a,Vector3 b)=>!(a==b);
  public static float Distance(Vector3 a,Vector3 b)=>(a-b).magnitude;public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;
  public void Normalize(){this=normalized;}public static Vector3 ProjectOnPlane(Vector3 a,Vector3 normal)=>a-normal*Dot(a,normal)/Math.Max(.000001f,normal.sqrMagnitude);public static float SignedAngle(Vector3 from,Vector3 to,Vector3 axis){var cross=new Vector3(from.y*to.z-from.z*to.y,from.z*to.x-from.x*to.z,from.x*to.y-from.y*to.x);return (float)(Math.Atan2(Dot(axis.normalized,cross),Dot(from,to))*180/Math.PI);}public static Vector3 MoveTowards(Vector3 a,Vector3 b,float max){var d=b-a;return d.magnitude<=max?b:a+d.normalized*max;}public static Vector3 ClampMagnitude(Vector3 a,float max)=>a.magnitude>max?a*(max/a.magnitude):a;public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*t;
  public override bool Equals(object value)=>value is Vector3 other&&this==other;public override int GetHashCode()=>HashCode.Combine(x,y,z);
 }
 public enum Space{Self,World}
 public struct Quaternion {public static Quaternion identity=>new Quaternion();public static Quaternion Euler(float a,float b,float c)=>identity;public static Quaternion Euler(Vector3 a)=>identity;public static Quaternion LookRotation(Vector3 a)=>identity;public static Quaternion Slerp(Quaternion a,Quaternion b,float c)=>a;public static Quaternion operator*(Quaternion a,Quaternion b)=>a;public static Vector3 operator*(Quaternion a,Vector3 b)=>b;}
 public struct Color {public float r,g,b,a;public Color(float x,float y,float z,float w=1){r=x;g=y;b=z;a=w;}public static Color white=>new Color(1,1,1);public static Color black=>new Color(0,0,0);public static Color clear=>new Color(0,0,0,0);public static Color red=>new Color(1,0,0);public static Color Lerp(Color x,Color y,float t)=>x;public static Color operator*(Color a,Color b)=>new Color(a.r*b.r,a.g*b.g,a.b*b.b,a.a*b.a);public static Color operator*(Color a,float b)=>new Color(a.r*b,a.g*b,a.b*b,a.a*b);public static implicit operator Color32(Color c)=>new Color32();}
 public struct Color32 {public byte r,g,b,a;public Color32(byte x,byte y,byte z,byte w){r=x;g=y;b=z;a=w;}public static implicit operator Color(Color32 c)=>new Color();}
 public static class ColorUtility {public static bool TryParseHtmlString(string s,out Color c){c=new Color();return true;}}
 public static class Mathf {public const float PI=3.14f;public const float Deg2Rad=.01f;public const float Rad2Deg=57f;public static int Max(int a,int b)=>Math.Max(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int Min(int a,int b)=>Math.Min(a,b);public static float Min(float a,float b)=>Math.Min(a,b);public static float Clamp(float a,float b,float c)=>Math.Max(b,Math.Min(c,a));public static int Clamp(int a,int b,int c)=>Math.Max(b,Math.Min(c,a));public static float Clamp01(float x)=>Clamp(x,0,1);public static float Floor(float a)=>(float)Math.Floor(a);public static float Ceil(float a)=>(float)Math.Ceiling(a);public static float Pow(float a,float b)=>(float)Math.Pow(a,b);public static int RoundToInt(float a)=>(int)Math.Round(a);public static int CeilToInt(float a)=>(int)Math.Ceiling(a);public static int FloorToInt(float a)=>(int)Math.Floor(a);public static float Sqrt(float a)=>(float)Math.Sqrt(a);public static float Abs(float a)=>Math.Abs(a);public static float Sin(float a)=>(float)Math.Sin(a);public static float Cos(float a)=>(float)Math.Cos(a);public static float Exp(float a)=>(float)Math.Exp(a);public static float Lerp(float a,float b,float t)=>a+(b-a)*t;public static float MoveTowards(float a,float b,float d)=>b;public static float InverseLerp(float a,float b,float v)=>a==b?0:Clamp01((v-a)/(b-a));public static float SmoothStep(float a,float b,float t){t=Clamp01(t);return Lerp(a,b,t*t*(3-2*t));}public static float SmoothDamp(float a,float b,ref float c,float d)=>b;public static bool Approximately(float a,float b)=>a==b;}
 public static class Time {public static float timeScale=1,deltaTime=.016f,unscaledDeltaTime=.016f,time,unscaledTime;public static int frameCount;}
 public static class Input {public static readonly HashSet<KeyCode> KeysDown=new HashSet<KeyCode>();public static readonly HashSet<int> MouseHeld=new HashSet<int>();public static readonly HashSet<int> MouseDown=new HashSet<int>();public static bool GetKey(KeyCode k)=>false;public static bool GetKeyDown(KeyCode k)=>KeysDown.Contains(k);public static bool GetButton(string k)=>false;public static bool GetButtonDown(string k)=>false;public static bool GetMouseButton(int b)=>MouseHeld.Contains(b);public static bool GetMouseButtonDown(int b)=>MouseDown.Contains(b);public static float GetAxis(string k)=>0;public static float GetAxisRaw(string k)=>0;}
 public enum KeyCode {R,E,B,V,Escape,Return,LeftShift,RightShift,Space}
 public static class Random {public static float value;public static int Range(int a,int b)=>a;public static float Range(float a,float b)=>a;public static Vector2 insideUnitCircle=>Vector2.zero;}
 public static class Debug {public static void Log(object o){}public static void LogWarning(object o,Object context=null){}public static void LogError(object o,Object context=null){}}
 public enum CursorLockMode{None,Locked,Confined}public static class Cursor {public static CursorLockMode lockState;public static bool visible;}
 public static class Screen {public static bool fullScreen;public static int width=1920,height=1080;public static Rect safeArea;}
 public static class Application {public static string persistentDataPath="/tmp";public static bool isFocused=true;public static bool isPlaying;public static bool CanStreamedLevelBeLoaded(string s)=>true;public static void Quit(){}}
 public static class PlayerPrefs {public static Dictionary<string,object> Values=new Dictionary<string,object>();public static int GetInt(string n,int d=0)=>Values.TryGetValue(n,out object v)?(int)v:d;public static float GetFloat(string n,float d=0)=>Values.TryGetValue(n,out object v)?(float)v:d;public static string GetString(string n,string d="")=>Values.TryGetValue(n,out object v)?(string)v:d;public static void SetInt(string n,int v){Values[n]=v;}public static void SetFloat(string n,float v){Values[n]=v;}public static void SetString(string n,string v){Values[n]=v;}public static void DeleteKey(string n){Values.Remove(n);}public static bool HasKey(string n)=>Values.ContainsKey(n);public static void Save(){}}

 public class Camera:Behaviour {public static Camera main;public float fieldOfView,nearClipPlane=.3f,farClipPlane=1000f;public Color backgroundColor;public Ray ViewportPointToRay(Vector3 p)=>new Ray();public Vector3 WorldToViewportPoint(Vector3 p)=>new Vector3(.5f,.5f,-1);}
 public class CharacterController:Collider {public bool isGrounded;public bool enabled;public float height,radius;public Vector3 velocity;public CollisionFlags Move(Vector3 p)=>CollisionFlags.None;}
 public class Collider:Component {public bool enabled=true;public bool isTrigger;public Bounds bounds;public Vector3 ClosestPoint(Vector3 p)=>p;}
 [Flags]public enum CollisionFlags{None=0,Above=1,Below=2,Sides=4}public class CapsuleCollider:Collider {public Vector3 center;public float height=2,radius=.3f;public int direction;}public class BoxCollider:Collider{public Vector3 center,size;}public class SphereCollider:Collider{public Vector3 center;public float radius;}
 public struct Bounds {public Vector3 center,size;public Bounds(Vector3 c,Vector3 s){center=c;size=s;}public Vector3 extents=>size*.5f;public Vector3 min=>center-extents;public Vector3 max=>center+extents;public bool Contains(Vector3 p)=>p.x>=min.x&&p.x<=max.x&&p.y>=min.y&&p.y<=max.y&&p.z>=min.z&&p.z<=max.z;public Vector3 ClosestPoint(Vector3 p)=>new Vector3(Mathf.Clamp(p.x,min.x,max.x),Mathf.Clamp(p.y,min.y,max.y),Mathf.Clamp(p.z,min.z,max.z));public void Encapsulate(Bounds b){var lo=new Vector3(Math.Min(min.x,b.min.x),Math.Min(min.y,b.min.y),Math.Min(min.z,b.min.z));var hi=new Vector3(Math.Max(max.x,b.max.x),Math.Max(max.y,b.max.y),Math.Max(max.z,b.max.z));center=(lo+hi)*.5f;size=hi-lo;}}
 public struct LayerMask {public int value;public static implicit operator int(LayerMask m)=>m.value;public static implicit operator LayerMask(int m)=>new LayerMask{value=m};public static int NameToLayer(string n)=>0;public static int GetMask(params string[] n)=>0;}
 public struct Ray {public Vector3 origin,direction;public Ray(Vector3 a,Vector3 b){origin=a;direction=b;}public Vector3 GetPoint(float d)=>origin+direction*d;}
 public struct RaycastHit {public Collider collider;public Transform transform;public Vector3 point,normal;public float distance;}
 public enum QueryTriggerInteraction {UseGlobal,Ignore,Collide}
 public static class Physics {
  public static Collider[] NextOccupants=new Collider[0];public static int OverlapCapsuleNonAlloc(Vector3 first,Vector3 last,float radius,Collider[] results,int mask,QueryTriggerInteraction mode){Array.Copy(NextOccupants,results,Math.Min(results.Length,NextOccupants.Length));return Math.Min(results.Length,NextOccupants.Length);}
  public static bool Linecast(Vector3 first,Vector3 last,int mask,QueryTriggerInteraction mode)=>Filter((last-first).magnitude,mask,mode).Length>0;
  public static void IgnoreCollision(Collider a,Collider b,bool ignore){}
  public const int DefaultRaycastLayers=~4;public static RaycastHit[] NextHits=new RaycastHit[0];
  private static RaycastHit[] Filter(float distance,int mask,QueryTriggerInteraction mode)=>NextHits.Where(h=>h.collider&&h.distance<=distance&&(h.collider.gameObject==null||(mask&(1<<h.collider.gameObject.layer))!=0)&&!(mode==QueryTriggerInteraction.Ignore&&h.collider.isTrigger)).ToArray();
  private static int Fill(RaycastHit[] buffer,float distance,int mask,QueryTriggerInteraction mode){var values=Filter(distance,mask,mode);Array.Copy(values,buffer,Math.Min(buffer.Length,values.Length));return Math.Min(buffer.Length,values.Length);}
  public static bool CheckSphere(Vector3 p,float radius,int mask,QueryTriggerInteraction mode=QueryTriggerInteraction.UseGlobal)=>false;
  public static bool Raycast(Ray ray,out RaycastHit hit,float distance,int mask=~0,QueryTriggerInteraction mode=QueryTriggerInteraction.UseGlobal){var values=Filter(distance,mask,mode).OrderBy(h=>h.distance).ToArray();hit=values.FirstOrDefault();return values.Length>0;}
  public static bool Raycast(Vector3 start,Vector3 direction,out RaycastHit hit,float distance,int mask=~0,QueryTriggerInteraction mode=QueryTriggerInteraction.UseGlobal)=>Raycast(new Ray(start,direction),out hit,distance,mask,mode);
  public static RaycastHit[] RaycastAll(Vector3 start,Vector3 direction,float distance,int mask=~0,QueryTriggerInteraction mode=QueryTriggerInteraction.UseGlobal)=>Filter(distance,mask,mode);
  public static RaycastHit[] RaycastAll(Ray ray,float distance,int mask=~0,QueryTriggerInteraction mode=QueryTriggerInteraction.UseGlobal)=>Filter(distance,mask,mode);
  public static int RaycastNonAlloc(Ray ray,RaycastHit[] buffer,float distance,int mask=~0,QueryTriggerInteraction mode=QueryTriggerInteraction.UseGlobal)=>Fill(buffer,distance,mask,mode);
  public static int RaycastNonAlloc(Vector3 start,Vector3 direction,RaycastHit[] buffer,float distance,int mask=~0,QueryTriggerInteraction mode=QueryTriggerInteraction.UseGlobal)=>Fill(buffer,distance,mask,mode);
  public static int SphereCastNonAlloc(Ray ray,float radius,RaycastHit[] buffer,float distance,int mask=~0,QueryTriggerInteraction mode=QueryTriggerInteraction.UseGlobal)=>Fill(buffer,distance,mask,mode);
  public static RaycastHit[] SphereCastAll(Ray ray,float radius,float distance,int mask=~0,QueryTriggerInteraction mode=QueryTriggerInteraction.UseGlobal)=>Filter(distance,mask,mode);
  public static void SyncTransforms(){}
 }
 public enum ForceMode{Impulse,VelocityChange}public enum RigidbodyInterpolation{None,Interpolate}public enum CollisionDetectionMode{Discrete,ContinuousSpeculative}
 public class Rigidbody:Component {public Vector3 position,velocity,linearVelocity;public Quaternion rotation;public RigidbodyConstraints constraints;public bool isKinematic,useGravity;public float mass,linearDamping,angularDamping,maxAngularVelocity;public RigidbodyInterpolation interpolation;public CollisionDetectionMode collisionDetectionMode;public void AddForce(Vector3 force,ForceMode mode){linearVelocity+=force/Math.Max(.001f,mass);}}
 public struct SoftJointLimit{public float limit;}public class CharacterJoint:Component{public Rigidbody connectedBody;public bool autoConfigureConnectedAnchor,enableCollision,enablePreprocessing,enableProjection;public Vector3 anchor,connectedAnchor,axis,swingAxis;public SoftJointLimit lowTwistLimit,highTwistLimit,swing1Limit,swing2Limit;public float projectionDistance,projectionAngle;}
 public class TerrainData:Object{public Vector3 size=new Vector3(1000,100,1000);}public class Terrain:Behaviour{public static Terrain[] activeTerrains=new Terrain[0];public TerrainData terrainData=new TerrainData();public float SampleHeight(Vector3 p)=>0;}
 [Flags]public enum RigidbodyConstraints{FreezeAll,FreezePosition,FreezeRotation}
 public class RuntimeAnimatorController:Object{} public class Animator:Behaviour {public bool isInitialized=true,applyRootMotion;public AnimatorCullingMode cullingMode;public float speed=1;public RuntimeAnimatorController runtimeAnimatorController;public void ResetTrigger(string name){}public AnimatorStateInfo GetCurrentAnimatorStateInfo(int layer)=>new AnimatorStateInfo();public AnimatorStateInfo GetNextAnimatorStateInfo(int layer)=>new AnimatorStateInfo();public bool IsInTransition(int layer)=>false;public void SetFloat(string name,float value,float dampTime,float deltaTime){}public void Play(int state,int layer=0,float normalizedTime=0){}public AnimatorControllerParameter[] parameters=new AnimatorControllerParameter[0];public void SetFloat(string n,float f){}public void SetTrigger(string n){}public void SetBool(string n,bool b){}public void CrossFade(string n,float f){}public void CrossFade(int n,float f){}public bool HasState(int l,int s)=>true;public static int StringToHash(string s)=>0;}
 public enum AnimatorCullingMode{AlwaysAnimate,CullUpdateTransforms}public struct AnimatorStateInfo{public int shortNameHash,fullPathHash;public float length,speed,speedMultiplier;}public class AnimatorControllerParameter {public string name;public AnimatorControllerParameterType type;}public enum AnimatorControllerParameterType{Float,Trigger,Bool}
 public class AudioSource:Behaviour {public bool playOnAwake,loop;public float spatialBlend,minDistance,maxDistance,dopplerLevel,pitch,volume;public AudioClip clip;public int priority;public bool ignoreListenerVolume;public void Play(){}public AudioRolloffMode rolloffMode;public void PlayOneShot(AudioClip c,float v=1){}public void Stop(){}}
 public class AudioClip:Object {public float length;public static AudioClip Create(string n,int samples,int channels,int frequency,bool stream)=>new AudioClip();public bool SetData(float[] data,int offset)=>true;}public enum AudioRolloffMode{Logarithmic}public static class AudioListener {public static float volume=1;public static bool pause;}
 public class AnimationClip:Object {public float length;}
 public enum ParticleSystemStopBehavior{StopEmittingAndClear}public enum ParticleSystemSimulationSpace{World,Local}public enum ParticleSystemCullingMode{Pause}public enum ParticleSystemShapeType{Box}
 public class Gradient{public void SetKeys(GradientColorKey[] c,GradientAlphaKey[] a){}}public struct GradientColorKey{public GradientColorKey(Color c,float t){}}public struct GradientAlphaKey{public GradientAlphaKey(float a,float t){}}
 public class ParticleSystem:Component {
  public bool isPlaying;public void Play(){isPlaying=true;}public void Stop(bool children=true,ParticleSystemStopBehavior behavior=ParticleSystemStopBehavior.StopEmittingAndClear){isPlaying=false;}
  public MainModule main=new MainModule();public EmissionModule emission=new EmissionModule();public ShapeModule shape=new ShapeModule();public VelocityOverLifetimeModule velocityOverLifetime=new VelocityOverLifetimeModule();public ColorOverLifetimeModule colorOverLifetime=new ColorOverLifetimeModule();
  public class MainModule{public bool loop,playOnAwake;public MinMaxCurve startLifetime,startSpeed,startSize;public MinMaxGradient startColor;public int maxParticles;public ParticleSystemSimulationSpace simulationSpace;public ParticleSystemCullingMode cullingMode;}
  public class EmissionModule{public float rateOverTime;}public class ShapeModule{public ParticleSystemShapeType shapeType;public Vector3 scale;}public class VelocityOverLifetimeModule{public bool enabled;public ParticleSystemSimulationSpace space;public MinMaxCurve x,y,z;}public class ColorOverLifetimeModule{public bool enabled;public MinMaxGradient color;}
  public struct MinMaxCurve{public MinMaxCurve(float a,float b){}public static implicit operator MinMaxCurve(float value)=>new MinMaxCurve();}public struct MinMaxGradient{public MinMaxGradient(Gradient g){}public static implicit operator MinMaxGradient(Color c)=>new MinMaxGradient();}
 }
 public class Shader:Object{public static int PropertyToID(string name)=>name.GetHashCode();public static Shader Find(string name)=>new Shader{name=name};}public class Material:Object{public Material(Shader s){}public Material(Material s){}public Color color;public Texture mainTexture;public int renderQueue;public bool HasProperty(string n)=>true;public bool HasProperty(int n)=>true;public Color GetColor(int n)=>color;public void EnableKeyword(string n){}public void SetColor(string n,Color c){}public void SetFloat(string n,float v){}public void SetInt(string n,int v){}}
 public class Mesh:Object{public Vector3[] vertices;public int[] triangles;public void RecalculateNormals(){}public void RecalculateBounds(){}}
 public class MeshFilter:Component{public Mesh sharedMesh;}public class MaterialPropertyBlock{public void SetColor(int key,Color color){}}public class Renderer:Component{public bool enabled=true;public void GetPropertyBlock(MaterialPropertyBlock block){}public void SetPropertyBlock(MaterialPropertyBlock block){}public Material sharedMaterial;public Bounds bounds;public bool receiveShadows;public Rendering.ShadowCastingMode shadowCastingMode;}public class SkinnedMeshRenderer:Renderer{public Transform[] bones=new Transform[0];public bool updateWhenOffscreen;}public class MeshRenderer:Renderer{}public class ParticleSystemRenderer:Renderer{public float maxParticleSize;}
 public enum LightType{Directional,Point}public enum LightShadows{None,Hard}public enum LightRenderMode{Auto}public class Light:Behaviour{public LightType type;public Color color;public float intensity,range,shadowStrength,shadowBias,shadowNormalBias,bounceIntensity;public LightShadows shadows;public LightRenderMode renderMode;}
 public enum FogMode{Linear}public static class RenderSettings{public static bool fog;public static FogMode fogMode;public static Color fogColor,ambientSkyColor,ambientEquatorColor,ambientGroundColor;public static float fogStartDistance,fogEndDistance,ambientIntensity,reflectionIntensity;public static Rendering.AmbientMode ambientMode;public static Light sun;public static Material skybox;}public static class DynamicGI{public static void UpdateEnvironment(){}}

 public class LineRenderer:Behaviour {public bool useWorldSpace;public int positionCount;public void SetPosition(int p,Vector3 v){}}
 public class Resources {public static T Load<T>(string s) where T:Object=>null;public static T GetBuiltinResource<T>(string s) where T:Object=>null;}
 public class Canvas:Behaviour {public RenderMode renderMode;public int sortingOrder;public float scaleFactor;public bool overrideSorting;public bool isRootCanvas;}
 public enum RenderMode {ScreenSpaceOverlay,ScreenSpaceCamera,WorldSpace}
 public class CanvasGroup:Behaviour {public bool blocksRaycasts,interactable;public float alpha;}
 public class Texture:Object {} public enum TextureFormat{RGBA32}public enum TextureWrapMode{Clamp}public enum FilterMode{Bilinear}public class Texture2D:Texture {public Texture2D(int w,int h,TextureFormat f,bool m){width=w;height=h;}public int width,height;public TextureWrapMode wrapMode;public FilterMode filterMode;public void SetPixel(int x,int y,Color c){}public void Apply(){}}public class Sprite:Object{}public class Font:Object{}
 public enum TextAnchor {MiddleCenter,MiddleLeft,UpperLeft}
 public class ControllerColliderHit { public Rigidbody rigidbody; public Collider collider;public Vector3 normal; } public class Collision {public Rigidbody rigidbody;}
}
namespace UnityEngine.SceneManagement {
 public struct Scene {public static bool operator==(Scene a,Scene b)=>a.name==b.name;public static bool operator!=(Scene a,Scene b)=>a.name!=b.name;public override bool Equals(object o)=>o is Scene s&&this==s;public override int GetHashCode()=>name?.GetHashCode()??0;public string name;public bool isLoaded;public GameObject[] GetRootGameObjects()=>new GameObject[0];}
 public enum LoadSceneMode{Single,Additive}
 public static class SceneManager {public static event Action<Scene,LoadSceneMode> sceneLoaded;public static event Action<Scene> sceneUnloaded;public static void LoadScene(string s){}public static Scene GetActiveScene()=>new Scene();public static void MoveGameObjectToScene(GameObject go,Scene scene){go.scene=scene;}}
}
namespace UnityEngine.AI {
 using UnityEngine;
 public class NavMeshAgent:Behaviour {public bool isOnNavMesh,isStopped;public float stoppingDistance,speed,acceleration,angularSpeed;public Vector3 velocity,desiredVelocity;public int agentTypeID;public void SetDestination(Vector3 v){}public void ResetPath(){}}
 public enum NavMeshObstacleShape{Box}public class NavMeshObstacle:Behaviour{public NavMeshObstacleShape shape;public Vector3 center,size;public bool carving,carveOnlyStationary;}public struct NavMeshTriangulation{public Vector3[] vertices;public int[] indices;}public class NavMeshPath {public NavMeshPathStatus status;public Vector3[] corners=new Vector3[0];}
 public enum NavMeshPathStatus{PathComplete,PathPartial,PathInvalid}
 public struct NavMeshQueryFilter {public int agentTypeID,areaMask;}
 public struct NavMeshHit {public Vector3 position;}
 public static class NavMesh {public static NavMeshTriangulation NextTriangulation=new NavMeshTriangulation{vertices=new Vector3[0],indices=new int[0]};public static NavMeshTriangulation CalculateTriangulation()=>NextTriangulation;public const int AllAreas=-1;public static bool NextSampleResult=true,NextPathResult=true;public static bool SamplePosition(Vector3 p,out NavMeshHit h,float d,NavMeshQueryFilter f){h=new NavMeshHit{position=p};return NextSampleResult;}public static bool SamplePosition(Vector3 p,out NavMeshHit h,float d,int f){h=new NavMeshHit{position=p};return NextSampleResult;}public static bool CalculatePath(Vector3 a,Vector3 b,NavMeshQueryFilter f,NavMeshPath p){p.status=NextPathResult?NavMeshPathStatus.PathComplete:NavMeshPathStatus.PathInvalid;return NextPathResult;}public static bool CalculatePath(Vector3 a,Vector3 b,int mask,NavMeshPath p){p.status=NextPathResult?NavMeshPathStatus.PathComplete:NavMeshPathStatus.PathInvalid;return NextPathResult;}}
}
namespace UnityEngine.Events {
 public delegate void UnityAction();public delegate void UnityAction<T>(T arg);
 public class UnityEventBase {public int GetPersistentEventCount()=>0;public Object GetPersistentTarget(int i)=>null;public string GetPersistentMethodName(int i)=>null;} public class UnityEvent:UnityEventBase {private readonly List<UnityAction> listeners=new List<UnityAction>();public void AddListener(UnityAction a){listeners.Add(a);}public void RemoveListener(UnityAction a){listeners.Remove(a);}public void RemoveAllListeners(){listeners.Clear();}public void Invoke(){foreach(var action in listeners.ToArray())action();}}
 public class UnityEvent<T>:UnityEventBase {private readonly List<UnityAction<T>> listeners=new List<UnityAction<T>>();public void AddListener(UnityAction<T> a){listeners.Add(a);}public void RemoveListener(UnityAction<T> a){listeners.Remove(a);}public void RemoveAllListeners(){listeners.Clear();}public void Invoke(T arg){foreach(var action in listeners.ToArray())action(arg);}}
}
namespace UnityEngine.UI {
 using UnityEngine;using UnityEngine.Events;
 public class Graphic:Behaviour {public Color color;public bool raycastTarget;public RectTransform rectTransform;}
 public class MaskableGraphic:Graphic {}
 public class RawImage:MaskableGraphic{public Texture texture;} public class Text:MaskableGraphic {public string text;public int fontSize;public Font font;}public class Image:MaskableGraphic {public Sprite sprite;public Type type;public float fillAmount;public FillMethod fillMethod;public int fillOrigin;public enum Type{Simple,Sliced,Filled}public enum FillMethod{Horizontal,Vertical,Radial360}}
 public class Selectable:Behaviour {public bool IsInteractable()=>interactable;public bool interactable;public ColorBlock colors;public Transition transition;public Graphic targetGraphic;public enum Transition{None,ColorTint,SpriteSwap,Animation}}
 public struct ColorBlock {public Color normalColor,highlightedColor,pressedColor,selectedColor,disabledColor;public float colorMultiplier,fadeDuration;}
 public class Button:Selectable {public UnityEvent onClick=new UnityEvent();}
 public class Slider:Selectable {public float minValue,maxValue,value;public bool wholeNumbers;public RectTransform handleRect,fillRect;public UnityEvent<float> onValueChanged=new UnityEvent<float>();public void SetValueWithoutNotify(float x){value=x;}}
 public class Toggle:Selectable {public bool isOn;public Graphic graphic;public UnityEvent<bool> onValueChanged=new UnityEvent<bool>();public void SetIsOnWithoutNotify(bool b){isOn=b;}}
 public class CanvasScaler:Behaviour {public ScaleMode uiScaleMode;public Vector2 referenceResolution;public float matchWidthOrHeight;public ScreenMatchMode screenMatchMode;public enum ScaleMode{ConstantPixelSize,ScaleWithScreenSize,ConstantPhysicalSize}public enum ScreenMatchMode{MatchWidthOrHeight,Expand,Shrink}}
 public class GraphicRaycaster:Behaviour {}
 public class LayoutElement:Behaviour {public float minWidth,minHeight,preferredWidth,preferredHeight,flexibleWidth,flexibleHeight;}
 public class VerticalLayoutGroup:Behaviour {public float spacing;public bool childControlHeight,childControlWidth,childForceExpandHeight,childForceExpandWidth;public TextAnchor childAlignment;public RectOffset padding;}
 public class HorizontalLayoutGroup:VerticalLayoutGroup{}
 public class ContentSizeFitter:Behaviour {public FitMode verticalFit,horizontalFit;public enum FitMode{Unconstrained,MinSize,PreferredSize}}
 public class RectOffset {public RectOffset(int a,int b,int c,int d){}}
}
namespace TMPro {
 using UnityEngine;using UnityEngine.UI;
 public class TMP_FontAsset:Object{}
 public enum TextAlignmentOptions {Left,Right,Center,Midline,TopLeft,TopRight,BottomLeft,BottomRight,MidlineLeft,MidlineRight}
 [Flags]public enum FontStyles {Normal=0,Bold=1,Italic=2}
 public enum TextOverflowModes {Overflow,Ellipsis,Truncate}
 public enum TextWrappingModes{Normal,NoWrap}public class TMP_Text:MaskableGraphic {public Vector4 margin;public TextWrappingModes textWrappingMode;public string text;public float fontSize,characterSpacing,lineSpacing;public TMP_FontAsset font;public TextAlignmentOptions alignment;public FontStyles fontStyle;public bool enableWordWrapping,enableAutoSizing;public float fontSizeMin,fontSizeMax;public TextOverflowModes overflowMode;public bool richText;public void SetText(string s){text=s;}}
 public class TextMeshProUGUI:TMP_Text {}public class TextMeshPro:TMP_Text {}
 public class TMP_InputField:Selectable {public string text;public int characterLimit;public TMP_Text textComponent;public Graphic placeholder;public Color caretColor,selectionColor;public bool customCaretColor;public void Select(){}public void ActivateInputField(){}}
 public static class TMP_Settings {public static TMP_FontAsset defaultFontAsset;}
}
namespace UnityEngine.EventSystems {
 using UnityEngine;
 public class EventSystem:Behaviour {public static EventSystem current;public bool IsPointerOverGameObject()=>false;public void SetSelectedGameObject(GameObject o){}}
 public class StandaloneInputModule:Behaviour {}
 public interface IPointerEnterHandler {void OnPointerEnter(PointerEventData e);}public class PointerEventData{}
}

namespace UnityEngine.Rendering {public enum AmbientMode{Trilight}public enum BlendMode{SrcAlpha,OneMinusSrcAlpha}public enum RenderQueue{Transparent}public enum ShadowCastingMode{Off}}
