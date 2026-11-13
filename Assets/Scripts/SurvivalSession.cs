using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public enum SurvivalPowerUp { MaxAmmo, DoublePoints, InstaKill }

/// <summary>Per-match progression. Installed automatically, so existing scenes need no wiring.</summary>
public sealed class SurvivalSession : MonoBehaviour
{
    public static SurvivalSession Instance { get; private set; }
    public int Points { get; private set; } = 500;
    public int Kills { get; private set; }
    public int Headshots { get; private set; }
    public int ShotsFired { get; private set; }
    public int Hits { get; private set; }
    public int WeaponTier { get; private set; }
    public int HealthBonus { get; private set; }
    public bool HasQuickReload { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool IsShopOpen { get; private set; }
    public float DamageMultiplier => 1f + WeaponTier * 0.5f;
    public float ReloadMultiplier => HasQuickReload ? 0.65f : 1f;
    public int UpgradeCost => 1500 * (WeaponTier + 1);
    public bool IsWeaponUpgradeUnlocked => WorldProgression.Instance == null || WorldProgression.Instance.ProgressionUnavailable ||
        ((WorldProgression.Instance.UnlockedZones & 1) != 0 && WorldProgression.Instance.PowerEnabled);
    public bool ArePerksUnlocked => WorldProgression.Instance == null || WorldProgression.Instance.ProgressionUnavailable || WorldProgression.Instance.PowerEnabled;
    public float ElapsedSeconds { get; private set; }
    public float HitMarkerUntil { get; private set; }
    public bool LastHitWasKill { get; private set; }
    public bool LastHitWasHeadshot { get; private set; }
    public string Message { get; private set; }
    public float MessageUntil { get; private set; }
    public string InteractionPrompt { get; private set; }
    public float InteractionUntil { get; private set; }
    public int LastPointsAwarded { get; private set; }
    public float PointsPopupUntil { get; private set; }

    public PlayerHealth Player { get; private set; }
    public GunSystem Gun { get; private set; }
    public RoundManager Rounds { get; private set; }
    public PlayerController Movement { get; private set; }
    public bool DoublePointsActive => !IsGameOver && Time.time < doublePointsUntil;
    public bool InstaKillActive => !IsGameOver && Time.time < instaKillUntil;
    public float DoublePointsRemaining => DoublePointsActive ? doublePointsUntil - Time.time : 0f;
    public float InstaKillRemaining => InstaKillActive ? instaKillUntil - Time.time : 0f;
    public int ActiveDropCount { get { drops.RemoveAll(drop => drop == null); return drops.Count; } }
    private float doublePointsUntil, instaKillUntil;
    private readonly List<PowerUpPickup> drops = new List<PowerUpPickup>();
    private SurvivalAudio feedback;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        Instance = null;
        SceneManager.sceneLoaded -= SceneLoaded;
        SceneManager.sceneLoaded += SceneLoaded;
    }

