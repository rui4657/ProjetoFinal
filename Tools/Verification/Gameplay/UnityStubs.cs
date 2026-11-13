using System;
using System.Collections;
using System.Collections.Generic;
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
  public static Dictionary<Type,Object> Registry=new Dictionary<Type,Object>();public string name;public static implicit operator bool(Object o)=>o!=null;
  public static T FindFirstObjectByType<T>() where T:Object=>Registry.TryGetValue(typeof(T),out Object value)?value as T:null;
  public static T[] FindObjectsByType<T>(FindObjectsSortMode s) where T:Object=>new T[0];
  public static T[] FindObjectsByType<T>(FindObjectsInactive i,FindObjectsSortMode s) where T:Object=>new T[0];
  public static T FindObjectOfType<T>() where T:Object=>null;
  public static T[] FindObjectsOfType<T>() where T:Object=>new T[0];
  public static T Instantiate<T>(T o,Vector3 p,Quaternion r) where T:Object=>o;
  public static T Instantiate<T>(T o) where T:Object=>o;
  public static void Destroy(Object o,float delay=0){}public static void DontDestroyOnLoad(Object o){}
 }
 public enum FindObjectsSortMode{None,InstanceID}public enum FindObjectsInactive{Include,Exclude}
 public class Component:Object {
  public GameObject gameObject;public Transform transform;
  public T GetComponent<T>() where T:class=>null;public T GetComponentInChildren<T>(bool inactive=false) where T:class=>null;public T GetComponentInParent<T>() where T:class=>null;
  public T[] GetComponentsInChildren<T>(bool inactive=false) where T:class=>new T[0];public T[] GetComponents<T>() where T:class=>new T[0];public bool TryGetComponent<T>(out T value) where T:class{value=null;return false;}
  public bool CompareTag(string t)=>false;
 }
 public class Behaviour:Component {public bool enabled=true;public bool isActiveAndEnabled=>enabled;}
 public class MonoBehaviour:Behaviour {public readonly List<Coroutine> Coroutines=new List<Coroutine>();public Coroutine StartCoroutine(IEnumerator e){var c=new Coroutine{Enumerator=e};Coroutines.Add(c);c.Tick();return c;}public void StopCoroutine(Coroutine c){c.Stopped=true;}public void StopAllCoroutines(){foreach(var c in Coroutines)c.Stopped=true;}}
 public class Coroutine:Object {public IEnumerator Enumerator;public bool Stopped,Complete;public bool Tick(){if(Stopped||Complete)return false;Complete=!Enumerator.MoveNext();return !Complete;}} public class WaitForSeconds {public WaitForSeconds(float s){}}public class WaitForSecondsRealtime {public WaitForSecondsRealtime(float s){}}
 public class GameObject:Object {
  public GameObject(string n,params Type[] components){name=n;}
  public bool activeSelf=true;public bool activeInHierarchy=>activeSelf;public int layer;public Transform transform;public SceneManagement.Scene scene;
  public void SetActive(bool v){activeSelf=v;}public T AddComponent<T>() where T:Component=>null;
  public T GetComponent<T>() where T:class=>null;public T GetComponentInChildren<T>(bool inactive=false) where T:class=>null;public T[] GetComponentsInChildren<T>(bool inactive=false) where T:class=>new T[0];
  public static GameObject Find(string n)=>null;public static GameObject FindGameObjectWithTag(string t)=>null;
 }
 public class Transform:Component {public Vector3 position,localPosition,localScale,eulerAngles,localEulerAngles;public Quaternion rotation,localRotation;public Vector3 forward,up,right;public Transform parent;public int childCount;public int GetSiblingIndex()=>0;public void SetSiblingIndex(int i){}public void SetAsFirstSibling(){}public void Rotate(Vector3 v){}public bool IsChildOf(Transform p)=>false;public void SetParent(Transform p,bool keep=true){}public Transform GetChild(int i)=>null;public Transform Find(string n)=>null;public Vector3 TransformDirection(Vector3 v)=>v;public Vector3 InverseTransformPoint(Vector3 v)=>v;}
 public class RectTransform:Transform {public Vector2 anchorMin,anchorMax,pivot,anchoredPosition,sizeDelta,offsetMin,offsetMax;public Rect rect;public void SetSizeWithCurrentAnchors(Axis a,float v){}public enum Axis{Horizontal,Vertical}}
 public struct Rect {public float x,y,width,height,xMin,yMin,xMax,yMax;}
 public struct Vector2 {public float x,y;public Vector2(float a,float b){x=a;y=b;}public static Vector2 ClampMagnitude(Vector2 a,float b)=>a;public static Vector2 Lerp(Vector2 a,Vector2 b,float t)=>a;public static Vector2 zero=>new Vector2();public static Vector2 one=>new Vector2(1,1);public static Vector2 operator*(Vector2 a,float b)=>a;public float sqrMagnitude=>0;public float magnitude=>0;}
 public struct Vector4 {public float x,y,z,w;public Vector4(float a,float b,float c,float d){x=a;y=b;z=c;w=d;}public static Vector4 zero=>new Vector4();} public struct Vector3 {public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public static Vector3 zero=>new Vector3();public static Vector3 one=>new Vector3(1,1,1);public static Vector3 up=>new Vector3(0,1,0);public static Vector3 down=>new Vector3(0,-1,0);public static Vector3 forward=>new Vector3(0,0,1);public static Vector3 right=>new Vector3(1,0,0);public float magnitude=>0;public float sqrMagnitude=>0;public Vector3 normalized=>this;public static Vector3 operator+(Vector3 a,Vector3 b)=>a;public static Vector3 operator-(Vector3 a,Vector3 b)=>a;public static Vector3 operator*(Vector3 a,float b)=>a;public static Vector3 operator*(float b,Vector3 a)=>a;public static Vector3 operator/(Vector3 a,float b)=>a;public static bool operator==(Vector3 a,Vector3 b)=>false;public static bool operator!=(Vector3 a,Vector3 b)=>true;public static float Distance(Vector3 a,Vector3 b)=>0;public static Vector3 MoveTowards(Vector3 a,Vector3 b,float c)=>a;public static Vector3 ClampMagnitude(Vector3 a,float b)=>a;public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a;public override bool Equals(object o)=>false;public override int GetHashCode()=>0;}
 public struct Quaternion {public static Quaternion identity=>new Quaternion();public static Quaternion Euler(float a,float b,float c)=>identity;public static Quaternion Euler(Vector3 a)=>identity;public static Quaternion LookRotation(Vector3 a)=>identity;public static Quaternion Slerp(Quaternion a,Quaternion b,float c)=>a;public static Quaternion operator*(Quaternion a,Quaternion b)=>a;public static Vector3 operator*(Quaternion a,Vector3 b)=>b;}
 public struct Color {public float r,g,b,a;public Color(float x,float y,float z,float w=1){r=x;g=y;b=z;a=w;}public static Color white=>new Color(1,1,1);public static Color black=>new Color(0,0,0);public static Color clear=>new Color(0,0,0,0);public static Color red=>new Color(1,0,0);public static Color Lerp(Color x,Color y,float t)=>x;public static implicit operator Color32(Color c)=>new Color32();}
 public struct Color32 {public byte r,g,b,a;public Color32(byte x,byte y,byte z,byte w){r=x;g=y;b=z;a=w;}public static implicit operator Color(Color32 c)=>new Color();}
 public static class ColorUtility {public static bool TryParseHtmlString(string s,out Color c){c=new Color();return true;}}
 public static class Mathf {public const float PI=3.14f;public const float Deg2Rad=.01f;public const float Rad2Deg=57f;public static int Max(int a,int b)=>Math.Max(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int Min(int a,int b)=>Math.Min(a,b);public static float Min(float a,float b)=>Math.Min(a,b);public static float Clamp(float a,float b,float c)=>Math.Max(b,Math.Min(c,a));public static int Clamp(int a,int b,int c)=>Math.Max(b,Math.Min(c,a));public static float Clamp01(float x)=>Clamp(x,0,1);public static float Pow(float a,float b)=>(float)Math.Pow(a,b);public static int RoundToInt(float a)=>(int)Math.Round(a);public static int CeilToInt(float a)=>(int)Math.Ceiling(a);public static int FloorToInt(float a)=>(int)Math.Floor(a);public static float Sqrt(float a)=>(float)Math.Sqrt(a);public static float Abs(float a)=>Math.Abs(a);public static float Sin(float a)=>(float)Math.Sin(a);public static float Cos(float a)=>(float)Math.Cos(a);public static float Exp(float a)=>(float)Math.Exp(a);public static float Lerp(float a,float b,float t)=>a+(b-a)*t;public static float MoveTowards(float a,float b,float d)=>b;public static float SmoothDamp(float a,float b,ref float c,float d)=>b;public static bool Approximately(float a,float b)=>a==b;}
 public static class Time {public static float timeScale=1,deltaTime=.016f,unscaledDeltaTime=.016f,time,unscaledTime;}
 public static class Input {public static readonly HashSet<KeyCode> KeysDown=new HashSet<KeyCode>();public static bool GetKey(KeyCode k)=>false;public static bool GetKeyDown(KeyCode k)=>KeysDown.Contains(k);public static bool GetButton(string k)=>false;public static bool GetButtonDown(string k)=>false;public static bool GetMouseButton(int b)=>false;public static bool GetMouseButtonDown(int b)=>false;public static float GetAxis(string k)=>0;public static float GetAxisRaw(string k)=>0;}
 public enum KeyCode {R,E,B,Escape,Return,LeftShift,RightShift,Space}
 public static class Random {public static int Range(int a,int b)=>a;public static float Range(float a,float b)=>a;public static Vector2 insideUnitCircle=>Vector2.zero;}
 public static class Debug {public static void Log(object o){}public static void LogWarning(object o,Object context=null){}public static void LogError(object o,Object context=null){}}
 public enum CursorLockMode{None,Locked,Confined}public static class Cursor {public static CursorLockMode lockState;public static bool visible;}
 public static class Screen {public static bool fullScreen;public static int width=1920,height=1080;public static Rect safeArea;}
 public static class Application {public static string persistentDataPath="/tmp";public static bool isFocused=true;public static bool isPlaying;public static bool CanStreamedLevelBeLoaded(string s)=>true;public static void Quit(){}}
 public static class PlayerPrefs {public static Dictionary<string,object> Values=new Dictionary<string,object>();public static int GetInt(string n,int d=0)=>Values.TryGetValue(n,out object v)?(int)v:d;public static float GetFloat(string n,float d=0)=>Values.TryGetValue(n,out object v)?(float)v:d;public static string GetString(string n,string d="")=>Values.TryGetValue(n,out object v)?(string)v:d;public static void SetInt(string n,int v){Values[n]=v;}public static void SetFloat(string n,float v){Values[n]=v;}public static void SetString(string n,string v){Values[n]=v;}public static void DeleteKey(string n){Values.Remove(n);}public static bool HasKey(string n)=>Values.ContainsKey(n);public static void Save(){}}

 public class Camera:Behaviour {public static Camera main;public float fieldOfView;public Ray ViewportPointToRay(Vector3 p)=>new Ray();}
 public class CharacterController:Collider {public bool isGrounded;public bool enabled;public float height,radius;public Vector3 velocity;public CollisionFlags Move(Vector3 p)=>CollisionFlags.None;}
 public class Collider:Component {public bool enabled;public bool isTrigger;public Bounds bounds;public Vector3 ClosestPoint(Vector3 p)=>p;}
 [Flags]public enum CollisionFlags{None=0,Above=1,Below=2,Sides=4}public class CapsuleCollider:Collider {}public class BoxCollider:Collider{}
 public struct Bounds {public Vector3 min,max,center,size,extents;}
 public struct LayerMask {public int value;public static implicit operator int(LayerMask m)=>m.value;public static implicit operator LayerMask(int m)=>new LayerMask{value=m};public static int NameToLayer(string n)=>0;public static int GetMask(params string[] n)=>0;}
 public struct Ray {public Vector3 origin,direction;public Ray(Vector3 a,Vector3 b){origin=a;direction=b;}public Vector3 GetPoint(float d)=>origin+direction*d;}
 public struct RaycastHit {public Collider collider;public Transform transform;public Vector3 point,normal;public float distance;}
 public enum QueryTriggerInteraction {UseGlobal,Ignore,Collide}
 public static class Physics {public const int DefaultRaycastLayers=~4;public static bool CheckSphere(Vector3 p,float r,int mask,QueryTriggerInteraction q=QueryTriggerInteraction.UseGlobal)=>false;public static bool Raycast(Ray r,out RaycastHit h,float d,int mask=~0,QueryTriggerInteraction q=QueryTriggerInteraction.UseGlobal){h=new RaycastHit();return false;}public static bool Raycast(Vector3 a,Vector3 b,out RaycastHit h,float d,int mask=~0,QueryTriggerInteraction q=QueryTriggerInteraction.UseGlobal){h=new RaycastHit();return false;}public static RaycastHit[] RaycastAll(Vector3 a,Vector3 b,float d,int mask=~0,QueryTriggerInteraction q=QueryTriggerInteraction.UseGlobal)=>new RaycastHit[0];public static int RaycastNonAlloc(Ray a,RaycastHit[] h,float d,int mask=~0,QueryTriggerInteraction q=QueryTriggerInteraction.UseGlobal)=>0;public static RaycastHit[] RaycastAll(Ray a,float d,int mask=~0,QueryTriggerInteraction q=QueryTriggerInteraction.UseGlobal)=>new RaycastHit[0];public static int RaycastNonAlloc(Vector3 a,Vector3 b,RaycastHit[] h,float d,int mask=~0,QueryTriggerInteraction q=QueryTriggerInteraction.UseGlobal)=>0;public static void SyncTransforms(){}}
 public class Rigidbody:Component {public Vector3 velocity,linearVelocity;public RigidbodyConstraints constraints;public bool isKinematic;}
 [Flags]public enum RigidbodyConstraints{FreezeAll,FreezePosition,FreezeRotation}
 public class Animator:Behaviour {public AnimatorControllerParameter[] parameters=new AnimatorControllerParameter[0];public void SetFloat(string n,float f){}public void SetTrigger(string n){}public void SetBool(string n,bool b){}public void CrossFade(string n,float f){}public void CrossFade(int n,float f){}public bool HasState(int l,int s)=>true;public static int StringToHash(string s)=>0;}
 public class AnimatorControllerParameter {public string name;public AnimatorControllerParameterType type;}public enum AnimatorControllerParameterType{Float,Trigger,Bool}
 public class AudioSource:Behaviour {public bool playOnAwake,loop;public float spatialBlend,minDistance,maxDistance,dopplerLevel,pitch,volume;public AudioRolloffMode rolloffMode;public void PlayOneShot(AudioClip c,float v=1){}public void Stop(){}}
 public class AudioClip:Object {public float length;public static AudioClip Create(string n,int samples,int channels,int frequency,bool stream)=>new AudioClip();public bool SetData(float[] data,int offset)=>true;}public enum AudioRolloffMode{Logarithmic}public static class AudioListener {public static float volume=1;public static bool pause;}
 public class AnimationClip:Object {public float length;}
 public class ParticleSystem:Component {public void Play(){}}
 public class LineRenderer:Behaviour {public bool useWorldSpace;public int positionCount;public void SetPosition(int p,Vector3 v){}}
 public class Resources {public static T Load<T>(string s) where T:Object=>null;public static T GetBuiltinResource<T>(string s) where T:Object=>null;}
 public class Canvas:Behaviour {public RenderMode renderMode;public int sortingOrder;public float scaleFactor;public bool overrideSorting;public bool isRootCanvas;}
 public enum RenderMode {ScreenSpaceOverlay,ScreenSpaceCamera,WorldSpace}
 public class CanvasGroup:Behaviour {public bool blocksRaycasts,interactable;public float alpha;}
 public class Texture:Object {} public enum TextureFormat{RGBA32}public enum TextureWrapMode{Clamp}public enum FilterMode{Bilinear}public class Texture2D:Texture {public Texture2D(int w,int h,TextureFormat f,bool m){}public int width;public TextureWrapMode wrapMode;public FilterMode filterMode;public void SetPixel(int x,int y,Color c){}public void Apply(){}}public class Sprite:Object{}public class Font:Object{}
 public enum TextAnchor {MiddleCenter,MiddleLeft,UpperLeft}
 public class ControllerColliderHit { public Rigidbody rigidbody; public Collider collider;public Vector3 normal; } public class Collision {public Rigidbody rigidbody;}
}
namespace UnityEngine.SceneManagement {
 public struct Scene {public string name;public bool isLoaded;public GameObject[] GetRootGameObjects()=>new GameObject[0];}
 public enum LoadSceneMode{Single,Additive}
 public static class SceneManager {public static event Action<Scene,LoadSceneMode> sceneLoaded;public static event Action<Scene> sceneUnloaded;public static void LoadScene(string s){}public static Scene GetActiveScene()=>new Scene();}
}
namespace UnityEngine.AI {
 using UnityEngine;
 public class NavMeshAgent:Behaviour {public bool isOnNavMesh,isStopped;public float stoppingDistance,speed;public Vector3 velocity,desiredVelocity;public int agentTypeID;public void SetDestination(Vector3 v){}public void ResetPath(){}}
 public class NavMeshPath {public NavMeshPathStatus status;}
 public enum NavMeshPathStatus{PathComplete,PathPartial,PathInvalid}
 public struct NavMeshQueryFilter {public int agentTypeID,areaMask;}
 public struct NavMeshHit {public Vector3 position;}
 public static class NavMesh {public const int AllAreas=-1;public static bool SamplePosition(Vector3 p,out NavMeshHit h,float d,NavMeshQueryFilter f){h=new NavMeshHit();return true;}public static bool SamplePosition(Vector3 p,out NavMeshHit h,float d,int f){h=new NavMeshHit();return true;}public static bool CalculatePath(Vector3 a,Vector3 b,NavMeshQueryFilter f,NavMeshPath p)=>true;}
}
namespace UnityEngine.Events {
 public delegate void UnityAction();public delegate void UnityAction<T>(T arg);
 public class UnityEventBase {public int GetPersistentEventCount()=>0;public Object GetPersistentTarget(int i)=>null;public string GetPersistentMethodName(int i)=>null;} public class UnityEvent:UnityEventBase {public void AddListener(UnityAction a){}public void RemoveListener(UnityAction a){}public void RemoveAllListeners(){}public void Invoke(){}}
 public class UnityEvent<T>:UnityEventBase {public void AddListener(UnityAction<T> a){}public void RemoveListener(UnityAction<T> a){}public void RemoveAllListeners(){}public void Invoke(T a){}}
}
namespace UnityEngine.UI {
 using UnityEngine;using UnityEngine.Events;
 public class Graphic:Behaviour {public Color color;public bool raycastTarget;public RectTransform rectTransform;}
 public class MaskableGraphic:Graphic {}
 public class RawImage:MaskableGraphic{public Texture texture;} public class Text:MaskableGraphic {public string text;public int fontSize;public Font font;}public class Image:MaskableGraphic {public Sprite sprite;public Type type;public float fillAmount;public FillMethod fillMethod;public int fillOrigin;public enum Type{Simple,Sliced,Filled}public enum FillMethod{Horizontal,Vertical,Radial360}}
 public class Selectable:Behaviour {public bool IsInteractable()=>interactable;public bool interactable;public ColorBlock colors;public Transition transition;public Graphic targetGraphic;public enum Transition{None,ColorTint,SpriteSwap,Animation}}
 public struct ColorBlock {public Color normalColor,highlightedColor,pressedColor,selectedColor,disabledColor;public float colorMultiplier,fadeDuration;}
 public class Button:Selectable {public UnityEvent onClick=new UnityEvent();}
 public class Slider:Selectable {public float minValue,maxValue,value;public bool wholeNumbers;public RectTransform handleRect,fillRect;public UnityEvent<float> onValueChanged=new UnityEvent<float>();public void SetValueWithoutNotify(float x){}}
 public class Toggle:Selectable {public bool isOn;public Graphic graphic;public UnityEvent<bool> onValueChanged=new UnityEvent<bool>();public void SetIsOnWithoutNotify(bool b){}}
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
 public class TextMeshProUGUI:TMP_Text {}
 public class TMP_InputField:Selectable {public string text;public int characterLimit;public TMP_Text textComponent;public Graphic placeholder;public Color caretColor,selectionColor;public bool customCaretColor;public void Select(){}public void ActivateInputField(){}}
 public static class TMP_Settings {public static TMP_FontAsset defaultFontAsset;}
}
namespace UnityEngine.EventSystems {
 using UnityEngine;
 public class EventSystem:Behaviour {public static EventSystem current;public bool IsPointerOverGameObject()=>false;public void SetSelectedGameObject(GameObject o){}}
 public class StandaloneInputModule:Behaviour {}
 public interface IPointerEnterHandler {void OnPointerEnter(PointerEventData e);}public class PointerEventData{}
}
