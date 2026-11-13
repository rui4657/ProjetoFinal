using UnityEngine;

// Health, weapons and sessions finish their normal initialisation before this loader.
[DefaultExecutionOrder(200)]
public class GameLoader : MonoBehaviour
{
    [Header("References")]
    public GunSystem gunSystem;
    private bool initialized;

    private void Start()
    {
        if (initialized) return;
        initialized = true;
        bool requestedLoad = PlayerPrefs.GetInt("load", 0) == 1;
        PlayerPrefs.DeleteKey("load"); // Consume once, after every Awake has completed.
        var saves = SaveSystem.EnsureInstance();
        if (gunSystem == null) gunSystem = FindFirstObjectByType<GunSystem>();
        var health = FindFirstObjectByType<PlayerHealth>();
        var movement = FindFirstObjectByType<PlayerController>();
        var rounds = FindFirstObjectByType<RoundManager>();
        var session = SurvivalSession.Instance;

        GameState state = null;
        bool loaded = requestedLoad && saves.TryLoad(out state);
        if (loaded)
        {
            if (session != null) session.ApplySavedProgress(state);
            int healthBonus = session != null ? session.HealthBonus : Mathf.Clamp(state.health_bonus, 0, 100);
            if (health != null) health.ApplyLoadedHealth(state.vida, health.MaxHP + healthBonus);
            if (gunSystem != null)
                gunSystem.ApplyLoadedGun(state.arma_nome, state.ammo_carregador, state.ammo_reserva);
            if (rounds != null) rounds.currentRound = Mathf.Max(1, state.round);
            Vector3 position = new Vector3(state.pos_x, state.pos_y, state.pos_z);
            if (movement != null && state.has_position != 0 && IsFinite(position))
                movement.ApplyLoadedPosition(position);
        }
        else
        {
            saves.ResetForNewGame();
            if (health != null) health.InitNewGame();
            if (gunSystem != null) gunSystem.InitNewGame();
            if (rounds != null) rounds.currentRound = 1;
            if (requestedLoad && session != null)
                session.ShowMessage("Save unavailable. Starting a new run.");
        }
        saves.BeginSession();
    }

    private static bool IsFinite(Vector3 value)
    {
        return !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
               !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
               !float.IsNaN(value.z) && !float.IsInfinity(value.z);
    }
}
