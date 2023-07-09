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
    private const int buttonCount = 5, textCount = buttonCount + 2;
    private static int selectedOption, resolutionIndex;
    private static bool fullscreen;
    private const float OptionX = 4.89f, OptionYTop = 2.31f, OptionInterval = 1.19f;

    private static GameObject keyMappingObject;
    private static Keyboard keyMapping;

    private static GameObject[] checkBoxObjects;
    private static SpriteBox[] checkBoxSprites;
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

        checkBoxObjects = new GameObject[4];
        checkBoxSprites = new SpriteBox[4];
        checkBoxObjects[2] = General.AddChild(optionObjects[1], "Fullscreen Checkbox");
        checkBoxObjects[3] = General.AddChild(checkBoxObjects[2], "Check Mark");
        for (int i = 2; i < 4; i++) checkBoxSprites[i] = checkBoxObjects[i].AddComponent<SpriteBox>();
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
        options[2].ChangeText("Settings");
        options[3].ChangeText("Credits");
        options[4].ChangeText("Quit");
        selectedOption = 0;
        options[0].SetSelected(true);
        for (int i = 1; i < buttonCount; i++) options[i].SetSelected(false);

        StartCoroutine(keyMapping.InitializeNonStatic());

        for (int i = 2; i < 4; i++)
        {
            checkBoxSprites[i].Initialize(Graphics.checkbox[i], "UI", i % 2 + 1, Vector3.zero);
        }
        checkBoxObjects[2].transform.localScale = new Vector3(.88f, .88f, 1);
        checkBoxObjects[3].transform.localScale = new Vector3(.71f, .71f, 1);
        checkBoxObjects[2].transform.localPosition = new Vector3(1.97f, 0, 0);
        checkBoxObjects[2].SetActive(false);
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

        bool mouseDetected = false, cancelSelected = false;
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
                            options[0].ChangeText("Resolution");
                            options[1].ChangeText("Controls");
                            options[2].ChangeText("Back");
                            options[2].SetSelected(false);
                            for (int i = 0; i < buttonCount; i++)
                            {
                                if (i <= 2) continue;
                                optionObjects[i].SetActive(false);
                            }
                            selectedOption = 0;
                            options[selectedOption].SetSelected(true);
                            titleState = TitleState.Settings;
                            break;
                        case 3:
                            options[3].ChangeText("Back");
                            optionObjects[3].transform.localPosition = new Vector3(OptionX, -1.52f, 0);
                            for (int i = 0; i < buttonCount; i++)
                            {
                                if (i == 3) continue;
                                optionObjects[i].SetActive(false);
                            }
                            creditsObject.SetActive(true);
                            titleState = TitleState.Credits;
                            break;
                        case 4:
#if UNITY_EDITOR
                                EditorApplication.isPlaying = false;
#else
                            Application.Quit();
