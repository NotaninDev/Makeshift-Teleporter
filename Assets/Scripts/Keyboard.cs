using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using String = System.String;

public class Keyboard : MonoBehaviour
{
    private static KeyCode[] defaultKeys;
    private static KeyCode[,] assinedKeys;

    private const int KeyNumber = 13;
    private GameObject[] keyTextObjects, keyObjects;
    private TMPUI[] keyTexts;
    private TMPButton[] keys;
    private int focus;
    private bool inputHandled, closeMenu;
    private bool waitingInput;

    private static readonly string keyMappingPath = Application.persistentDataPath + "/controls.txt";
    [Serializable]
    public class Preference
    {
        public KeyCode KeySelect, KeyMenu, KeyUp, KeyLeft, KeyDown, KeyRight,
            KeyUndo, KeyReset, KeyPlayerUp, KeyPlayerLeft, KeyPlayerDown, KeyPlayerRight, KeyCamera;
        public Preference()
        {
            if (defaultKeys != null && defaultKeys.Length >= KeyNumber)
            {
                KeySelect = defaultKeys[0];
                KeyMenu = defaultKeys[1];
                KeyUp = defaultKeys[2];
                KeyLeft = defaultKeys[3];
                KeyDown = defaultKeys[4];
                KeyRight = defaultKeys[5];
                KeyUndo = defaultKeys[6];
                KeyReset = defaultKeys[7];
                KeyPlayerUp = defaultKeys[8];
                KeyPlayerLeft = defaultKeys[9];
                KeyPlayerDown = defaultKeys[10];
                KeyPlayerRight = defaultKeys[11];
                KeyCamera = defaultKeys[12];
            }
            else
            {
                if (defaultKeys == null) Debug.LogWarning(String.Format("Preference: defaultKeys is not initialized"));
                else Debug.LogWarning(String.Format("Preference: the length of defaultKeys ({0}) is shorter than {1}", defaultKeys.Length, KeyNumber));
            }
        }
    }

    void Awake()
    {
        keyTextObjects = new GameObject[KeyNumber + 2];
        keyObjects = new GameObject[KeyNumber + 2];
        keyTexts = new TMPUI[KeyNumber + 2];
        keys = new TMPButton[KeyNumber + 2];
    }

