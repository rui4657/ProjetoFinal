using UnityEngine;
using System.Data;
using Mono.Data.SqliteClient;
using System.IO;

public class Database
{
    private string connectionString;

    public Database()
    {
        string dbPath = Path.Combine(Application.persistentDataPath, "savegame.db");
        connectionString = "URI=file:" + dbPath;

        Debug.Log("DB Path: " + dbPath);

        CreateTableIfNotExists();
    }

    private void CreateTableIfNotExists()
    {
        using (var conn = new SqliteConnection(connectionString))
        {
            conn.Open();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS GameState (
                        id INTEGER PRIMARY KEY,
                        vida INTEGER,
                        arma_nome TEXT,
                        ammo_carregador INTEGER,
                        ammo_reserva INTEGER,
                        round INTEGER,
                        pos_x REAL,
                        pos_y REAL,
                        pos_z REAL
                    );";
                cmd.ExecuteNonQuery();
            }
        }
    }

    public void SaveGame(GameState state)
    {
        using (var conn = new SqliteConnection(connectionString))
        {
            conn.Open();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT OR REPLACE INTO GameState
                    (id, vida, arma_nome, ammo_carregador, ammo_reserva, round, pos_x, pos_y, pos_z)
                    VALUES (@id, @vida, @arma_nome, @ammo_carregador, @ammo_reserva, @round, @pos_x, @pos_y, @pos_z);";

                cmd.Parameters.Add(new SqliteParameter("@id", state.id));
                cmd.Parameters.Add(new SqliteParameter("@vida", state.vida));
                cmd.Parameters.Add(new SqliteParameter("@arma_nome", state.arma_nome));
                cmd.Parameters.Add(new SqliteParameter("@ammo_carregador", state.ammo_carregador));
                cmd.Parameters.Add(new SqliteParameter("@ammo_reserva", state.ammo_reserva));
                cmd.Parameters.Add(new SqliteParameter("@round", state.round));
                cmd.Parameters.Add(new SqliteParameter("@pos_x", state.pos_x));
                cmd.Parameters.Add(new SqliteParameter("@pos_y", state.pos_y));
                cmd.Parameters.Add(new SqliteParameter("@pos_z", state.pos_z));

                cmd.ExecuteNonQuery();
            }
        }
    }

    public GameState LoadGame()
    {
        using (var conn = new SqliteConnection(connectionString))
        {
            conn.Open();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT id, vida, arma_nome, ammo_carregador, ammo_reserva, round, pos_x, pos_y, pos_z
                    FROM GameState
                    WHERE id = 1;";

                using (IDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        GameState gs = new GameState
                        {
                            id = reader.GetInt32(0),
                            vida = reader.GetInt32(1),
                            arma_nome = reader.GetString(2),
                            ammo_carregador = reader.GetInt32(3),
                            ammo_reserva = reader.GetInt32(4),
                            round = reader.GetInt32(5),
                            pos_x = (float)reader.GetDouble(6),
                            pos_y = (float)reader.GetDouble(7),
                            pos_z = (float)reader.GetDouble(8)
                        };

                        return gs;
                    }
                }
            }
        }

        return null;
    }

    // ------------------- NOVO: EXISTS (para mostrar/esconder botões) -------------------
    public bool HasSave(int id = 1)
    {
        using (var conn = new SqliteConnection(connectionString))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT 1 FROM GameState WHERE id = @id LIMIT 1;";
                cmd.Parameters.Add(new SqliteParameter("@id", id));

                using (IDataReader r = cmd.ExecuteReader())
                    return r.Read();
            }
        }
    }

    // ------------------- NOVO: DELETE (apagar o save) -------------------
    public void DeleteSave(int id = 1)
    {
        using (var conn = new SqliteConnection(connectionString))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM GameState WHERE id = @id;";
                cmd.Parameters.Add(new SqliteParameter("@id", id));
                cmd.ExecuteNonQuery();
            }
        }
    }
}
