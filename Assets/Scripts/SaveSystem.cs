using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    // Sistema de Save
    public static SaveSystem instance;
    // Referência para a base de dados
    private Database db;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        db = new Database();
    }

    // Versão antiga (4 parâmetros) continua a existir se precisares
    public void Save(int vida, string armaNome, int ammoMag, int ammoReserva)
    {
        Save(vida, armaNome, ammoMag, ammoReserva, 1, Vector3.zero);
    }

    // Versão completa com ronda e posição
    public void Save(int vida, string armaNome, int ammoMag, int ammoReserva, int round, Vector3 playerPos)
    {
        GameState gs = new GameState
        {
            id = 1,
            vida = vida,
            arma_nome = armaNome,
            ammo_carregador = ammoMag,
            ammo_reserva = ammoReserva,
            round = round,
            pos_x = playerPos.x,
            pos_y = playerPos.y,
            pos_z = playerPos.z
        };

        db.SaveGame(gs);
        Debug.Log($"SAVE OK | HP={vida}, Arma={armaNome}, Mag={ammoMag}, Res={ammoReserva}, Round={round}, Pos={playerPos}");
    }

    public GameState Load()
    {
        GameState gs = db.LoadGame();

        if (gs == null || gs.vida <= 0)
        {
            Debug.Log("SEM SAVE ou SAVE INVÁLIDO (vida <= 0) - a criar novo...");

            gs = new GameState
            {
                id = 1,
                vida = 100,
                arma_nome = "Ak",
                ammo_carregador = 30,
                ammo_reserva = 120,
                round = 1,
                pos_x = 0f,
                pos_y = 0f,
                pos_z = 0f
            };

            db.SaveGame(gs);
        }

        Debug.Log($"LOAD OK | HP={gs.vida}, Arma={gs.arma_nome}, Mag={gs.ammo_carregador}, Res={gs.ammo_reserva}, Round={gs.round}, Pos=({gs.pos_x},{gs.pos_y},{gs.pos_z})");
        return gs;
    }
    public void UpdateRound(int newRound)
    {
        GameState gs = Load();
        if (gs == null) return;

        gs.round = newRound;
        db.SaveGame(gs);
    }



}
