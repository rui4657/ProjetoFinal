using UnityEngine;
using UnityEngine.AI;

/// <summary>Chooses navigable positions throughout the active districts, not three fixed spawn transforms.</summary>
public class ZombieSpawner : MonoBehaviour
{
    public GameObject zombiePrefab;
    public Transform[] spawnPoints;
    public enum SpawnMode { RandomEachZombie, RotateByRound }
    public SpawnMode spawnMode = SpawnMode.RandomEachZombie;
    public float navMeshSearchRadius = 2f;
    public float spawnScatterRadius = 1.2f;
    public float minimumPlayerDistance = 8f;
    public float maximumPlayerDistance = 42f;
    public int CandidateCount { get; private set; }
    public bool IsConfigured => zombiePrefab != null || prefabs[0] != null;

    private readonly GameObject[] prefabs = new GameObject[4];
    private PlayerHealth player;
    private Camera view;
    private NavMeshPath path;
    private NavMeshTriangulation triangulation;
    private float nextMeshRefresh;
    private int ordinal, directionIndex;
    private readonly Collider[] occupancy = new Collider[12];

    private void Awake()
    {
        player = FindFirstObjectByType<PlayerHealth>();
        view = player != null ? player.GetComponentInChildren<Camera>() : Camera.main;
        path = new NavMeshPath();
        string[] names = { "Walker", "Runner", "Brute", "Spitter" };
        for (int i = 0; i < names.Length; i++) prefabs[i] = Resources.Load<GameObject>("Zombies/" + names[i]);
    }

    public void BeginRound(int round)
    {
        ordinal = 0;
        RefreshMesh();
    }

    private void RefreshMesh()
    {
        triangulation = NavMesh.CalculateTriangulation();
        CandidateCount = triangulation.indices != null ? triangulation.indices.Length / 3 : 0;
        nextMeshRefresh = Time.time + 5;
    }

    public bool SpawnZombie(int currentRound) => SpawnZombie(currentRound, FindFirstObjectByType<RoundManager>());

    public bool SpawnZombie(int currentRound, RoundManager owner)
    {
        if (!IsConfigured || player == null || player.IsDead) return false;
        var session = SurvivalSession.Instance;
        if (session != null && session.IsGameOver) return false;
        if (path == null) path = new NavMeshPath();
        if (Time.time >= nextMeshRefresh || triangulation.indices == null) RefreshMesh();
        ZombieArchetype kind = ZombieProfiles.Choose(currentRound, ordinal);
        // Rare specialists stay readable and affordable to simulate, rather than filling the entire wave.
        if (kind == ZombieArchetype.Spitter || kind == ZombieArchetype.Brute)
        {
            int alive = 0;
            foreach (var zombie in FindObjectsByType<ZombieAI>(FindObjectsSortMode.None))
                if (!zombie.IsDead && zombie.archetype == kind) alive++;
            if (alive >= (kind == ZombieArchetype.Spitter ? 3 : 2)) kind = ZombieArchetype.Runner;
        }
        GameObject template = prefabs[(int)kind];
        if (template == null) { kind = ZombieArchetype.Walker; template = prefabs[0] != null ? prefabs[0] : zombiePrefab; }
        if (template == null) return false;
        var agentTemplate = template.GetComponent<NavMeshAgent>();
        var filter = new NavMeshQueryFilter { agentTypeID = agentTemplate != null ? agentTemplate.agentTypeID : 0, areaMask = NavMesh.AllAreas };
        float sector = (directionIndex++ % 8) * 45f;
        for (int attempt = 0; attempt < 48; attempt++)
        {
            Vector3 candidate;
            if (attempt % 3 == 2 && CandidateCount > 0)
            {
                int triangle = Random.Range(0, CandidateCount) * 3;
                Vector3 a = triangulation.vertices[triangulation.indices[triangle]], b = triangulation.vertices[triangulation.indices[triangle + 1]], c = triangulation.vertices[triangulation.indices[triangle + 2]];
                float u = Random.value, v = Random.value; if (u + v > 1) { u = 1 - u; v = 1 - v; }
                candidate = a + (b - a) * u + (c - a) * v;
            }
            else
            {
                float angle = (sector + attempt * 137.5f + Random.Range(-18f, 18f)) * Mathf.Deg2Rad;
                float distance = Random.Range(Mathf.Max(7, minimumPlayerDistance), Mathf.Max(12, maximumPlayerDistance));
                candidate = player.transform.position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * distance;
            }
            if (!TryPosition(candidate, filter, template, attempt < 36, out Vector3 position)) continue;
            Vector3 facing = player.transform.position - position; facing.y = 0;
            var spawned = Instantiate(template, position, Quaternion.LookRotation(facing.sqrMagnitude > .01f ? facing : Vector3.forward));
            var zombie = spawned.GetComponent<ZombieAI>(); var agent = spawned.GetComponent<NavMeshAgent>();
            if (zombie == null || agent == null || !agent.enabled || !agent.isOnNavMesh) { Destroy(spawned); return false; }
            zombie.archetype = kind;
            zombie.InitializeForRound(currentRound, owner); ordinal++;
            return true;
        }
        return false;
    }

    private bool TryPosition(Vector3 candidate, NavMeshQueryFilter filter, GameObject template, bool requireHidden, out Vector3 position)
    {
        position = Vector3.zero;
        if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, Mathf.Max(.5f, navMeshSearchRadius), filter)) return false;
        Vector3 delta = hit.position - player.transform.position;
        float horizontal = new Vector2(delta.x, delta.z).magnitude;
        if (horizontal < Mathf.Max(7, minimumPlayerDistance) || horizontal > Mathf.Max(12, maximumPlayerDistance)) return false;
        var world = WorldProgression.Instance;
        if (world != null && !world.IsInsideOpenArea(hit.position)) return false;
        if (!NavMesh.SamplePosition(player.transform.position, out NavMeshHit target, 4, filter)
            || !NavMesh.CalculatePath(hit.position, target.position, filter, path) || path.status != NavMeshPathStatus.PathComplete) return false;
        if (world != null && path.corners != null)
            foreach (Vector3 corner in path.corners) if (!world.IsInsideOpenArea(corner)) return false;
        float height = 3f;
        float radius = .4f;
        var capsule = template.GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            height = Mathf.Max(2, capsule.height * template.transform.localScale.y);
            radius = Mathf.Max(.3f, capsule.radius * Mathf.Max(template.transform.localScale.x, template.transform.localScale.z));
        }
        int count = Physics.OverlapCapsuleNonAlloc(hit.position + Vector3.up * (radius + .1f), hit.position + Vector3.up * Mathf.Max(radius + .1f, height - radius - .1f), radius,
            occupancy, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (count > 0) return false;
        if (view != null && requireHidden)
        {
            Vector3 head = hit.position + Vector3.up * 1.6f;
            Vector3 viewport = view.WorldToViewportPoint(head);
            bool inView = viewport.z > 0 && viewport.x > -.05f && viewport.x < 1.05f && viewport.y > -.1f && viewport.y < 1.1f;
            if (inView && !Physics.Linecast(view.transform.position, head, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return false;
        }
        // A visible emergency candidate must still be well away from the player; no enemy appears at arm's length.
        if (!requireHidden && horizontal < 12) return false;
        position = hit.position; return true;
    }
}
