using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TMPUI : MonoBehaviour
{
    public bool Valid { get; private set; }

    // Rect is RectTransform of the image, not the text
    public RectTransform Rect { get; private set; }
    public Image Image { get; private set; }
    public TextMeshProUGUI Text { get; private set; }

    private bool fixedSize;
    private Vector2 imageSize;

    private Color32 color;
    private Sprite sprite;

    void Awake()
    {
        Rect = gameObject.GetComponent<RectTransform>();
        if (Rect == null)
        {
            Debug.LogWarning($"TMPUI.Awake: Rect Transform not found");
            Valid = false;
            return;
        }
        Image = gameObject.GetComponent<Image>();
        if (Image == null)
        {
            Debug.LogWarning($"TMPUI.Awake: Image not found");
            Valid = false;
            return;
        }
        Transform child = transform.Find("Text");
        if (child == null)
        {
            Debug.LogWarning($"TMPUI.Awake: child \"Text\" not found");
            Valid = false;
            return;
        }
        Text = child.GetComponent<TextMeshProUGUI>();
        if (Text == null)
        {
            Debug.LogWarning($"TMPUI.Awake: TextMeshProUGUI not found");
            Valid = false;
            return;
        }

        Valid = true;
    }

    // if fixedSize is false, buttonSize is treated as a buffer size
    public void Initialize(string text, Color32 color, Sprite sprite, Vector2 buttonAnchor, Graphics.FontName font = Graphics.FontName.Mops, float fontSize = 48f, float lineSpacing = 0, bool fixedSize = true, Vector2 imageSize = new Vector2())
    {
        if (!Valid)
        {
            Debug.LogWarning($"TMPUI.Initialize: this button is invalid");
            return;
        }
        this.color = color;
        this.sprite = sprite;

        Text.autoSizeTextContainer = true;
        Text.font = Graphics.fonts[(int)font];
        Text.color = color;
        Text.lineSpacing = lineSpacing;
        Text.alignment = TextAlignmentOptions.Midline;
        ChangeText(text);

        this.fixedSize = fixedSize;
        this.imageSize = imageSize;
        Image.sprite = sprite;
        Rect.anchorMax = Rect.anchorMin = buttonAnchor;
        if (imageSize == new Vector2()) imageSize = new Vector2(300, 60);
        Rect.sizeDelta = fixedSize ? imageSize : imageSize + new Vector2(Text.preferredWidth, Text.preferredHeight);

        Image.enabled = sprite != null;
    }

    public void ChangeText(string text)
    {
        if (!Valid)
        {
            Debug.LogWarning($"TMPUI.ChangeText: this button is invalid");
            return;
        }
        Text.text = text;
        Vector2 textAreaSize = new Vector2(Text.preferredWidth, Text.preferredHeight);
        Text.rectTransform.sizeDelta = textAreaSize;
        Text.ForceMeshUpdate();
        textAreaSize = new Vector2(Text.preferredWidth, Text.preferredHeight);
        Text.rectTransform.sizeDelta = textAreaSize;

        if (!fixedSize) Rect.sizeDelta = imageSize + textAreaSize;
    }
}
