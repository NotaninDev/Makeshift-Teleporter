using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class History : MonoBehaviour
{
    public enum Type
    {
        Player, // position
        Block, // position, type, connection code
        MoveCount,
    }
    public struct HistoryUnit
    {
        public Type type;
        public int target;
        public Vector2Int position;
        public bool flag;
        public MapData.BlockType blockType;
    }
    public static Stack<HistoryUnit> historyTurn = null;
    public static Stack<Stack<HistoryUnit>> history;

    public static void Initialize()
    {
        history = new Stack<Stack<HistoryUnit>>();
    }

    // initialize the next turn's history container
    public static void StartTurn()
    {
        if (historyTurn != null)
        {
            if (historyTurn.Count > 0) history.Push(historyTurn);
        }
        historyTurn = new Stack<HistoryUnit>();
    }
    public static void AddHistory(Type type, int target)
    {
        switch (type)
        {
            case Type.MoveCount:
                HistoryUnit unit = new HistoryUnit();
                unit.type = type;
                unit.target = target;
                historyTurn.Push(unit);
                break;
            default:
                Debug.LogWarning(String.Format("AddHistory(int): type {0} is invalid", Enum.GetNames(typeof(Type))[(int)type]));
                break;
        }
    }
    public static void AddHistory(Type type, Vector2Int position)
    {
        switch (type)
        {
            case Type.Player:
                HistoryUnit unit = new HistoryUnit();
                unit.type = type;
                unit.position = position;
                historyTurn.Push(unit);
                break;
            default:
                Debug.LogWarning(String.Format("AddHistory(Vector2Int): type {0} is invalid", Enum.GetNames(typeof(Type))[(int)type]));
                break;
        }
    }
    public static void AddHistory(Type type, Vector2Int position, MapData.BlockType blockType, int code)
    {
        switch (type)
        {
            case Type.Block:
                HistoryUnit unit = new HistoryUnit();
                unit.type = type;
                unit.position = position;
                unit.blockType = blockType;
                unit.target = code;
                historyTurn.Push(unit);
                break;
            default:
                Debug.LogWarning(String.Format("AddHistory(Vector2Int, BlockType, int): type {0} is invalid", Enum.GetNames(typeof(Type))[(int)type]));
                break;
        }
    }

    // undo a turn
    // returns false if there was no history
    public static void RollBack()
    {
        if (history.Count == 0)
        {
            Debug.LogWarning("RollBack: no history registered to roll back");
            return;
        }
        Stack<HistoryUnit> undoneHistory = history.Pop();
        while (undoneHistory.Count > 0)
        {
            HistoryUnit unit = undoneHistory.Pop();
            MainGame.RollBack(unit);
        }
    }
}
