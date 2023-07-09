using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using String = System.String;

public class Menu : MonoBehaviour
{
    private GameObject shadowObject;
    private enum MenuState { Menu }
    private MenuState state;

    private const int optionNumber = 2;
    private GameObject[] buttonObjects;
    private TMPButton[] buttons;
    private int focus;
    private bool inputHandled;

    private GameObject keyMappingObject;
    private Keyboard keyMapping;

    // options: resume, key, main menu

    void Awake()
    {
        buttonObjects = new GameObject[optionNumber];
        buttons = new TMPButton[optionNumber];
        for (int i = 0; i < optionNumber; i++)
        {
            buttonObjects[i] = General.InstantiateChild(Graphics.ButtonPrefab, gameObject, "Option" + i);
            buttons[i] = buttonObjects[i].GetComponent<TMPButton>();
            if (buttons[i] == null)
            {
                Debug.LogError($"Menu.Awake: TMPButton component not found");
                for (int j = i; j >= 0; j--) Destroy(buttonObjects[j]);
                return;
            }
        }
        keyMappingObject = General.AddChild(gameObject, "Key Mapping");
        RectTransform keyRect = keyMappingObject.AddComponent<RectTransform>();
        keyRect.anchorMin = Vector2.zero;
        keyRect.anchorMax = Vector2.one;
        keyRect.sizeDelta = Vector2.zero;
        keyMapping = keyMappingObject.AddComponent<Keyboard>();
    }
    public void Initialize(GameObject shadow)
    {
        UnityEvent<int> focusEvent = new UnityEvent<int>(), clickEvent = new UnityEvent<int>();
        focusEvent.AddListener(FocusButton);
        clickEvent.AddListener(ClickButton);

        for (int i = 0; i < optionNumber; i++)
        {
            buttons[i].Initialize(null, Graphics.Blue, Graphics.Green, Graphics.optionBox[2], Graphics.optionBox[0], new Vector2(.5f, .75f - .12f * i), focusEvent, clickEvent, i);
        }
        buttons[0].ChangeText("Resume");
        buttons[1].ChangeText("Return to menu");

        shadowObject = shadow;
        state = MenuState.Menu;
        focus = 0;
        buttons[focus].Focus(true);
        inputHandled = false;
        StartCoroutine(keyMapping.InitializeNonStatic());
    }

    void LateUpdate()
    {
        inputHandled = false;
    }

    public void ResetSelection()
    {
        focus = 0;
        for (int i = 0; i < optionNumber; i++)
        {
            buttons[i].Focus(i == focus);
        }
        inputHandled = false;
    }

    private void FocusButton(int buttonID)
    {
        if (!inputHandled)
        {
            buttons[focus].Focus(false);
            focus = buttonID;
            buttons[focus].Focus(true);
        }
    }

    private void ClickButton(int buttonID)
    {
        if (!inputHandled && focus == buttonID)
        {
            DoMenuAction();
            inputHandled = true;
        }
    }

    // return true if there was an input
    // the output is used for handling an edge case when the menu is closed
    // to prevent closing it doesn't trigger any action in the gameplay
    public bool HandleInput()
    {
        if (inputHandled) return true;

        switch (state)
        {
            case MenuState.Menu:
                if (Keyboard.GetDown())
                {
                    buttons[focus].Focus(false);
                    focus++;
                    if (focus >= optionNumber) focus = 0;
                    buttons[focus].Focus(true);
                    return true;
                }
                else if (Keyboard.GetUp())
                {
                    buttons[focus].Focus(false);
                    focus--;
                    if (focus < 0) focus = optionNumber - 1;
                    buttons[focus].Focus(true);
                    return true;
                }
                else if (Keyboard.GetSelect())
                {
                    DoMenuAction();
                    return true;
                }
                else if (Keyboard.GetCancel())
                {
                    shadowObject.SetActive(false);
                    return true;
                }
                return false;
            default:
                Debug.LogWarning($"Menu.HandleInput: not implemented for type {Enum.GetName(typeof(MenuState), state)}");
                state = MenuState.Menu;
                return false;
        }
    }

    private void DoMenuAction()
    {
        switch (focus)
        {
            case 0:
                shadowObject.SetActive(false);
                return;
            case 1:
                shadowObject.SetActive(false);
                SceneLoader.sceneEvent.Invoke("TitleScene");
                return;
        }
    }
}
