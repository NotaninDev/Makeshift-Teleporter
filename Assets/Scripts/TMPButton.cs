using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class TMPButton : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public bool Valid { get; private set; }

    // Rect is RectTransform of the button, not the text
    public RectTransform Rect { get; private set; }
    public Image Image { get; private set; }
    public Button Button { get; private set; }
    public TextMeshProUGUI Text { get; private set; }

    private bool fixedSize;
    private Vector2 buttonSize;

    // 2 colors/sprites
    private Color32[] colors;
    private Sprite[] sprites;

    private UnityEvent<int> focusEvent, clickEvent;
    private int buttonID;

    void Awake()
    {
        Rect = gameObject.GetComponent<RectTransform>();
        if (Rect == null)
        {
            Debug.LogWarning($"TMPButton.Awake: Rect Transform not found");
            Valid = false;
            return;
        }
        Image = gameObject.GetComponent<Image>();
        if (Image == null)
        {
            Debug.LogWarning($"TMPButton.Awake: Image not found");
            Valid = false;
            return;
        }
        Button = gameObject.GetComponent<Button>();
        if (Button == null)
        {
            Debug.LogWarning($"TMPButton.Awake: Button not found");
            Valid = false;
            return;
        }
        Transform child = transform.Find("Text");
        if (child == null)
        {
            Debug.LogWarning($"TMPButton.Awake: child \"Text\" not found");
            Valid = false;
            return;
        }
        Text = child.GetComponent<TextMeshProUGUI>();
        if (Text == null)
        {
            Debug.LogWarning($"TMPButton.Awake: TextMeshProUGUI not found");
            Valid = false;
            return;
        }

        Valid = true;
    }

    // if fixedSize is false, buttonSize is treated as a buffer size
    public void Initialize(string text, Color32 color, Color32 colorFocus, Sprite sprite, Sprite spriteFocus, Vector2 buttonAnchor, UnityEvent<int> focusEvent, UnityEvent<int> clickEvent, int buttonID, Graphics.FontName font = Graphics.FontName.Mops, float fontSize = 48f, float lineSpacing = 0, bool fixedSize = true, Vector2 buttonSize = new Vector2())
    {
        if (!Valid)
        {
            Debug.LogWarning($"TMPButton.Initialize: this button is invalid");
            return;
        }
        colors = new Color32[2];
        sprites = new Sprite[2];
        colors[0] = color; colors[1] = colorFocus;
        sprites[0] = sprite; sprites[1] = spriteFocus;

        Text.autoSizeTextContainer = true;
        Text.font = Graphics.fonts[(int)font];
        Text.color = color;
        Text.lineSpacing = lineSpacing;
        Text.alignment = TextAlignmentOptions.Midline;
        ChangeText(text);

        this.fixedSize = fixedSize;
        this.buttonSize = buttonSize;
        Image.sprite = sprite;
        Rect.anchorMax = Rect.anchorMin = buttonAnchor;
        if (buttonSize == new Vector2()) buttonSize = new Vector2(300, 60);
        Rect.sizeDelta = fixedSize ? buttonSize : buttonSize + new Vector2(Text.preferredWidth, Text.preferredHeight);

        this.focusEvent = focusEvent;
        this.clickEvent = clickEvent;
        this.buttonID = buttonID;
    }

    public void Focus(bool focused)
    {
        if (!Valid)
        {
            Debug.LogWarning($"TMPButton.Focus: this button is invalid");
            return;
        }
        Image.sprite = focused ? sprites[1] : sprites[0];
        Text.color = focused ? colors[1] : colors[0];
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (focusEvent != null) focusEvent.Invoke(buttonID);
    }

    public void OnPointerClick(PointerEventData pointerEventData)
    {
        if (pointerEventData.button == PointerEventData.InputButton.Left && clickEvent != null) clickEvent.Invoke(buttonID);
    }

    public void ChangeText(string text)
    {
        if (!Valid)
        {
            Debug.LogWarning($"TMPButton.ChangeText: this button is invalid");
            return;
        }
        Text.text = text;
        Vector2 textAreaSize = new Vector2(Text.preferredWidth, Text.preferredHeight);
        Text.rectTransform.sizeDelta = textAreaSize;
        Text.ForceMeshUpdate();
        textAreaSize = new Vector2(Text.preferredWidth, Text.preferredHeight);
        Text.rectTransform.sizeDelta = textAreaSize;

        if (!fixedSize) Rect.sizeDelta = buttonSize + textAreaSize;
    }
}
