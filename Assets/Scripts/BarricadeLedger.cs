using System;
using System.Globalization;

/// <summary>Repair state and rewards remain bounded, including after save/continue.</summary>
public sealed class BarricadeLedger
{
    public const int PlanksPerWindow = 6, WindowCount = 11, RewardPerPlank = 10, RoundRewardLimit = 120;
    private readonly int[] planks = new int[WindowCount];
    public int RewardRound { get; private set; } = 1;
    public int EarnedThisRound { get; private set; }
    public BarricadeLedger() { for (int i = 0; i < planks.Length; i++) planks[i] = PlanksPerWindow; }
    public int Count(int id) => id >= 0 && id < planks.Length ? planks[id] : 0;
    public bool Damage(int id, int amount)
    {
        if (id < 0 || id >= planks.Length || amount <= 0 || planks[id] == 0) return false;
        planks[id] -= Math.Min(planks[id], amount); return true;
    }
    public bool Repair(int id, int round, out int reward)
    {
        reward = 0;
        if (id < 0 || id >= planks.Length || planks[id] >= PlanksPerWindow) return false;
        round = Math.Max(1, round);
        if (round > RewardRound) { RewardRound = round; EarnedThisRound = 0; }
        planks[id]++;
        reward = Math.Min(RewardPerPlank, RoundRewardLimit - EarnedThisRound);
        EarnedThisRound += reward; return true;
    }
    public string Encode() => "v1;" + RewardRound + ";" + EarnedThisRound + ";" + string.Join(",", planks);
    public static bool TryDecode(string text, out BarricadeLedger ledger)
    {
        ledger = null;
        if (string.IsNullOrEmpty(text) || text.Length > 180) return false;
        string[] parts = text.Split(';');
        if (parts.Length != 4 || parts[0] != "v1" || !Number(parts[1], out int round) || !Number(parts[2], out int earned)) return false;
        string[] values = parts[3].Split(','); if (values.Length != WindowCount) return false;
        var result = new BarricadeLedger();
        for (int i = 0; i < values.Length; i++)
        { if (!Number(values[i], out int count)) return false; result.planks[i] = Math.Max(0, Math.Min(PlanksPerWindow, count)); }
        result.RewardRound = Math.Max(1, round); result.EarnedThisRound = Math.Max(0, Math.Min(RoundRewardLimit, earned));
        ledger = result; return true;
    }
    private static bool Number(string text, out int value) => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
}
