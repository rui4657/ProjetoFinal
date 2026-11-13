using System;

public readonly struct QuarantineRect
{
    public readonly float MinX, MaxX, MinZ, MaxZ;
    public QuarantineRect(float minX, float maxX, float minZ, float maxZ)
    { MinX = minX; MaxX = maxX; MinZ = minZ; MaxZ = maxZ; }
    public bool Contains(float x, float z, float margin = 0)
        => x >= MinX - margin && x <= MaxX + margin && z >= MinZ - margin && z <= MaxZ + margin;
}

/// <summary>Authored village sectors. Used by physical fences, saves, spawn selection and HUD.</summary>
public static class QuarantineRules
{
    public const int Church = 1, Village = 2, Outskirts = 4;
    public const int ChurchRound = 3, VillageRound = 5, OutskirtsRound = 8;
    public static readonly QuarantineRect Start = new QuarantineRect(432, 480, 540, 564);
    public static readonly QuarantineRect Churchyard = new QuarantineRect(432, 480, 540, 620);
    public static readonly QuarantineRect VillageBounds = new QuarantineRect(400, 496, 494, 636);

    public static int Normalize(int flags)
    {
        flags = Math.Max(0, flags) & 7;
        if ((flags & Outskirts) != 0) flags |= Church | Village;
        if ((flags & Village) != 0) flags |= Church;
        return flags;
    }

    public static int ForRound(int flags, int round)
    {
        if (round >= ChurchRound) flags |= Church;
        if (round >= VillageRound) flags |= Village;
        if (round >= OutskirtsRound) flags |= Outskirts;
        return Normalize(flags);
    }

    public static QuarantineRect Bounds(int flags)
        => (flags & Village) != 0 ? VillageBounds : (flags & Church) != 0 ? Churchyard : Start;

    public static bool Contains(float x, float z, int flags, float margin = 0)
    {
        if (float.IsNaN(x) || float.IsInfinity(x) || float.IsNaN(z) || float.IsInfinity(z)) return false;
        return (flags & Outskirts) != 0 || Bounds(flags).Contains(x, z, margin);
    }
}
