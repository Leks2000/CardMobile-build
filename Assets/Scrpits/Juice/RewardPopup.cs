using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Reusable modal popup foundation: title + body + OK button.
/// Usage: <c>RewardPopup.Show("Reward", "+40 coins", () => ...);</c>
/// Built from plain Images + TMP at runtime (no art needed). If a prefab exists at
/// Resources/Juice/RewardPopup (with a RewardPopup component and its fields assigned), it is used instead.
/// </summary>
public class RewardPopup : MonoBehaviour
{
    public CanvasGroup group;
    public RectTransform panel;
    public TMP_Text titleText;
    public TMP_Text bodyText;
    public Button okButton;

    private Action onClose;
    private bool closing;

    /// <summary>Opens a popup on its own overlay canvas. onClose is invoked after the close animation.</summary>
    public static RewardPopup Show(string title, string body, Action onClose = null)
    {
        RewardPopup popup;
        var prefab = Resources.Load<RewardPopup>("Juice/RewardPopup");
        if (prefab != null)
        {
            popup = Instantiate(prefab);
        }
        else
        {
            popup = Build();
        }
        popup.Open(title, body, onClose);
        return popup;
    }

    public void Open(string title, string body, Action onCloseCallback)
    {
        onClose = onCloseCallback;
        closing = false;
        if (titleText != null) titleText.text = title;
        if (bodyText != null) bodyText.text = body;
        if (okButton != null)
        {
            okButton.onClick.RemoveListener(Close);
            okButton.onClick.AddListener(Close);
        }
        gameObject.SetActive(true);
        StartCoroutine(Animate(true));
    }

    public void Close()
    {
        if (closing) return;
        closing = true;
        StartCoroutine(Animate(false));
    }

    private IEnumerator Animate(bool opening)
    {
        if (group != null) group.interactable = false;
        const float dur = 0.22f;
        for (float t = 0; t < dur; t += Time.unscaledDeltaTime)
        {
            float p = t / dur;
            float e = opening ? 1f + 2.70158f * Mathf.Pow(p - 1f, 3) + 1.70158f * Mathf.Pow(p - 1f, 2) : 1f - p * p;
            if (group != null) group.alpha = opening ? Mathf.Clamp01(p * 2f) : 1f - p;
            if (panel != null) panel.localScale = Vector3.one * Mathf.LerpUnclamped(opening ? 0.6f : 0.8f, 1f, e);
            yield return null;
        }
        if (group != null) group.alpha = opening ? 1f : 0f;
        if (panel != null) panel.localScale = Vector3.one;
        if (opening)
        {
            if (group != null) group.interactable = true;
            if (titleText != null) CombatFx.Punch(titleText.transform, 0.2f, 0.35f);
        }
        else
        {
            var cb = onClose;
            onClose = null;
            Destroy(gameObject);
            cb?.Invoke();
        }
    }

    private static RewardPopup Build()
    {
        var root = new GameObject("RewardPopup", typeof(RectTransform));
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();
        var popup = root.AddComponent<RewardPopup>();
        popup.group = root.AddComponent<CanvasGroup>();

        // dim background (blocks input behind the modal)
        var dim = NewRect("Dim", root.transform);
        Stretch(dim);
        dim.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.6f);

        popup.panel = NewRect("Panel", root.transform);
        popup.panel.sizeDelta = new Vector2(760, 440);
        popup.panel.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.09f, 0.16f, 0.97f);
        var outline = popup.panel.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.85f, 0.3f, 0.9f);
        outline.effectDistance = new Vector2(4, -4);

        popup.titleText = NewText("Title", popup.panel, 64, FontStyles.Bold, CombatFx.VictoryGold);
        var trt = popup.titleText.rectTransform;
        trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1); trt.pivot = new Vector2(0.5f, 1);
        trt.sizeDelta = new Vector2(-40, 110); trt.anchoredPosition = new Vector2(0, -20);

        popup.bodyText = NewText("Body", popup.panel, 40, FontStyles.Normal, Color.white);
        var brt = popup.bodyText.rectTransform;
        brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(1, 1);
        brt.offsetMin = new Vector2(40, 140); brt.offsetMax = new Vector2(-40, -140);

        var btnRt = NewRect("OK", popup.panel);
        btnRt.anchorMin = btnRt.anchorMax = new Vector2(0.5f, 0);
        btnRt.pivot = new Vector2(0.5f, 0);
        btnRt.sizeDelta = new Vector2(260, 96);
        btnRt.anchoredPosition = new Vector2(0, 30);
        var btnImg = btnRt.gameObject.AddComponent<Image>();
        btnImg.color = new Color(0.3f, 0.75f, 0.4f, 1f);
        popup.okButton = btnRt.gameObject.AddComponent<Button>();
        popup.okButton.targetGraphic = btnImg;
        btnRt.gameObject.AddComponent<ButtonPunch>();
        var okText = NewText("Text", btnRt, 48, FontStyles.Bold, Color.white);
        Stretch(okText.rectTransform);
        okText.text = "OK";

        return popup;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static TMP_Text NewText(string name, Transform parent, float size, FontStyles style, Color color)
    {
        var rt = NewRect(name, parent);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = CombatFx.Font;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }
}
