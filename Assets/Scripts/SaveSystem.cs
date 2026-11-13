using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-200)]
public class SaveSystem : MonoBehaviour
{
    public static SaveSystem instance;
    [SerializeField, Min(0.1f)] private float saveDelay = 2f;
    [SerializeField, Min(5f)] private float autosaveInterval = 15f;
    private Database db;
    private PlayerHealth player;
    private GunSystem gun;
    private RoundManager rounds;
    private bool gameplayReady;
    private bool dirty;
    private float saveAt;
    private float nextAutosave;
    private float nextWriteAllowed;
    private GameState legacySnapshot;

    public static SaveSystem EnsureInstance()
    {
        if (instance == null)
        {
            var existing = FindFirstObjectByType<SaveSystem>();
            if (existing != null) instance = existing;
            else new GameObject("SaveSystem").AddComponent<SaveSystem>();
        }
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(this); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        db = new Database();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Additive) return;
        player = null; gun = null; rounds = null;
        gameplayReady = false;
        dirty = false;
        legacySnapshot = null;
    }

    public void BeginSession()
    {
        player = FindFirstObjectByType<PlayerHealth>();
        gun = FindFirstObjectByType<GunSystem>();
        rounds = FindFirstObjectByType<RoundManager>();
        gameplayReady = player != null && gun != null;
        nextAutosave = Time.unscaledTime + autosaveInterval;
        RequestSave();
    }

    public void DiscardPendingSave()
    { dirty = false; legacySnapshot = null; gameplayReady = false; }

    public void ResetForNewGame() { DeleteSave(); }

    public void DeleteSave()
    {
        DiscardPendingSave();
        if (db != null) db.DeleteSave();
    }

    public void RequestSave()
    {
        if (!gameplayReady || player == null || player.IsDead) return;
        // Do not push the deadline forward for every bullet in a held burst.
        if (!dirty) saveAt = Time.unscaledTime + Mathf.Max(0.1f, saveDelay);
        dirty = true;
    }

    private void Update()
    {
        if (gameplayReady && Time.unscaledTime >= nextAutosave)
        {
            nextAutosave = Time.unscaledTime + autosaveInterval;
            RequestSave();
        }
        if (dirty && Time.unscaledTime >= saveAt && Time.unscaledTime >= nextWriteAllowed)
            FlushSave();
    }

    public void FlushSave()
    {
        if (db == null || Time.unscaledTime < nextWriteAllowed) return;
        GameState state = CaptureSnapshot();
        if (state == null && gameplayReady) { dirty = false; legacySnapshot = null; return; }
        if (state == null) state = legacySnapshot;
        if (state == null || state.vida <= 0) return;
        if (db.SaveGame(state)) { dirty = false; legacySnapshot = null; }
        else { nextWriteAllowed = Time.unscaledTime + 10f; }
    }

    private GameState CaptureSnapshot()
    {
        if (!gameplayReady || player == null || gun == null || player.IsDead || player.CurrentHP <= 0)
            return null;
        Vector3 position = player.transform.position;
        var state = new GameState
        {
            vida = player.CurrentHP, arma_nome = gun.gunName,
            ammo_carregador = gun.currentAmmo, ammo_reserva = gun.reserveAmmo,
            round = rounds != null ? Mathf.Max(1, rounds.currentRound) : 1,
            pos_x = position.x, pos_y = position.y, pos_z = position.z, has_position = 1
        };
        CopyProgress(state);
        return state;
    }

    private static void CopyProgress(GameState state)
    {
        var session = SurvivalSession.Instance;
        if (session == null) return;
        state.points = session.Points;
        state.kills = session.Kills;
        state.headshots = session.Headshots;
        state.weapon_tier = session.WeaponTier;
        state.health_bonus = session.HealthBonus;
        state.reload_perk = session.HasQuickReload ? 1 : 0;
    }

    // Retained for existing Unity events and external scripts; writes are debounced.
    public void Save(int vida, string armaNome, int ammoMag, int ammoReserva)
    {
        int round = rounds != null ? rounds.currentRound : 1;
        Vector3 position = player != null ? player.transform.position : Vector3.zero;
        Save(vida, armaNome, ammoMag, ammoReserva, round, position);
    }

    public void Save(int vida, string armaNome, int ammoMag, int ammoReserva, int round, Vector3 playerPos)
    {
        if (vida <= 0 || (player != null && player.IsDead)) return;
        legacySnapshot = new GameState
        {
            vida = vida, arma_nome = armaNome, ammo_carregador = ammoMag, ammo_reserva = ammoReserva,
            round = Mathf.Max(1, round), pos_x = playerPos.x, pos_y = playerPos.y, pos_z = playerPos.z,
            has_position = playerPos != Vector3.zero ? 1 : 0
        };
        CopyProgress(legacySnapshot);
        if (!dirty) saveAt = Time.unscaledTime + Mathf.Max(0.1f, saveDelay);
        dirty = true;
    }

    public bool TryLoad(out GameState state)
    {
        state = db != null ? db.LoadGame() : null;
        return state != null;
    }

    public GameState Load()
    { TryLoad(out GameState state); return state; }

    public void UpdateRound(int newRound)
    {
        if (rounds != null) rounds.currentRound = Mathf.Max(1, newRound);
        RequestSave();
    }

    private void OnApplicationPause(bool paused) { if (paused) FlushSave(); }
    private void OnApplicationQuit() { FlushSave(); PlayerPrefs.Save(); }
    private void OnDestroy()
    {
        if (instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
    }
}
