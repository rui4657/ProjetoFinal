using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Exploration and physical services placed in the original Flooded Grounds map.</summary>
public sealed class WorldProgression : MonoBehaviour
{
    public const int ChurchZone = 1;
    public const int GuardZone = 2;
    public const int ChurchCost = 750;
    public const int PowerCost = 1250;
    public static WorldProgression Instance { get; private set; }
    public int UnlockedZones { get; private set; }
    public bool PowerEnabled { get; private set; }
    public bool HasPlacementError { get; private set; }
    public bool ProgressionUnavailable => built && (church == null || generator == null);
    public bool HasObjectiveLocation => NextObjectiveStation != null;
    public Vector3 SafeSpawn { get; private set; }
    public WorldInteraction FocusedInteraction => focused;
    public bool ConsumedInteractionThisFrame => lastActivationFrame == Time.frameCount;

    // These are authored landmark coordinates, not a replacement procedural arena.
    private static readonly Vector3 AuthoredSpawn = new Vector3(445.367f, 17.83f, 563.14f);
    private static readonly Vector3 ChurchGate = new Vector3(455.9676f, 17.13f, 563.28f);
    private static readonly Vector3 GuardHouse = new Vector3(466.3704f, 17.99245f, 521.0355f);
    private readonly List<WorldInteraction> stations = new List<WorldInteraction>();
    private PlayerHealth player;
    private Camera view;
    private WorldInteraction focused, activeInteraction, church, generator, arsenal, fortitude, quickReload;
    private int lastActivationFrame = -1;
    private bool built;
    private bool hasNavigation;
    private float nextPresentation;

    public string ObjectiveText
    {
        get
        {
            if (ProgressionUnavailable) return "ARSENAL [B] DISPONÍVEL ENTRE RONDAS";
            if ((UnlockedZones & ChurchZone) == 0) return "RECLAMA O PÁTIO DA IGREJA · 750 PONTOS";
            if (!PowerEnabled) return "LIGA A ENERGIA NO POSTO DE GUARDA · 1250 PONTOS";
            var session = SurvivalSession.Instance;
            if (session != null && session.WeaponTier < 3)
                return arsenal == null ? "MELHORA A ARMA · [B] ENTRE RONDAS"
                    : "MELHORA A ARMA · NÍVEL " + (session.WeaponTier + 1) + " · " + session.UpgradeCost + " PONTOS";
            if (session != null && session.HealthBonus < 100)
                return fortitude == null ? "REFORÇA A VIDA · [B] ENTRE RONDAS" : "REFORÇA A VIDA NO POSTO FORTITUDE · 2000 PONTOS";
            if (session != null && !session.HasQuickReload)
                return quickReload == null ? "ATIVA A RECARGA RÁPIDA · [B] ENTRE RONDAS" : "ATIVA A RECARGA RÁPIDA · 1500 PONTOS";
            return "PREPARA-TE E SOBREVIVE À PRÓXIMA RONDA";
        }
    }

    private WorldInteraction NextObjectiveStation
    {
        get
        {
            if (ProgressionUnavailable) return null;
            if ((UnlockedZones & ChurchZone) == 0) return church;
            if (!PowerEnabled) return generator;
            var session = SurvivalSession.Instance;
            if (session == null) return null;
            if (session.WeaponTier < 3) return arsenal;
            if (session.HealthBonus < 100) return fortitude;
            if (!session.HasQuickReload) return quickReload;
            return null;
        }
    }

    public Vector3 ObjectivePosition => NextObjectiveStation != null ? NextObjectiveStation.transform.position : SafeSpawn;