    public IEnumerator InitializeNonStatic()
    {
        float start = Time.time, timeout = .5f;
        while (Graphics.TextUIPrefab == null || Graphics.ButtonPrefab == null)
        {
            if (Time.time - start > timeout)
            {
                Debug.LogError($"Keyboard.InitializeNonStatic: UI prefab was not loaded in {timeout} seconds");
                yield break;
            }
            yield return new WaitForSeconds(0);
        }

        for (int i = 0; i < KeyNumber + 2; i++)
        {
            keyTextObjects[i] = General.InstantiateChild(Graphics.TextUIPrefab, gameObject, i < KeyNumber ? GetControlName(i) : null);
            keyTexts[i] = keyTextObjects[i].GetComponent<TMPUI>();
            if (keyTexts[i] == null)
            {
                Debug.LogError($"Keyboard.Awake: TMPUI component not found");
                for (int j = i; j >= 0; j--)
                {
                    Destroy(keyTextObjects[j]);
                    Destroy(keyObjects[j]);
                }
                yield break;
            }
            keyObjects[i] = General.InstantiateChild(Graphics.ButtonPrefab, gameObject, i < KeyNumber ? $"Button {GetControlName(i)}" : null);
            keys[i] = keyObjects[i].GetComponent<TMPButton>();
            if (keys[i] == null)
            {
                Debug.LogError($"Keyboard.Awake: TMButton component not found");
                for (int j = i; j >= 0; j--)
                {
                    Destroy(keyTextObjects[j]);
                    Destroy(keyObjects[j]);
                }
                yield break;
            }
        }

        keyTextObjects[KeyNumber].name = "Text Menu";
        keyTextObjects[KeyNumber + 1].name = "Text Gameplay";
        keyObjects[KeyNumber].name = "Control Back";
        keyObjects[KeyNumber + 1].name = "Control Default";

        float topY = .82f, intervalY = .1f, pullX = .04f;
        UnityEvent<int> focusEvent = new UnityEvent<int>(), clickEvent = new UnityEvent<int>();
        focusEvent.AddListener(FocusButton);
        clickEvent.AddListener(ClickButton);
        for (int i = 0; i < 6; i++)
        {
            keyTexts[i].Initialize(GetControlName(i), Graphics.White, null, new Vector2(.235f + pullX, topY - intervalY * i), fixedSize: false, imageSize: new Vector2(10, 0));
            keyTexts[i].Rect.pivot = new Vector2(1f, .5f);
            keys[i].Initialize(GetKeyName(i), Graphics.Blue, Graphics.Green, Graphics.optionBox[2], Graphics.optionBox[0], new Vector2(.34f + pullX, topY - intervalY * i), focusEvent, clickEvent, i, buttonSize: new Vector2(240, 60));
        }
        for (int i = 6; i < KeyNumber; i++)
        {
            keyTexts[i].Initialize(GetControlName(i), Graphics.White, null, new Vector2(.735f - pullX, topY - intervalY * (i - 6)), fixedSize: false, imageSize: new Vector2(10, 0));
            keyTexts[i].Rect.pivot = new Vector2(1f, .5f);
            keys[i].Initialize(GetKeyName(i), Graphics.Blue, Graphics.Green, Graphics.optionBox[2], Graphics.optionBox[0], new Vector2(.84f - pullX, topY - intervalY * (i - 6)), focusEvent, clickEvent, i, buttonSize: new Vector2(240, 60));
        }

        keyTexts[KeyNumber].Initialize("<b>Menu</b>", Graphics.White, null, new Vector2(.25f + pullX, topY + intervalY));
        keyTexts[KeyNumber + 1].Initialize("<b>Gameplay</b>", Graphics.White, null, new Vector2(.75f - pullX, topY + intervalY));
        keys[KeyNumber].Initialize("Back", Graphics.Blue, Graphics.Green, Graphics.optionBox[2], Graphics.optionBox[0], new Vector2(.25f + pullX, topY - intervalY * 7), focusEvent, clickEvent, KeyNumber);
        keys[KeyNumber + 1].Initialize("Default", Graphics.Blue, Graphics.Green, Graphics.optionBox[2], Graphics.optionBox[0], new Vector2(.75f - pullX, topY - intervalY * 7.2f), focusEvent, clickEvent, KeyNumber + 1);

        gameObject.SetActive(false);
    }
    public static void Initialize()
    {
        defaultKeys = new KeyCode[KeyNumber];
        assinedKeys = new KeyCode[KeyNumber, 2];
        defaultKeys[0] = KeyCode.Space;
        defaultKeys[1] = KeyCode.Escape;
        defaultKeys[2] = KeyCode.UpArrow;
        defaultKeys[3] = KeyCode.LeftArrow;
        defaultKeys[4] = KeyCode.DownArrow;
        defaultKeys[5] = KeyCode.RightArrow;
        defaultKeys[6] = KeyCode.Z;
        defaultKeys[7] = KeyCode.R;
        defaultKeys[8] = KeyCode.UpArrow;
        defaultKeys[9] = KeyCode.LeftArrow;
        defaultKeys[10] = KeyCode.DownArrow;
        defaultKeys[11] = KeyCode.RightArrow;
        defaultKeys[12] = KeyCode.LeftControl;
        for (int i = 0; i < KeyNumber; i++) assinedKeys[i, 0] = defaultKeys[i];

        // load key mapping
        if (File.Exists(keyMappingPath))
        {
            try
            {
                string encodedPreference = File.ReadAllText(keyMappingPath);
                Preference preference = new Preference();
                JsonUtility.FromJsonOverwrite(encodedPreference, preference);
                assinedKeys[0, 0] = preference.KeySelect;
                assinedKeys[1, 0] = preference.KeyMenu;
                assinedKeys[2, 0] = preference.KeyUp;
                assinedKeys[3, 0] = preference.KeyLeft;
                assinedKeys[4, 0] = preference.KeyDown;
                assinedKeys[5, 0] = preference.KeyRight;
                assinedKeys[6, 0] = preference.KeyUndo;
                assinedKeys[7, 0] = preference.KeyReset;
                assinedKeys[8, 0] = preference.KeyPlayerUp;
                assinedKeys[9, 0] = preference.KeyPlayerLeft;
                assinedKeys[10, 0] = preference.KeyPlayerDown;
                assinedKeys[11, 0] = preference.KeyPlayerRight;
                assinedKeys[12, 0] = preference.KeyCamera;
            }
            catch (Exception e)
            {
                Debug.LogError(String.Format("Initialize: error occured while loading key mapping: {0}", e));
            }
        }

        for (int i = 0; i < KeyNumber; i++) assinedKeys[i, 1] = GetPairKey(assinedKeys[i, 0]);
    }
    public void ResetPosition()
    {
        focus = 0;
        inputHandled = closeMenu = false;
        for (int i = 0; i < KeyNumber + 2; i++) keys[i].Focus(i == focus);
        waitingInput = false;
    }

    void LateUpdate()
    {
        inputHandled = closeMenu = false;
    }

    private void FocusButton(int buttonID)
    {
        if (!inputHandled && !waitingInput)
        {
            keys[focus].Focus(false);
            focus = buttonID;
            keys[focus].Focus(true);
        }
    }
    private void ClickButton(int buttonID)
    {
        if (!inputHandled && focus == buttonID && !waitingInput)
        {
            closeMenu = SelectButton();
            inputHandled = true;
        }
    }

