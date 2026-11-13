using UnityEngine;

public readonly struct BreachSite
{
    public readonly int Id, RequiredZone;
    public readonly bool Vertical;
    public readonly float Constant, Along;
    public readonly Vector3 Inward;
    public BreachSite(int id, bool vertical, float constant, float along, int zone, Vector3 inward)
    { Id = id; Vertical = vertical; Constant = constant; Along = along; RequiredZone = zone; Inward = inward; }
    public Vector3 Position => new Vector3(Vertical ? Constant : Along, 0, Vertical ? Along : Constant);
}

/// <summary>Authored entrances and optional loops. No random prop placement.</summary>
public static class SurvivalMapLayout
{
    public const float BreachWidth = 4;
    public const int ShortcutCost = 650;
    public static readonly BreachSite[] Breaches = {
        new BreachSite(0,true,432,550,0,Vector3.right),
        new BreachSite(1,true,480,554,0,-Vector3.right),
        new BreachSite(2,false,540,440,0,Vector3.forward),
        new BreachSite(3,false,564,474,0,-Vector3.forward),
        new BreachSite(4,true,432,594,1,Vector3.right),
        new BreachSite(5,false,620,470,1,-Vector3.forward),
        new BreachSite(6,true,480,608,1,-Vector3.right),
        new BreachSite(7,true,400,532,2,Vector3.right),
        new BreachSite(8,false,494,426,2,Vector3.forward),
        new BreachSite(9,true,496,604,2,-Vector3.right),
        new BreachSite(10,false,636,476,2,-Vector3.forward)
    };
}
