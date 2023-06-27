using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using String = System.String;

public class MainGame : MonoBehaviour
{
    private UnityEvent mainEvent;
    private static GameState gameState;
    private enum GameState
    {
        Prepare,
        Ready,
        Move,
        Finish
    }
    private bool skip;

    private static int level;
    private static string levelTag;
    private static GameObject mapObject;
    public static Map map;
    public static MapData mapData, initialState;
    public const int LevelCount = 20;
    public const float MoveDuration = .3f;

    private static int moveCount, totalMoveCount;

    private enum InputType
    {
        None,
        Undo,
        Up,
        Right,
        Down,
        Left
    }
    private static InputType lastInput;
    private static float lastInputTime;
    private const float UndoInterval = .1f, FirstIntervalRate = 3f;
    private static bool secondUndo, inputInterrupted, canMove, teleported;
    private static IEnumerator waiting;

    private const int optionCount = 5;
    private static GameObject[] optionObjects;
    private static Option[] options;
    private static GameObject shadowObject;
    private static Menu menu;

    static MainGame()
    {
        mapData = new MapData();
    }

    void Awake()
    {
        mapObject = General.AddChild(gameObject, "Map");
        map = mapObject.AddComponent<Map>();
        optionObjects = new GameObject[optionCount];
        options = new Option[optionCount];
        for (int i = 0; i < optionCount; i++)
        {
            optionObjects[i] = General.AddChild(gameObject, "Message Box" + i.ToString());
            options[i] = optionObjects[i].AddComponent<Option>();
        }
        GameObject menuObject = GameObject.FindWithTag("Menu Panel");
        if (menuObject == null)
        {
            Debug.LogWarning("MainGame: Menu Panel not found");
            menuObject = General.AddChild(gameObject, "Menu");
            shadowObject = menuObject;
        }
        else shadowObject = menuObject.transform.parent.gameObject;
        menu = menuObject.AddComponent<Menu>();

        level = GameManager.level;
        levelTag = MapData.GetLevelTag(level);
        General.UpdateLastPlayedLevel(levelTag);
        mapData.Initialize(levelTag);
        initialState = mapData.Clone();

        lastInput = InputType.None;
        lastInputTime = -1;
        secondUndo = true;
        inputInterrupted = false;
        canMove = true;
        teleported = false;
        mainEvent = new UnityEvent();
        mainEvent.AddListener(ChangeState);
    }
    void Start()
    {
        for (int i = 0; i < optionCount; i++) optionObjects[i].SetActive(false);
        options[0].Initialize("Message", 100, null, 1.2f, 1f, 101, null, Graphics.FontName.Mops,
            5f, Graphics.LightBrown, new Vector2(.6f, .12f), false, lineSpacing: -6f);
        for (int i = 1; i < optionCount; i++)
        {
            options[i].Initialize("Message", 0, null, 1f, 1f, 1, null, Graphics.FontName.Mops, 6f, Graphics.WhiteBrown,
                Vector2.zero, false, lineSpacing: -6f, alignment: TextAlignmentOptions.Midline);
        }
        menu.Initialize(shadowObject);
        shadowObject.SetActive(false);

        gameState = GameState.Ready;
        skip = false;
        map.Initialize(mapData);
        moveCount = 0;
        totalMoveCount = 0;
        History.Initialize();
        InitializeHistoryTurn();
        options[0].ChangeText(MapData.GetLevelName(levelTag));

        optionObjects[0].SetActive(true);
        optionObjects[0].transform.localPosition = new Vector3(-8.8f + options[0].Size.x * 1.2f / 2, -4.57f, 0);
        switch (levelTag)
        {
            case "pocket3":
                options[1].ChangeText($"{Keyboard.GetKeyName(8)} {Keyboard.GetKeyName(9)} {Keyboard.GetKeyName(10)} {Keyboard.GetKeyName(11)}{Environment.NewLine}Move");
                optionObjects[1].transform.localPosition = new Vector3(-6.19f, 3.09f, 0);
                optionObjects[1].SetActive(true);
                break;
        }
    }

