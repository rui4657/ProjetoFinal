using UnityEngine;
using UnityEngine.AI;

public class ZombieSpawner : MonoBehaviour
{
    public GameObject zombiePrefab;
    public Transform[] spawnPoints;

    public enum SpawnMode
    {
        RandomEachZombie,   // cada zombie nasce num ponto aleatório
        RotateByRound       // cada ronda usa um ponto diferente (1->0, 2->1, 3->2, ...)
    }

    [Header("Spawn")]
    public SpawnMode spawnMode = SpawnMode.RandomEachZombie;
    public float navMeshSearchRadius = 2f;

    // AGORA DEVOLVE BOOL (true = spawn ok, false = falhou)
    public bool SpawnZombie(int currentRound)
    {
        if (zombiePrefab == null)
        {
            Debug.LogError("ZombieSpawner: zombiePrefab não está atribuído!");
            return false;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("ZombieSpawner: Sem spawn points definidos!");
            return false;
        }

        Transform sp = ChooseSpawnPoint(currentRound);
        if (sp == null) return false;

        if (NavMesh.SamplePosition(sp.position, out NavMeshHit hit, navMeshSearchRadius, NavMesh.AllAreas))
        {
            Instantiate(zombiePrefab, hit.position, Quaternion.identity);
            return true;
        }

        Debug.LogWarning("ZombieSpawner: SpawnPoint fora do NavMesh!");
        return false;
    }

    Transform ChooseSpawnPoint(int round)
    {
        if (spawnMode == SpawnMode.RotateByRound)
        {
            int idx = (round - 1) % spawnPoints.Length;
            if (idx < 0) idx = 0;
            return spawnPoints[idx];
        }

        return spawnPoints[Random.Range(0, spawnPoints.Length)];
    }
}
