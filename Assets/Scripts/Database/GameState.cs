using System;

// One recovery slot. A continued run restarts its current round.
[Serializable]
public class GameState
{
    public int id = 1;
    public int vida = 100;
    public string arma_nome = "AK";
    public int ammo_carregador = 30;
    public int ammo_reserva = 120;
    public int round = 1;
    public float pos_x;
    public float pos_y;
    public float pos_z;
    public int has_position;
    public int points = 500;
    public int kills;
    public int headshots;
    public int weapon_tier;
    public int health_bonus;
    public int reload_perk;
    public int unlocked_zones;
    public int power_enabled;
    public float elapsed_seconds;
    public string weapon_loadout = "";
    public string barricades = "";
    public int shortcut_flags;
}
