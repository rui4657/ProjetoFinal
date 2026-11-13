using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Physical perimeter fences and gates, with the same limits used by saves and spawns.</summary>
public sealed class QuarantineZones : MonoBehaviour
{
    private sealed class Gate
    {
        public int RequiredFlag;
        public int ShortcutBit;
        public Transform Leaf;
        public BoxCollider Collider;
        public NavMeshObstacle Obstacle;
        public Quaternion ClosedRotation;
        public float OpenBlend;
        public WorldInteraction Station;
    }
    private readonly List<Gate> gates = new List<Gate>();
    private readonly HashSet<string> wallKeys = new HashSet<string>();
    private readonly HashSet<string> barrierKeys = new HashSet<string>();
    private readonly Dictionary<string, Gate> gateKeys = new Dictionary<string, Gate>();
    private WorldProgression world;
    private Vector3 offset, lastSafePosition;
    private GameObject fencePrefab, doorPrefab;
    private Material fallbackMaterial;
    private int flags;
    private float nextBoundaryCheck, nextWarning;

    public static QuarantineZones Create(WorldProgression owner)
    {
        var go = new GameObject("Quarantine Perimeters");
        go.transform.SetParent(owner.transform, false);
        var zones = go.AddComponent<QuarantineZones>();
        zones.world = owner; zones.offset = owner.ZoneOffset; zones.lastSafePosition = owner.SafeSpawn;
        zones.fencePrefab = Resources.Load<GameObject>("World/QuarantineFence");
        zones.doorPrefab = Resources.Load<GameObject>("World/QuarantineDoor");
        var shader = Shader.Find("Standard");
        if (shader != null) { zones.fallbackMaterial = new Material(shader); zones.fallbackMaterial.color = new Color(.24f, .27f, .24f); }
        zones.Build(); zones.ApplyProgress(owner.UnlockedZones);
        return zones;
    }

    public void ApplyProgress(int unlocked)
    {
        flags = QuarantineRules.Normalize(unlocked);
        foreach (var gate in gates)
        {
            bool open = IsGateOpen(gate);
            gate.Collider.enabled = !open;
            gate.Obstacle.enabled = !open;
        }
    }

    public WorldInteraction PortalFor(int flag)
    {
        WorldInteraction nearest = null; float distance = float.PositiveInfinity;
        foreach (var gate in gates)
        {
            if (gate.RequiredFlag != flag || gate.ShortcutBit != 0 || gate.Station == null) continue;
            float candidate = (gate.Station.transform.position - world.SafeSpawn).sqrMagnitude;
            if (candidate < distance) { nearest = gate.Station; distance = candidate; }
        }
        return nearest;
    }

    private void Build()
    {
        // Nested perimeters leave real routes between districts. Shared sides are built once.
        Ring(QuarantineRules.Start, 0);
        Ring(QuarantineRules.Churchyard, 1);
        Ring(QuarantineRules.VillageBounds, 2);
    }

    private void Ring(QuarantineRect rect, int level)
    {
        Side(rect.MinX, rect.MaxX, rect.MinZ, false, level == 2 ? 450 : 466,
            level == 2 ? QuarantineRules.Outskirts : QuarantineRules.Village, "ACESSO À VILA");
        Side(rect.MinX, rect.MaxX, rect.MaxZ, false, level == 0 ? 456 : level == 1 ? 448 : 440,
            level == 0 ? QuarantineRules.Church : level == 1 ? QuarantineRules.Village : QuarantineRules.Outskirts,
            level == 0 ? "ACESSO À IGREJA" : "ACESSO NORTE");
        if (level == 2)
        {
            Side(rect.MinZ, rect.MaxZ, rect.MinX, true, 563, QuarantineRules.Outskirts, "ACESSO OESTE");
            Side(rect.MinZ, rect.MaxZ, rect.MaxX, true, 563, QuarantineRules.Outskirts, "ACESSO ESTE");
        }
        else { ClosedSide(rect.MinZ, rect.MaxZ, rect.MinX); ClosedSide(rect.MinZ, rect.MaxZ, rect.MaxX); }
    }