#endif
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
            case TitleState.Settings:
                for (int i = 0; i < 3; i++)
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
                    if (selectedOption >= 3) selectedOption = 0;
                    options[selectedOption].SetSelected(true);
                }
                else if (Keyboard.GetUp())
                {
                    options[selectedOption].SetSelected(false);
                    selectedOption--;
                    if (selectedOption < 0) selectedOption = 2;
                    options[selectedOption].SetSelected(true);
                }
                else if (Keyboard.GetSelect() || options[selectedOption].Mouse.GetMouseClick())
                {
                    switch (selectedOption)
                    {
                        case 0:
                            // initialize the resolution option
                            resolutionIndex = -1;
                            Resolution[] resolutions = Screen.resolutions;
                            for (int i = 0; i < resolutions.Length; i++)
                            {
                                if (Screen.currentResolution.width == resolutions[i].width &&
                                    Screen.currentResolution.height == resolutions[i].height &&
                                    Screen.currentResolution.refreshRateRatio.value == resolutions[i].refreshRateRatio.value)
                                {
                                    resolutionIndex = i;
                                    break;
                                }
                            }
                            if (resolutionIndex < 0) resolutionIndex = 0;
                            arrowObjects[2].SetActive(true);
                            arrowObjects[3].SetActive(true);
                            options[0].ChangeTextBoxSize(new Vector2(3.6f, .6f), true);
                            SetResolutionText(resolutionIndex);

                            // initialize the fullscreen option
                            fullscreen = Screen.fullScreen;
                            checkBoxObjects[2].SetActive(true);
                            checkBoxObjects[3].SetActive(fullscreen);
                            checkBoxSprites[2].spriteRenderer.sprite = Graphics.checkbox[2];
                            checkBoxSprites[3].spriteRenderer.sprite = Graphics.checkbox[3];

                            options[1].ChangeText("Fullscreen");
                            options[2].ChangeText("Apply");
                            options[3].ChangeText("Back");
                            optionObjects[3].SetActive(true);
                            options[selectedOption].SetSelected(false);
                            selectedOption = 0;
                            options[selectedOption].SetSelected(true);
                            titleState = TitleState.Resolution;
                            break;
                        case 1:
                            options[selectedOption].SetSelected(false);
                            for (int i = 0; i < 3; i++) optionObjects[i].SetActive(false);
                            keyMappingObject.SetActive(true);
                            keyMapping.ResetPosition();
                            logoObject.SetActive(false);
                            versionObject.SetActive(false);
                            titleState = TitleState.Controls;
                            break;
                        case 2:
                            cancelSelected = true;
                            break;
                        default:
                            Debug.LogWarning($"TitleScreen.Update: not implemented for option {selectedOption}");
                            break;
                    }
                }
                if (Keyboard.GetCancel() || cancelSelected)
                {
                    options[0].ChangeText("Start");
                    options[1].ChangeText("Select level");
                    options[2].ChangeText("Settings");
                    options[selectedOption].SetSelected(false);
                    for (int i = 0; i < buttonCount; i++)
                    {
                        optionObjects[i].SetActive(true);
                    }
                    selectedOption = 0;
                    options[selectedOption].SetSelected(true);
                    titleState = TitleState.MainMenu;
                }
                break;
            case TitleState.Resolution:
                for (int i = 0; i < 4; i++)
                {
                    if (options[i].Mouse.GetMouseEnter())
                    {
                        mouseDetected = true;
                        switch (selectedOption)
                        {
                            case 0:
                                arrowSprites[2].spriteRenderer.sprite = Graphics.arrow[resolutionIndex == 0 ? 3 : 2];
                                arrowSprites[3].spriteRenderer.sprite = Graphics.arrow[resolutionIndex == (Screen.resolutions.Length - 1) ? 3 : 2];
                                break;
                            case 1:
                                checkBoxSprites[2].spriteRenderer.sprite = Graphics.checkbox[2];
                                checkBoxSprites[3].spriteRenderer.sprite = Graphics.checkbox[3];
                                break;
                        }
                        options[selectedOption].SetSelected(false);
                        selectedOption = i;
                        options[selectedOption].SetSelected(true);
                        switch (selectedOption)
                        {
                            case 0:
                                arrowSprites[2].spriteRenderer.sprite = Graphics.arrow[resolutionIndex == 0 ? 1 : 0];
                                arrowSprites[3].spriteRenderer.sprite = Graphics.arrow[resolutionIndex == (Screen.resolutions.Length - 1) ? 1 : 0];
                                break;
                            case 1:
                                checkBoxSprites[2].spriteRenderer.sprite = Graphics.checkbox[0];
                                checkBoxSprites[3].spriteRenderer.sprite = Graphics.checkbox[1];
                                break;
                        }
                        break;
                    }
                }
                if (mouseDetected) break;
                if (selectedOption == 0 && (Keyboard.GetRight() || arrowSprites[3].Mouse.GetMouseClick()) &&
                    resolutionIndex < Screen.resolutions.Length - 1)
                {
                    resolutionIndex++;
                    SetResolutionText(resolutionIndex);
                }
                else if (selectedOption == 0 && (Keyboard.GetLeft() || arrowSprites[2].Mouse.GetMouseClick()) && resolutionIndex > 0)
                {
                    resolutionIndex--;
                    SetResolutionText(resolutionIndex);
                }
                else if (Keyboard.GetDown())
                {
                    switch (selectedOption)
                    {
                        case 0:
                            arrowSprites[2].spriteRenderer.sprite = Graphics.arrow[resolutionIndex == 0 ? 3 : 2];
                            arrowSprites[3].spriteRenderer.sprite = Graphics.arrow[resolutionIndex == (Screen.resolutions.Length - 1) ? 3 : 2];
                            checkBoxSprites[2].spriteRenderer.sprite = Graphics.checkbox[0];
                            checkBoxSprites[3].spriteRenderer.sprite = Graphics.checkbox[1];
                            break;
                        case 1:
                            checkBoxSprites[2].spriteRenderer.sprite = Graphics.checkbox[2];
                            checkBoxSprites[3].spriteRenderer.sprite = Graphics.checkbox[3];
                            break;
                        case 3:
                            arrowSprites[2].spriteRenderer.sprite = Graphics.arrow[resolutionIndex == 0 ? 1 : 0];
                            arrowSprites[3].spriteRenderer.sprite = Graphics.arrow[resolutionIndex == (Screen.resolutions.Length - 1) ? 1 : 0];
                            break;
                    }
                    options[selectedOption].SetSelected(false);
                    selectedOption++;
                    if (selectedOption >= 4) selectedOption = 0;
                    options[selectedOption].SetSelected(true);
                }
                else if (Keyboard.GetUp())
                {
                    switch (selectedOption)
                    {
                        case 0:
                            arrowSprites[2].spriteRenderer.sprite = Graphics.arrow[resolutionIndex == 0 ? 3 : 2];
                            arrowSprites[3].spriteRenderer.sprite = Graphics.arrow[resolutionIndex == (Screen.resolutions.Length - 1) ? 3 : 2];
                            break;
                        case 1:
                            arrowSprites[2].spriteRenderer.sprite = Graphics.arrow[resolutionIndex == 0 ? 1 : 0];
                            arrowSprites[3].spriteRenderer.sprite = Graphics.arrow[resolutionIndex == (Screen.resolutions.Length - 1) ? 1 : 0];
                            checkBoxSprites[2].spriteRenderer.sprite = Graphics.checkbox[2];
                            checkBoxSprites[3].spriteRenderer.sprite = Graphics.checkbox[3];
                            break;
                        case 2:
                            checkBoxSprites[2].spriteRenderer.sprite = Graphics.checkbox[0];
                            checkBoxSprites[3].spriteRenderer.sprite = Graphics.checkbox[1];
                            break;
                    }
                    options[selectedOption].SetSelected(false);
                    selectedOption--;
                    if (selectedOption < 0) selectedOption = 3;
                    options[selectedOption].SetSelected(true);
                }
                else if (Keyboard.GetSelect() || options[selectedOption].Mouse.GetMouseClick())
                {
                    switch (selectedOption)
                    {
                        case 0:
                            // resolution
                            break;
                        case 1:
                            fullscreen = !fullscreen;
                            checkBoxObjects[3].SetActive(fullscreen);
                            break;
                        case 2:
                            Graphics.SetResolution(Screen.resolutions[resolutionIndex].width,
                                Screen.resolutions[resolutionIndex].height, fullscreen, Screen.resolutions[resolutionIndex].refreshRateRatio);
                            break;
                        case 3:
                            cancelSelected = true;
                            break;
                        default:
                            Debug.LogWarning($"TitleScreen.Update: not implemented for option {selectedOption}");
                            break;
                    }
                }
                if (Keyboard.GetCancel() || cancelSelected)
                {
                    options[0].ChangeText("Resolution");
                    options[0].ChangeTextBoxSize(new Vector2(2.3f, .6f), true);
                    arrowObjects[2].SetActive(false);
                    arrowObjects[3].SetActive(false);
                    checkBoxObjects[2].SetActive(false);
                    options[1].ChangeText("Controls");
                    options[2].ChangeText("Back");
                    options[3].ChangeText("Credits");
                    options[selectedOption].SetSelected(false);
                    optionObjects[3].SetActive(false);
                    selectedOption = 0;
                    options[selectedOption].SetSelected(true);
                    titleState = TitleState.Settings;
                }
                break;
            case TitleState.Controls:
                if (keyMapping.HandleInput())
                {
                    keyMappingObject.SetActive(false);
                    for (int i = 0; i < 3; i++) optionObjects[i].SetActive(true);
                    selectedOption = 0;
                    options[selectedOption].SetSelected(true);
                    logoObject.SetActive(true);
                    versionObject.SetActive(true);
                    titleState = TitleState.Settings;
                }
                break;
            case TitleState.Credits:
                if (Keyboard.GetSelect() || options[3].Mouse.GetMouseClick() || Keyboard.GetCancel())
                {
                    options[3].ChangeText("Credits");
                    optionObjects[3].transform.localPosition = new Vector3(OptionX, OptionYTop - OptionInterval * 3, 0);
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
    private static void SetResolutionText(int index)
    {
        Resolution[] resolutions = Screen.resolutions;
        options[0].ChangeText($"{resolutions[index].width} x {resolutions[index].height} " +
            $"@ {resolutions[index].refreshRateRatio.value} Hz");
        arrowSprites[2].spriteRenderer.sprite = Graphics.arrow[index == 0 ? 1 : 0];
        arrowSprites[3].spriteRenderer.sprite = Graphics.arrow[index == (resolutions.Length - 1) ? 1 : 0];
    }

    // return the most probable level the player would play first
    private static int GetFirstLevel()
    {
        int level = MapData.GetTagIndex(General.LastPlayedLevel);
        return level >= 0 ? level : 0;
    }
}
