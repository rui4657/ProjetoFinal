using TMPro;
using UnityEngine;

/// <summary>Small, visible world rewards. Timers use game time and freeze while paused.</summary>
public sealed class PowerUpPickup : MonoBehaviour
{
    public SurvivalPowerUp Kind { get; private set; }
    public float ExpiresAt { get; private set; }
    private SurvivalSession owner;
    private Transform symbol;
    private TextMeshPro label;
    private Material material;
    private Mesh mesh;
    private Vector3 restingPosition;
    private string title;
    private int lastSeconds = -1;
    private bool motionEnabled = true;
    private float nextSettingsRead;
    private readonly RaycastHit[] sightHits = new RaycastHit[16];

    public static PowerUpPickup Create(SurvivalSession session, SurvivalPowerUp kind, Vector3 position)
    {
        if (session == null || session.IsGameOver) return null;
        var go = new GameObject("Power-up " + kind);
        go.layer = 2; // Cosmetic geometry must never intercept bullets or AI sight.
        go.transform.SetParent(session.transform, false);
        go.transform.position = position + Vector3.up * 0.75f;
        var pickup = go.AddComponent<PowerUpPickup>();
        pickup.owner = session;
        pickup.Kind = kind;
        pickup.restingPosition = go.transform.position;
        pickup.ExpiresAt = Time.time + 25f;
        pickup.BuildVisual();
        return pickup;
    }

    void BuildVisual()
    {
        Color tint = Kind == SurvivalPowerUp.MaxAmmo ? new Color(0.25f, 0.9f, 0.55f)
            : Kind == SurvivalPowerUp.DoublePoints ? new Color(1f, 0.72f, 0.22f) : new Color(0.95f, 0.24f, 0.22f);
        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader != null)
        {
            material = new Material(shader);
            material.color = tint;
            if (material.HasProperty("_EmissionColor"))
            { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", tint * 1.3f); }
            var body = new GameObject("Reward crystal", typeof(MeshFilter), typeof(MeshRenderer));
            body.layer = 2; body.transform.SetParent(transform, false); symbol = body.transform;
            mesh = new Mesh(); mesh.name = "Reward octahedron";
            mesh.vertices = new[] { new Vector3(0, .34f, 0), new Vector3(0, -.34f, 0),
                new Vector3(.23f, 0, 0), new Vector3(0, 0, .23f), new Vector3(-.23f, 0, 0), new Vector3(0, 0, -.23f) };
            mesh.triangles = new[] { 0,3,2, 0,4,3, 0,5,4, 0,2,5, 1,2,3, 1,3,4, 1,4,5, 1,5,2 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            body.GetComponent<MeshFilter>().sharedMesh = mesh;
            body.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
        var titleObject = new GameObject("Reward label", typeof(RectTransform), typeof(TextMeshPro));
        titleObject.layer = 2; titleObject.transform.SetParent(transform, false); titleObject.transform.localPosition = Vector3.up * 0.65f;
        label = titleObject.GetComponent<TextMeshPro>(); label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = 2f; label.color = tint; label.alignment = TextAlignmentOptions.Center;
        title = Kind == SurvivalPowerUp.MaxAmmo ? "MUNIÇÕES" : Kind == SurvivalPowerUp.DoublePoints ? "2× PONTOS" : "INSTA-KILL";
        label.text = title;
        label.rectTransform.sizeDelta = new Vector2(2.6f, 0.55f);
    }

    void Update()
    {
        if (owner == null || owner.IsGameOver || Time.time >= ExpiresAt) { Destroy(gameObject); return; }
        if (Time.timeScale <= 0f || owner.IsShopOpen) return;
        if (Time.unscaledTime >= nextSettingsRead)
        {
            motionEnabled = PlayerPrefs.GetInt("opt_motion", 1) != 0;
            nextSettingsRead = Time.unscaledTime + 0.5f;
        }
        transform.position = restingPosition + Vector3.up * (motionEnabled ? Mathf.Sin(Time.time * 2.5f) * 0.08f : 0f);
        if (symbol != null && motionEnabled) symbol.Rotate(Vector3.up, Time.deltaTime * 55f, Space.Self);
        if (label != null && owner.Gun != null && owner.Gun.playerCam != null)
            label.transform.rotation = owner.Gun.playerCam.transform.rotation;
        int seconds = Mathf.CeilToInt(ExpiresAt - Time.time);
        if (label != null && seconds != lastSeconds)
        {
            lastSeconds = seconds;
            label.text = title + (seconds <= 5 ? "  " + seconds + "s" : "");
        }
        if (owner.Player == null || owner.Player.IsDead) return;
        Vector3 delta = owner.Player.transform.position - restingPosition;
        if (delta.sqrMagnitude <= 2.6f && CanReach())
        {
            owner.ActivatePowerUp(Kind);
            Destroy(gameObject);
        }
    }

    bool CanReach()
    {
        Vector3 start = owner.Player.transform.position + Vector3.up * 0.5f;
        Vector3 direction = restingPosition - start;
        float distance = direction.magnitude;
        if (distance < 0.05f) return true;
        int count = Physics.RaycastNonAlloc(start, direction / distance, sightHits, distance,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (count == sightHits.Length) return false;
        for (int i = 0; i < count; i++)
        {
            var hit = sightHits[i].collider;
            if (hit != null && !hit.transform.IsChildOf(owner.Player.transform)) return false;
        }
        return true;
    }

    void OnDestroy()
    {
        if (material != null) Destroy(material);
        if (mesh != null) Destroy(mesh);
    }
}
