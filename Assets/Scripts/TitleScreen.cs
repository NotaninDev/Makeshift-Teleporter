using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using TMPro;
using String = System.String;
using System;

public class TitleScreen : MonoBehaviour
{
    private TitleState titleState;
    private enum TitleState
    {
        MainMenu,
        LevelSelect,
        Settings,
        Controls,
        Resolution,
        Credits
    }

    private static GameObject creditsObject, notanBirdObject;
    private static SpriteBox notanBird;
    private static Option credits;

    private static GameObject[] optionObjects;
    private static Option[] options;
    private static Option levelOption;
    private static GameObject versionObject;
    private static Option version;
    private static int level;
    private static string[] levelNames;
    private static GameObject[] arrowObjects;
    private static SpriteBox[] arrowSprites;
    private const int buttonCount = 3, textCount = buttonCount + 2;
    private static int selectedOption, resolutionIndex;
    private static bool fullscreen;
    private const float OptionX = 4.89f, OptionYTop = 2.31f, OptionInterval = 1.19f;

    private static GameObject keyMappingObject;
    private static Keyboard keyMapping;

    private static GameObject logoObject;
    private static GameObject[] logoBlockObjects;
    private static SpriteBox logo;
    private static Block[] logoBlocks;

    private static UnityEvent buttonEvent;