    void Update()
    {
        if (SceneLoader.Loading) return;

        // go back to the main menu
        bool noMoreInput = false;
        if (!shadowObject.activeInHierarchy && Keyboard.GetMenu())
        {
            shadowObject.SetActive(true);
            menu.ResetSelection();
            lastInput = InputType.None;
            noMoreInput = true;
        }
        else if (shadowObject.activeInHierarchy)
        {
            menu.HandleInput();
            lastInput = InputType.None;
            noMoreInput = true;
        }

        if (!Keyboard.GetUndo(pressedDown: false))
        {
            lastInputTime = -1;
            secondUndo = true;
        }

        switch (gameState)
        {
            case GameState.Ready:
                if (shadowObject.activeInHierarchy) break;
                float wait = -1;
                inputInterrupted |= teleported;
                if (teleported)
                {
                    lastInput = InputType.None;
                    teleported = false;
                }

                // reset
                if (!noMoreInput && Keyboard.GetReset() && moveCount > 0)
                {
                    RegisterHistory(mapData, initialState);
                    History.AddHistory(History.Type.MoveCount, moveCount);
                    mapData.Reset(initialState);
                    map.Reset();
                    moveCount = 0;
                    InitializeHistoryTurn();
                    noMoreInput = true;
                }

                // undo
                if (!noMoreInput && (Keyboard.GetUndo(pressedDown: true) || Keyboard.GetUndo(pressedDown: false) && !inputInterrupted && lastInput == InputType.Undo && (lastInputTime < 0 || secondUndo && Time.time >= lastInputTime + UndoInterval * FirstIntervalRate || !secondUndo && Time.time >= lastInputTime + UndoInterval)) && totalMoveCount > 0)
                {
                    if (Keyboard.GetUndo(pressedDown: true)) secondUndo = true;
                    else if (secondUndo && lastInputTime >= 0) secondUndo = false;
                    lastInputTime = Time.time;
                    if (moveCount > 0)
                    {
                        moveCount--;
                        totalMoveCount--;
                    }
                    History.RollBack();
                    InitializeHistoryTurn();
                    lastInput = InputType.Undo;
                    noMoreInput = true;
                }

                //if (!noMoreInput && Input.GetKeyDown(KeyCode.N))
                //{
                //    gameState = GameState.Move;
                //    skip = true;
                //    noMoreInput = true;
                //}

                // up
                if (!noMoreInput && (Keyboard.GetPlayerUp(pressedDown: true) || Keyboard.GetPlayerUp(pressedDown: false) && !inputInterrupted && lastInput == InputType.Up && canMove))
                {
                    MapData tempMap = mapData.Clone();
                    canMove = mapData.Move(MapData.Direction.Up, out teleported);
                    if (canMove)
                    {
                        moveCount++;
                        totalMoveCount++;
                        RegisterHistory(tempMap, mapData);
                        InitializeHistoryTurn();
                    }
                    if (Keyboard.GetPlayerUp(pressedDown: true) || canMove)
                    {
                        wait = map.Move(tempMap, MapData.Direction.Up) + .02f;
                        gameState = GameState.Move;
                        lastInput = InputType.Up;
                        noMoreInput = true;
                    }
                }

                // right
                if (!noMoreInput && (Keyboard.GetPlayerRight(pressedDown: true) || Keyboard.GetPlayerRight(pressedDown: false) && !inputInterrupted && lastInput == InputType.Right && canMove))
                {
                    MapData tempMap = mapData.Clone();
                    canMove = mapData.Move(MapData.Direction.Right, out teleported);
                    if (canMove)
                    {
                        moveCount++;
                        totalMoveCount++;
                        RegisterHistory(tempMap, mapData);
                        InitializeHistoryTurn();
                    }
                    if (Keyboard.GetPlayerRight(pressedDown: true) || canMove)
                    {
                        wait = map.Move(tempMap, MapData.Direction.Right) + .02f;
                        gameState = GameState.Move;
                        lastInput = InputType.Right;
                        noMoreInput = true;
                    }
                }

                // down
                if (!noMoreInput && (Keyboard.GetPlayerDown(pressedDown: true) || Keyboard.GetPlayerDown(pressedDown: false) && !inputInterrupted && lastInput == InputType.Down && canMove))
                {
                    MapData tempMap = mapData.Clone();
                    canMove = mapData.Move(MapData.Direction.Down, out teleported);
                    if (canMove)
                    {
                        moveCount++;
                        totalMoveCount++;
                        RegisterHistory(tempMap, mapData);
                        InitializeHistoryTurn();
                    }
                    if (Keyboard.GetPlayerDown(pressedDown: true) || canMove)
                    {
                        wait = map.Move(tempMap, MapData.Direction.Down) + .02f;
                        gameState = GameState.Move;
                        lastInput = InputType.Down;
                        noMoreInput = true;
                    }
                }

                // left
                if (!noMoreInput && (Keyboard.GetPlayerLeft(pressedDown: true) || Keyboard.GetPlayerLeft(pressedDown: false) && !inputInterrupted && lastInput == InputType.Left && canMove))
                {
                    MapData tempMap = mapData.Clone();
                    canMove = mapData.Move(MapData.Direction.Left, out teleported);
                    if (canMove)
                    {
                        moveCount++;
                        totalMoveCount++;
                        RegisterHistory(tempMap, mapData);
                        InitializeHistoryTurn();
                    }
                    if (Keyboard.GetPlayerLeft(pressedDown: true) || canMove)
                    {
                        wait = map.Move(tempMap, MapData.Direction.Left) + .02f;
                        gameState = GameState.Move;
                        lastInput = InputType.Left;
                        noMoreInput = true;
                    }
                }
                inputInterrupted = false;
                if (wait > 0 || skip)
                {
                    lastInputTime = -1;
                    secondUndo = true;
                    waiting = General.WaitEvent(mainEvent, wait);
                    StartCoroutine(waiting);
                }
                if (!noMoreInput)
                {
                    if (!((lastInput == InputType.Undo && Keyboard.GetUndo(pressedDown: false)) ||
                        (lastInput == InputType.Up && Keyboard.GetPlayerUp(pressedDown: false)) ||
                        (lastInput == InputType.Right && Keyboard.GetPlayerRight(pressedDown: false)) ||
                        (lastInput == InputType.Down && Keyboard.GetPlayerDown(pressedDown: false)) ||
                        (lastInput == InputType.Left && Keyboard.GetPlayerLeft(pressedDown: false))))
                    {
                        lastInput = InputType.None;
                    }
                }
                break;
            case GameState.Move:
                if (Keyboard.GetAnyKeyPressed() && !mapData.Win())
                {
                    if (waiting != null) StopCoroutine(waiting);
                    switch (lastInput)
                    {
                        case InputType.None:
                            Debug.LogWarning($"Updata-Move: last input was not registered");
                            break;

                        case InputType.Up:
                        case InputType.Right:
                        case InputType.Down:
                        case InputType.Left:
                            map.StopAnimation();
                            break;
                        default:
                            Debug.LogWarning($"Updata-Move: not implemented for InputType {Enum.GetName(typeof(InputType), lastInput)}");
                            break;
                    }
                    inputInterrupted = true;
                    gameState = GameState.Ready;
                    goto case GameState.Ready;
                }
                break;
            case GameState.Finish:
                break;
            default:
                Debug.LogWarning($"Update: not implemented for type {Enum.GetName(typeof(GameState), gameState)}");
                break;
        }
    }

