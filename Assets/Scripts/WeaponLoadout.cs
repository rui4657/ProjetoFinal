using System;
using System.Globalization;

public sealed class WeaponSlot
{
    public WeaponId Id { get; private set; }
    public int Magazine, Reserve;
    public WeaponSlot(WeaponId id, int magazine, int reserve)
    {
        Id = id; var spec = WeaponCatalog.Get(id);
        Magazine = Math.Max(0, Math.Min(spec.Magazine, magazine));
        Reserve = Math.Max(0, Math.Min(spec.ReserveLimit, reserve));
    }
    public bool IsFull => Magazine >= WeaponCatalog.Get(Id).Magazine && Reserve >= WeaponCatalog.Get(Id).ReserveLimit;
    public void Fill() { var spec = WeaponCatalog.Get(Id); Magazine = spec.Magazine; Reserve = spec.ReserveLimit; }
}

/// <summary>Two slots, no duplicated ammunition on switches. Versioned bounded recovery format.</summary>
public sealed class WeaponLoadout
{
    private readonly WeaponSlot[] slots = new WeaponSlot[2];
    public int Active { get; private set; }
    public WeaponSlot Current => slots[Active];
    public WeaponSlot Get(int index) => index >= 0 && index < 2 ? slots[index] : null;
    public WeaponLoadout() { var s = WeaponCatalog.Get(WeaponId.Pistol); slots[0] = new WeaponSlot(s.Id, s.Magazine, s.StartingReserve); }
    public int IndexOf(WeaponId id) { for (int i = 0; i < 2; i++) if (slots[i] != null && slots[i].Id == id) return i; return -1; }
    public bool Switch(int index) { if (Get(index) == null || index == Active) return false; Active = index; return true; }
    public void Acquire(WeaponId id)
    {
        if (!WeaponCatalog.IsValid(id)) return;
        int existing = IndexOf(id);
        if (existing >= 0) { slots[existing].Fill(); Active = existing; return; }
        int target = slots[0] == null ? 0 : slots[1] == null ? 1 : Active;
        var spec = WeaponCatalog.Get(id); slots[target] = new WeaponSlot(id, spec.Magazine, spec.StartingReserve); Active = target;
    }
    public void FillAll() { foreach (var slot in slots) if (slot != null) slot.Fill(); }
    public string Encode()
    {
        string result = "v1;" + Active;
        foreach (var slot in slots) result += ";" + (slot == null ? "-" : (int)slot.Id + "," + slot.Magazine + "," + slot.Reserve);
        return result;
    }
    public static bool TryDecode(string text, out WeaponLoadout loadout)
    {
        loadout = null;
        if (string.IsNullOrEmpty(text) || text.Length > 160) return false;
        string[] parts = text.Split(';');
        if (parts.Length != 4 || parts[0] != "v1" || !Number(parts[1], out int active) || active < 0 || active > 1) return false;
        var parsed = new WeaponLoadout(); parsed.slots[0] = parsed.slots[1] = null;
        for (int i = 0; i < 2; i++)
        {
            if (parts[i + 2] == "-") continue;
            string[] values = parts[i + 2].Split(',');
            if (values.Length != 3 || !Number(values[0], out int id) || !WeaponCatalog.IsValid((WeaponId)id)
                || !Number(values[1], out int mag) || !Number(values[2], out int reserve)) return false;
            if (parsed.IndexOf((WeaponId)id) >= 0) return false;
            parsed.slots[i] = new WeaponSlot((WeaponId)id, mag, reserve);
        }
        if (parsed.slots[active] == null) return false;
        parsed.Active = active; loadout = parsed; return true;
    }
    private static bool Number(string s, out int value) => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    public static WeaponLoadout FromLegacy(string name, int magazine, int reserve)
    {
        var parsed = new WeaponLoadout(); parsed.slots[0] = new WeaponSlot(WeaponCatalog.Legacy(name), magazine, reserve); return parsed;
    }
}