    void Awake()
    {
        titleState = TitleState.MainMenu;
        creditsObject = General.AddChild(gameObject, "Credits");
        credits = creditsObject.AddComponent<Option>();
        notanBirdObject = General.AddChild(creditsObject, "Notan Bird");
        notanBird = notanBirdObject.AddComponent<SpriteBox>();
        optionObjects = new GameObject[textCount];
        options = new Option[buttonCount];
        for (int i = 0; i < buttonCount; i++)
        {
            optionObjects[i] = General.AddChild(gameObject, "Option" + i.ToString());
            options[i] = optionObjects[i].AddComponent<Option>();
        }
        optionObjects[buttonCount] = General.AddChild(gameObject, "Arrow Option");
        levelOption = optionObjects[buttonCount].AddComponent<Option>();
        versionObject = General.AddChild(gameObject, "Version");
        version = versionObject.AddComponent<Option>();
        level = GameManager.level;
        levelNames = new String[MainGame.LevelCount];
        for (int i = 0; i < levelNames.Length; i++)
        {
            levelNames[i] = MapData.GetLevelName(MapData.GetLevelTag(i));
        }
        arrowObjects = new GameObject[4];
        arrowSprites = new SpriteBox[4];
        for (int i = 0; i < 2; i++)
        {
            arrowObjects[i] = General.AddChild(optionObjects[buttonCount], "Arrow" + i);
            arrowSprites[i] = arrowObjects[i].AddComponent<SpriteBox>();

            arrowObjects[i + 2] = General.AddChild(optionObjects[0], "Arrow" + (i + 2));
            arrowSprites[i + 2] = arrowObjects[i + 2].AddComponent<SpriteBox>();
        }

        keyMappingObject = GameObject.FindWithTag("Menu Panel");
        if (keyMappingObject == null)
        {
            Debug.LogWarning("TitleScreen: Menu Panel not found.");
            keyMappingObject = General.AddChild(gameObject, "Key Mapping");
        }
        keyMapping = keyMappingObject.AddComponent<Keyboard>();

        logoObject = General.AddChild(gameObject, "Logo");
        logo = logoObject.AddComponent<SpriteBox>();

        logoBlockObjects = new GameObject[2];
        logoBlocks = new Block[2];
        for (int i = 0; i < 2; i++)
        {
            logoBlockObjects[i] = General.AddChild(logoObject, $"Logo Block {i}");
            logoBlocks[i] = logoBlockObjects[i].AddComponent<Block>();
        }
    }
    void Start()
    {
        creditsObject.transform.localPosition = new Vector3(OptionX, .29f, 0);
        notanBird.Initialize(Graphics.credits, "UI", 1, new Vector3(1.47f, -0.41f, 0));
        notanBird.spriteRenderer.color = Graphics.White;
        notanBirdObject.transform.localScale = Vector3.one * .4f;
        credits.Initialize("UI", 0, null, 1f, 1f, 1, $"<size=6f>Made by</size>{Environment.NewLine}<pos=3>Notan",
            Graphics.FontName.Mops, 7.2f, Graphics.White, new Vector2(.6f, .12f), false, alignment: TextAlignmentOptions.Left);
        creditsObject.SetActive(false);
        for (int i = 0; i < buttonCount; i++)
        {
            options[i].Initialize("UI", 0, Graphics.optionBox[2], Graphics.optionBox[0], 1.25f, 1f, 1, null, Graphics.FontName.Mops, 4.5f,
                Graphics.Blue, Graphics.Green, new Vector2(2.3f, .6f), true, alignment: TextAlignmentOptions.Center, useCollider: true);
        }
        levelOption.Initialize("UI", 0, Graphics.optionBox[2], Graphics.optionBox[0], 1.25f, 1f, 1, null, Graphics.FontName.Mops, 4.5f,
            Graphics.Blue, Graphics.Green, new Vector2(.6f, .12f), false, alignment: TextAlignmentOptions.Center, useCollider: true);
        version.Initialize("UI", 0, null, 1f, 1f, 1, "Made for Thinky Puzzle Game Jam 3", Graphics.FontName.Mops, 6f, Graphics.White, Vector2.zero, false);

        optionObjects[buttonCount].transform.localPosition = new Vector3(OptionX, 1.15f - OptionInterval, 0);
        optionObjects[buttonCount].SetActive(false);
        versionObject.transform.localPosition = new Vector3(3.62f, -4.06f, 0);
        for (int i = 0; i < 2; i++)
        {
            arrowObjects[i].transform.eulerAngles = new Vector3(0, 0, i == 0 ? 90 : -90);
            arrowObjects[i].transform.localScale = new Vector3(1f, 1f, 1);
            arrowSprites[i].Initialize(Graphics.arrow[0], "UI", 1, Vector3.zero, useCollider: true);

            arrowObjects[i + 2].transform.eulerAngles = new Vector3(0, 0, i == 0 ? 90 : -90);
            arrowObjects[i + 2].transform.localScale = new Vector3(1f, 1f, 1);
            arrowSprites[i + 2].Initialize(Graphics.arrow[0], "UI", 1, new Vector3(2.42f * (i * 2 - 1), 0, 0), useCollider: true);
            arrowObjects[i + 2].SetActive(false);
        }

        for (int i = 0; i < buttonCount; i++) optionObjects[i].transform.localPosition = new Vector3(OptionX, OptionYTop - OptionInterval * i, 0);

        options[0].ChangeText("Start");
        options[1].ChangeText("Select level");
        options[2].ChangeText("Credits");
        selectedOption = 0;
        options[0].SetSelected(true);
        for (int i = 1; i < buttonCount; i++) options[i].SetSelected(false);

        StartCoroutine(keyMapping.InitializeNonStatic());

        logo.Initialize(Graphics.logo, "Background", 1, new Vector3(-3.04f, 1.43f, 0));
        logoObject.transform.localScale = new Vector3(1.8f, 1.8f, 1);
        for (int i = 0; i < 2; i++)
        {
            logoBlocks[i].Initialize(true, true, true, true);
            logoBlockObjects[i].transform.localScale = Vector3.one * .43f;
        }
        logoBlockObjects[0].transform.localPosition = new Vector3(-1.92f, -.33f, 0);
        logoBlockObjects[1].transform.localPosition = new Vector3(1.67f, .41f, 0);
    }
    void Update()
    {
        if (SceneLoader.Loading) return;

        bool mouseDetected = false;
        switch (titleState)
        {
            case TitleState.MainMenu:
                for (int i = 0; i < buttonCount; i++)
                {
                    if (options[i].Mouse.GetMouseEnter())
                    {
                        mouseDetected = true;
                        options[selectedOption].SetSelected(false);
                        selectedOption = i;
                        options[selectedOption].SetSelected(true);
                        break;
                    }
                }
                if (mouseDetected) break;
                if (Keyboard.GetDown())
                {
                    options[selectedOption].SetSelected(false);
                    selectedOption++;
                    if (selectedOption >= buttonCount) selectedOption = 0;
                    options[selectedOption].SetSelected(true);
                }
                else if (Keyboard.GetUp())
                {
                    options[selectedOption].SetSelected(false);
                    selectedOption--;
                    if (selectedOption < 0) selectedOption = buttonCount - 1;
                    options[selectedOption].SetSelected(true);
                }
                else if (Keyboard.GetSelect() || options[selectedOption].Mouse.GetMouseClick())
                {
                    switch (selectedOption)
                    {
                        case 0:
                            if (GameManager.previousScene == "")
                            {
                                level = GetFirstLevel();
                                GameManager.level = level;
                            }
                            SceneLoader.sceneEvent.Invoke("MainScene");
                            break;
                        case 1:
                            options[1].ChangeText("Back");
                            optionObjects[1].transform.localPosition = new Vector3(OptionX, -.57f, 0);
                            optionObjects[buttonCount].transform.localPosition = new Vector3(OptionX, 1.15f, 0);
                            options[1].SetSelected(false);
                            for (int i = 0; i < buttonCount; i++)
                            {
                                if (i == 1) continue;
                                optionObjects[i].SetActive(false);
                            }
                            selectedOption = 0;
                            if (GameManager.previousScene == "") level = GetFirstLevel();
                            else level = GameManager.level;
                            if (level < 0 || level >= MainGame.LevelCount) level = 0;
                            levelOption.SetSelected(true);
                            SetLevelName(level);
                            optionObjects[buttonCount].SetActive(true);
                            titleState = TitleState.LevelSelect;
                            break;
                        case 2:
                            options[2].ChangeText("Back");
                            optionObjects[2].transform.localPosition = new Vector3(OptionX, -1.52f, 0);
                            for (int i = 0; i < buttonCount; i++)
                            {
                                if (i == 2) continue;
                                optionObjects[i].SetActive(false);
                            }
                            creditsObject.SetActive(true);
                            titleState = TitleState.Credits;
                            break;
                    }
                }
                break;
            case TitleState.LevelSelect:
                if (selectedOption == 0 && (Keyboard.GetRight() || arrowSprites[1].Mouse.GetMouseClick()) && level < MainGame.LevelCount - 1)
                {
                    level++;
                    SetLevelName(level);
                }
                else if (selectedOption == 0 && (Keyboard.GetLeft() || arrowSprites[0].Mouse.GetMouseClick()) && level > 0)
                {
                    level--;
                    SetLevelName(level);
                }
                else if (selectedOption == 0 && (Keyboard.GetDown() || Keyboard.GetUp()) || options[1].Mouse.GetMouseEnter())
                {
                    selectedOption = 1;
                    levelOption.SetSelected(false);
                    options[1].SetSelected(true);
                    arrowSprites[0].spriteRenderer.sprite = (level == 0 ? Graphics.arrow[3] : Graphics.arrow[2]);
                    arrowSprites[1].spriteRenderer.sprite = (level == MainGame.LevelCount - 1 ? Graphics.arrow[3] : Graphics.arrow[2]);
                }
                else if (selectedOption == 1 && (Keyboard.GetDown() || Keyboard.GetUp()) || levelOption.Mouse.GetMouseEnter())
                {
                    selectedOption = 0;
                    levelOption.SetSelected(true);
                    options[1].SetSelected(false);
                    arrowSprites[0].spriteRenderer.sprite = (level == 0 ? Graphics.arrow[1] : Graphics.arrow[0]);
                    arrowSprites[1].spriteRenderer.sprite = (level == MainGame.LevelCount - 1 ? Graphics.arrow[1] : Graphics.arrow[0]);
                }
                else if ((Keyboard.GetSelect() || options[1].Mouse.GetMouseClick()) && selectedOption == 1 || Keyboard.GetCancel())
                {
                    options[1].ChangeText("Select level");
                    options[1].SetSelected(true);
                    optionObjects[1].transform.localPosition = new Vector3(OptionX, OptionYTop - OptionInterval, 0);
                    for (int i = 0; i < buttonCount; i++) optionObjects[i].SetActive(true);
                    selectedOption = 1;
                    levelOption.SetSelected(false);
                    optionObjects[buttonCount].SetActive(false);
                    titleState = TitleState.MainMenu;
                }
                else if ((Keyboard.GetSelect() || levelOption.Mouse.GetMouseClick()) && selectedOption == 0)
                {
                    GameManager.level = level;
                    SceneLoader.sceneEvent.Invoke("MainScene");
                }
                break;
            case TitleState.Credits:
                if (Keyboard.GetSelect() || options[2].Mouse.GetMouseClick() || Keyboard.GetCancel())
                {
                    options[2].ChangeText("Credits");
                    optionObjects[2].transform.localPosition = new Vector3(OptionX, OptionYTop - OptionInterval * 2, 0);
                    for (int i = 0; i < buttonCount; i++) optionObjects[i].SetActive(true);
                    creditsObject.SetActive(false);
                    titleState = TitleState.MainMenu;
                }
                break;
            default:
                Debug.LogWarning(String.Format("Update: not implemented for State {0}", titleState.ToString()));
                break;
        }
    }

    private static void SetLevelName(int level)
    {
        levelOption.ChangeText($"{levelNames[level]}");
        for (int i = 0; i < 2; i++)
        {
            arrowObjects[i].transform.localPosition = new Vector3(.12f * (i * 2 - 1), -.70f, 0);
        }
        arrowSprites[0].spriteRenderer.sprite = (level == 0 ? Graphics.arrow[1] : Graphics.arrow[0]);
        arrowSprites[1].spriteRenderer.sprite = (level == MainGame.LevelCount - 1 ? Graphics.arrow[1] : Graphics.arrow[0]);
    }

    // return the most probable level the player would play first
    private static int GetFirstLevel()
    {
        int level = MapData.GetTagIndex(General.LastPlayedLevel);
        return level >= 0 ? level : 0;
    }
}