    public static WorldProgression Ensure()
    {
        if (Instance == null)
        {
            var existing = FindFirstObjectByType<WorldProgression>();
            if (existing != null) Instance = existing;
            else new GameObject("World Progression").AddComponent<WorldProgression>();
        }
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        player = FindFirstObjectByType<PlayerHealth>();
        view = Camera.main;
        if (view == null) view = FindFirstObjectByType<Camera>();
        SafeSpawn = player != null && IsFinite(player.transform.position) ? player.transform.position : AuthoredSpawn;
        hasNavigation = NavMesh.SamplePosition(SafeSpawn, out NavMeshHit _, 4f, NavMesh.AllAreas);
    }

    private void Start() => BuildWorld();

    public void RestoreProgress(int zones, bool power)
    {
        UnlockedZones = Mathf.Max(0, zones) & (ChurchZone | GuardZone);
        PowerEnabled = power || (UnlockedZones & GuardZone) != 0;
        if (PowerEnabled) UnlockedZones |= GuardZone;
        nextPresentation = 0f;
    }

    /// <summary>Checkpoints unlock services, not invisible walls across the authored zombie routes.</summary>
    public bool IsPositionAllowed(Vector3 position)
    {
        if (!IsFinite(position)) return false;
        Vector3 delta = position - SafeSpawn;
        if (delta.x * delta.x + delta.z * delta.z > 220f * 220f || Mathf.Abs(delta.y) > 60f) return false;
        if (!hasNavigation) return true;
        if (!NavMesh.SamplePosition(position, out NavMeshHit hit, 3f, NavMesh.AllAreas)) return false;
        Vector3 error = position - hit.position;
        return Mathf.Abs(error.y) <= 3f && error.x * error.x + error.z * error.z <= 4f;
    }

    public void Register(WorldInteraction station)
    {
        if (station != null && !stations.Contains(station)) stations.Add(station);
    }

    public void Unregister(WorldInteraction station)
    {
        stations.Remove(station);
        if (focused == station) focused = null;
    }

    private void Update()
    {
        var session = SurvivalSession.Instance;
        if (!IsPlaying(session)) { focused = null; return; }
        focused = FindFocus();
        if (focused != null)
        {
            session.ShowInteraction("[E] " + focused.Title + " · " + Detail(focused));
            if (Input.GetKeyDown(KeyCode.E)) TryActivate(focused);
        }
        if (Time.unscaledTime >= nextPresentation)
        {
            nextPresentation = Time.unscaledTime + 0.05f;
            foreach (var station in stations)
            {
                if (station == null) continue;
                bool complete = station.Kind == WorldInteraction.InteractionKind.ChurchCheckpoint
                    ? (UnlockedZones & ChurchZone) != 0
                    : station.Kind == WorldInteraction.InteractionKind.GuardCheckpoint && PowerEnabled;
                bool checkpoint = station.Kind == WorldInteraction.InteractionKind.ChurchCheckpoint
                    || station.Kind == WorldInteraction.InteractionKind.GuardCheckpoint;
                station.RefreshPresentation(Detail(station), !checkpoint && ServiceUnlocked(station), complete, station == focused);
            }
        }
    }

    public bool TryActivate(WorldInteraction station)
    {
        var session = SurvivalSession.Instance;
        if (!IsPlaying(session) || !CanReach(station) || lastActivationFrame == Time.frameCount) return false;
        lastActivationFrame = Time.frameCount;
        if (ProgressionUnavailable && (station.Kind == WorldInteraction.InteractionKind.ChurchCheckpoint
            || station.Kind == WorldInteraction.InteractionKind.GuardCheckpoint))
        {
            session.ShowMessage("Os postos estão indisponíveis nesta partida. Usa [B] entre rondas.");
            return false;
        }
        if (station.Kind == WorldInteraction.InteractionKind.ChurchCheckpoint)
        {
            if ((UnlockedZones & ChurchZone) != 0) { session.ShowMessage("O pátio da igreja já está reclamado."); return false; }
            if (!session.TrySpend(ChurchCost)) return false;
            UnlockedZones |= ChurchZone;
            session.ShowMessage("PÁTIO RECLAMADO · ARSENAL LOCALIZADO. FALTA LIGAR A ENERGIA.", 5f);
            SaveProgress();
            return true;
        }
        if (station.Kind == WorldInteraction.InteractionKind.GuardCheckpoint)
        {
            if (PowerEnabled) { session.ShowMessage("A energia já está ligada."); return false; }
            if (!session.TrySpend(PowerCost)) return false;
            UnlockedZones |= GuardZone;
            PowerEnabled = true;
            session.ShowMessage("ENERGIA RESTABELECIDA · ARSENAL E VANTAGENS DISPONÍVEIS.", 5f);
            SaveProgress();
            return true;
        }
        if (!ServiceUnlocked(station)) { session.ShowMessage(LockedReason(station)); return false; }
        if (!station.TryGetService(out WorldService service)) return false;
        activeInteraction = station;
        try { return session.UseWorldService(service); }
        finally { activeInteraction = null; }
    }

