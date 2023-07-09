using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Border : MonoBehaviour
{
    private static float virtualHeight, virtualWidth;
    private static GameObject[] borderObject;
    private static SpriteRenderer[] borderRenderer;

    void Awake()
    {
        // initialize the borders
        borderObject = new GameObject[4];
        borderRenderer = new SpriteRenderer[4];
        for (int i = 0; i < 4; i++)
        {
            borderObject[i] = General.AddChild(gameObject, "Boarder" + i.ToString());
            borderObject[i].layer = LayerMask.NameToLayer("UI");
            borderRenderer[i] = borderObject[i].AddComponent<SpriteRenderer>();
            borderRenderer[i].sortingLayerName = "Border";
            borderRenderer[i].sortingOrder = 30000;
        }
    }

    void Start()
    {
        if ((float)Screen.height / (float)Screen.width > Graphics.ScreenRatio)
        {
            virtualWidth = (float)Screen.width / Screen.height * Graphics.ScreenRatio * Graphics.Width;
            virtualHeight = (float)Screen.width / Screen.height * Graphics.ScreenRatio * Graphics.Height;
        }
        else
        {
            virtualHeight = Graphics.Height;
            virtualWidth = Graphics.Width;
        }

        float realHeight = Camera.main.orthographicSize * 2, realWidth = realHeight * Screen.width / Screen.height;
        float spriteWidth = Graphics.plainWhite.bounds.size.x;
        for (int i = 0; i < 4; i++)
        {
            borderRenderer[i].sprite = Graphics.plainWhite;
            borderRenderer[i].color = Graphics.BorderColor;
            borderObject[i].transform.localPosition = new Vector3(((i + 1) % 2 * (1 - i)) * (virtualWidth / 2 + (realWidth - virtualWidth) / 4),
                (i % 2 * (2 - i)) * (virtualHeight / 2 + (realHeight - virtualHeight) / 4), 0);
            if (i % 2 == 0) { borderObject[i].transform.localScale = new Vector3((realWidth - virtualWidth) / 2 / spriteWidth, realHeight / spriteWidth, 0); }
            else { borderObject[i].transform.localScale = new Vector3(realWidth / spriteWidth, (realHeight - virtualHeight) / 2 / spriteWidth, 0); }
        }
    }

    public static void Resize(int width, int height)
    {
        if ((float)height / width > Graphics.ScreenRatio)
        {
            virtualWidth = (float)width / height * Graphics.ScreenRatio * Graphics.Width;
            virtualHeight = (float)width / height * Graphics.ScreenRatio * Graphics.Height;
        }
        else
        {
            virtualHeight = Graphics.Height;
            virtualWidth = Graphics.Width;
        }

        float realHeight = Camera.main.orthographicSize * 2, realWidth = realHeight * width / height;
        float spriteWidth = Graphics.plainWhite.bounds.size.x;
        for (int i = 0; i < 4; i++)
        {
            borderObject[i].transform.localPosition = new Vector3(((i + 1) % 2 * (1 - i)) * (virtualWidth / 2 + (realWidth - virtualWidth) / 4),
                (i % 2 * (2 - i)) * (virtualHeight / 2 + (realHeight - virtualHeight) / 4), 0);
            if (i % 2 == 0) { borderObject[i].transform.localScale = new Vector3((realWidth - virtualWidth) / 2 / spriteWidth, realHeight / spriteWidth, 0); }
            else { borderObject[i].transform.localScale = new Vector3(realWidth / spriteWidth, (realHeight - virtualHeight) / 2 / spriteWidth, 0); }
        }
    }
}