    private void ClosedSide(float min, float max, float x)
    {
        BuildSpan(min, max, x, true, 0, 0, null);
    }

    private void Side(float min, float max, float constant, bool vertical, float gateAt, int required, string title)
    {
        BuildSpan(min, max, constant, vertical, gateAt, required, title);
    }

    private sealed class Opening
    {
        public float Start, End;
        public int Required, Shortcut;
        public string Title;
        public int Breach = -1;
    }
    private void BuildSpan(float min, float max, float constant, bool vertical, float gateAt, int required, string title)
    {
        var openings = new List<Opening>();
        if (required != 0)
        {
            float start = min + Mathf.Floor((gateAt - 2 - min) / 2) * 2;
            float end = Mathf.Min(max, min + Mathf.Ceil((gateAt + 2 - min) / 2) * 2);
            openings.Add(new Opening { Start = start, End = end, Required = required, Title = title });
        }
        foreach (var site in SurvivalMapLayout.Breaches)
            if (site.Vertical == vertical && Mathf.Abs(site.Constant - constant) < .01f && site.Along - 2 >= min && site.Along + 2 <= max)
                openings.Add(new Opening { Start = site.Along - 2, End = site.Along + 2, Breach = site.Id });
        float shortcutAt = constant == 432 ? 578 : 584;
        if (vertical && (constant == 432 || constant == 480) && shortcutAt - 2 >= min && shortcutAt + 2 <= max)
            openings.Add(new Opening { Start = shortcutAt - 2, End = shortcutAt + 2, Required = 2,
                Shortcut = constant == 432 ? 1 : 2, Title = constant == 432 ? "ATALHO OESTE" : "ATALHO ESTE" });
        openings.Sort((a, b) => a.Start.CompareTo(b.Start));
        float cursor = min;
        foreach (var opening in openings)
        {
            SolidSpan(cursor, opening.Start, constant, vertical); cursor = opening.End;
            Vector3 center = MapPoint(vertical ? constant : (opening.Start + opening.End) * .5f,
                vertical ? (opening.Start + opening.End) * .5f : constant);
            if (opening.Breach >= 0)
            {
                bool exists = false; foreach (var entry in world.Barricades.Entries) if (entry.Site.Id == opening.Breach) exists = true;
                if (!exists) BarricadeEntry.Create(world.Barricades, SurvivalMapLayout.Breaches[opening.Breach], center);
            }
            else
            {
                string key = (vertical ? "V" : "H") + constant + ":" + opening.Start + ":" + opening.End;
                if (!gateKeys.ContainsKey(key)) MakeGate(center, opening.End - opening.Start, vertical,
                    opening.Required, opening.Title, key, opening.Shortcut);
            }
        }
        SolidSpan(cursor, max, constant, vertical);
    }
    private void SolidSpan(float min, float max, float constant, bool vertical)
    {
        for (float a = min; a < max - .01f; a += 2)
        {
            float b = Mathf.Min(max, a + 2); string key = (vertical ? "V" : "H") + constant + ":" + a + ":" + b;
            if (wallKeys.Add(key)) MakeWall(MapPoint(vertical ? constant : a, vertical ? a : constant),
                MapPoint(vertical ? constant : b, vertical ? b : constant), vertical);
        }
        WallBarrier(min, max, constant, vertical);
    }

    private Vector3 MapPoint(float x, float z)
    {
        var point = new Vector3(x + offset.x, world.SafeSpawn.y, z + offset.z);
        point.y = Ground(point);
        return point;
    }

