﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Map : MonoBehaviour
{
    private const float CellSize = 1.28f, SpriteScale = 1.02f;
    public static Vector3 mapOffset;
    private static Vector2Int[] directionDictionary;
    public MapData mapData { get; private set; }
    private string levelTag;

    private GameObject playerObject, targetObject, tileParent, blockParent;
    private SpriteBox playerSprite, targetSprite;
    private GameObject[,] blockObjects;
    private int blockCount;
    private Stack<GameObject> blockPool;

    private const float MoveTime = .1f, StuckTime = .1f, TeleportTime = .1f;
    private const float StuckScale = 1f / StuckTime / StuckTime;
    private const float BrokenBlockMaxRotation = 18f, BrokenBlockScaleDiff = .2f;
    private IEnumerator playerAnimation, blockGroupAnimation, teleportationAnimation;
    private GameObject blockGroupParent;

    // Scale is the scale of the map
    public float Scale { get; private set; }

    public enum Direction
    {
        Up, Right, Down, Left
    }

    static Map()
    {
        directionDictionary = new Vector2Int[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
    }

    void Awake()
    {
        mapData = new MapData();

        playerObject = General.AddChild(gameObject, "Player");
        playerSprite = playerObject.AddComponent<SpriteBox>();
        targetObject = General.AddChild(gameObject, "Target");
        targetSprite = targetObject.AddComponent<SpriteBox>();
        tileParent = General.AddChild(gameObject, "Tile Parent");
        blockParent = General.AddChild(gameObject, "Block Parent");
        blockGroupParent = General.AddChild(gameObject, "Block Group Parent");
        blockPool = new Stack<GameObject>();
    }

    public void Initialize(MapData mapData)
    {
        // load raw data
        this.mapData = mapData;
        levelTag = mapData.LevelTag;

        // set the map size and position
        float tempWidth = CellSize * (mapData.Size.x + 2), tempHeight = CellSize * (mapData.Size.y + 2);
        if (tempHeight / tempWidth > Graphics.ScreenRatio)
        {
            Scale = Graphics.Height / tempHeight;
        }
        else
        {
            Scale = Graphics.Width / tempWidth;
        }
        mapOffset = new Vector3(CellSize * (mapData.Size.x - 1) * -.5f, CellSize * ((mapData.Size.y - 1) * -.5f), 0);
        transform.localScale = Vector3.one * Scale;
        transform.localPosition = Vector3.zero;

        blockObjects = new GameObject[mapData.Size.x, mapData.Size.y];
        blockCount = 0;
        for (int i = 0; i < mapData.Size.x; i++)
        {
            for (int j = 0; j < mapData.Size.y; j++)
            {
                bool isWall = mapData.Walls[i, j];
                GameObject tileObject = General.AddChild(tileParent, $"{(isWall ? "Wall" : "Floor")} ({i}, {j})");
                SpriteBox tileSprite = tileObject.AddComponent<SpriteBox>();
                tileSprite.Initialize(Graphics.tile[(isWall ? 2 : 0) + ((i + j) % 2)], "Tile", isWall ? -1: -2, Get3DPoint(i, j));
                tileObject.transform.localScale = Vector3.one * SpriteScale;

                // blocks
                Vector2Int coordinates = new Vector2Int(i, j);
                if (mapData.HasBlock(i, j))
                {
                    blockCount++;
                    blockObjects[i, j] = General.AddChild(blockParent, $"Block {blockCount}");
                    Block tempBlock = blockObjects[i, j].AddComponent<Block>();
                    tempBlock.Initialize(!mapData.IsConnected(coordinates, MapData.Direction.Up), !mapData.IsConnected(coordinates, MapData.Direction.Right), !mapData.IsConnected(coordinates, MapData.Direction.Down), !mapData.IsConnected(coordinates, MapData.Direction.Left));
                    blockObjects[i, j].transform.localPosition = Get3DPoint(i, j);
                }
            }
        }

        playerSprite.Initialize(Graphics.player[0], "Tile", 1, Get3DPoint(mapData.Player.x, mapData.Player.y));
        targetSprite.Initialize(Graphics.tile[4], "Tile", -1, Get3DPoint(mapData.Target.x, mapData.Target.y));
    }
    // reset the level
    // requires mapData to be reset beforehand
    public void Reset()
    {
        MovePlayer();
        for (int i = 0; i < mapData.Size.x; i++)
        {
            for (int j = 0; j < mapData.Size.y; j++)
            {
                MoveBlock(i, j);
            }
        }
    }

    public float Move(MapData previousMap, MapData.Direction direction)
    {
        bool stuck = mapData.Player == previousMap.Player;
        playerAnimation = stuck ? AnimateStuck() : Graphics.Move(playerObject, Get3DPoint(previousMap.Player), Get3DPoint(previousMap.Player + directionDictionary[(int)direction]), MoveTime);
        if (playerAnimation != null) StartCoroutine(playerAnimation);

        Dictionary<Vector2Int, GameObject> movedBlockObjects = new Dictionary<Vector2Int, GameObject>();
        if (mapData.BlockGroupMoved)
        {
            blockGroupParent.transform.localPosition = Get3DPoint(previousMap.Player);
            for (int i = 0; i < mapData.Size.x; i++)
            {
                for (int j = 0; j < mapData.Size.y; j++)
                {
                    if (mapData.BlockMoved[i, j])
                    {
                        Vector2Int coordinates = new Vector2Int(i, j);
                        movedBlockObjects[coordinates] = blockObjects[i, j];
                        blockObjects[i, j] = null;
                        movedBlockObjects[coordinates].transform.SetParent(blockGroupParent.transform);
                    }
                }
            }
            foreach (KeyValuePair<Vector2Int, GameObject> pair in movedBlockObjects)
            {
                int x = pair.Key.x,
                    y = pair.Key.y;
                blockObjects[x, y] = pair.Value;
            }
            blockGroupAnimation = Graphics.Move(blockGroupParent, Get3DPoint(previousMap.Player), Get3DPoint(previousMap.Player + directionDictionary[(int)direction]), MoveTime);
            StartCoroutine(blockGroupAnimation);
        }
        if (mapData.TurnHistory.Count > 0)
        {
            teleportationAnimation = AnimateTeleportation(previousMap, direction);
            StartCoroutine(teleportationAnimation);
        }
        return stuck ? StuckTime : (MoveTime + TeleportTime * mapData.TurnHistory.Count);
    }

    private IEnumerator AnimateStuck()
    {
        float start = Time.time, scale;
        do
        {
            scale = (float)(Math.Pow(Time.time - start - StuckTime / 2, 2) - Math.Pow(StuckTime / 2, 2)) * StuckScale + 1;
            playerObject.transform.localScale = Vector3.one * scale;
            yield return null;
        } while (Time.time - start < StuckTime);
        playerObject.transform.localScale = Vector3.one;
    }

    private IEnumerator AnimateTeleportation(MapData previousMap, MapData.Direction direction)
    {
        yield return new WaitForSeconds(MoveTime);
        while (blockGroupParent.transform.childCount > 0)
        {
            blockGroupParent.transform.GetChild(0).SetParent(blockParent.transform);
        }

        // move blocks in previousMap
        int[,] blockBuffer = new int[mapData.Size.x, mapData.Size.y];
        for (int i = 0; i < mapData.Size.x; i++)
        {
            for (int j = 0; j < mapData.Size.y; j++)
            {
                if (mapData.BlockMoved[i, j])
                {
                    blockBuffer[i, j] = previousMap.BlockConnection[i, j];
                    previousMap.Blocks[i, j] = MapData.BlockType.None;
                    previousMap.BlockConnection[i, j] = 0;
                }
            }
        }
        for (int i = 0; i < mapData.Size.x; i++)
        {
            for (int j = 0; j < mapData.Size.y; j++)
            {
                if (mapData.BlockMoved[i, j])
                {
                    Vector2Int blockTargetPosition = new Vector2Int(i, j) + directionDictionary[(int)direction];
                    previousMap.Blocks[blockTargetPosition.x, blockTargetPosition.y] = MapData.BlockType.Block;
                    previousMap.BlockConnection[blockTargetPosition.x, blockTargetPosition.y] = blockBuffer[i, j];
                }
            }
        }

        while (mapData.TurnHistory.Count > 0)
        {
            for (int i = 0; i < mapData.Size.x; i++)
            {
                for (int j = 0; j < mapData.Size.y; j++)
                {
                    MoveBlock(i, j, previousMap);
                }
            }

            MapData.MicroHistory microHistory = mapData.TurnHistory.Dequeue();
            playerObject.transform.localPosition = Get3DPoint(microHistory.JumpPoint);

            float start = Time.time;
            do
            {
                for (int i = 0; i < mapData.Size.x; i++)
                {
                    for (int j = 0; j < mapData.Size.y; j++)
                    {
                        if (microHistory.Broke[i, j])
                        {
                            Block brokenBlock = blockObjects[i, j].GetComponent<Block>();
                            brokenBlock.LerpColor((Time.time - start) / TeleportTime);
                            blockObjects[i, j].transform.localScale = Vector3.one * (1 - BrokenBlockScaleDiff * (Time.time - start) / TeleportTime);
                            blockObjects[i, j].transform.localEulerAngles = new Vector3(0, 0, BrokenBlockMaxRotation * (Time.time - start) / TeleportTime);
                        }
                    }
                }
                playerObject.transform.localScale = Vector3.one * (1 - .25f * (TeleportTime - (Time.time - start)) / TeleportTime);
                yield return null;
            } while (Time.time - start < TeleportTime);
            for (int i = 0; i < mapData.Size.x; i++)
            {
                for (int j = 0; j < mapData.Size.y; j++)
                {
                    if (microHistory.Broke[i, j])
                    {
                        blockObjects[i, j].SetActive(false);
                        blockPool.Push(blockObjects[i, j]);
                        blockObjects[i, j] = null;
                    }
                }
            }
            playerObject.transform.localScale = Vector3.one;
        }
    }

    // transform a 2D map point to a 3D world point
    public static Vector3 Get3DPoint(Vector2Int _2dPoint) { return new Vector3(_2dPoint.x, _2dPoint.y, 0) * CellSize + mapOffset; }
    public static Vector3 Get3DPoint(int x, int y) { return new Vector3(x, y, 0) * CellSize + mapOffset; }

    public void MovePlayer()
    {
        playerObject.transform.localPosition = Get3DPoint(mapData.Player);
        playerObject.transform.localScale = Vector3.one;
    }
    public void MoveBlock(int x, int y, MapData mapData = null)
    {
        Vector2Int coordinates = new Vector2Int(x, y);
        if (mapData == null) mapData = this.mapData;
        if (mapData.HasBlock(x, y))
        {
            if (blockObjects[x, y] == null)
            {
                if (blockPool.Count > 0)
                {
                    blockObjects[x, y] = blockPool.Pop();
                    blockObjects[x, y].SetActive(true);
                    Block tempBlock = blockObjects[x, y].GetComponent<Block>();
                    tempBlock.SetFrameActive(!mapData.IsConnected(coordinates, MapData.Direction.Up), !mapData.IsConnected(coordinates, MapData.Direction.Right), !mapData.IsConnected(coordinates, MapData.Direction.Down), !mapData.IsConnected(coordinates, MapData.Direction.Left));
                    tempBlock.LerpColor(0);
                }
                else
                {
                    blockCount++;
                    blockObjects[x, y] = General.AddChild(blockParent, $"Block {blockCount}");
                    Block tempBlock = blockObjects[x, y].AddComponent<Block>();
                    tempBlock.Initialize(!mapData.IsConnected(coordinates, MapData.Direction.Up), !mapData.IsConnected(coordinates, MapData.Direction.Right), !mapData.IsConnected(coordinates, MapData.Direction.Down), !mapData.IsConnected(coordinates, MapData.Direction.Left));
                }
            }
            else
            {
                Block tempBlock = blockObjects[x, y].GetComponent<Block>();
                tempBlock.Initialize(!mapData.IsConnected(coordinates, MapData.Direction.Up), !mapData.IsConnected(coordinates, MapData.Direction.Right), !mapData.IsConnected(coordinates, MapData.Direction.Down), !mapData.IsConnected(coordinates, MapData.Direction.Left));
            }
            blockObjects[x, y].transform.localPosition = Get3DPoint(x, y);
            blockObjects[x, y].transform.localEulerAngles = Vector3.zero;
            blockObjects[x, y].transform.localScale = Vector3.one;
        }
        else
        {
            if (blockObjects[x, y] != null)
            {
                blockObjects[x, y].SetActive(false);
                blockPool.Push(blockObjects[x, y]);
                blockObjects[x, y] = null;
            }
        }
    }

    public void StopAnimation()
    {
        if (playerAnimation != null) StopCoroutine(playerAnimation);
        if (blockGroupAnimation != null) StopCoroutine(blockGroupAnimation);
        if (teleportationAnimation != null) StopCoroutine(teleportationAnimation);

        // update objects' position
        MovePlayer();

        while (blockGroupParent.transform.childCount > 0)
        {
            blockGroupParent.transform.GetChild(0).SetParent(blockParent.transform);
        }
        for (int i = 0; i < mapData.Size.x; i++)
        {
            for (int j = 0; j < mapData.Size.y; j++)
            {
                MoveBlock(i, j);
            }
        }
    }
}
