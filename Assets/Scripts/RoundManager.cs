using System.Collections;
using UnityEngine;

/// <summary>Owns one wave at a time, including its pending spawns.</summary>
public class RoundManager : MonoBehaviour
{
    [Header("Rounds")]
    [Min(1)] public int currentRound = 1;
    [Min(1)] public int zombiesPerRound = 5;
    [Min(0.1f)] public float spawnRate = 1.5f;
    [Min(1)] public int maximumActiveZombies = 28;
    [Min(0.1f)] public float minimumSpawnInterval = 0.28f;
    [Min(1)] public int maximumSpawnFailures = 8;

    [Header("Preparation")]
    [Min(0f)] public float initialPreparationTime = 8f;
    [Min(0f)] public float intermissionTime = 12f;
    [HideInInspector] public float intermissionTimer;
    [HideInInspector] public bool inIntermission;
    [HideInInspector] public bool isGameStarting = true;

    [Header("References")]
    public ZombieSpawner spawner;

    public int ZombiesAlive => zombiesAlive;
    public int ZombiesRemaining => zombiesAlive + pendingSpawns;
    public bool IsSpawning { get; private set; }
    public bool HasSpawnError { get; private set; }
    public bool IsStopped => stopped;

    private int zombiesAlive;
    private int zombiesSpawned;
    private int pendingSpawns;
    private bool waveStarted;
    private bool stopped;
    private Coroutine spawnRoutine;
    private PlayerHealth player;

    private void Start()
    {
        if (spawner == null) spawner = FindFirstObjectByType<ZombieSpawner>();
        player = FindFirstObjectByType<PlayerHealth>();
        currentRound = Mathf.Max(1, currentRound);
        inIntermission = true;
        isGameStarting = true;
        intermissionTimer = Mathf.Max(0f, initialPreparationTime);
    }

    private void Update()
    {
        if (stopped || Time.timeScale <= 0f || (player != null && player.IsDead)) return;
        if (SurvivalSession.Instance != null && SurvivalSession.Instance.IsGameOver) return;

        if (inIntermission)
        {
            intermissionTimer = Mathf.Max(0f, intermissionTimer - Time.deltaTime);
            if (intermissionTimer <= 0f) StartRound();
        }
    }

    private void StartRound()
    {
        if (stopped || IsSpawning || zombiesAlive > 0) return;
        if (spawner == null || !spawner.IsConfigured)
        {
            BlockSpawning("Configure a zombie prefab and at least one spawn point.");
            return;
        }

        inIntermission = false;
        isGameStarting = false;
        HasSpawnError = false;
        waveStarted = true;
        zombiesSpawned = 0;
        zombiesAlive = 0;
        pendingSpawns = GetWaveSize(currentRound, zombiesPerRound);
        spawner.BeginRound(currentRound);
        IsSpawning = true;
        spawnRoutine = StartCoroutine(SpawnWave());
    }

    // A bounded curve keeps later waves playable without spawning thousands of objects.
    public static int GetWaveSize(int round, int baseCount)
    {
        return Mathf.Clamp(Mathf.Clamp(baseCount, 1, 250) + Mathf.Clamp(round, 1, 1000) * 7, 1, 250);
    }

    private IEnumerator SpawnWave()
    {
        int consecutiveFailures = 0;
        while (pendingSpawns > 0 && !stopped)
        {
            // Wait, rather than dropping the rest of a wave when the active cap is reached.
            while (!stopped && (Time.timeScale <= 0f || zombiesAlive >= Mathf.Max(1, maximumActiveZombies)))
                yield return null;
            if (stopped) yield break;

            if (spawner != null && spawner.SpawnZombie(currentRound, this))
            {
                pendingSpawns--;
                zombiesSpawned++;
                zombiesAlive++;
                consecutiveFailures = 0;
            }
            else if (++consecutiveFailures >= Mathf.Max(1, maximumSpawnFailures))
            {
                BlockSpawning("No reachable zombie spawn positions were found. Check the baked NavMesh and spawn points.");
                yield break;
            }

            float interval = Mathf.Max(minimumSpawnInterval, Mathf.Min(spawnRate, 1f) * Mathf.Pow(0.91f, Mathf.Clamp(currentRound - 1, 0, 100)));
            yield return new WaitForSeconds(Mathf.Max(0.1f, interval));
        }

        IsSpawning = false;
        spawnRoutine = null;
        TryCompleteRound();
    }

    public void ZombieDied()
    {
        if (stopped || !waveStarted || zombiesAlive <= 0) return;
        zombiesAlive--;
        TryCompleteRound();
    }

    private void TryCompleteRound()
    {
        // Living count alone is insufficient: early kills must not complete a spawning wave.
        if (stopped || !waveStarted || inIntermission || IsSpawning || pendingSpawns > 0 || zombiesAlive > 0) return;
        waveStarted = false;
        int completedRound = currentRound;
        currentRound++;
        inIntermission = true;
        intermissionTimer = Mathf.Max(0f, intermissionTime);
        if (SurvivalSession.Instance != null) SurvivalSession.Instance.OnRoundCompleted(completedRound);
    }

    public void SkipIntermission()
    {
        if (!stopped && inIntermission && !HasSpawnError) intermissionTimer = 0f;
    }

    /// <summary>Stops new spawns immediately when the run ends.</summary>
    public void StopWaves()
    {
        stopped = true;
        inIntermission = false;
        IsSpawning = false;
        pendingSpawns = 0;
        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        spawnRoutine = null;
    }

    private void BlockSpawning(string reason)
    {
        // Do not award points or endlessly advance empty waves after a configuration error.
        HasSpawnError = true;
        stopped = true;
        IsSpawning = false;
        inIntermission = false;
        pendingSpawns = 0;
        spawnRoutine = null;
        Debug.LogError("RoundManager: " + reason, this);
    }

    private void OnDisable()
    {
        StopWaves();
    }
}
