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
        var world = WorldProgression.Instance;
        Vector3 initialPosition = movement != null ? movement.transform.position : Vector3.zero;

        GameState state = null;
        bool loaded = requestedLoad && saves.TryLoad(out state);
        if (loaded)
        {
            // Open saved checkpoints and restore power before validating saved position.
            if (world != null) world.RestoreProgress(state.unlocked_zones, state.power_enabled != 0, state.round);
            if (session != null) session.ApplySavedProgress(state);
            int healthBonus = session != null ? session.HealthBonus : Mathf.Clamp(state.health_bonus, 0, 100);
            if (health != null) health.ApplyLoadedHealth(state.vida, health.MaxHP + healthBonus);
            if (gunSystem != null)
                gunSystem.ApplyLoadedGun(state.arma_nome, state.ammo_carregador, state.ammo_reserva);
            if (rounds != null) rounds.currentRound = Mathf.Max(1, state.round);
            RestorePosition(state, movement, world, initialPosition, session);
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

    private static void RestorePosition(GameState state, PlayerController movement,
        WorldProgression world, Vector3 initialPosition, SurvivalSession session)
    {
        if (movement == null) return;
        Vector3 position = new Vector3(state.pos_x, state.pos_y, state.pos_z);
        if (state.has_position != 0 && IsFinite(position) &&
            (world == null || world.IsPositionAllowed(position)))
        {
            movement.ApplyLoadedPosition(position);
            return;
        }

        // Never replace a rejected snapshot with the old Vector3.zero placeholder.
        // Keep the actual scene spawn; only use the world fallback if that is unsafe.
        Vector3 fallback = initialPosition;
        if (!IsFinite(fallback) || (world != null && !world.IsPositionAllowed(fallback)))
            fallback = world != null ? world.SafeSpawn : initialPosition;
        if (fallback != Vector3.zero && IsFinite(fallback) &&
            (world == null || world.IsPositionAllowed(fallback)) && fallback != movement.transform.position)
            movement.ApplyLoadedPosition(fallback);
        if (state.has_position != 0 && session != null)
            session.ShowMessage("Posição guardada inválida. Voltaste ao ponto inicial.", 4f);
    }

    private static bool IsFinite(Vector3 value)
    {
        return !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
               !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
               !float.IsNaN(value.z) && !float.IsInfinity(value.z);
    }
}