    public bool CanUseService(WorldService service)
    {
        if (!IsPlaying(SurvivalSession.Instance)) return false;
        WorldInteraction station = activeInteraction != null ? activeInteraction : focused;
        return station != null && station.TryGetService(out WorldService offered) && offered == service
            && ServiceUnlocked(station) && CanReach(station);
    }

    private bool IsPlaying(SurvivalSession session)
    {
        return session != null && !session.IsGameOver && !session.IsShopOpen && Time.timeScale > 0f
            && player != null && !player.IsDead;
    }

    private WorldInteraction FindFocus()
    {
        WorldInteraction nearest = null;
        float best = float.PositiveInfinity;
        foreach (var station in stations)
        {
            if (!CanReach(station)) continue;
            float distance = (station.FocusPoint - view.transform.position).sqrMagnitude;
            if (distance < best) { nearest = station; best = distance; }
        }
        return nearest;
    }

    private bool CanReach(WorldInteraction station)
    {
        if (station == null || station.Owner != this || !station.isActiveAndEnabled || player == null || view == null) return false;
        Vector3 offset = station.FocusPoint - view.transform.position;
        float distance = offset.magnitude;
        if (distance > station.InteractionRange || distance < 0.01f) return false;
        if (Vector3.Dot(view.transform.forward, offset / distance) < 0.55f) return false;
        if (Physics.Raycast(view.transform.position, offset / distance, out RaycastHit hit,
            Mathf.Max(0f, distance - 0.08f), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider == null || !hit.collider.transform.IsChildOf(station.GeometryRoot)) return false;
        }
        return true;
    }

    private bool ServiceUnlocked(WorldInteraction station)
    {
        if (ProgressionUnavailable) return true;
        if (station.Kind == WorldInteraction.InteractionKind.WeaponUpgrade)
            return (UnlockedZones & ChurchZone) != 0 && PowerEnabled;
        if (station.Kind == WorldInteraction.InteractionKind.HealthUpgrade || station.Kind == WorldInteraction.InteractionKind.QuickReload)
            return PowerEnabled;
        return true;
    }

    private string LockedReason(WorldInteraction station)
    {
        if (station.Kind == WorldInteraction.InteractionKind.WeaponUpgrade && (UnlockedZones & ChurchZone) == 0)
            return "Reclama primeiro o pátio da igreja (750 pontos).";
        return "Liga primeiro a energia no posto de guarda (1250 pontos).";
    }