    private float Ground(Vector3 point)
    {
        foreach (var terrain in Terrain.activeTerrains)
        {
            Vector3 origin = terrain.transform.position, size = terrain.terrainData.size;
            if (point.x >= origin.x && point.x <= origin.x + size.x && point.z >= origin.z && point.z <= origin.z + size.z)
                return terrain.SampleHeight(point) + origin.y;
        }
        return Physics.Raycast(point + Vector3.up * 40, Vector3.down, out RaycastHit hit, 80,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ? hit.point.y : world.SafeSpawn.y - 1;
    }

    private void MakeWall(Vector3 first, Vector3 last, bool vertical)
    {
        Vector3 center = (first + last) * .5f;
        float low = Mathf.Min(first.y, last.y) - .8f, high = Mathf.Max(first.y, last.y) + 4;
        float width = new Vector2(last.x - first.x, last.z - first.z).magnitude + .06f;
        var go = new GameObject("Quarantine fence"); go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(center.x, low, center.z);
        AddArt(go.transform, fencePrefab, width, high - low);
        go.transform.rotation = vertical ? Quaternion.Euler(0, 90, 0) : Quaternion.identity;
    }

    private void WallBarrier(float min, float max, float constant, bool vertical)
    {
        if (max - min < .05f) return;
        string key = (vertical ? "V" : "H") + constant + ":" + min + ":" + max;
        if (!barrierKeys.Add(key)) return;
        float low = float.PositiveInfinity, high = float.NegativeInfinity;
        for (float p = min; p <= max; p += 1)
        { float y = MapPoint(vertical ? constant : p, vertical ? p : constant).y; low = Mathf.Min(low, y); high = Mathf.Max(high, y); }
        var center = MapPoint(vertical ? constant : (min + max) * .5f, vertical ? (min + max) * .5f : constant);
        var go = new GameObject("Fence collision and navigation"); go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(center.x, (low + high) * .5f + 1.5f, center.z);
        go.transform.rotation = vertical ? Quaternion.Euler(0, 90, 0) : Quaternion.identity;
        AddBarrier(go, Vector3.zero, new Vector3(max - min + .08f, high - low + 6, .65f));
    }

    private void MakeGate(Vector3 point, float width, bool vertical, int required, string title, string key, int shortcut = 0)
    {
        Vector3 along = vertical ? Vector3.forward : Vector3.right;
        float low = Mathf.Min(point.y, Mathf.Min(Ground(point - along * width * .5f), Ground(point + along * width * .5f)));
        float high = Mathf.Max(point.y, Mathf.Max(Ground(point - along * width * .5f), Ground(point + along * width * .5f)));
        float height = high - low + 4;
        point.y = low;
        var go = new GameObject(title); go.transform.SetParent(transform, false); go.transform.position = point;
        go.transform.rotation = vertical ? Quaternion.Euler(0, 90, 0) : Quaternion.identity;
        var hinge = new GameObject("Gate hinge").transform; hinge.SetParent(go.transform, false);
        hinge.localPosition = new Vector3(-width * .5f, 0, 0);
        var leaf = new GameObject("Gate leaf").transform; leaf.SetParent(hinge, false);
        leaf.localPosition = new Vector3(width * .5f, 0, 0);
        // Fit in local axes before rotating the portal into the authored perimeter.
        Quaternion rotation = go.transform.rotation; go.transform.rotation = Quaternion.identity;
        AddArt(leaf, doorPrefab != null ? doorPrefab : fencePrefab, width, height);
        go.transform.rotation = rotation;
        var barrier = AddBarrier(go, Vector3.up * (height * .5f - .5f), new Vector3(width + .15f, height + 1, .9f));
        var gate = new Gate { RequiredFlag = required, ShortcutBit = shortcut, Leaf = hinge, Collider = barrier,
            Obstacle = go.GetComponent<NavMeshObstacle>(), ClosedRotation = hinge.localRotation };
        gates.Add(gate); gateKeys.Add(key, gate);
        var kind = shortcut != 0 ? WorldInteraction.InteractionKind.Shortcut : required == QuarantineRules.Church ? WorldInteraction.InteractionKind.ChurchGate
            : required == QuarantineRules.Village ? WorldInteraction.InteractionKind.VillageGate : WorldInteraction.InteractionKind.OutskirtsGate;
        var controls = new GameObject("Gate controls"); controls.transform.SetParent(go.transform, false);
        float inside = go.transform.InverseTransformPoint(world.SafeSpawn).z >= 0 ? 1 : -1;
        controls.transform.localPosition = new Vector3(-width * .5f - .4f, 0, inside * .8f);
        Vector3 controlPoint = controls.transform.position; controlPoint.y = Ground(controlPoint); controls.transform.position = controlPoint;
        controls.transform.localRotation = inside < 0 ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;
        var post = GameObject.CreatePrimitive(PrimitiveType.Cube); post.name = "Control post";
        post.transform.SetParent(controls.transform, false); post.transform.localPosition = Vector3.up * .75f;
        post.transform.localScale = new Vector3(.14f, 1.5f, .14f); post.GetComponent<Renderer>().sharedMaterial = fallbackMaterial;
        post.GetComponent<Collider>().enabled = false;
        var station = controls.AddComponent<WorldInteraction>(); station.Configure(world, kind, title, 3.6f, go.transform);
        if (shortcut != 0) station.ConfigureShortcut(shortcut);
        station.BuildPresentation(true);
        gate.Station = station;
    }

    private BoxCollider AddBarrier(GameObject go, Vector3 center, Vector3 size)
    {
        var collider = go.AddComponent<BoxCollider>(); collider.center = center; collider.size = size;
        var obstacle = go.AddComponent<NavMeshObstacle>(); obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.center = center; obstacle.size = size; obstacle.carving = true; obstacle.carveOnlyStationary = false;
        return collider;
    }

    private void AddArt(Transform root, GameObject prefab, float width, float height)
    {
        if (prefab != null)
        {
            // Scale an axis-aligned parent: scaling the mesh after a 90-degree turn
            // would stretch its thickness instead of fitting the visible opening.
            var fitting = new GameObject("Fence art fitting").transform; fitting.SetParent(root, false);
            var art = Instantiate(prefab, fitting); art.transform.localPosition = Vector3.zero;
            art.transform.localRotation = Quaternion.identity; art.transform.localScale = Vector3.one;
            foreach (var collider in art.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            var renderers = art.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                if (bounds.size.z > bounds.size.x) art.transform.localRotation = Quaternion.Euler(0, 90, 0);
                bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                if (bounds.size.x > .01f && bounds.size.y > .01f)
                {
                    fitting.localScale = new Vector3(width / bounds.size.x, height / bounds.size.y, 1);
                    bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                    fitting.position -= new Vector3(bounds.center.x - root.position.x,
                        bounds.min.y - root.position.y, bounds.center.z - root.position.z);
                    return;
                }
            }
            Destroy(fitting.gameObject);
        }
        var fallback = GameObject.CreatePrimitive(PrimitiveType.Cube); fallback.name = "Gate panel";
        fallback.transform.SetParent(root, false); fallback.transform.localPosition = Vector3.up * height * .5f;
        fallback.transform.localScale = new Vector3(width, height, .2f);
        fallback.GetComponent<Renderer>().sharedMaterial = fallbackMaterial;
        fallback.GetComponent<Collider>().enabled = false;
    }

    private void LateUpdate()
    {
        foreach (var gate in gates)
        {
            gate.OpenBlend = Mathf.MoveTowards(gate.OpenBlend, IsGateOpen(gate) ? 1 : 0, Time.deltaTime * 1.5f);
            gate.Leaf.localRotation = gate.ClosedRotation * Quaternion.Euler(0, -100 * Mathf.SmoothStep(0, 1, gate.OpenBlend), 0);
        }
        var session = SurvivalSession.Instance;
        if (session == null || session.IsGameOver || session.Player == null || Time.timeScale <= 0 || Time.time < nextBoundaryCheck) return;
        nextBoundaryCheck = Time.time + .15f;
        Vector3 position = session.Player.transform.position;
        if (world.IsInsideOpenArea(position)) lastSafePosition = position;
        else if (session.Movement != null)
        {
            session.Movement.ApplyLoadedPosition(lastSafePosition);
            if (Time.time > nextWarning) { session.ShowMessage("ZONA FECHADA · USA OS PORTÕES ASSINALADOS"); nextWarning = Time.time + 3; }
        }
    }

    private bool IsGateOpen(Gate gate) => gate.ShortcutBit != 0 ? (world.ShortcutFlags & gate.ShortcutBit) != 0 : (flags & gate.RequiredFlag) != 0;

    private void OnDestroy() { if (fallbackMaterial != null) Destroy(fallbackMaterial); }
}
