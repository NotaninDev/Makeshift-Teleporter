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

        if (up) InitializeFrame(0);
        else Destroy(frameObjects[0]);
        if (right) InitializeFrame(1);
        else Destroy(frameObjects[1]);
        if (down) InitializeFrame(2);
        else Destroy(frameObjects[2]);
        if (left) InitializeFrame(3);
        else Destroy(frameObjects[3]);
    }
    private void InitializeFrame(int i)
    {
        frameSprites[i].Initialize(Graphics.tile[6 + i], "Tile", 4, Vector3.zero);
    }
}
