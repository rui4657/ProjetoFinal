using System.Collections.Generic;
using UnityEngine;

public sealed class BarricadeNetwork : MonoBehaviour
{
    public readonly List<BarricadeEntry> Entries = new List<BarricadeEntry>();
    public BarricadeLedger Ledger { get; private set; } = new BarricadeLedger();
    public WorldProgression World { get; private set; }
    public void Initialize(WorldProgression world) { World = world; }
    public void Restore(string encoded)
    {
        Ledger = BarricadeLedger.TryDecode(encoded, out BarricadeLedger restored) ? restored : new BarricadeLedger();
        foreach (var entry in Entries) if (entry != null) entry.RefreshBoards(true);
    }
    public bool Repair(BarricadeEntry entry)
    {
        var session = SurvivalSession.Instance;
        if (entry == null || entry.Network != this || session == null || session.IsGameOver || Time.timeScale <= 0 || session.IsShopOpen) return false;
        int round = session.Rounds != null ? session.Rounds.currentRound : 1;
        if (!Ledger.Repair(entry.Site.Id, round, out int reward)) return false;
        if (reward > 0) session.AwardBonus(reward);
        entry.RefreshBoards(false); Save(); return true;
    }
    public void Save() { if (SaveSystem.instance != null) SaveSystem.instance.RequestSave(); }
}
