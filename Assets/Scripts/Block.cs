using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block : MonoBehaviour
{
    private GameObject bubbleObject;
    private SpriteBox blockSprite, bubbleSprite;
    private GameObject[] frameObjects;
    private SpriteBox[] frameSprites;

    void Awake()
    {
        blockSprite = gameObject.AddComponent<SpriteBox>();
        bubbleObject = General.AddChild(gameObject, "Bubble");
        bubbleSprite = bubbleObject.AddComponent<SpriteBox>();
        frameObjects = new GameObject[4];
        frameSprites = new SpriteBox[4];
        for (int i = 0; i < 4; i++)
        {
            frameObjects[i] = General.AddChild(gameObject, $"Fram {i}");
            frameSprites[i] = frameObjects[i].AddComponent<SpriteBox>();
        }
    }

    public void Initialize(bool up, bool right, bool down, bool left)
    {
        blockSprite.Initialize(Graphics.tile[5], "Tile", 2, Vector3.zero);
        bubbleSprite.Initialize(Graphics.tile[10], "Tile", 3, Vector3.zero);
        blockSprite.spriteRenderer.color = Graphics.SemiTransparent;

        for (int i = 0; i < 4; i++) InitializeFrame(i);
        SetFrameActive(up, right, down, left);
    }
    private void InitializeFrame(int i)
    {
        frameSprites[i].Initialize(Graphics.tile[6 + i], "Tile", 4, Vector3.zero);
    }

    public void SetFrameActive(bool up, bool right, bool down, bool left)
    {
        frameObjects[0].SetActive(up);
        frameObjects[1].SetActive(right);
        frameObjects[2].SetActive(down);
        frameObjects[3].SetActive(left);
    }
    public void LerpColor(float t)
    {
        blockSprite.spriteRenderer.color = Color32.Lerp(Graphics.SemiTransparent, Graphics.Transparent, t);
        bubbleSprite.spriteRenderer.color = Color32.Lerp(Color.white, Graphics.Transparent, t);
        for (int i = 0; i < 4; i++)
        {
            frameSprites[i].spriteRenderer.color = Color32.Lerp(Color.white, Graphics.Transparent, t);
        }
    }
}