    static void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Additive || Instance != null) return;
        if (FindFirstObjectByType<PlayerHealth>() == null) return;
        new GameObject("Survival Session").AddComponent<SurvivalSession>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Player = FindFirstObjectByType<PlayerHealth>();
        Gun = FindFirstObjectByType<GunSystem>();
        Rounds = FindFirstObjectByType<RoundManager>();
        Movement = FindFirstObjectByType<PlayerController>();
        WorldProgression.Ensure();
        LevelAtmosphere.Ensure();
        feedback = gameObject.AddComponent<SurvivalAudio>();
        gameObject.AddComponent<SurvivalHUD>();
    }

    void Start()
    {
        ShowMessage("SOBREVIVE À PRIMEIRA RONDA", 4f);
    }

    void Update()
    {
        if (IsGameOver) return;
        ElapsedSeconds += Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (IsShopOpen) CloseShop();
            else if (Time.timeScale > 0f && Rounds != null && Rounds.inIntermission) OpenShop();
            else if (Time.timeScale > 0f) ShowMessage("O arsenal abre entre rondas.");
        }
        if (!IsShopOpen && Time.timeScale > 0f && Input.GetKeyDown(KeyCode.Return) && Rounds != null)
            Rounds.SkipIntermission();
    }

    public void RegisterShot()
    {
        if (!IsGameOver) ShotsFired++;
    }

    public void RegisterHit(bool killed, bool headshot, bool isMelee = false, bool countAccuracy = true)
    {
        if (IsGameOver) return;
        if (!isMelee && countAccuracy) Hits++;
        LastHitWasKill = killed;
        LastHitWasHeadshot = headshot;
        HitMarkerUntil = Time.unscaledTime + (killed ? 0.24f : 0.13f);
        int reward = killed ? (isMelee ? 130 : headshot ? 100 : 60) : 10;
        if (killed) { Kills++; if (headshot) Headshots++; }
        AwardPoints(reward);
        if (feedback != null) feedback.Hit(killed, headshot);
        if (SaveSystem.instance != null) SaveSystem.instance.RequestSave();
    }

    void AwardPoints(int amount)
    {
        long reward = (long)Mathf.Max(0, amount) * (DoublePointsActive ? 2 : 1);
        Points = (int)System.Math.Min(int.MaxValue, (long)Points + reward);
        LastPointsAwarded = (int)System.Math.Min(int.MaxValue, reward);
        PointsPopupUntil = Time.unscaledTime + 0.8f;
    }

    public void AwardBonus(int amount)
    {
        if (IsGameOver) return;
        AwardPoints(amount);
        if (SaveSystem.instance != null) SaveSystem.instance.RequestSave();
    }

    public void ActivatePowerUp(SurvivalPowerUp kind)
    {
        if (IsGameOver) return;
        switch (kind)
        {
            case SurvivalPowerUp.MaxAmmo:
                if (Gun == null) return;
                Gun.RefillAllWeapons();
                ShowMessage("MUNIÇÕES MÁXIMAS", 3f);
                break;
            case SurvivalPowerUp.DoublePoints:
                doublePointsUntil = Time.time + 30f;
                ShowMessage("PONTOS A DOBRAR · 30 SEGUNDOS", 3f);
                break;
            case SurvivalPowerUp.InstaKill:
                instaKillUntil = Time.time + 10f;
                ShowMessage("MORTE INSTANTÂNEA · 10 SEGUNDOS", 3f);
                break;
        }
        if (feedback != null) feedback.Purchase();
    }

    public void TryDropPowerUp(Vector3 position)
    {
        if (IsGameOver || ActiveDropCount >= 3 || Random.value > 0.08f) return;
        if (float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z) ||
            float.IsInfinity(position.x) || float.IsInfinity(position.y) || float.IsInfinity(position.z)) return;
        var kind = (SurvivalPowerUp)Random.Range(0, 3);
        var pickup = PowerUpPickup.Create(this, kind, position);
        if (pickup != null) drops.Add(pickup);
    }

    public bool TrySpend(int amount)
    {
        if (IsGameOver || amount < 0) return false;
        if (Points < amount) { ShowMessage("Pontos insuficientes."); return false; }
        Points -= amount;
        if (SaveSystem.instance != null) SaveSystem.instance.RequestSave();
        return true;
    }

    public void OnRoundCompleted(int completedRound)
    {
        if (IsGameOver) return;
        int bonus = 100 + Mathf.Min(completedRound, 50) * 25;
        AwardPoints(bonus);
        if (feedback != null) feedback.RoundComplete();
        ShowMessage("RONDA CONCLUÍDA  +" + LastPointsAwarded + " PONTOS", 4f);
        if (SaveSystem.instance != null) SaveSystem.instance.RequestSave();
    }

    public void OpenShop()
    {
        if (IsGameOver || Rounds == null || !Rounds.inIntermission || Time.timeScale <= 0f) return;
        IsShopOpen = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseShop()
    {
        if (!IsShopOpen) return;
        IsShopOpen = false;
        Time.timeScale = IsGameOver ? 0f : 1f;
        Cursor.lockState = IsGameOver ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = IsGameOver;
    }

    public void BuyAmmo()
    { if (CanBuy()) TryBuyAmmo(); }

    bool TryBuyAmmo()
    {
        if (Gun == null) return false;
        if (Gun.reserveAmmo >= Gun.maxReserveAmmo && Gun.currentAmmo >= Gun.magSize)
        { ShowMessage("Munições completas."); return false; }
        if (!TrySpend(500)) return false;
        Gun.AddReserveAmmo(Gun.maxReserveAmmo, true);
        if (feedback != null) feedback.Purchase();
        ShowMessage("Munições reabastecidas.");
        return true;
    }

    public void BuyWeaponUpgrade()
    { if (CanBuy()) TryBuyWeaponUpgrade(); }

    bool TryBuyWeaponUpgrade()
    {
        if (!IsWeaponUpgradeUnlocked) { ShowMessage("Liberta a igreja e ativa o gerador primeiro."); return false; }
        if (WeaponTier >= 3 || !TrySpend(UpgradeCost)) return false;
        WeaponTier++;
        if (feedback != null) feedback.Purchase();
        ShowMessage("ARMA MELHORADA · NÍVEL " + WeaponTier);
        return true;
    }

    public void BuyHealthUpgrade()
    { if (CanBuy()) TryBuyHealthUpgrade(); }

    bool TryBuyHealthUpgrade()
    {
        if (!ArePerksUnlocked) { ShowMessage("Ativa o gerador para usar as melhorias."); return false; }
        if (Player == null || HealthBonus >= 100 || !TrySpend(2000)) return false;
        HealthBonus += 50;
        Player.IncreaseMaxHealth(50);
        if (feedback != null) feedback.Purchase();
        ShowMessage("FORTITUDE · +50 VIDA MÁXIMA");
        return true;
    }

    public void BuyQuickReload()
    { if (CanBuy()) TryBuyQuickReload(); }

    bool TryBuyQuickReload()
    {
        if (!ArePerksUnlocked) { ShowMessage("Ativa o gerador para usar as melhorias."); return false; }
        if (HasQuickReload || !TrySpend(1500)) return false;
        HasQuickReload = true;
        if (feedback != null) feedback.Purchase();
        ShowMessage("RECARGA RÁPIDA ATIVA");
        return true;
    }

    public bool UseWorldService(WorldService service)
    {
        var world = WorldProgression.Instance;
        if (IsGameOver || world == null || !world.CanUseService(service)) return false;
        switch (service)
        {
            case WorldService.Ammo: return TryBuyAmmo();
            case WorldService.WeaponUpgrade: return TryBuyWeaponUpgrade();
            case WorldService.HealthUpgrade: return TryBuyHealthUpgrade();
            case WorldService.QuickReload: return TryBuyQuickReload();
            default: return false;
        }
    }

    bool CanBuy() => IsShopOpen && !IsGameOver && Rounds != null && Rounds.inIntermission;

    public void ApplySavedProgress(GameState state)
    {
        if (state == null) return;
        Points = Mathf.Max(0, state.points);
        Kills = Mathf.Max(0, state.kills);
        Headshots = Mathf.Clamp(state.headshots, 0, Kills);
        WeaponTier = Mathf.Clamp(state.weapon_tier, 0, 3);
        HealthBonus = Mathf.Clamp(state.health_bonus, 0, 100);
        HasQuickReload = state.reload_perk != 0;
        ElapsedSeconds = float.IsNaN(state.elapsed_seconds) || float.IsInfinity(state.elapsed_seconds)
            ? 0f : Mathf.Clamp(state.elapsed_seconds, 0f, 31536000f);
    }

    public void ShowMessage(string message, float seconds = 2.5f)
    {
        Message = message;
        MessageUntil = Time.unscaledTime + seconds;
    }

    public void ShowInteraction(string prompt)
    {
        InteractionPrompt = prompt;
        InteractionUntil = Time.unscaledTime + 0.15f;
    }

    public void EndGame()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        IsShopOpen = false;
        doublePointsUntil = instaKillUntil = 0f;
        foreach (var drop in drops) if (drop != null) Destroy(drop.gameObject);
        drops.Clear();
        if (Rounds != null) Rounds.StopWaves();
        if (SaveSystem.instance != null) SaveSystem.instance.DeleteSave();
        int round = Rounds != null ? Rounds.currentRound : 1;
        PlayerPrefs.SetInt("best_round", Mathf.Max(PlayerPrefs.GetInt("best_round", 0), round));
        PlayerPrefs.SetInt("best_kills", Mathf.Max(PlayerPrefs.GetInt("best_kills", 0), Kills));
        PlayerPrefs.Save();
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Retry()
    {
        if (Rounds != null) Rounds.StopWaves();
        PlayerPrefs.SetInt("load", 0);
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMenu()
    {
        if (!IsGameOver && SaveSystem.instance != null) SaveSystem.instance.FlushSave();
        if (Rounds != null) Rounds.StopWaves();
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (Application.CanStreamedLevelBeLoaded("MainMenu")) SceneManager.LoadScene("MainMenu");
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
