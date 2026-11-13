using UnityEngine;

/// <summary>Original hard-surface silhouettes for the new weapons; the authored AK is retained in first person.</summary>
public static class WeaponModels
{
    public static Material Material(Color color, float metal = .6f)
    {
        var shader = Shader.Find("Standard"); if (shader == null) return null;
        var material = new Material(shader); material.color = color;
        material.SetFloat("_Metallic", metal); material.SetFloat("_Glossiness", .32f); return material;
    }
    public static Transform Create(WeaponId id, Transform parent, Material steel, Material grip, Material accent)
    {
        var root = new GameObject(WeaponCatalog.Get(id).Name + " model").transform; root.SetParent(parent, false);
        if (id == WeaponId.Pistol)
        {
            Box(root, "Frame", new Vector3(0, -.018f, .03f), new Vector3(.047f, .04f, .22f), steel);
            Box(root, "Slide", new Vector3(0, .02f, .035f), new Vector3(.048f, .047f, .25f), steel);
            var handle = Box(root, "Grip", new Vector3(0, -.09f, -.055f), new Vector3(.045f, .115f, .065f), grip);
            handle.localRotation = Quaternion.Euler(-12, 0, 0);
            Box(root, "magazine", new Vector3(0, -.15f, -.06f), new Vector3(.049f, .014f, .067f), accent);
            Barrel(root, "Bore", new Vector3(0, .02f, .163f), .012f, .012f, grip);
            Guard(root, -.035f, .006f, steel);
            for (int i = 0; i < 7; i++) Box(root, "Slide serration", new Vector3(.025f, .02f, -.078f + .007f * i), new Vector3(.002f, .038f, .002f), grip);
            Box(root, "rearSight", new Vector3(0, .052f, -.07f), new Vector3(.03f, .012f, .012f), grip);
            Box(root, "Front sight", new Vector3(0, .05f, .135f), new Vector3(.008f, .012f, .012f), accent);
            Marker(root, "Muzzle", new Vector3(0, .02f, .18f));
        }
        else if (id == WeaponId.Shotgun)
        {
            Box(root, "Receiver", Vector3.zero, new Vector3(.065f, .075f, .24f), steel);
            Barrel(root, "Barrel", new Vector3(0, .022f, .35f), .015f, .5f, steel);
            Barrel(root, "Magazine tube", new Vector3(0, -.025f, .33f), .016f, .46f, steel);
            var pump = Box(root, "Pump", new Vector3(0, -.02f, .31f), new Vector3(.075f, .07f, .18f), grip);
            for (int i = 0; i < 8; i++) Box(pump, "Pump rib", new Vector3(0, -.003f, -.075f + i * .02f), new Vector3(.078f, .074f, .006f), accent);
            var stock = Box(root, "Stock", new Vector3(0, -.04f, -.25f), new Vector3(.058f, .1f, .27f), grip);
            stock.localRotation = Quaternion.Euler(-12, 0, 0);
            Box(root, "Butt pad", new Vector3(0, -.068f, -.385f), new Vector3(.065f, .12f, .027f), accent);
            Guard(root, -.058f, -.055f, steel);
            Box(root, "rearSight", new Vector3(0, .045f, -.06f), new Vector3(.03f, .012f, .015f), accent);
            Box(root, "Front bead", new Vector3(0, .045f, .58f), new Vector3(.007f, .01f, .01f), accent);
            Box(root, "Ejection port", new Vector3(.034f, .01f, .035f), new Vector3(.003f, .028f, .065f), accent);
            Marker(root, "Muzzle", new Vector3(0, .022f, .61f));
        }
        else
        {
            bool rifle = id == WeaponId.Rifle;
            Box(root, "Receiver", Vector3.zero, new Vector3(.065f, .075f, .28f), steel);
            Box(root, "Upper rail", new Vector3(0, .045f, .015f), new Vector3(.032f, .015f, .23f), accent);
            Barrel(root, "Barrel", new Vector3(0, .01f, rifle ? .35f : .25f), .012f, rifle ? .44f : .25f, steel);
            Box(root, "Handguard", new Vector3(0, -.005f, .19f), new Vector3(.075f, .07f, .15f), grip);
            var handle = Box(root, "Pistol grip", new Vector3(0, -.105f, -.065f), new Vector3(.05f, .13f, .065f), grip);
            handle.localRotation = Quaternion.Euler(-14, 0, 0);
            var magazine = Box(root, "magazine", new Vector3(0, -.13f, .065f), new Vector3(.05f, .19f, .065f), accent);
            magazine.localRotation = Quaternion.Euler(rifle ? 15 : 0, 0, 0);
            Box(root, "Stock rail", new Vector3(0, .005f, -.23f), new Vector3(.025f, .035f, .2f), steel);
            Box(root, "Shoulder stock", new Vector3(0, -.025f, -.34f), new Vector3(.055f, .11f, .06f), grip);
            for (int i = 0; i < 5; i++) Box(root, "Handguard vent", new Vector3(.039f, .005f, .14f + .025f * i), new Vector3(.003f, .026f, .012f), accent);
            Guard(root, -.06f, -.015f, steel);
            Box(root, "rearSight", new Vector3(0, .07f, -.08f), new Vector3(.035f, .028f, .017f), steel);
            Box(root, "Front sight", new Vector3(0, .064f, rifle ? .48f : .32f), new Vector3(.013f, .03f, .018f), accent);
            Marker(root, "Muzzle", new Vector3(0, .01f, rifle ? .58f : .39f));
        }
        return root;
    }
    private static Transform Box(Transform root, string name, Vector3 position, Vector3 scale, Material material)
        => Piece(root, name, PrimitiveType.Cube, position, scale, material);
    private static void Barrel(Transform root, string name, Vector3 point, float radius, float length, Material material)
    {
        var barrel = Piece(root, name, PrimitiveType.Cylinder, point, new Vector3(radius * 2, length * .5f, radius * 2), material);
        barrel.localRotation = Quaternion.Euler(90, 0, 0);
    }
    private static void Guard(Transform root, float height, float depth, Material material)
    {
        Box(root, "Trigger guard base", new Vector3(0, height - .025f, depth), new Vector3(.018f, .009f, .07f), material);
        Box(root, "Trigger guard front", new Vector3(0, height, depth + .035f), new Vector3(.018f, .06f, .009f), material);
        Box(root, "Trigger", new Vector3(0, height + .005f, depth), new Vector3(.009f, .03f, .01f), material);
    }
    public static Transform Piece(Transform root, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
    {
        var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(root, false);
        go.transform.localPosition = position; go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        var collider = go.GetComponent<Collider>(); collider.enabled = false; Object.Destroy(collider);
        return go.transform;
    }
    private static void Marker(Transform parent, string name, Vector3 position)
    { var marker = new GameObject(name).transform; marker.SetParent(parent, false); marker.localPosition = position; }
}
