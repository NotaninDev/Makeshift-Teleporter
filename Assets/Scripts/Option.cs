using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using String = System.String;

public class Option : MonoBehaviour
{
    [SerializeField]
    private bool selected;
    public bool Selected { get { return selected; } }

    private bool hasExtraSprite;
    public GameObject[] SpriteObjects;
    private GameObject[] textObjects;
    private SpriteRenderer[] optionRenderers;
    private TextMeshPro[] texts;
    private MeshRenderer[] textRenderers;
    private RectTransform[] textTransforms;
    private float defaultScale, expansionScale;
    private Vector2 margin, textAreaSize;
    public Vector2 Size { get { return optionRenderers[0].size; } }
    private bool fixedSize, useDefaultAreaSize;

    public bool UseCollider { get; private set; }
    private new BoxCollider2D collider;
    public MouseCollider Mouse { get; private set; }

    void Awake()
    {
        SpriteObjects = new GameObject[2];
        textObjects = new GameObject[2];
        optionRenderers = new SpriteRenderer[2];
        texts = new TextMeshPro[2];
        textRenderers = new MeshRenderer[2];
        textTransforms = new RectTransform[2];
        for (int i = 0; i < 2; i++)
        {
            SpriteObjects[i] = General.AddChild(gameObject, "Sprite" + i.ToString());
            textObjects[i] = General.AddChild(SpriteObjects[i], "Text" + i.ToString());
            optionRenderers[i] = SpriteObjects[i].AddComponent<SpriteRenderer>();
            texts[i] = textObjects[i].AddComponent<TextMeshPro>();
            textRenderers[i] = textObjects[i].GetComponent<MeshRenderer>();
            textTransforms[i] = textObjects[i].GetComponent<RectTransform>();
            if (textRenderers[i] == null)
            {
                Debug.LogWarning("Awake: MeshRenderer not found");
                return;
            }
            if (textTransforms[i] == null)
            {
                Debug.LogWarning("Awake: TextTransform not found");
                return;
            }
        }
        SpriteObjects[1].SetActive(false);
        collider = gameObject.AddComponent<BoxCollider2D>();
        Mouse = gameObject.AddComponent<MouseCollider>();
        Mouse.Initialize(collider);
    }

    // Initialize for options with no extra sprite
    public void Initialize(string sortingLayerName, int sortingOrder, Sprite sprite, float defaultScale, float expansionScale, int textSortingOrder, string text, Graphics.FontName font, float fontSize, Color32 color, Vector2 margin, bool fixedSize, float lineSpacing = 0, TextAlignmentOptions alignment = TextAlignmentOptions.Midline, Vector2 textAreaSize = new Vector2(),
        bool useCollider = false)
    {
        selected = false;
        hasExtraSprite = false;
        Destroy(SpriteObjects[1]);
        optionRenderers[0].sortingLayerName = sortingLayerName;
        optionRenderers[0].sortingOrder = sortingOrder;
        optionRenderers[0].sprite = sprite;
        optionRenderers[0].drawMode = SpriteDrawMode.Sliced;
        textRenderers[0].sortingLayerName = sortingLayerName;
        textRenderers[0].sortingOrder = textSortingOrder;
        texts[0].text = text == null ? "AzByCx" : text;
        texts[0].font = Graphics.fonts[(int)font];
        texts[0].fontSize = fontSize;
        texts[0].color = color;
        texts[0].lineSpacing = lineSpacing;
        texts[0].alignment = alignment;
        useDefaultAreaSize = (textAreaSize == new Vector2());
        this.textAreaSize = (useDefaultAreaSize ? new Vector2(20, 5) : textAreaSize);
        texts[0].ForceMeshUpdate();
        textTransforms[0].sizeDelta = useDefaultAreaSize ? new Vector2(texts[0].preferredWidth, texts[0].preferredHeight) : this.textAreaSize;
        texts[0].ForceMeshUpdate();
        SpriteObjects[0].transform.localScale = new Vector3(defaultScale, defaultScale, 1);
        this.defaultScale = defaultScale;
        this.expansionScale = expansionScale;

        this.margin = margin;
        this.fixedSize = fixedSize;
        optionRenderers[0].size = fixedSize ? margin : new Vector2(texts[0].preferredWidth + margin.x, texts[0].preferredHeight + margin.y);
        UseCollider = useCollider;
        collider.enabled = UseCollider;
        Mouse.enabled = UseCollider;
        if (UseCollider) collider.size = optionRenderers[0].size;
    }
    // Initialize for options with an extra sprite
    public void Initialize(string sortingLayerName, int sortingOrder, Sprite sprite, Sprite extraSprite, float defaultScale, float expansionScale, int textSortingOrder, string text, Graphics.FontName font, float fontSize, Color32 color, Color32 extraColor, Vector2 margin, bool fixedSize, float lineSpacing = 0, TextAlignmentOptions alignment = TextAlignmentOptions.Midline, Vector2 textAreaSize = new Vector2(), bool useCollider = false)
    {
        selected = false;
        hasExtraSprite = true;
        useDefaultAreaSize = (textAreaSize == new Vector2());
        this.textAreaSize = (useDefaultAreaSize ? new Vector2(20, 5) : textAreaSize);
        for (int i = 0; i < 2; i++)
        {
            optionRenderers[i].sortingLayerName = sortingLayerName;
            optionRenderers[i].sortingOrder = sortingOrder;
            optionRenderers[i].drawMode = SpriteDrawMode.Sliced;
            textRenderers[i].sortingLayerName = sortingLayerName;
            textRenderers[i].sortingOrder = textSortingOrder;
            texts[i].text = text == null ? "AzByCx" : text;
            texts[i].font = Graphics.fonts[(int)font];
            texts[i].fontSize = fontSize;
            texts[i].lineSpacing = lineSpacing;
            texts[i].alignment = alignment;
            texts[i].ForceMeshUpdate();
            textTransforms[i].sizeDelta = useDefaultAreaSize ? new Vector2(texts[i].preferredWidth, texts[i].preferredHeight) : this.textAreaSize;
            texts[i].ForceMeshUpdate();
        }
        optionRenderers[0].sprite = sprite;
        optionRenderers[1].sprite = extraSprite;
        SpriteObjects[0].transform.localScale = new Vector3(defaultScale, defaultScale, 1);
        SpriteObjects[1].transform.localScale = new Vector3(defaultScale * expansionScale, defaultScale * expansionScale, 1);
        texts[0].color = color;
        texts[1].color = extraColor;

        this.margin = margin;
        this.fixedSize = fixedSize;
        for (int i = 0; i < 2; i++)
        {
            optionRenderers[i].size = fixedSize ? margin : new Vector2(texts[i].preferredWidth + margin.x, texts[i].preferredHeight + margin.y);
        }
        UseCollider = useCollider;
        collider.enabled = UseCollider;
        Mouse.enabled = UseCollider;
        if (UseCollider) collider.size = optionRenderers[0].size;
    }