    private IEnumerator EndGame()
    {
        General.AddSolvedLevel(levelTag);
        if (skip) yield return new WaitForSeconds(0);
        else yield return new WaitForSeconds(.8f);
        level++;
        GameManager.level++;
        if (level < LevelCount)
        {
            SceneLoader.sceneEvent.Invoke("MainScene");
        }
        else
        {
            GameManager.level = LevelCount - 1;
            SceneLoader.sceneEvent.Invoke("TitleScene");
        }
    }
    private void ChangeState()
    {
        switch (gameState)
        {
            case GameState.Prepare:
                gameState = GameState.Ready;
                break;

            case GameState.Move:
                if (mapData.Win() || skip)
                {
                    StartCoroutine(EndGame());
                    gameState = GameState.Finish;
                }
                else
                {
                    map.StopAnimation();
                    gameState = GameState.Ready;
                }
                break;

            default:
                Debug.LogWarning(String.Format("ChangeState: not implemented yet for state {0}", Enum.GetNames(typeof(GameState))[(int)gameState]));
                break;
        }
    }

    private static void InitializeHistoryTurn()
    {
        History.StartTurn();
    }
    private static void RegisterHistory(MapData oldMap, MapData newMap)
    {
        History.AddHistory(History.Type.Player, oldMap.Player);
        for (int i = 0; i < newMap.Size.x; i++)
        {
            for (int j = 0; j < newMap.Size.y; j++)
            {
                if (oldMap.Blocks[i, j] != newMap.Blocks[i, j] || oldMap.BlockConnection[i, j] != newMap.BlockConnection[i, j])
                {
                    History.AddHistory(History.Type.Block, new Vector2Int(i, j), oldMap.Blocks[i, j], oldMap.BlockConnection[i, j]);
                }
            }
        }
    }
    public static void RollBack(History.HistoryUnit unit)
    {
        switch (unit.type)
        {
            case History.Type.Player:
                mapData.Player = unit.position;
                map.MovePlayer();
                break;
            case History.Type.Block:
                mapData.Blocks[unit.position.x, unit.position.y] = unit.blockType;
                mapData.BlockConnection[unit.position.x, unit.position.y] = unit.target;
                map.MoveBlock(unit.position.x, unit.position.y);
                break;
            case History.Type.MoveCount:
                moveCount = unit.target;
                break;
            default:
                Debug.LogWarning(String.Format("RollBack: not implemented for type {0}", Enum.GetNames(typeof(History.Type))[(int)unit.type]));
                break;
        }
    }
}
