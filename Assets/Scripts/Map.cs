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
    private Dictionary<Vector2Int, GameObject> blocks;

    private const float MoveTime = .1f, StuckTime = .1f;
    private const float StuckScale = 1f / StuckTime / StuckTime;
    private IEnumerator moveAnimation;

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
        blocks = new Dictionary<Vector2Int, GameObject>();
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
                    GameObject blockObject = General.AddChild(blockParent, $"Block ({i}, {j})");
                    blocks[coordinates] = blockObject;
                    Block block = blockObject.AddComponent<Block>();
                    block.Initialize(mapData.IsConnected(coordinates, MapData.Direction.Up), mapData.IsConnected(coordinates, MapData.Direction.Right), mapData.IsConnected(coordinates, MapData.Direction.Down), mapData.IsConnected(coordinates, MapData.Direction.Left));
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
        moveAnimation = stuck ? AnimateStuck() : Graphics.Move(playerObject, Get3DPoint(previousMap.Player), Get3DPoint(mapData.Player), MoveTime);
        if (moveAnimation != null) StartCoroutine(moveAnimation);
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

    public void StopAnimation()
    {
        if (moveAnimation != null) StopCoroutine(moveAnimation);

        // update objects' position
        MovePlayer();
    }
}
