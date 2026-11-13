using UnityEngine;

public static class WorldArt
{
    // Called while the parent uses identity rotation. A separate fitting parent preserves mesh axes.
    public static void Fit(GameObject prefab, Transform parent, Vector3 size)
    {
        var fitting = new GameObject("Art fitting").transform; fitting.SetParent(parent, false);
        var art = Object.Instantiate(prefab, fitting); art.transform.localPosition = Vector3.zero;
        art.transform.localRotation = Quaternion.identity; art.transform.localScale = Vector3.one;
        foreach (var collider in art.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        var renderers = art.GetComponentsInChildren<Renderer>(true); if (renderers.Length == 0) return;
        Bounds bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        if (bounds.size.z > bounds.size.x && bounds.size.z > bounds.size.y) art.transform.localRotation = Quaternion.Euler(0, 90, 0);
        else if (bounds.size.y > bounds.size.x) art.transform.localRotation = Quaternion.Euler(0, 0, 90);
        bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        fitting.localScale = new Vector3(size.x / Mathf.Max(.001f, bounds.size.x), size.y / Mathf.Max(.001f, bounds.size.y), size.z / Mathf.Max(.001f, bounds.size.z));
        bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        fitting.position -= bounds.center - parent.position;
    }
}
