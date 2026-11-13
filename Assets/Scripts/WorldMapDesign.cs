using TMPro;
using UnityEngine;

/// <summary>Supplies and wayfinding have fixed roles along the authored church/village routes.</summary>
public sealed class WorldMapDesign : MonoBehaviour
{
    private Material signMaterial, postMaterial;
    public void Build(WorldProgression world)
    {
        Offer(world, WeaponId.SMG, 0, new Vector3(468, 18, 550), false);
        Offer(world, WeaponId.Shotgun, 1, new Vector3(449, 18, 577), false);
        Offer(world, WeaponId.Rifle, 2, new Vector3(464, 18, 534), false);
        Offer(world, WeaponId.Rifle, 2, new Vector3(471, 18, 528), true);
        signMaterial = WeaponModels.Material(new Color(.13f, .21f, .19f), .15f);
        postMaterial = WeaponModels.Material(new Color(.24f, .2f, .14f), .05f);
        Sign(world, new Vector3(450, 18, 554), "IGREJA · NORTE\nVILA · SUL", "PÁTIO DE CONTENÇÃO");
        Sign(world, new Vector3(460, 18, 583), "ATALHOS · ESTE / OESTE\nSAÍDA NORTE · VILA", "PÁTIO DA IGREJA");
        Sign(world, new Vector3(456, 18, 530), "ENERGIA · POSTO DE GUARDA\nAK-74 / CAIXA · ARMAZÉM", "VILA");
    }
    private static void Offer(WorldProgression world, WeaponId id, int access, Vector3 preferred, bool mystery)
    {
        var station = world.Place(mystery ? "Mystery supply chest" : "Weapon rack " + id,
            mystery ? WorldInteraction.InteractionKind.MysteryBox : WorldInteraction.InteractionKind.WallWeapon,
            mystery ? "CAIXA MISTERIOSA" : WeaponCatalog.Get(id).Name, preferred + world.ZoneOffset, null, false);
        if (station == null) return;
        station.ConfigureOffer(id, access); station.gameObject.AddComponent<WeaponRack>().Build(station, mystery);
    }
    private void Sign(WorldProgression world, Vector3 desired, string directions, string district)
    {
        if (!world.TryReachableGround(desired + world.ZoneOffset, out Vector3 ground)) return;
        var root = new GameObject(district + " wayfinding").transform; root.SetParent(transform, false); root.position = ground;
        Vector3 facing = world.SafeSpawn - ground; facing.y = 0; if (facing.sqrMagnitude > .01f) root.rotation = Quaternion.LookRotation(facing);
        WeaponModels.Piece(root, "Sign post", PrimitiveType.Cube, Vector3.up * 1.15f, new Vector3(.09f, 2.3f, .09f), postMaterial);
        WeaponModels.Piece(root, "Route board", PrimitiveType.Cube, new Vector3(0, 2.1f, 0), new Vector3(2.3f, .85f, .1f), signMaterial);
        var label = new GameObject("Directions", typeof(RectTransform), typeof(TextMeshPro)); label.transform.SetParent(root, false);
        label.transform.localPosition = new Vector3(0, 2.1f, .06f); label.transform.localRotation = Quaternion.Euler(0, 180, 0);
        var text = label.GetComponent<TextMeshPro>(); text.font = TMP_Settings.defaultFontAsset; text.fontSize = 2;
        text.text = district + "\n<size=65%>" + directions + "</size>"; text.color = new Color(.9f, .87f, .72f);
        text.alignment = TextAlignmentOptions.Center; text.rectTransform.sizeDelta = new Vector2(2.15f, .8f);
    }
    private void OnDestroy() { if (signMaterial != null) Destroy(signMaterial); if (postMaterial != null) Destroy(postMaterial); }
}
