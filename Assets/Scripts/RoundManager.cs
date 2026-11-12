using UnityEngine;
using System.Collections;

public class RoundManager : MonoBehaviour
{
    [Header("Rounds")]
    public int currentRound = 1;        // ronda atual
    public int zombiesPerRound = 5;     // base
    public float spawnRate = 1.5f;      // tempo entre spawns

    [Header("Intervalo entre rondas / início")]
    public float intermissionTime = 30f;    // segundos de pausa (entre rondas e no início)
    [HideInInspector] public float intermissionTimer = 0f; // contador atual
    [HideInInspector] public bool inIntermission = false;  // estamos na pausa?
    [HideInInspector] public bool isGameStarting = true;   // true só na pausa inicial

    [Header("References")]
    public ZombieSpawner spawner;       // referência ao spawner

    private int zombiesSpawned = 0;     // zombies spawnados na ronda
    private int zombiesAlive = 0;       // zombies vivos na ronda


    void Start()
    {
        // Em vez de começar logo a ronda, começamos com uma pausa de 30s
        inIntermission = true;
        isGameStarting = true;
        intermissionTimer = intermissionTime;
        Debug.Log("Jogo começa em " + intermissionTime + " segundos");
    }

    void Update()
    {
        // Se estamos na pausa (início ou entre rondas), conta o tempo para baixo
        if (inIntermission)
        {
            intermissionTimer -= Time.deltaTime;

            if (intermissionTimer <= 0f)
            {
                inIntermission = false;
                StartRound();
            }
        }
    }

    void StartRound()
    {
        Debug.Log("===== RONDA " + currentRound + " =====");

        // a partir daqui já não é "início de jogo"
        if (isGameStarting)
            isGameStarting = false;

        zombiesSpawned = 0;
        zombiesAlive = 0;

        // A tua fórmula original
        int totalZombies = zombiesPerRound + (currentRound * 5);

        StartCoroutine(SpawnWave(totalZombies));
    }

    IEnumerator SpawnWave(int totalZombies)
    {
        for (int i = 1; i <= totalZombies; i++)
        {
            if (spawner == null)
            {
                Debug.LogWarning("RoundManager: Spawner não ligado no Inspector!");
                yield return new WaitForSeconds(spawnRate);
                continue;
            }

            bool spawned = spawner.SpawnZombie(currentRound);

            if (spawned)
            {
                zombiesSpawned++;
                zombiesAlive++;
            }
            else
            {
                // opcional: não incrementa vivos, para não bloquear ronda
            }

            yield return new WaitForSeconds(spawnRate);
        }

        Debug.Log("Total de zumbis na ronda " + currentRound + ": " + totalZombies);
    }


    public void ZombieDied()
    {
        zombiesAlive--;

        if (zombiesAlive == 0 && !inIntermission) // ronda completa
        {
            Debug.Log("RONDA " + currentRound + " COMPLETA!");

            // ativa intervalo de preparação
            inIntermission = true;
            intermissionTimer = intermissionTime;

            Debug.Log("Próxima ronda: " + (currentRound + 1) + " em " + intermissionTime + " segundos");

            // sobe o número da próxima ronda; ela começa quando o timer chegar a 0
            currentRound++;
        }
    }
}
