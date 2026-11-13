using TMPro;
using UnityEngine;

public enum WorldService { Ammo, WeaponUpgrade, HealthUpgrade, QuickReload }

/// <summary>A physical point of interest. Input is owned by WorldProgression, never by each station.</summary>
public sealed class WorldInteraction : MonoBehaviour
{
    public enum InteractionKind { ChurchCheckpoint, GuardCheckpoint, Ammo, WeaponUpgrade, HealthUpgrade, QuickReload }

    public WorldProgression Owner { get; private set; }
    public InteractionKind Kind { get; private set; }
    public string Title { get; private set; }
    public float InteractionRange { get; private set; } = 3f;
    public Transform GeometryRoot { get; private set; }
    public Vector3 FocusPoint => transform.position + Vector3.up * 1.25f;

    private TextMeshPro caption;
    private Light statusLight;
    private Transform lever;
    private Material metal, screen, trim;
    private string lastCaption;
    private bool presentationBuilt;

    public void Configure(WorldProgression owner, InteractionKind kind, string title,
        float range = 3f, Transform geometryRoot = null)
    {
        Owner = owner;
        Kind = kind;
        Title = title;
        InteractionRange = Mathf.Clamp(range, 1f, 5f);
        GeometryRoot = geometryRoot != null ? geometryRoot : transform;
        if (Owner != null) Owner.Register(this);
    }

    public bool TryGetService(out WorldService service)
    {
        switch (Kind)
        {
            case InteractionKind.Ammo: service = WorldService.Ammo; return true;
            case InteractionKind.WeaponUpgrade: service = WorldService.WeaponUpgrade; return true;
            case InteractionKind.HealthUpgrade: service = WorldService.HealthUpgrade; return true;
            case InteractionKind.QuickReload: service = WorldService.QuickReload; return true;
            default: service = WorldService.Ammo; return false;
        }
    }

    /// <summary>Small equipment and signs complement the authored props; no arena geometry is replaced.</summary>
    public void BuildPresentation(bool attachToExistingProp)
    {
        if (presentationBuilt) return;
        presentationBuilt = true;
        Shader shader = Shader.Find("Standard");
        if (shader == null) return;
        metal = MakeMaterial(shader, new Color(0.08f, 0.11f, 0.12f), 0.75f, 0.32f);
        trim = MakeMaterial(shader, new Color(0.44f, 0.39f, 0.23f), 0.8f, 0.24f);
        screen = MakeMaterial(shader, new Color(0.11f, 0.35f, 0.27f), 0.1f, 0.4f);
        screen.EnableKeyword("_EMISSION");

        // The narrow control head sits above the worn cabinet/table from the existing art pack.
        Vector3 head = new Vector3(0f, attachToExistingProp ? 1.55f : 1.3f, 0.02f);
        Piece("Control Housing", PrimitiveType.Cube, head, new Vector3(0.88f, 0.54f, 0.23f), metal);
        Piece("Upper Brass Trim", PrimitiveType.Cube, head + new Vector3(0, 0.27f, 0), new Vector3(0.9f, 0.035f, 0.25f), trim);
        Piece("Lower Brass Trim", PrimitiveType.Cube, head + new Vector3(0, -0.27f, 0), new Vector3(0.9f, 0.035f, 0.25f), trim);
        Piece("Status Strip", PrimitiveType.Cube, head + new Vector3(-0.36f, 0, 0.125f), new Vector3(0.035f, 0.42f, 0.015f), screen);
        Piece("Left Support", PrimitiveType.Cylinder, head + new Vector3(-0.27f, -0.3f, -0.06f), new Vector3(0.04f, 0.16f, 0.04f), trim);
        Piece("Right Support", PrimitiveType.Cylinder, head + new Vector3(0.27f, -0.3f, -0.06f), new Vector3(0.04f, 0.16f, 0.04f), trim);
        lever = new GameObject("Switch Hinge").transform;
        lever.SetParent(transform, false);
        lever.localPosition = head + new Vector3(0.33f, -0.13f, 0.17f);
        var stalk = Piece("Switch", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.045f, 0.08f, 0.045f), trim);
        stalk.transform.SetParent(lever, false);
        stalk.transform.localPosition = new Vector3(0, 0.08f, 0);
        var grip = Piece("Switch Grip", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.075f, metal);
        grip.transform.SetParent(lever, false);
        grip.transform.localPosition = new Vector3(0, 0.17f, 0);

        var textObject = new GameObject("Station Plate", typeof(RectTransform), typeof(TextMeshPro));
        textObject.transform.SetParent(transform, false);
        textObject.transform.localPosition = head + new Vector3(-0.015f, 0.015f, 0.129f);
        textObject.transform.localRotation = Quaternion.Euler(0, 180, 0);
        caption = textObject.GetComponent<TextMeshPro>();
        caption.font = TMP_Settings.defaultFontAsset;
        caption.fontSize = 1.55f;
        caption.color = new Color(0.91f, 0.88f, 0.72f);
        caption.alignment = TextAlignmentOptions.Center;
        caption.textWrappingMode = TextWrappingModes.NoWrap;
        caption.overflowMode = TextOverflowModes.Ellipsis;
        caption.rectTransform.sizeDelta = new Vector2(0.66f, 0.39f);

        var beacon = new GameObject("Station Beacon", typeof(Light));
        beacon.transform.SetParent(transform, false);
        beacon.transform.localPosition = new Vector3(0f, head.y + 0.45f, 0.18f);
        statusLight = beacon.GetComponent<Light>();
        statusLight.type = LightType.Point;
        statusLight.range = 3f;
        statusLight.intensity = 0.8f;
        statusLight.shadows = LightShadows.None;
    }

    public void RefreshPresentation(string detail, bool ready, bool completed, bool focused)
    {
        string next = Title + "\n<size=65%>" + detail + "</size>";
        if (caption != null && next != lastCaption) { caption.text = next; lastCaption = next; }
        Color color = ready || completed ? new Color(0.17f, 0.72f, 0.54f) : new Color(0.85f, 0.42f, 0.14f);
        if (screen != null)
        {
            screen.color = color * 0.3f;
            screen.SetColor("_EmissionColor", color * (focused ? 2.2f : 1.1f));
        }
        if (statusLight != null) { statusLight.color = color; statusLight.intensity = focused ? 1.35f : 0.65f; }
        if (lever != null)
            lever.localRotation = Quaternion.Slerp(lever.localRotation, Quaternion.Euler(completed ? -35 : 35, 0, 0),
                1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
    }

    private GameObject Piece(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
    {
        var piece = GameObject.CreatePrimitive(type);
        piece.name = name;
        piece.transform.SetParent(transform, false);
        piece.transform.localPosition = position;
        piece.transform.localScale = scale;
        piece.GetComponent<Renderer>().sharedMaterial = material;
        // Equipment is placed beside the existing walkways; do not alter the baked navigation graph.
        var collider = piece.GetComponent<Collider>();
        if (collider != null) { collider.enabled = false; Destroy(collider); }
        return piece;
    }

    private static Material MakeMaterial(Shader shader, Color color, float metallic, float smoothness)
    {
        var material = new Material(shader);
        material.color = color;
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Glossiness", smoothness);
        return material;
    }

    private void OnDestroy()
    {
        if (Owner != null) Owner.Unregister(this);
        if (metal != null) Destroy(metal);
        if (screen != null) Destroy(screen);
        if (trim != null) Destroy(trim);
    }
}