    // returns true when the control menu is closed
    public bool HandleInput()
    {
        if (inputHandled) return closeMenu;

        if (waitingInput)
        {
            WaitKeyAssign();
            return false;
        }
        else
        {
            if (GetSelect())
            {
                return SelectButton();
            }
            if (GetCancel())
            {
                SaveKeyMapping();
                return true;
            }
            if (GetDown())
            {
                switch (focus)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 6:
                    case 7:
                    case 8:
                    case 9:
                    case 10:
                    case 11:
                        keys[focus].Focus(false);
                        focus++;
                        keys[focus].Focus(true);
                        return false;
                    case 5:
                        keys[focus].Focus(false);
                        focus = 13;
                        keys[focus].Focus(true);
                        return false;
                    case 12:
                        keys[focus].Focus(false);
                        focus = 14;
                        keys[focus].Focus(true);
                        return false;
                    case 13:
                        keys[focus].Focus(false);
                        focus = 0;
                        keys[focus].Focus(true);
                        return false;
                    case 14:
                        keys[focus].Focus(false);
                        focus = 6;
                        keys[focus].Focus(true);
                        return false;
                    default:
                        Debug.LogWarning(String.Format("HandleInput-down: not implemented for option {0}", focus));
                        break;
                }
            }
            if (GetUp())
            {
                switch (focus)
                {
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                    case 7:
                    case 8:
                    case 9:
                    case 10:
                    case 11:
                    case 12:
                        keys[focus].Focus(false);
                        focus--;
                        keys[focus].Focus(true);
                        return false;
                    case 0:
                        keys[focus].Focus(false);
                        focus = 13;
                        keys[focus].Focus(true);
                        return false;
                    case 6:
                        keys[focus].Focus(false);
                        focus = 14;
                        keys[focus].Focus(true);
                        return false;
                    case 13:
                        keys[focus].Focus(false);
                        focus = 5;
                        keys[focus].Focus(true);
                        return false;
                    case 14:
                        keys[focus].Focus(false);
                        focus = 12;
                        keys[focus].Focus(true);
                        return false;
                    default:
                        Debug.LogWarning(String.Format("HandleInput-up: not implemented for option {0}", focus));
                        break;
                }
            }
            if (GetRight() || GetLeft())
            {
                switch (focus)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                        keys[focus].Focus(false);
                        focus += 6;
                        keys[focus].Focus(true);
                        return false;
                    case 6:
                    case 7:
                    case 8:
                    case 9:
                    case 10:
                    case 11:
                        keys[focus].Focus(false);
                        focus -= 6;
                        keys[focus].Focus(true);
                        return false;
                    case 12:
                        keys[focus].Focus(false);
                        focus = 5;
                        keys[focus].Focus(true);
                        return false;
                    case 13:
                        keys[focus].Focus(false);
                        focus = 14;
                        keys[focus].Focus(true);
                        return false;
                    case 14:
                        keys[focus].Focus(false);
                        focus = 13;
                        keys[focus].Focus(true);
                        return false;
                    default:
                        Debug.LogWarning(String.Format("HandleInput-right/left: not implemented for option {0}", focus));
                        break;
                }
            }
        }
        return false;
    }
    private void WaitKeyAssign()
    {
        KeyCode inputKey = CheckKeyInput();
        if (inputKey != KeyCode.None)
        {
            switch (focus)
            {
                case 0:
                case 1:
                    for (int i = 0; i < 6; i++)
                    {
                        if (i == focus) continue;
                        if (assinedKeys[i, 0] == inputKey || assinedKeys[i, 1] == inputKey)
                        {
                            AssignKey(i, assinedKeys[focus, 0]);
                            break;
                        }
                    }
                    for (int i = 6; i < KeyNumber; i++)
                    {
                        if (assinedKeys[i, 0] == inputKey || assinedKeys[i, 1] == inputKey)
                        {
                            AssignKey(i, assinedKeys[focus, 0]);
                            break;
                        }
                    }
                    AssignKey(focus, inputKey);
                    waitingInput = false;
                    break;
                case 2:
                case 3:
                case 4:
                case 5:
                    for (int i = 0; i < 6; i++)
                    {
                        if (i == focus) continue;
                        if (assinedKeys[i, 0] == inputKey || assinedKeys[i, 1] == inputKey)
                        {
                            if (i == 0 || i == 1)
                            {
                                for (int j = 6; j < KeyNumber; j++)
                                {
                                    if (assinedKeys[j, 0] == assinedKeys[focus, 0] || assinedKeys[j, 1] == assinedKeys[focus, 0])
                                    {
                                        AssignKey(j, inputKey);
                                        break;
                                    }
                                }
                            }
                            AssignKey(i, assinedKeys[focus, 0]);
                            break;
                        }
                    }
                    AssignKey(focus, inputKey);
                    waitingInput = false;
                    break;
                case 6:
                case 7:
                case 8:
                case 9:
                case 10:
                case 11:
                case 12:
                    for (int i = 0; i < 2; i++)
                    {
                        if (assinedKeys[i, 0] == inputKey || assinedKeys[i, 1] == inputKey)
                        {
                            for (int j = 2; j < 6; j++)
                            {
                                if (assinedKeys[j, 0] == assinedKeys[focus, 0] || assinedKeys[j, 1] == assinedKeys[focus, 0])
                                {
                                    AssignKey(j, inputKey);
                                    break;
                                }
                            }
                            AssignKey(i, assinedKeys[focus, 0]);
                            break;
                        }
                    }
                    for (int i = 6; i < KeyNumber; i++)
                    {
                        if (i == focus) continue;
                        if (assinedKeys[i, 0] == inputKey || assinedKeys[i, 1] == inputKey)
                        {
                            AssignKey(i, assinedKeys[focus, 0]);
                            break;
                        }
                    }
                    AssignKey(focus, inputKey);
                    waitingInput = false;
                    break;
                default:
                    Debug.LogWarning($"Keyboard.WaitKeyAssign: not implemented for option {focus}");
                    waitingInput = false;
                    break;
            }
        }
    }
    private bool SelectButton()
    {
        switch (focus)
        {
            case 0:
            case 1:
            case 2:
            case 3:
            case 4:
            case 5:
            case 6:
            case 7:
            case 8:
            case 9:
            case 10:
            case 11:
            case 12:
                keys[focus].ChangeText("<color=#402b18>...</color>");
                waitingInput = true;
                return false;
            case 13:
                SaveKeyMapping();
                return true;
            case 14:
                for (int i = 0; i < KeyNumber; i++) AssignKey(i, defaultKeys[i]);
                return false;
            default:
                Debug.LogWarning($"Keyboard.SelectButton: not implemented for option {focus}");
                return false;
        }
    }
    private static void SaveKeyMapping()
    {
        Preference preference = new Preference();
        string encodedPreference;
        preference.KeySelect = assinedKeys[0, 0];
        preference.KeyMenu = assinedKeys[1, 0];
        preference.KeyUp = assinedKeys[2, 0];
        preference.KeyLeft = assinedKeys[3, 0];
        preference.KeyDown = assinedKeys[4, 0];
        preference.KeyRight = assinedKeys[5, 0];
        preference.KeyUndo = assinedKeys[6, 0];
        preference.KeyReset = assinedKeys[7, 0];
        preference.KeyPlayerUp = assinedKeys[8, 0];
        preference.KeyPlayerLeft = assinedKeys[9, 0];
        preference.KeyPlayerDown = assinedKeys[10, 0];
        preference.KeyPlayerRight = assinedKeys[11, 0];
        preference.KeyCamera = assinedKeys[12, 0];
        encodedPreference = JsonUtility.ToJson(preference);
        try
        {
            File.WriteAllText(keyMappingPath, encodedPreference);
        }
        catch (Exception e)
        {
            Debug.LogError(String.Format("SaveKeyMapping: error occured while saving: {0}", e));
        }
    }

    private static string GetControlName(int n)
    {
        switch (n)
        {
            case 0:
                return "Select";
            case 1:
                return "Menu/Cancel";
            case 2:
                return "Up";
            case 3:
                return "Left";
            case 4:
                return "Down";
            case 5:
                return "Right";
            case 6:
                return "Undo";
            case 7:
                return "Reset";
            case 8:
                return "Player Up";
            case 9:
                return "Player Left";
            case 10:
                return "Player Down";
            case 11:
                return "Player Right";
            case 12:
                return "Camera";
            default:
                Debug.LogWarning(String.Format("GetControlName: not implemented for number {0}", n));
                return "Error";
        }
    }
    private static int GetControlID(string name)
    {
        switch (name)
        {
            case "Select":
                return 0;
            case "Menu/Cancel":
                return 1;
            case "Up":
                return 2;
            case "Right":
                return 3;
            case "Down":
                return 4;
            case "Left":
                return 5;
            case "Undo":
                return 6;
            case "Reset":
                return 7;
            case "Player Up":
                return 8;
            case "Player Right":
                return 9;
            case "Player Down":
                return 10;
            case "Player Left":
                return 11;
            case "Camera":
                return 12;
            default:
                return -1;
        }
    }
    // GetKeyName(KeyCode key) returns a key name of the specified keycode
    // GetKeyName(int n) returns a key name of the n-th assigned key
    private static string GetKeyName(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.A:
            case KeyCode.B:
            case KeyCode.C:
            case KeyCode.D:
            case KeyCode.E:
            case KeyCode.F:
            case KeyCode.G:
            case KeyCode.H:
            case KeyCode.I:
            case KeyCode.J:
            case KeyCode.K:
            case KeyCode.L:
            case KeyCode.M:
            case KeyCode.N:
            case KeyCode.O:
            case KeyCode.P:
            case KeyCode.Q:
            case KeyCode.R:
            case KeyCode.S:
            case KeyCode.T:
            case KeyCode.U:
            case KeyCode.V:
            case KeyCode.W:
            case KeyCode.X:
            case KeyCode.Y:
            case KeyCode.Z:
            case KeyCode.Backspace:
            case KeyCode.Delete:
            case KeyCode.Tab:
            case KeyCode.Clear:
            case KeyCode.Return:
            case KeyCode.Pause:
            case KeyCode.Escape:
            case KeyCode.Space:
            case KeyCode.Insert:
            case KeyCode.Home:
            case KeyCode.End:
            case KeyCode.PageUp:
            case KeyCode.PageDown:
            case KeyCode.F1:
            case KeyCode.F2:
            case KeyCode.F3:
            case KeyCode.F4:
            case KeyCode.F5:
            case KeyCode.F6:
            case KeyCode.F7:
            case KeyCode.F8:
            case KeyCode.F9:
            case KeyCode.F10:
            case KeyCode.F11:
            case KeyCode.F12:
            case KeyCode.F13:
            case KeyCode.F14:
            case KeyCode.F15:
            case KeyCode.Numlock:
            case KeyCode.CapsLock:
            case KeyCode.ScrollLock:
            case KeyCode.AltGr:
            case KeyCode.Help:
            case KeyCode.Print:
            case KeyCode.SysReq:
            case KeyCode.Break:
            case KeyCode.Menu:
                return Enum.GetName(typeof(KeyCode), key);
            case KeyCode.Keypad0:
            case KeyCode.Alpha0:
                return "0";
            case KeyCode.Keypad1:
            case KeyCode.Alpha1:
                return "1";
            case KeyCode.Keypad2:
            case KeyCode.Alpha2:
                return "2";
            case KeyCode.Keypad3:
            case KeyCode.Alpha3:
                return "3";
            case KeyCode.Keypad4:
            case KeyCode.Alpha4:
                return "4";
            case KeyCode.Keypad5:
            case KeyCode.Alpha5:
                return "5";
            case KeyCode.Keypad6:
            case KeyCode.Alpha6:
                return "6";
            case KeyCode.Keypad7:
            case KeyCode.Alpha7:
                return "7";
            case KeyCode.Keypad8:
            case KeyCode.Alpha8:
                return "8";
            case KeyCode.Keypad9:
            case KeyCode.Alpha9:
                return "9";
            case KeyCode.KeypadPeriod:
            case KeyCode.Period:
                return ".";
            case KeyCode.KeypadDivide:
            case KeyCode.Slash:
                return "/";
            case KeyCode.KeypadMultiply:
            case KeyCode.Asterisk:
                return "*";
            case KeyCode.KeypadMinus:
            case KeyCode.Minus:
                return "-";
            case KeyCode.KeypadPlus:
            case KeyCode.Plus:
                return "+";
            case KeyCode.KeypadEnter:
                return "Enter";
            case KeyCode.KeypadEquals:
            case KeyCode.Equals:
                return "=";
            case KeyCode.UpArrow:
                return "↑";
            case KeyCode.RightArrow:
                return "→";
            case KeyCode.DownArrow:
                return "↓";
            case KeyCode.LeftArrow:
                return "←";
            case KeyCode.Exclaim:
                return "!";
            case KeyCode.DoubleQuote:
                return "\"";
            case KeyCode.Hash:
                return "#";
            case KeyCode.Dollar:
                return "$";
            case KeyCode.Percent:
                return "%";
            case KeyCode.Ampersand:
                return "&";
            case KeyCode.Quote:
                return "'";
            case KeyCode.LeftParen:
                return "(";
            case KeyCode.RightParen:
                return ")";
            case KeyCode.Comma:
                return ",";
            case KeyCode.Colon:
                return ":";
            case KeyCode.Semicolon:
                return ";";
            case KeyCode.Less:
                return "<";
            case KeyCode.Greater:
                return ">";
            case KeyCode.Question:
                return "?";
            case KeyCode.At:
                return "@";
            case KeyCode.LeftBracket:
                return "[";
            case KeyCode.Backslash:
                return "\\";
            case KeyCode.RightBracket:
                return "]";
            case KeyCode.Caret:
                return "^";
            case KeyCode.Underscore:
                return "_";
            case KeyCode.BackQuote:
                return "`";
            case KeyCode.LeftCurlyBracket:
                return "{";
            case KeyCode.Pipe:
                return "|";
            case KeyCode.RightCurlyBracket:
                return "}";
            case KeyCode.Tilde:
                return "~";
            case KeyCode.RightShift:
                return "Shift R";
            case KeyCode.LeftShift:
                return "Shift L";
            case KeyCode.RightControl:
                return "Control R";
            case KeyCode.LeftControl:
                return "Control L";
            case KeyCode.RightAlt:
                return "Alt R";
            case KeyCode.LeftAlt:
                return "Alt L";
            case KeyCode.LeftCommand:
                return "Command L";
            case KeyCode.LeftWindows:
                return "Windows L";
            case KeyCode.RightCommand:
                return "Command R";
            case KeyCode.RightWindows:
                return "Windows R";
            default:
                Debug.LogWarning(String.Format("GetControlName: not implemented for key {0}", Enum.GetName(typeof(KeyCode), key)));
                return "ERROR";
        }
    }
    public static string GetKeyName(int n)
    {
        if (n < 0 || n >= KeyNumber)
        {
            Debug.LogWarning(String.Format("GetKeyName: invalid number {0}", n));
            return "ERROR";
        }
        return GetKeyName(assinedKeys[n, 0]);
    }
    private static KeyCode GetPairKey(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.A:
            case KeyCode.B:
            case KeyCode.C:
            case KeyCode.D:
            case KeyCode.E:
            case KeyCode.F:
            case KeyCode.G:
            case KeyCode.H:
            case KeyCode.I:
            case KeyCode.J:
            case KeyCode.K:
            case KeyCode.L:
            case KeyCode.M:
            case KeyCode.N:
            case KeyCode.O:
            case KeyCode.P:
            case KeyCode.Q:
            case KeyCode.R:
            case KeyCode.S:
            case KeyCode.T:
            case KeyCode.U:
            case KeyCode.V:
            case KeyCode.W:
            case KeyCode.X:
            case KeyCode.Y:
            case KeyCode.Z:
            case KeyCode.Backspace:
            case KeyCode.Delete:
            case KeyCode.Tab:
            case KeyCode.Clear:
            case KeyCode.Return:
            case KeyCode.Pause:
            case KeyCode.Escape:
            case KeyCode.Space:
            case KeyCode.Insert:
            case KeyCode.Home:
            case KeyCode.End:
            case KeyCode.PageUp:
            case KeyCode.PageDown:
            case KeyCode.F1:
            case KeyCode.F2:
            case KeyCode.F3:
            case KeyCode.F4:
            case KeyCode.F5:
            case KeyCode.F6:
            case KeyCode.F7:
            case KeyCode.F8:
            case KeyCode.F9:
            case KeyCode.F10:
            case KeyCode.F11:
            case KeyCode.F12:
            case KeyCode.F13:
            case KeyCode.F14:
            case KeyCode.F15:
            case KeyCode.Numlock:
            case KeyCode.CapsLock:
            case KeyCode.ScrollLock:
            case KeyCode.AltGr:
            case KeyCode.Help:
            case KeyCode.Print:
            case KeyCode.SysReq:
            case KeyCode.Break:
            case KeyCode.Menu:
            case KeyCode.KeypadEnter:
            case KeyCode.UpArrow:
            case KeyCode.RightArrow:
            case KeyCode.DownArrow:
            case KeyCode.LeftArrow:
            case KeyCode.Exclaim:
            case KeyCode.DoubleQuote:
            case KeyCode.Hash:
            case KeyCode.Dollar:
            case KeyCode.Percent:
            case KeyCode.Ampersand:
            case KeyCode.Quote:
            case KeyCode.LeftParen:
            case KeyCode.RightParen:
            case KeyCode.Comma:
            case KeyCode.Colon:
            case KeyCode.Semicolon:
            case KeyCode.Less:
            case KeyCode.Greater:
            case KeyCode.Question:
            case KeyCode.At:
            case KeyCode.LeftBracket:
            case KeyCode.Backslash:
            case KeyCode.RightBracket:
            case KeyCode.Caret:
            case KeyCode.Underscore:
            case KeyCode.BackQuote:
            case KeyCode.LeftCurlyBracket:
            case KeyCode.Pipe:
            case KeyCode.RightCurlyBracket:
            case KeyCode.Tilde:
            case KeyCode.RightShift:
            case KeyCode.LeftShift:
            case KeyCode.RightControl:
            case KeyCode.LeftControl:
            case KeyCode.RightAlt:
            case KeyCode.LeftAlt:
            case KeyCode.LeftCommand:
            case KeyCode.LeftWindows:
            case KeyCode.RightCommand:
            case KeyCode.RightWindows:
                return KeyCode.None;

            case KeyCode.Keypad0:
                return KeyCode.Alpha0;
            case KeyCode.Alpha0:
                return KeyCode.Keypad0;

            case KeyCode.Keypad1:
                return KeyCode.Alpha1;
            case KeyCode.Alpha1:
                return KeyCode.Keypad1;

            case KeyCode.Keypad2:
                return KeyCode.Alpha2;
            case KeyCode.Alpha2:
                return KeyCode.Keypad2;

            case KeyCode.Keypad3:
                return KeyCode.Alpha3;
            case KeyCode.Alpha3:
                return KeyCode.Keypad3;

            case KeyCode.Keypad4:
                return KeyCode.Alpha4;
            case KeyCode.Alpha4:
                return KeyCode.Keypad4;

            case KeyCode.Keypad5:
                return KeyCode.Alpha5;
            case KeyCode.Alpha5:
                return KeyCode.Keypad5;

            case KeyCode.Keypad6:
                return KeyCode.Alpha6;
            case KeyCode.Alpha6:
                return KeyCode.Keypad6;

            case KeyCode.Keypad7:
                return KeyCode.Alpha7;
            case KeyCode.Alpha7:
                return KeyCode.Keypad7;

            case KeyCode.Keypad8:
                return KeyCode.Alpha8;
            case KeyCode.Alpha8:
                return KeyCode.Keypad8;

            case KeyCode.Keypad9:
                return KeyCode.Alpha9;
            case KeyCode.Alpha9:
                return KeyCode.Keypad9;

            case KeyCode.KeypadPeriod:
                return KeyCode.Period;
            case KeyCode.Period:
                return KeyCode.KeypadPeriod;

            case KeyCode.KeypadDivide:
                return KeyCode.Slash;
            case KeyCode.Slash:
                return KeyCode.KeypadDivide;

            case KeyCode.KeypadMultiply:
                return KeyCode.Asterisk;
            case KeyCode.Asterisk:
                return KeyCode.KeypadMultiply;

            case KeyCode.KeypadMinus:
                return KeyCode.Minus;
            case KeyCode.Minus:
                return KeyCode.KeypadMinus;

            case KeyCode.KeypadPlus:
                return KeyCode.Plus;
            case KeyCode.Plus:
                return KeyCode.KeypadPlus;

            case KeyCode.KeypadEquals:
                return KeyCode.Equals;
            case KeyCode.Equals:
                return KeyCode.KeypadEquals;

            default:
                Debug.LogWarning(String.Format("GetPairKey: not implemented for key {0}", Enum.GetName(typeof(KeyCode), key)));
                return KeyCode.None;
        }
    }

    // CheckKeyInput returns one valid key input that is pressed down
    private static KeyCode CheckKeyInput()
    {
        KeyCode[] validKeys = { KeyCode.A, KeyCode.B, KeyCode.C, KeyCode.D, KeyCode.E, KeyCode.F, KeyCode.G, KeyCode.H, KeyCode.I, KeyCode.J,
            KeyCode.K, KeyCode.L, KeyCode.M, KeyCode.N, KeyCode.O, KeyCode.P, KeyCode.Q, KeyCode.R, KeyCode.S, KeyCode.T, KeyCode.U, KeyCode.V,
            KeyCode.W, KeyCode.X, KeyCode.Y, KeyCode.Z, KeyCode.Backspace, KeyCode.Delete, KeyCode.Tab, KeyCode.Clear, KeyCode.Return,
            KeyCode.Pause, KeyCode.Escape, KeyCode.Space, KeyCode.Insert, KeyCode.Home, KeyCode.End, KeyCode.PageUp, KeyCode.PageDown, KeyCode.F1,
            KeyCode.F2, KeyCode.F3, KeyCode.F4, KeyCode.F5, KeyCode.F6, KeyCode.F7, KeyCode.F8, KeyCode.F9, KeyCode.F10, KeyCode.F11, KeyCode.F12,
            KeyCode.F13, KeyCode.F14, KeyCode.F15, KeyCode.Numlock, KeyCode.CapsLock, KeyCode.ScrollLock, KeyCode.AltGr, KeyCode.Help,
            KeyCode.Print, KeyCode.SysReq, KeyCode.Break, KeyCode.Menu, KeyCode.Keypad0, KeyCode.Alpha0, KeyCode.Keypad1, KeyCode.Alpha1,
            KeyCode.Keypad2, KeyCode.Alpha2, KeyCode.Keypad3, KeyCode.Alpha3, KeyCode.Keypad4, KeyCode.Alpha4, KeyCode.Keypad5, KeyCode.Alpha5,
            KeyCode.Keypad6, KeyCode.Alpha6, KeyCode.Keypad7, KeyCode.Alpha7, KeyCode.Keypad8, KeyCode.Alpha8, KeyCode.Keypad9, KeyCode.Alpha9,
            KeyCode.KeypadPeriod, KeyCode.Period, KeyCode.KeypadDivide, KeyCode.Slash, KeyCode.KeypadMultiply, KeyCode.Asterisk,
            KeyCode.KeypadMinus, KeyCode.Minus, KeyCode.KeypadPlus, KeyCode.Plus, KeyCode.KeypadEnter, KeyCode.KeypadEquals, KeyCode.Equals,
            KeyCode.UpArrow, KeyCode.RightArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.Exclaim, KeyCode.DoubleQuote, KeyCode.Hash,
            KeyCode.Dollar, KeyCode.Percent, KeyCode.Ampersand, KeyCode.Quote, KeyCode.LeftParen, KeyCode.RightParen, KeyCode.Comma,
            KeyCode.Colon, KeyCode.Semicolon, KeyCode.Less, KeyCode.Greater, KeyCode.Question, KeyCode.At, KeyCode.LeftBracket, KeyCode.Backslash,
            KeyCode.RightBracket, KeyCode.Caret, KeyCode.Underscore, KeyCode.BackQuote, KeyCode.LeftCurlyBracket, KeyCode.Pipe,
            KeyCode.RightCurlyBracket, KeyCode.Tilde, KeyCode.RightShift, KeyCode.LeftShift, KeyCode.RightControl, KeyCode.LeftControl,
            KeyCode.RightAlt, KeyCode.LeftAlt, KeyCode.LeftCommand, KeyCode.LeftWindows, KeyCode.RightCommand, KeyCode.RightWindows };
        foreach (KeyCode validKey in validKeys) if (Input.GetKeyDown(validKey)) return validKey;
        return KeyCode.None;
    }
    private void AssignKey(int n, KeyCode key)
    {
        if (n < 0 || n >= KeyNumber)
        {
            Debug.LogWarning($"Keyboard.AssignKey: invalid number {n}");
            return;
        }
        assinedKeys[n, 0] = key;
        assinedKeys[n, 1] = GetPairKey(key);
        keys[n].ChangeText(GetKeyName(key));
    }

    // call these function only in Update()
    public static bool GetNumberKeyPressed()
    {
        return Input.GetKeyDown(KeyCode.Keypad0) || Input.GetKeyDown(KeyCode.Keypad1) || Input.GetKeyDown(KeyCode.Keypad2) ||
            Input.GetKeyDown(KeyCode.Keypad3) || Input.GetKeyDown(KeyCode.Keypad4) || Input.GetKeyDown(KeyCode.Keypad5) ||
            Input.GetKeyDown(KeyCode.Keypad6) || Input.GetKeyDown(KeyCode.Keypad7) || Input.GetKeyDown(KeyCode.Keypad8) ||
            Input.GetKeyDown(KeyCode.Keypad9) || Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Alpha1) ||
            Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Alpha4) ||
            Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Alpha7) ||
            Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Alpha9);
    }

    public static bool GetSelect(bool pressedDown = true)
    {
        return pressedDown ? Input.GetKeyDown(assinedKeys[0, 0]) || Input.GetKeyDown(assinedKeys[0, 1]) :
            Input.GetKey(assinedKeys[0, 0]) || Input.GetKey(assinedKeys[0, 1]);
    }
    public static bool GetMenu(bool pressedDown = true)
    {
        return pressedDown ? Input.GetKeyDown(assinedKeys[1, 0]) || Input.GetKeyDown(assinedKeys[1, 1]) :
            Input.GetKey(assinedKeys[1, 0]) || Input.GetKey(assinedKeys[1, 1]);
    }
    public static bool GetCancel(bool pressedDown = true)
    {
        return pressedDown ? Input.GetKeyDown(assinedKeys[1, 0]) || Input.GetKeyDown(assinedKeys[1, 1]) :
            Input.GetKey(assinedKeys[1, 0]) || Input.GetKey(assinedKeys[1, 1]);
    }
    public static bool GetUp(bool pressedDown = true)
    {
        return pressedDown ? Input.GetKeyDown(assinedKeys[2, 0]) || Input.GetKeyDown(assinedKeys[2, 1]) :
            Input.GetKey(assinedKeys[2, 0]) || Input.GetKey(assinedKeys[2, 1]);
    }
    public static bool GetLeft(bool pressedDown = true)
    {
        return pressedDown ? Input.GetKeyDown(assinedKeys[3, 0]) || Input.GetKeyDown(assinedKeys[3, 1]) :
            Input.GetKey(assinedKeys[3, 0]) || Input.GetKey(assinedKeys[3, 1]);
    }
    public static bool GetDown(bool pressedDown = true)
    {
        return pressedDown ? Input.GetKeyDown(assinedKeys[4, 0]) || Input.GetKeyDown(assinedKeys[4, 1]) :
            Input.GetKey(assinedKeys[4, 0]) || Input.GetKey(assinedKeys[4, 1]);
    }
    public static bool GetRight(bool pressedDown = true)
    {
        return pressedDown ? Input.GetKeyDown(assinedKeys[5, 0]) || Input.GetKeyDown(assinedKeys[5, 1]) :
            Input.GetKey(assinedKeys[5, 0]) || Input.GetKey(assinedKeys[5, 1]);
    }
    public static bool GetUndo(bool pressedDown = true)
    {
        return pressedDown ? Input.GetKeyDown(assinedKeys[6, 0]) || Input.GetKeyDown(assinedKeys[6, 1]) :
            Input.GetKey(assinedKeys[6, 0]) || Input.GetKey(assinedKeys[6, 1]);
    }
    public static bool GetReset(bool pressedDown = true)
    {
        return pressedDown ? Input.GetKeyDown(assinedKeys[7, 0]) || Input.GetKeyDown(assinedKeys[7, 1]) :
            Input.GetKey(assinedKeys[7, 0]) || Input.GetKey(assinedKeys[7, 1]);
    }
    public static bool GetPlayerUp(bool pressedDown = true)
    {
        return pressedDown ? Input.GetKeyDown(assinedKeys[8, 0]) || Input.GetKeyDown(assinedKeys[8, 1]) :
            Input.GetKey(assinedKeys[8, 0]) || Input.GetKey(assinedKeys[8, 1]);
    }
    public static bool GetPlayerLeft(bool pressedDown = true)
    {
        return pressedDown ? Input.GetKeyDown(assinedKeys[9, 0]) || Input.GetKeyDown(assinedKeys[9, 1]) :
            Input.GetKey(assinedKeys[9, 0]) || Input.GetKey(assinedKeys[9, 1]);
    }
    public static bool GetPlayerDown(bool pressedDown = true)
    {
        return pressedDown ? Input.GetKeyDown(assinedKeys[10, 0]) || Input.GetKeyDown(assinedKeys[10, 1]) :
            Input.GetKey(assinedKeys[10, 0]) || Input.GetKey(assinedKeys[10, 1]);
    }
    public static bool GetPlayerRight(bool pressedDown = true)
    {
        return pressedDown ? Input.GetKeyDown(assinedKeys[11, 0]) || Input.GetKeyDown(assinedKeys[11, 1]) :
            Input.GetKey(assinedKeys[11, 0]) || Input.GetKey(assinedKeys[11, 1]);
    }
    public static bool GetCamera(bool pressedDown = true)
    {
        return pressedDown ? Input.GetKeyDown(assinedKeys[12, 0]) || Input.GetKeyDown(assinedKeys[12, 1]) :
            Input.GetKey(assinedKeys[12, 0]) || Input.GetKey(assinedKeys[12, 1]);
    }

    public static bool GetActionKeyPressed()
    {
        return GetPlayerUp() || GetPlayerRight() || GetPlayerDown() || GetPlayerLeft();
    }

    public static bool GetCommandKeyPressed()
    {
        return GetUndo() || GetReset();
    }

    public static bool GetAnyKeyPressed()
    {
        return GetActionKeyPressed() || GetCommandKeyPressed();
    }
}