    private string Detail(WorldInteraction station)
    {
        var session = SurvivalSession.Instance;
        switch (station.Kind)
        {
            case WorldInteraction.InteractionKind.ChurchCheckpoint:
                return ProgressionUnavailable ? "USA [B] ENTRE RONDAS" : (UnlockedZones & ChurchZone) != 0 ? "PÁTIO RECLAMADO" : "750 PONTOS";
            case WorldInteraction.InteractionKind.GuardCheckpoint:
                return ProgressionUnavailable ? "USA [B] ENTRE RONDAS" : PowerEnabled ? "ENERGIA ATIVA" : "1250 PONTOS · LIGAR ENERGIA";
            case WorldInteraction.InteractionKind.Ammo:
                return session != null && session.Gun != null && session.Gun.currentAmmo >= session.Gun.magSize
                    && session.Gun.reserveAmmo >= session.Gun.maxReserveAmmo ? "MUNIÇÕES COMPLETAS" : "500 PONTOS · MUNIÇÕES COMPLETAS";
            case WorldInteraction.InteractionKind.WeaponUpgrade:
                if (!ServiceUnlocked(station)) return "REQUER IGREJA + ENERGIA";
                return session != null && session.WeaponTier >= 3 ? "NÍVEL MÁXIMO" : (session != null ? session.UpgradeCost : 1500) + " PONTOS · +50% DANO";
            case WorldInteraction.InteractionKind.HealthUpgrade:
                return !ServiceUnlocked(station) ? "REQUER ENERGIA" : session != null && session.HealthBonus >= 100 ? "FORTITUDE MÁXIMA" : "2000 PONTOS · +50 VIDA";
            case WorldInteraction.InteractionKind.QuickReload:
                return !ServiceUnlocked(station) ? "REQUER ENERGIA" : session != null && session.HasQuickReload ? "RECARGA RÁPIDA ATIVA" : "1500 PONTOS · RECARGA RÁPIDA";
            default: return "";
        }
    }

    private void SaveProgress()
    {
        nextPresentation = 0f;
        var session = SurvivalSession.Instance;
        if (session != null)
        {
            var sound = session.GetComponent<SurvivalAudio>();
            if (sound != null) sound.Purchase();
        }
        if (SaveSystem.instance != null) SaveSystem.instance.RequestSave();
    }

