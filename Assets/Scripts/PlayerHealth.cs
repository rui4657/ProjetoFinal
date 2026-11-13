using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Min(1)] public int maxHP = 100;
    public int CurrentHP { get; private set; }
    public int MaxHP => maxHP;
    public bool IsDead { get; private set; }
    public float LastDamageTime { get; private set; } = float.NegativeInfinity;
    public event Action<int> Damaged;

    [Header("Regeneração")]
    [Min(0f)] public float regenerationDelay = 5f;
    [Min(0f)] public float regenerationPerSecond = 12f;

    [Header("Game Over")]
    [SerializeField] private string menuSceneName = "MainMenu";
    [SerializeField] private float gameOverDelay = 2f;

    private float regenerationRemainder;
    private RoundManager roundManager;

    void Awake() => InitNewGame();
    void Start() => roundManager = FindFirstObjectByType<RoundManager>();

    void Update()
    {
        if (IsDead || Time.timeScale <= 0f || CurrentHP >= maxHP || Time.time - LastDamageTime < regenerationDelay) return;
        regenerationRemainder += Mathf.Max(0f, regenerationPerSecond) * Time.deltaTime;
        int recovered = Mathf.FloorToInt(regenerationRemainder);
        if (recovered <= 0) return;
        regenerationRemainder -= recovered;
        CurrentHP = Mathf.Min(maxHP, CurrentHP + recovered);
        RequestSave();
    }

    public void InitNewGame()
    {
        maxHP = Mathf.Max(1, maxHP);
        CurrentHP = maxHP;
        IsDead = false;
        LastDamageTime = float.NegativeInfinity;
        regenerationRemainder = 0f;
    }

    public void ApplyLoadedHealth(int savedHealth, int savedMaxHealth = 0)
    {
        if (savedMaxHealth > 0) maxHP = savedMaxHealth;
        CurrentHP = Mathf.Clamp(savedHealth, 1, Mathf.Max(1, maxHP));
        IsDead = false;
        LastDamageTime = Time.time;
        regenerationRemainder = 0f;
    }

    public void IncreaseMaxHealth(int amount)
    {
        if (amount <= 0 || IsDead) return;
        maxHP += amount;
        CurrentHP = Mathf.Min(maxHP, CurrentHP + amount);
        RequestSave();
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0 || Time.timeScale <= 0f) return;
        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        LastDamageTime = Time.time;
        regenerationRemainder = 0f;
        Damaged?.Invoke(amount);
        if (CurrentHP <= 0) Die();
        else RequestSave();
    }

    void Die()
    {
        if (IsDead) return;
        IsDead = true;
        if (!roundManager) roundManager = FindFirstObjectByType<RoundManager>();
        if (roundManager) roundManager.StopWaves();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (SurvivalSession.Instance != null) SurvivalSession.Instance.EndGame();
        else StartCoroutine(GameOverRoutine());
    }

    IEnumerator GameOverRoutine()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, gameOverDelay));
        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded(menuSceneName)) SceneManager.LoadScene(menuSceneName);
        else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void RequestSave()
    {
        if (SaveSystem.instance != null && !IsDead) SaveSystem.instance.RequestSave();
    }
}
