using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using Mono.Data.SqliteClient;
using UnityEngine;

// Connections are short-lived; schema changes and writes are atomic.
public class Database
{
    private readonly string connectionString;
    public bool IsAvailable { get; private set; }
    private bool reportedError;

    public Database()
    {
        connectionString = "URI=file:" + Path.Combine(Application.persistentDataPath, "savegame.db");
        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            EnsureSchema();
            IsAvailable = true;
        }
        catch (Exception ex) { Report(ex); }
    }

    private void EnsureSchema()
    {
        using (var conn = new SqliteConnection(connectionString))
        {
            conn.Open();
            using (IDbTransaction transaction = conn.BeginTransaction())
            {
                using (IDbCommand cmd = conn.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = @"CREATE TABLE IF NOT EXISTS GameState (
                        id INTEGER PRIMARY KEY, vida INTEGER, arma_nome TEXT,
                        ammo_carregador INTEGER, ammo_reserva INTEGER);";
                    cmd.ExecuteNonQuery();
                }
                var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (IDbCommand cmd = conn.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = "PRAGMA table_info(GameState);";
                    using (IDataReader reader = cmd.ExecuteReader())
                        while (reader.Read()) columns.Add(reader.GetString(1));
                }
                // Only constant column names reach this migration statement.
                string[] names = { "round", "pos_x", "pos_y", "pos_z", "has_position",
                    "points", "kills", "headshots", "weapon_tier", "health_bonus", "reload_perk",
                    "unlocked_zones", "power_enabled", "elapsed_seconds" };
                for (int i = 0; i < names.Length; i++)
                {
                    if (columns.Contains(names[i])) continue;
                    string type = names[i].StartsWith("pos_", StringComparison.Ordinal) || names[i] == "elapsed_seconds"
                        ? "REAL" : "INTEGER";
                    int defaultValue = names[i] == "round" ? 1 : names[i] == "points" ? 500 : 0;
                    using (IDbCommand cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = transaction;
                        cmd.CommandText = "ALTER TABLE GameState ADD COLUMN " + names[i] + " " + type +
                            " NOT NULL DEFAULT " + defaultValue.ToString(CultureInfo.InvariantCulture) + ";";
                        cmd.ExecuteNonQuery();
                    }
                }
                transaction.Commit();
            }
        }
    }

    public bool SaveGame(GameState state)
    {
        if (!IsAvailable || state == null || state.vida <= 0) return false;
        try
        {
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using (IDbTransaction transaction = conn.BeginTransaction())
                using (IDbCommand cmd = conn.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = @"INSERT OR REPLACE INTO GameState
                        (id, vida, arma_nome, ammo_carregador, ammo_reserva, round,
                         pos_x, pos_y, pos_z, has_position, points, kills, headshots,
                         weapon_tier, health_bonus, reload_perk, unlocked_zones, power_enabled, elapsed_seconds)
                        VALUES (@id, @vida, @arma, @mag, @reserve, @round,
                         @x, @y, @z, @position, @points, @kills, @headshots,
                         @tier, @health, @reload, @zones, @power, @elapsed);";
                    Add(cmd, "@id", state.id);
                    Add(cmd, "@vida", state.vida);
                    Add(cmd, "@arma", state.arma_nome ?? "AK");
                    Add(cmd, "@mag", Math.Max(0, state.ammo_carregador));
                    Add(cmd, "@reserve", Math.Max(0, state.ammo_reserva));
                    Add(cmd, "@round", Math.Max(1, state.round));
                    Add(cmd, "@x", state.pos_x);
                    Add(cmd, "@y", state.pos_y);
                    Add(cmd, "@z", state.pos_z);
                    Add(cmd, "@position", state.has_position != 0 ? 1 : 0);
                    Add(cmd, "@points", Math.Max(0, state.points));
                    Add(cmd, "@kills", Math.Max(0, state.kills));
                    Add(cmd, "@headshots", Math.Max(0, state.headshots));
                    Add(cmd, "@tier", Math.Max(0, state.weapon_tier));
                    Add(cmd, "@health", Math.Max(0, state.health_bonus));
                    Add(cmd, "@reload", state.reload_perk != 0 ? 1 : 0);
                    Add(cmd, "@zones", Math.Max(0, state.unlocked_zones));
                    Add(cmd, "@power", state.power_enabled != 0 ? 1 : 0);
                    Add(cmd, "@elapsed", SafeElapsed(state.elapsed_seconds));
                    cmd.ExecuteNonQuery();
                    transaction.Commit();
                }
            }
            return true;
        }
        catch (Exception ex) { Report(ex); return false; }
    }

    public GameState LoadGame()
    {
        if (!IsAvailable) return null;
        try
        {
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using (IDbCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT id, vida, arma_nome, ammo_carregador, ammo_reserva,
                        round, pos_x, pos_y, pos_z, has_position, points, kills, headshots,
                        weapon_tier, health_bonus, reload_perk, unlocked_zones, power_enabled, elapsed_seconds
                        FROM GameState WHERE id = 1;";
                    using (IDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read()) return null;
                        var state = new GameState
                        {
                            id = ReadInt(reader, 0, 1), vida = ReadInt(reader, 1, 100),
                            arma_nome = reader.IsDBNull(2) ? "AK" : reader.GetString(2),
                            ammo_carregador = ReadInt(reader, 3, 30), ammo_reserva = ReadInt(reader, 4, 120),
                            round = Math.Max(1, ReadInt(reader, 5, 1)),
                            pos_x = ReadFloat(reader, 6), pos_y = ReadFloat(reader, 7), pos_z = ReadFloat(reader, 8),
                            has_position = ReadInt(reader, 9), points = Math.Max(0, ReadInt(reader, 10, 500)),
                            kills = Math.Max(0, ReadInt(reader, 11)), headshots = Math.Max(0, ReadInt(reader, 12)),
                            weapon_tier = ReadInt(reader, 13), health_bonus = ReadInt(reader, 14),
                            reload_perk = ReadInt(reader, 15),
                            unlocked_zones = Math.Max(0, ReadInt(reader, 16)),
                            power_enabled = ReadInt(reader, 17) != 0 ? 1 : 0,
                            elapsed_seconds = SafeElapsed(ReadFloat(reader, 18))
                        };
                        // Original saves did not carry an explicit position-valid flag.
                        if (state.pos_x != 0f || state.pos_y != 0f || state.pos_z != 0f) state.has_position = 1;
                        return state.vida > 0 ? state : null;
                    }
                }
            }
        }
        catch (Exception ex) { Report(ex); return null; }
    }

    public bool HasSave(int id = 1)
    {
        if (!IsAvailable) return false;
        try
        {
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using (IDbCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT 1 FROM GameState WHERE id = @id AND vida > 0 LIMIT 1;";
                    Add(cmd, "@id", id);
                    using (IDataReader reader = cmd.ExecuteReader()) return reader.Read();
                }
            }
        }
        catch (Exception ex) { Report(ex); return false; }
    }

    public bool DeleteSave(int id = 1)
    {
        if (!IsAvailable) return false;
        try
        {
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using (IDbCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM GameState WHERE id = @id;";
                    Add(cmd, "@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
            return true;
        }
        catch (Exception ex) { Report(ex); return false; }
    }

    private static void Add(IDbCommand cmd, string name, object value)
    { cmd.Parameters.Add(new SqliteParameter(name, value)); }
    private static int ReadInt(IDataReader reader, int index, int fallback = 0)
    { return reader.IsDBNull(index) ? fallback : Convert.ToInt32(reader.GetValue(index), CultureInfo.InvariantCulture); }
    private static float ReadFloat(IDataReader reader, int index)
    { return reader.IsDBNull(index) ? 0f : Convert.ToSingle(reader.GetValue(index), CultureInfo.InvariantCulture); }
    private static float SafeElapsed(float value)
    { return float.IsNaN(value) || float.IsInfinity(value) ? 0f : Math.Max(0f, value); }
    private void Report(Exception ex)
    {
        if (reportedError) return;
        reportedError = true;
        Debug.LogWarning("Save data is unavailable. The game can continue without saving. " + ex.Message);
    }
}