    private void BuildWorld()
    {
        if (built) return;
        built = true;
        Vector3 gate = Landmark("Struct_Fence1_Gate_A", ChurchGate);
        Vector3 guard = Landmark("GuardHouse", GuardHouse);
        church = Place("Controlo do pátio", WorldInteraction.InteractionKind.ChurchCheckpoint,
            "PÁTIO DA IGREJA", gate + new Vector3(-2f, 0f, -2.25f), "FieldCabinet");
        arsenal = Place("Arsenal da igreja", WorldInteraction.InteractionKind.WeaponUpgrade,
            "ARSENAL", gate + new Vector3(2.3f, 0f, 2.2f), "FieldWorkbench");
        generator = Place("Gerador do posto", WorldInteraction.InteractionKind.GuardCheckpoint,
            "POSTO DE GUARDA", guard + new Vector3(-4f, 0f, 6f), "FieldCabinet");
        fortitude = Place("Posto Fortitude", WorldInteraction.InteractionKind.HealthUpgrade,
            "FORTITUDE", guard + new Vector3(1f, 0f, 6f), "FieldCabinet");
        quickReload = Place("Posto Recarga", WorldInteraction.InteractionKind.QuickReload,
            "RECARGA RÁPIDA", guard + new Vector3(4.5f, 0f, 6f), "FieldCabinet");

        // The old ammo marker is a HumanScale helper at the distant barn. Replace that helper only,
        // keeping its authored location and leaving the environment/navigation untouched.
        var legacy = FindObjectsByType<AmmoStation>(FindObjectsSortMode.None);
        foreach (var oldStation in legacy)
        {
            var replacement = Place("Reabastecimento do celeiro", WorldInteraction.InteractionKind.Ammo,
                "REABASTECIMENTO", oldStation.transform.position, "FieldCabinet");
            if (replacement == null) continue;
            oldStation.enabled = false;
            foreach (var renderer in oldStation.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
        }
        Place("Reabastecimento inicial", WorldInteraction.InteractionKind.Ammo,
            "REABASTECIMENTO", SafeSpawn + new Vector3(-3f, -1f, 3f), "FieldCabinet");
    }

    private WorldInteraction Place(string name, WorldInteraction.InteractionKind kind, string title,
        Vector3 preferred, string resource)
    {
        if (!TryReachableGround(preferred, out Vector3 landing))
        {
            HasPlacementError = true;
            Debug.LogWarning("WorldProgression: no connected NavMesh position for " + name + " near " + preferred
                + ". The station was not created. Check the authored map bake.");
            return null;
        }
        var root = new GameObject(name);
        root.transform.SetParent(transform, false);
        root.transform.position = landing;
        Vector3 facing = SafeSpawn - root.transform.position;
        facing.y = 0f;
        if (facing.sqrMagnitude > 0.01f) root.transform.rotation = Quaternion.LookRotation(facing);
        var prefab = Resources.Load<GameObject>("World/" + resource);
        if (prefab != null)
        {
            var prop = Instantiate(prefab, root.transform);
            prop.transform.localPosition = Vector3.zero;
            prop.transform.localRotation = Quaternion.identity;
            prop.transform.localScale = Vector3.one;
            foreach (var collider in prop.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            FitProp(prop, resource == "FieldWorkbench" ? 0.9f : 1.25f);
        }
        var station = root.AddComponent<WorldInteraction>();
        station.Configure(this, kind, title);
        station.BuildPresentation(false);
        return station;
    }

    private bool TryReachableGround(Vector3 desired, out Vector3 landing)
    {
        landing = Vector3.zero;
        if (hasNavigation)
        {
            Vector3[] offsets = { Vector3.zero, new Vector3(-2.5f, 0, 0), new Vector3(2.5f, 0, 0),
                new Vector3(0, 0, -2.5f), new Vector3(0, 0, 2.5f), new Vector3(-7f, 0, 0),
                new Vector3(7f, 0, 0), new Vector3(0, 0, -7f), new Vector3(0, 0, 7f) };
            foreach (float radius in new[] { 2f, 5f, 10f })
            {
                foreach (var offset in offsets)
                {
                    if (!NavMesh.SamplePosition(desired + offset, out NavMeshHit hit, radius, NavMesh.AllAreas)) continue;
                    if (!StationSpaceAvailable(hit.position)) continue;
                    if (!NavMesh.SamplePosition(SafeSpawn, out NavMeshHit start, 4f, NavMesh.AllAreas)) continue;
                    var path = new NavMeshPath();
                    if (NavMesh.CalculatePath(start.position, hit.position, NavMesh.AllAreas, path)
                        && path.status == NavMeshPathStatus.PathComplete) { landing = hit.position; return true; }
                }
            }
        }
        return false;
    }

    private bool StationSpaceAvailable(Vector3 position)
    {
        foreach (var station in stations)
        {
            if (station == null) continue;
            Vector3 offset = station.transform.position - position;
            if (offset.x * offset.x + offset.z * offset.z < 2.1f * 2.1f) return false;
        }
        return true;
    }

    private static Vector3 Landmark(string name, Vector3 fallback)
    {
        var landmark = GameObject.Find(name);
        return landmark != null ? landmark.transform.position : fallback;
    }

    private static void FitProp(GameObject prop, float height)
    {
        var renderers = prop.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;
        Bounds bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        if (bounds.size.y < 0.01f) return;
        prop.transform.localScale *= height / bounds.size.y;
        bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        Vector3 offset = new Vector3(bounds.center.x - prop.transform.parent.position.x,
            bounds.min.y - prop.transform.parent.position.y, bounds.center.z - prop.transform.parent.position.z);
        prop.transform.position -= offset;
    }

    private static bool IsFinite(Vector3 p) => !float.IsNaN(p.x) && !float.IsInfinity(p.x)
        && !float.IsNaN(p.y) && !float.IsInfinity(p.y) && !float.IsNaN(p.z) && !float.IsInfinity(p.z);

    private void OnDestroy() { if (Instance == this) Instance = null; }
}
