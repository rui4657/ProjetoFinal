using System;

public enum WeaponId { Pistol, SMG, Shotgun, Rifle }

public readonly struct WeaponSpec
{
    public readonly WeaponId Id;
    public readonly string Name;
    public readonly int Damage, Magazine, StartingReserve, ReserveLimit, Pellets, Price, AmmoPrice;
    public readonly float Rate, Range, ReloadSeconds, HipSpread, AimSpread, Recoil;
    public readonly bool Automatic, ShellReload;
    public WeaponSpec(WeaponId id, string name, int damage, int magazine, int reserve, int limit,
        float rate, float range, float reload, float hip, float aim, float recoil, bool automatic,
        int price, int ammoPrice, int pellets = 1, bool shellReload = false)
    {
        Id = id; Name = name; Damage = damage; Magazine = magazine; StartingReserve = reserve;
        ReserveLimit = limit; Rate = rate; Range = range; ReloadSeconds = reload;
        HipSpread = hip; AimSpread = aim; Recoil = recoil; Automatic = automatic;
        Price = price; AmmoPrice = ammoPrice; Pellets = pellets; ShellReload = shellReload;
    }
}

public static class WeaponCatalog
{
    public static bool IsValid(WeaponId id) => (int)id >= 0 && (int)id <= 3;
    public static WeaponSpec Get(WeaponId id)
    {
        switch (id)
        {
            case WeaponId.SMG: return new WeaponSpec(id, "SM-40", 18, 32, 160, 256, 12, 75, 1.8f, 1.1f, .16f, .6f, true, 1200, 450);
            case WeaponId.Shotgun: return new WeaponSpec(id, "SG-8", 14, 8, 40, 80, 1.05f, 32, .55f, 4.2f, 2.8f, 2.3f, false, 1500, 500, 8, true);
            case WeaponId.Rifle: return new WeaponSpec(id, "AK-74", 20, 30, 120, 300, 8, 100, 2.1f, .85f, .08f, .9f, true, 1800, 600);
            default: return new WeaponSpec(WeaponId.Pistol, "PX-9", 24, 12, 60, 120, 4.5f, 65, 1.45f, .9f, .08f, 1.1f, false, 500, 250);
        }
    }

    public static WeaponId Legacy(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return WeaponId.Rifle;
        string value = name.Trim().ToUpperInvariant();
        if (value == "PX-9" || value == "PISTOL") return WeaponId.Pistol;
        if (value == "SM-40" || value == "SMG") return WeaponId.SMG;
        if (value == "SG-8" || value == "SHOTGUN") return WeaponId.Shotgun;
        return WeaponId.Rifle; // Every legacy run used the authored AK.
    }

    public static float DamageAt(WeaponId id, float distance)
        => id == WeaponId.Shotgun ? Math.Max(.25f, Math.Min(1f, 1f - Math.Max(0, distance - 8f) / 32f)) : 1f;
}
