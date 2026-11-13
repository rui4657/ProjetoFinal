using System;

public enum ZombieArchetype { Walker, Runner, Brute, Spitter }

public readonly struct ZombieProfile
{
    public readonly string Name;
    public readonly int Health, Damage;
    public readonly float Speed, Cooldown, Windup;
    public ZombieProfile(string name, int health, int damage, float speed, float cooldown, float windup)
    { Name = name; Health = health; Damage = damage; Speed = speed; Cooldown = cooldown; Windup = windup; }
}

/// <summary>Each prefab has one role. No zombie receives the other roles' attacks.</summary>
public static class ZombieProfiles
{
    public static ZombieProfile Get(ZombieArchetype kind)
    {
        switch (kind)
        {
            case ZombieArchetype.Runner: return new ZombieProfile("CORREDOR", 70, 20, 6.2f, 0.95f, 0.24f);
            case ZombieArchetype.Brute: return new ZombieProfile("BRUTAMONTES", 330, 45, 2.7f, 1.8f, 0.7f);
            case ZombieArchetype.Spitter: return new ZombieProfile("CUSPIDOR", 95, 24, 3f, 3.4f, 0.8f);
            default: return new ZombieProfile("INFETADO", 100, 28, 3.1f, 1.3f, 0.35f);
        }
    }

    public static ZombieArchetype Choose(int round, int ordinal)
    {
        round = Math.Max(1, round); ordinal = Math.Max(0, ordinal);
        int bruteInterval = round >= 8 ? 12 : 18;
        if (round >= 5 && ordinal % bruteInterval == bruteInterval - 1) return ZombieArchetype.Brute;
        if (round >= 3 && ordinal % 6 == 4) return ZombieArchetype.Spitter;
        if (round >= 2 && (ordinal % 3 == 1 || (round >= 8 && ordinal % 4 == 2))) return ZombieArchetype.Runner;
        return ZombieArchetype.Walker;
    }

    public static int HealthForRound(ZombieArchetype kind, int round)
        => (int)Math.Min(35000d, Math.Round(Get(kind).Health * Math.Pow(1.14d, round <= 1 ? 0 : Math.Min(45, round - 1))));
}
