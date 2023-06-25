using System;
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
    private Stack<GameObject> blockPool;

    private const float MoveTime = .1f, StuckTime = .1f;
    private const float StuckScale = 1f / StuckTime / StuckTime;
    private IEnumerator playerAnimation, blockGroupAnimation;
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
        int blockCount = 0;
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
    }

    public float Move(MapData previousMap, MapData.Direction direction)
    {
        bool stuck = mapData.Player == previousMap.Player;
        playerAnimation = stuck ? AnimateStuck() : Graphics.Move(playerObject, Get3DPoint(previousMap.Player), Get3DPoint(mapData.Player), MoveTime);
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
            blockGroupAnimation = Graphics.Move(blockGroupParent, Get3DPoint(previousMap.Player), Get3DPoint(mapData.Player), MoveTime);
            StartCoroutine(blockGroupAnimation);
        }
        return stuck ? StuckTime : MoveTime;
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

    // transform a 2D map point to a 3D world point
    public static Vector3 Get3DPoint(Vector2Int _2dPoint) { return new Vector3(_2dPoint.x, _2dPoint.y, 0) * CellSize + mapOffset; }
    public static Vector3 Get3DPoint(int x, int y) { return new Vector3(x, y, 0) * CellSize + mapOffset; }

    public void MovePlayer()
    {
        playerObject.transform.localPosition = Get3DPoint(mapData.Player);
        playerObject.transform.localScale = Vector3.one;
    }
    public void MoveBlock(int x, int y)
    {
        //
        // to do: implement this lol
        //
        blockObjects[x, y].transform.localPosition = Get3DPoint(x, y);
    }

    public void StopAnimation()
    {
        if (playerAnimation != null) StopCoroutine(playerAnimation);
        if (blockGroupAnimation != null) StopCoroutine(blockGroupAnimation);

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
                if (blockObjects[i, j] != null)
                {
                    blockPool.Push(blockObjects[i, j]);
                    blockObjects[i, j] = null;
                }
            }
        }
        for (int i = 0; i < mapData.Size.x; i++)
        {
            for (int j = 0; j < mapData.Size.y; j++)
            {
                if (mapData.HasBlock(i, j))
                {
                    blockObjects[i, j] = blockPool.Pop();
                    MoveBlock(i, j);
                }
            }
        }
    }
}