    public void ChangeText(string optionText)
    {
        Vector2 textBoxSize;
        texts[0].text = optionText;
        textTransforms[0].sizeDelta = textAreaSize;
        texts[0].ForceMeshUpdate();
        textBoxSize = new Vector2(texts[0].preferredWidth, texts[0].preferredHeight);
        if (useDefaultAreaSize) textTransforms[0].sizeDelta = textBoxSize;
        if (!fixedSize) optionRenderers[0].size = textBoxSize + margin;
        texts[0].ForceMeshUpdate();
        if (hasExtraSprite)
        {
            texts[1].text = optionText;
            textTransforms[1].sizeDelta = textAreaSize;
            texts[1].ForceMeshUpdate();
            textBoxSize = new Vector2(texts[1].preferredWidth, texts[1].preferredHeight);
            if (useDefaultAreaSize) textTransforms[1].sizeDelta = textBoxSize;
            if (!fixedSize) optionRenderers[1].size = textBoxSize + margin;
            texts[1].ForceMeshUpdate();
        }
        if (UseCollider) collider.size = optionRenderers[0].size;
    }
    public void ChangeTextBoxSize(Vector2 margin, bool fixedSize)
    {
        this.margin = margin;
        this.fixedSize = fixedSize;
        optionRenderers[0].size = fixedSize ? margin : new Vector2(texts[0].preferredWidth + margin.x, texts[0].preferredHeight + margin.y);
        if (hasExtraSprite)
        {
            optionRenderers[1].size = fixedSize ? margin : new Vector2(texts[1].preferredWidth + margin.x, texts[1].preferredHeight + margin.y);
        }
    }
    public void ChangeFontSize(float fontSize)
    {
        Vector2 textBoxSize;
        texts[0].fontSize = fontSize;
        textTransforms[0].sizeDelta = textAreaSize;
        texts[0].ForceMeshUpdate();
        textBoxSize = new Vector2(texts[0].preferredWidth, texts[0].preferredHeight);
        if (useDefaultAreaSize) textTransforms[0].sizeDelta = textBoxSize;
        if (!fixedSize) optionRenderers[0].size = textBoxSize + margin;
        texts[0].ForceMeshUpdate();
        if (hasExtraSprite)
        {
            texts[1].fontSize = fontSize;
            textTransforms[1].sizeDelta = textAreaSize;
            texts[1].ForceMeshUpdate();
            textBoxSize = new Vector2(texts[1].preferredWidth, texts[1].preferredHeight);
            if (useDefaultAreaSize) textTransforms[1].sizeDelta = textBoxSize;
            if (!fixedSize) optionRenderers[1].size = textBoxSize + margin;
            texts[1].ForceMeshUpdate();
        }
        if (UseCollider) collider.size = optionRenderers[0].size;
    }
    public void ChangeColor(Color32 color)
    {
        texts[0].color = color;
    }
    public void ChangeSortingOrder(int spriteOrder, int textOrder)
    {
        optionRenderers[0].sortingOrder = spriteOrder;
        textRenderers[0].sortingOrder = textOrder;
        if (hasExtraSprite)
        {
            optionRenderers[1].sortingOrder = spriteOrder;
            textRenderers[1].sortingOrder = textOrder;
        }
    }

    public void SetSelected(bool selected)
    {
        if (this.selected == selected) return;
        this.selected = selected;
        if (hasExtraSprite)
        {
            SpriteObjects[0].SetActive(!selected);
            SpriteObjects[1].SetActive(selected);
        }
        else
        {
            SpriteObjects[0].transform.localScale = selected ? new Vector3(defaultScale * expansionScale, defaultScale * expansionScale, 1) :
                new Vector3(defaultScale, defaultScale, 1);
        }
    }
}
