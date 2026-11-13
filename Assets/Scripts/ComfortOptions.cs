using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Adds the same independent comfort setting to either existing options panel.</summary>
public static class ComfortOptions
{
    private const string ObjectName = "ComfortMotionOption";
    private const string PreferenceKey = "opt_motion";

    public static void Install(Toggle fullscreenReference)
    {
        if (fullscreenReference == null) return;
        var reference = fullscreenReference.GetComponent<RectTransform>();
        if (reference == null || reference.parent == null) return;
        Transform parent = reference.parent;
        if (parent.Find(ObjectName) != null) return;

        // Both options layouts use a 20px fullscreen toggle at scale 2 and a 50px
        // label. A 40px row at y=-122 leaves a clear gap above Back (top=-190).
        float referenceHeight = Mathf.Max(50f, reference.rect.height * Mathf.Abs(reference.localScale.y));
        var rowObject = new GameObject(ObjectName, typeof(RectTransform), typeof(Image), typeof(Toggle));
        rowObject.layer = fullscreenReference.gameObject.layer;
        var row = rowObject.GetComponent<RectTransform>();
        row.SetParent(parent, false);
        row.anchorMin = reference.anchorMin;
        row.anchorMax = reference.anchorMin;
        row.pivot = new Vector2(0f, 0.5f);
        row.localScale = Vector3.one;
        row.anchoredPosition = new Vector2(reference.anchoredPosition.x - 265f,
            reference.anchoredPosition.y - referenceHeight - 24f);
        row.sizeDelta = new Vector2(560f, 40f);

        // The transparent row is a full-width click target, including its label.
        var hitArea = rowObject.GetComponent<Image>();
        hitArea.color = new Color(0f, 0f, 0f, 0f);
        hitArea.raycastTarget = true;

        var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.layer = rowObject.layer;
        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(row, false);
        labelRect.anchorMin = labelRect.anchorMax = new Vector2(0f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = new Vector2(250f, 40f);
        var label = labelObject.GetComponent<TextMeshProUGUI>();
        var existingText = fullscreenReference.GetComponentInChildren<TMP_Text>(true);
        if (existingText == null) existingText = parent.GetComponentInChildren<TMP_Text>(true);
        if (existingText != null && existingText.font != null) label.font = existingText.font;
        label.text = "Movimento reduzido da arma";
        label.color = new Color(0.95f, 0.95f, 0.95f, 1f);
        label.fontSize = 22f;
        label.enableAutoSizing = true;
        label.fontSizeMin = 18f;
        label.fontSizeMax = 22f;
        label.alignment = TextAlignmentOptions.Left;
        label.raycastTarget = false;

        Image checkbox = CreateSquare("Checkbox", row, new Vector2(265f, 0f), 40f);
        Image checkmark = CreateSquare("Checkmark", checkbox.rectTransform, new Vector2(8f, 0f), 24f);
        checkmark.color = new Color(0.94f, 0.74f, 0.39f, 1f);
        checkmark.raycastTarget = false;

        var toggle = rowObject.GetComponent<Toggle>();
        toggle.targetGraphic = checkbox;
        toggle.graphic = checkmark;
        var colors = toggle.colors;
        colors.normalColor = new Color(0.22f, 0.24f, 0.26f, 1f);
        colors.highlightedColor = new Color(0.38f, 0.40f, 0.43f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.15f, 0.17f, 0.19f, 1f);
        colors.disabledColor = new Color(0.12f, 0.13f, 0.14f, 0.6f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        toggle.colors = colors;
        checkbox.color = colors.normalColor;
        toggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(PreferenceKey, 1) == 0);
        toggle.onValueChanged.AddListener(reducedMotion =>
        {
            PlayerPrefs.SetInt(PreferenceKey, reducedMotion ? 0 : 1);
            PlayerPrefs.Save();
        });
    }

    private static Image CreateSquare(string name, RectTransform parent, Vector2 position, float size)
    {
        var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        gameObject.layer = parent.gameObject.layer;
        var rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(size, size);
        return gameObject.GetComponent<Image>();
    }
}
