using UnityEngine;
using UnityEngine.AI;

public class ZombieSpawner : MonoBehaviour
{
    public GameObject zombiePrefab;
    public Transform[] spawnPoints;

    public enum SpawnMode
    {
        RandomEachZombie,
        RotateByRound
    }

    [Header("Spawn")]
    public SpawnMode spawnMode = SpawnMode.RandomEachZombie;
    [Min(0.1f)] public float navMeshSearchRadius = 2f;
    [Min(0f)] public float spawnScatterRadius = 1.2f;
    [Min(0f)] public float minimumPlayerDistance = 5f;

    public bool IsConfigured => zombiePrefab != null && spawnPoints != null && spawnPoints.Length > 0;

    private PlayerHealth player;
    private NavMeshPath spawnPath;

    private void Awake()
    {
        player = FindFirstObjectByType<PlayerHealth>();
        spawnPath = new NavMeshPath();
    }

    public bool SpawnZombie(int currentRound)
    {
        return SpawnZombie(currentRound, FindFirstObjectByType<RoundManager>());
    }

    public bool SpawnZombie(int currentRound, RoundManager owner)
    {
        if (!IsConfigured) return false;
        if (player != null && player.IsDead) return false;
        if (SurvivalSession.Instance != null && SurvivalSession.Instance.IsGameOver) return false;

        int first = spawnMode == SpawnMode.RotateByRound
            ? Mathf.Max(0, currentRound - 1) % spawnPoints.Length
            : Random.Range(0, spawnPoints.Length);

        NavMeshAgent templateAgent = zombiePrefab.GetComponent<NavMeshAgent>();
        NavMeshQueryFilter filter = new NavMeshQueryFilter
        {
            agentTypeID = templateAgent != null ? templateAgent.agentTypeID : 0,
            areaMask = NavMesh.AllAreas
        };

        // A bad point must not discard a zombie if another point is valid.
        for (int attempt = 0; attempt < spawnPoints.Length; attempt++)
        {
            Transform point = spawnPoints[(first + attempt) % spawnPoints.Length];
            if (point == null) continue;
            Vector2 scatter = Random.insideUnitCircle * Mathf.Max(0f, spawnScatterRadius);
            Vector3 candidate = point.position + new Vector3(scatter.x, 0f, scatter.y);
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, Mathf.Max(0.1f, navMeshSearchRadius), filter)) continue;
            if (player != null)
            {
                if ((player.transform.position - hit.position).sqrMagnitude < minimumPlayerDistance * minimumPlayerDistance) continue;
                // NavMesh islands can accept a spawn but strand the wave permanently.
                if (!NavMesh.SamplePosition(player.transform.position, out NavMeshHit playerHit, 4f, filter)) continue;
                if (!NavMesh.CalculatePath(hit.position, playerHit.position, filter, spawnPath) || spawnPath.status != NavMeshPathStatus.PathComplete) continue;
            }

            GameObject spawned = Instantiate(zombiePrefab, hit.position, point.rotation);
            ZombieAI zombie = spawned.GetComponent<ZombieAI>();
            NavMeshAgent agent = spawned.GetComponent<NavMeshAgent>();
            if (zombie == null || agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                Destroy(spawned);
                return false;
            }

            zombie.InitializeForRound(currentRound, owner);
            return true;
        }

        return false;
    }
}
