using System.Collections;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using UnityEngine;
using String = System.String;

public class MapData
{
    private static Dictionary<string, string> levelNames;
    private static List<string> levelTags;

    // MaxRow, MaxColumn are not used
    public const int MaxRow = 18, MaxColumn = 32;
    public string LevelTag { get; private set; }
    public Vector2Int Size { get; private set; }
    public Vector2Int Player { get; set; }
    public Vector2Int Target { get; private set; }

    public bool[,] Walls;

    public enum BlockType { None, Block, Crumb }
    public BlockType[,] Blocks;
    public int[,] BlockConnection;
    public static int EncodeConnection(bool[] connected)
    {
        if (connected.Length != 4)
        {
            Debug.LogWarning($"MapData.EncodeConnection: connected is length {connected.Length}");
            return 0;
        }
        int code = 0;
        for (int i = 0; i < connected.Length; i++)
        {
            code <<= 1;
            code |= connected[i] ? 1 : 0;
        }
        return code;
    }
    public static int EncodeConnection(bool up, bool right, bool down, bool left)
    {
        bool[] connected = new bool[4];
        connected[0] = up;
        connected[1] = right;
        connected[2] = down;
        connected[3] = left;
        return EncodeConnection(connected);
    }
    public static bool[] DecodeConnection(int code)
    {
        bool[] connected = new bool[4];
        for (int i = 3; i >= 0; i--)
        {
            connected[i] = (code & 1) != 0;
            code >>= 1;
        }
        return connected;
    }

    public enum Direction { Up, Right, Down, Left }
    private static Vector2Int[] directionDictionary;

    // MicroHistory is to store data about what happens in each execution of game logic within a turn
    public bool BlockGroupMoved { get; private set; }
    public bool[,] BlockMoved;
    public class MicroHistory
    {
        public Vector2Int JumpPoint { get; set; }
        public bool[,] Broke;
        public MicroHistory(Vector2Int Size)
        {
            Broke = new bool[Size.x, Size.y];
        }
    }
    public Queue<MicroHistory> TurnHistory { get; private set; }


    static MapData()
    {
        directionDictionary = new Vector2Int[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
    }

    public void Initialize(string tag)
    {
        LevelTag = tag;

        bool success;
        success = LoadMapData(tag);
        if (!success)
        {
            switch (tag)
            {
                case "debugalpha":
                    Size = new Vector2Int(8, 6);
                    Player = new Vector2Int(6, 2);
                    Target = new Vector2Int(0, 5);
                    Walls = new bool[Size.x, Size.y];
                    Blocks = new BlockType[Size.x, Size.y];
                    BlockConnection = new int[Size.x, Size.y];
                    for (int i = 1; i < 2; i++)
                    {
                        for (int j = 1; j < 5; j++)
                        {
                            Walls[i, j] = true;
                        }
                    }
                    break;

                case "debug":
                default:
                    if (tag != "debug")
                    {
                        Debug.LogWarning($"Initialize: level {tag} is not pre-defined");
                    }
                    Size = new Vector2Int(8, 6);
                    Player = new Vector2Int(3, 1);
                    Target = new Vector2Int(7, 0);
                    Walls = new bool[Size.x, Size.y];
                    Blocks = new BlockType[Size.x, Size.y];
                    BlockConnection = new int[Size.x, Size.y];
                    for (int i = 4; i < 7; i++)
                    {
                        for (int j = 1; j < 3; j++)
                        {
                            Walls[i, j] = true;
                        }
                    }
                    break;
            }
        }

        if (!InMap(Player))
        {
            Debug.LogWarning($"MapData.Initialize: start position {Player} is out of the map in level {tag}");
            Player = Vector2Int.zero;
        }
    }
    public MapData Clone()
    {
        MapData clone = (MapData)this.MemberwiseClone();
        clone.Walls = (bool[,])Walls.Clone();
        clone.Blocks = (BlockType[,])Blocks.Clone();
        clone.BlockConnection = (int[,])BlockConnection.Clone();
        return clone;
    }
    public void Reset(MapData initialState)
    {
        Player = initialState.Player;
        Blocks = (BlockType[,])initialState.Blocks.Clone();
        BlockConnection = (int[,])initialState.BlockConnection.Clone();
    }
    public bool Win()
    {
        return Player == Target;
    }

    public bool Move(Direction direction, out bool teleported)
    {
        BlockGroupMoved = false;
        BlockMoved = new bool[Size.x, Size.y];
        TurnHistory = new Queue<MicroHistory>();
        teleported = false;
        Vector2Int targetPosition = Player + directionDictionary[(int)direction];
        if (!InMap(targetPosition) || Walls[targetPosition.x, targetPosition.y]) return false;

        if (HasBlock(Player))
        {
            bool[,] used = new bool[Size.x, Size.y];
            HashSet<Vector2Int> group = GetConnectedBlocks(Player, used);
            if (GroupIsBlocked(group, direction, used))
            {
                // check if player can get out
                if (!HasBlock(targetPosition))
                {
                    Player = targetPosition;
                    RemoveCrumbs();
                    return true;
                }
            }
            else
            {
                // move the blocks
                BlockGroupMoved = true;
                int[,] blockBuffer = new int[Size.x, Size.y];
                foreach (Vector2Int coordinates in group)
                {
                    BlockMoved[coordinates.x, coordinates.y] = true;
                    blockBuffer[coordinates.x, coordinates.y] = BlockConnection[coordinates.x, coordinates.y];
                    Blocks[coordinates.x, coordinates.y] = BlockType.None;
                    BlockConnection[coordinates.x, coordinates.y] = 0;
                }
                foreach (Vector2Int coordinates in group)
                {
                    Vector2Int blockTargetPosition = coordinates + directionDictionary[(int)direction];
                    Blocks[blockTargetPosition.x, blockTargetPosition.y] = BlockType.Block;
                    BlockConnection[blockTargetPosition.x, blockTargetPosition.y] = blockBuffer[coordinates.x, coordinates.y];
                }

                Player = targetPosition;
                RemoveCrumbs();

                while (Teleport()) { teleported = true; }

                return true;
            }
        }
        else
        {
            Player = targetPosition;
            RemoveCrumbs();
            while (Teleport()) { teleported = true; }
            return true;
        }
        return false;
    }
    public bool InMap(int x, int y)
    {
        return x >= 0 && y >= 0 && x < Size.x && y < Size.y;
    }
    public bool InMap(Vector2Int coordinates)
    {
        return coordinates.x >= 0 && coordinates.y >= 0 && coordinates.x < Size.x && coordinates.y < Size.y;
    }
    public bool HasBlock(int x, int y)
    {
        return InMap(new Vector2Int(x, y)) && Blocks[x, y] == BlockType.Block;
    }
    public bool HasBlock(Vector2Int coordinates)
    {
        return HasBlock(coordinates.x, coordinates.y);
    }

    // returns if the block at `coordinates` is connected to the block in `direction`
    public bool IsConnected(Vector2Int coordinates, Direction direction)
    {
        if (!HasBlock(coordinates)) return false;
        int x = coordinates.x,
            y = coordinates.y;
        return DecodeConnection(BlockConnection[x, y])[(int)direction];
    }

    private HashSet<Vector2Int> GetConnectedBlocks(Vector2Int coordinates, bool[,] used, bool ignoreFrame = false)
    {
        HashSet<Vector2Int> group = new HashSet<Vector2Int>();
        Queue<Vector2Int> unvisited = new Queue<Vector2Int>();
        unvisited.Enqueue(coordinates);

        while (unvisited.Count > 0)
        {
            Vector2Int next = unvisited.Dequeue();
            group.Add(next);
            used[next.x, next.y] = true;
            for (int i = 0; i < 4; i++)
            {
                Vector2Int target = next + directionDictionary[i];
                if (!InMap(target) || used[target.x, target.y]) continue;
                if (ignoreFrame)
                {
                    if (HasBlock(target)) unvisited.Enqueue(target);
                }
                else
                {
                    if (IsConnected(next, (Direction)i)) unvisited.Enqueue(target);
                }
            }
        }
        return group;
    }

    private bool GroupIsBlocked(HashSet<Vector2Int> group, Direction direction, bool[,] inGroup)
    {
        foreach (Vector2Int coordinates in group)
        {
            Vector2Int targetPosition = coordinates + directionDictionary[(int)direction];
            if (!InMap(targetPosition) || Walls[targetPosition.x, targetPosition.y] || HasBlock(targetPosition) && !inGroup[targetPosition.x, targetPosition.y])
            {
                return true;
            }
        }
        return false;
    }

    private bool Teleport()
    {
        if (!HasBlock(Player)) return false;

        bool[,] used = new bool[Size.x, Size.y];
        HashSet<Vector2Int> origin, destination = null;
        origin = GetConnectedBlocks(Player, used, ignoreFrame: true);

        bool foundSameShape = false;
        for (int i = 0; i < Size.x; i++)
        {
            for (int j = 0; j < Size.y; j++)
            {
                if (!HasBlock(i, j) || used[i, j]) continue;
                HashSet<Vector2Int> tempSet = GetConnectedBlocks(new Vector2Int(i, j), used, ignoreFrame: true);
                if (AreSameShape(origin, tempSet))
                {
                    if (foundSameShape) return false;
                    destination = tempSet;
                    foundSameShape = true;
                }
            }
        }
        if (!foundSameShape) return false;

        MicroHistory microHistory = new MicroHistory(Size);

        // get blocks that break
        used = new bool[Size.x, Size.y];
        HashSet<Vector2Int> originPart;
        originPart = GetConnectedBlocks(Player, used, ignoreFrame: false);
        foreach (Vector2Int coordinates in originPart)
        {
            microHistory.Broke[coordinates.x, coordinates.y] = true;
            Blocks[coordinates.x, coordinates.y] = BlockType.Crumb;
            BlockConnection[coordinates.x, coordinates.y] = 0;
        }

        Vector2Int bottomLeftOrigin, bottomLeftDestination;
        bottomLeftOrigin = GetBottomLeft(origin);
        bottomLeftDestination = GetBottomLeft(destination);
        microHistory.JumpPoint = Player + (bottomLeftDestination - bottomLeftOrigin);
        Player = microHistory.JumpPoint;
        TurnHistory.Enqueue(microHistory);
        return true;
    }
    private bool AreSameShape(HashSet<Vector2Int> shape1, HashSet<Vector2Int> shape2)
    {
        if (shape1.Count != shape2.Count) return false;
        Vector2Int bottomLeft1, bottomLeft2;
        bottomLeft1 = GetBottomLeft(shape1);
        bottomLeft2 = GetBottomLeft(shape2);

        // check for identity
        foreach (Vector2Int coordinates in shape1)
        {
            if (!shape2.Contains(coordinates + (bottomLeft2 - bottomLeft1)))
            {
                return false;
            }
        }
        return true;
    }

    // Get the bottom left point in shape
    // Only works if all points of shape are in the map
    private Vector2Int GetBottomLeft(HashSet<Vector2Int> shape)
    {
        int left = Size.x, bottom = Size.y;
        foreach (Vector2Int coordinates in shape)
        {
            if (coordinates.x < left) left = coordinates.x;
            if (coordinates.y < bottom) bottom = coordinates.y;
        }
        return new Vector2Int(left, bottom);
    }

    private void RemoveCrumbs()
    {
        for (int i = 0; i < Size.x; i++)
        {
            for (int j = 0; j < Size.y; j++)
            {
                if (Blocks[i, j] == BlockType.Crumb) Blocks[i, j] = BlockType.None;
            }
        }
    }

    public static void LoadLevelNames()
    {
        Match lines = ExtractLines("Maps/level names");
        levelNames = new Dictionary<String, String>();
        levelTags = new List<String>();
        string line;
        int count = 0;
        Match match;
        string dataTag, data;
        while (lines.Success)
        {
            line = ReadNonEmptyLine(ref lines, ref count);
            if (line == null) break;

            // level name
            match = Regex.Match(line, "^\\s*([0-9a-z]+)\\s*:\\s*(\\S(?:.*\\S)?)\\s*$", RegexOptions.Compiled);
            if (!match.Success)
            {
                Debug.LogWarning(String.Format("LoadLevelNames: invalid level name format at line {0}", count));
                return;
            }
            dataTag = match.Groups[1].Value;
            data = match.Groups[2].Value;
            levelNames.Add(dataTag, data);
            levelTags.Add(dataTag);
        }
    }
    public static int GetTagIndex(string tag)
    {
        return levelTags.IndexOf(tag);
    }
    public static string GetLevelTag(int level)
    {
        if (level >= 0 && level < levelTags.Count) return levelTags[level];
        else return (level % 2 == 0) ? "debugalpha" : "debug";
    }
    public static string GetLevelName(string tag)
    {
        try
        {
            return levelNames[tag];
        }
        catch (KeyNotFoundException)
        {
            Debug.LogWarning($"GetLevelName: no assigned name for level tag {tag}");
            return $"untitled: {tag}";
        }
    }

    private enum BlockShape { None, Corner, Up, Right, UpRight }

    // load map from a text file
    // returns if loading succeeded
    private bool LoadMapData(string tag)
    {
        bool playerLoaded = false, targetLoaded = false;

        Match lines = ExtractLines("Maps/" + tag);
        string line;
        int count = 0;
        bool success;

        Match match;
        string dataTag, data;
        Vector2Int tempVector2;

        // read size
        line = ReadNonEmptyLine(ref lines, ref count);
        if (line == null)
        {
            Debug.LogWarning($"LoadMapData: no text detected");
            return false;
        }
        match = Regex.Match(line, @"^\s*(\w+)\s*:\s*((?:\S(?:.*\S)?)?)\s*$", RegexOptions.Compiled);
        if (!match.Success)
        {
            Debug.LogWarning($"LoadMapData: invalid map file format at line {count} for level {tag}");
            return false;
        }
        dataTag = match.Groups[1].Value;
        data = match.Groups[2].Value;
        if (dataTag.ToLower() != "size")
        {
            Debug.LogWarning($"LoadMapData-size: tag {dataTag} is wrong");
            return false;
        }
        tempVector2 = ParseVector2Int(data, out success);
        if (success)
        {
            Size = tempVector2;
            Walls = new bool[Size.x, Size.y];
        }
        else
        {
            Debug.LogWarning($"LoadMapData-size: invalid value {data} in level {tag}");
            return false;
        }

        // read map data
        BlockShape[,] blockShapes = new BlockShape[Size.x, Size.y];
        Blocks = new BlockType[Size.x, Size.y];
        for (int j = Size.y - 1; j >= 0; j--)
        {
            line = ReadNonEmptyLine(ref lines, ref count);
            if (line == null)
            {
                Debug.LogWarning($"LoadMapData-map: {Size.y - j} lines missing");
                return false;
            }
            match = Regex.Match(line, @"^\s*(\S+)\s*$", RegexOptions.Compiled);
            if (!match.Success)
            {
                Debug.LogWarning($"LoadMapData: invalid map file format at line {count} for level {tag}");
                return false;
            }

            data = match.Groups[1].Value;
            if (data.Length != Size.x)
            {
                Debug.LogWarning($"LoadMapData: text length {data.Length} at line {count} is wrong for level {tag}");
                return false;
            }
            bool rightLock = false;
            for (int i = 0; i < Size.x; i++)
            {
                char c = Char.ToLower(data[i]);

                // check if '>' or 'l' is connected to a blcok on the right
                if (rightLock)
                {
                    switch (c)
                    {
                        case 'o':
                        case '>':
                        case '^':
                        case 'l':
                            rightLock = false;
                            break;

                        default:
                            Debug.LogWarning($"LoadMapData: invalid connection \'{Char.ToLower(data[i - 1])}\' at {i - 1}, {j}; line {count} in level {tag}");
                            return false;
                    }
                }

                switch (c)
                {
                    case '#':
                        Walls[i, j] = true;
                        break;
                    case '.':
                        break;
                    case 'p':
                        if (playerLoaded)
                        {
                            Debug.LogWarning($"LoadMapData: player is defined twice; line {count} in level {tag}");
                            return false;
                        }
                        Player = new Vector2Int(i, j);
                        playerLoaded = true;
                        break;
                    case '@':
                        if (targetLoaded)
                        {
                            Debug.LogWarning($"LoadMapData: target is defined twice; line {count} in level {tag}");
                            return false;
                        }
                        Target = new Vector2Int(i, j);
                        targetLoaded = true;
                        break;

                    case 'o':
                        Blocks[i, j] = BlockType.Block;
                        blockShapes[i, j] = BlockShape.Corner;
                        break;
                    case '>':
                        if (i == Size.x - 1)
                        {
                            Debug.LogWarning($"LoadMapData: \'{c}\' can't be at the right most column; line {count} in level {tag}");
                            return false;
                        }
                        Blocks[i, j] = BlockType.Block;
                        blockShapes[i, j] = BlockShape.Right;
                        rightLock = true;
                        break;
                    case '^':
                        if (j == Size.y - 1)
                        {
                            Debug.LogWarning($"LoadMapData: \'{c}\' can't be at the top row; line {count} in level {tag}");
                            return false;
                        }
                        if (!HasBlock(i, j + 1))
                        {
                            Debug.LogWarning($"LoadMapData: invalid connection \'{c}\' at {i}, {j}; line {count} in level {tag}");
                            return false;
                        }
                        Blocks[i, j] = BlockType.Block;
                        blockShapes[i, j] = BlockShape.Up;
                        break;
                    case 'l':
                        if (i == Size.x - 1)
                        {
                            Debug.LogWarning($"LoadMapData: \'{c}\' can't be at the right most column; line {count} in level {tag}");
                            return false;
                        }
                        if (j == Size.y - 1)
                        {
                            Debug.LogWarning($"LoadMapData: \'{c}\' can't be at the top row; line {count} in level {tag}");
                            return false;
                        }
                        if (!HasBlock(i, j + 1))
                        {
                            Debug.LogWarning($"LoadMapData: invalid connection \'{c}\' at {i}, {j}; line {count} in level {tag}");
                            return false;
                        }
                        Blocks[i, j] = BlockType.Block;
                        blockShapes[i, j] = BlockShape.UpRight;
                        rightLock = true;
                        break;

                    default:
                        Debug.LogWarning($"LoadMapData: invalid tile type \'{c}\' ({Size.y - 1 - j}, {i}) at line {count} in level {tag}");
                        return false;
                }
            }
        }

        if (playerLoaded && targetLoaded)
        {
            BlockConnection = new int[Size.x, Size.y];
            for (int i = 0; i < Size.x; i++)
            {
                for (int j = 0; j < Size.y; j++)
                {
                    if (Blocks[i, j] == BlockType.Block)
                    {
                        BlockShape shape = blockShapes[i, j];
                        bool up = shape == BlockShape.Up || shape == BlockShape.UpRight,
                            right = shape == BlockShape.Right || shape == BlockShape.UpRight,
                            down = InMap(i, j - 1) && (blockShapes[i, j - 1] == BlockShape.Up || blockShapes[i, j - 1] == BlockShape.UpRight),
                            left = InMap(i - 1, j) && (blockShapes[i - 1, j] == BlockShape.Right || blockShapes[i - 1, j] == BlockShape.UpRight);
                        BlockConnection[i, j] = EncodeConnection(up, right, down, left);
                    }
                }
            }
            return true;
        }
        else
        {
            List<string> missingData = new List<String>();
            if (!playerLoaded) missingData.Add("player");
            if (!targetLoaded) missingData.Add("target");
            if (missingData.Count > 0) Debug.LogWarning($"LoadMapData: these data are missing in level {tag}: {String.Join(", ", missingData)}");
            return false;
        }
    }

    // returns in success if parsing succeeded
    private static Vector2Int ParseVector2Int(string text, out bool success, bool suppressWarning = false)
    {
        Match match = Regex.Match(text, @"^([+-]?\d{1,3})\s*,\s*([+-]?\d{1,3})$", RegexOptions.Compiled);
        if (!match.Success)
        {
            if (!suppressWarning) Debug.LogWarning($"ParseVector2Int: invalid format \"{text}\"");
            success = false;
            return new Vector2Int();
        }
        success = true;
        return new Vector2Int(Int32.Parse(match.Groups[1].Value), Int32.Parse(match.Groups[2].Value));
    }
    // returns in success if parsing succeeded
    private static Vector3Int ParseVector3Int(string text, out bool success, bool suppressWarning = false)
    {
        Match match = Regex.Match(text, @"^([+-]?\d{1,3})\s*,\s*([+-]?\d{1,3})\s*,\s*([+-]?\d{1,3})$", RegexOptions.Compiled);
        if (!match.Success)
        {
            if (!suppressWarning) Debug.LogWarning($"ParseVector3Int: invalid format \"{text}\"");
            success = false;
            return new Vector3Int();
        }
        success = true;
        return new Vector3Int(Int32.Parse(match.Groups[1].Value), Int32.Parse(match.Groups[2].Value), Int32.Parse(match.Groups[3].Value));
    }
    // returns in success if parsing succeeded
    // get 2 corners on a diagonal of a rectangle
    // in the form of "rect x0, y0, x1, y1"
    private static void ParseRectangle(string text, out Vector2Int vector0, out Vector2Int vector1, out bool success)
    {
        Match match = Regex.Match(text,
            @"^(?i:rect)\s*([+-]?\d{1,3})\s*,\s*([+-]?\d{1,3})\s*,\s*([+-]?\d{1,3})\s*,\s*([+-]?\d{1,3})$", RegexOptions.Compiled);
        if (!match.Success)
        {
            Debug.LogWarning($"ParseRectangle: invalid format \"{text}\"");
            success = false;
            vector0 = vector1 = new Vector2Int();
            return;
        }
        success = true;
        vector0 = new Vector2Int(Int32.Parse(match.Groups[1].Value), Int32.Parse(match.Groups[2].Value));
        vector1 = new Vector2Int(Int32.Parse(match.Groups[3].Value), Int32.Parse(match.Groups[4].Value));
        if (vector0.x > vector1.x || vector0.y > vector1.y)
        {
            Debug.LogWarning($"ParseRectangle: wrong corners \"{text}\"");
            success = false;
            vector0 = vector1 = new Vector2Int();
            return;
        }
        return;
    }
    // returns in success if parsing succeeded
    // get 2 corners on a diagonal of a rectangle and an additional integer
    // in the form of "rect x0, y0, x1, y1, d"
    private static void ParseRectangle(string text, out Vector2Int vector0, out Vector2Int vector1, out int d, out bool success)
    {
        Match match = Regex.Match(text,
            @"^(?i:rect)\s*([+-]?\d{1,3})\s*,\s*([+-]?\d{1,3})\s*,\s*([+-]?\d{1,3})\s*,\s*([+-]?\d{1,3}),\s*([+-]?\d{1,3})$",
            RegexOptions.Compiled);
        if (!match.Success)
        {
            Debug.LogWarning($"ParseRectangle: invalid format \"{text}\"");
            success = false;
            vector0 = vector1 = new Vector2Int();
            d = 0;
            return;
        }
        success = true;
        vector0 = new Vector2Int(Int32.Parse(match.Groups[1].Value), Int32.Parse(match.Groups[2].Value));
        vector1 = new Vector2Int(Int32.Parse(match.Groups[3].Value), Int32.Parse(match.Groups[4].Value));
        d = Int32.Parse(match.Groups[5].Value);
        if (vector0.x > vector1.x || vector0.y > vector1.y)
        {
            Debug.LogWarning($"ParseRectangle: wrong corners \"{text}\"");
            success = false;
            vector0 = vector1 = new Vector2Int();
            d = 0;
            return;
        }
        return;
    }

    // extract lines from the .txt file
    private static Match ExtractLines(string path)
    {
        string fileText;
        TextAsset textFile = Resources.Load<TextAsset>(path);
        if (textFile == null)
        {
            Debug.LogWarning($"LoadMapData: failed to load file {path}");
            fileText = "";
        }
        else fileText = textFile.text;

        return Regex.Match(fileText, "^(.*?)$", RegexOptions.Multiline | RegexOptions.Compiled);
    }
    // comment lines count as empty
    // returns null if there's no non-empty line
    private static string ReadNonEmptyLine(ref Match match, ref int count)
    {
        string line;
        while (match.Success)
        {
            count++;
            line = match.Value;
            match = match.NextMatch();
            if (!String.IsNullOrWhiteSpace(line))
            {
                Match commentMatch = Regex.Match(line, @"^\s*//", RegexOptions.Compiled);
                if (!commentMatch.Success) return line;
            }
        }
        return null;
    }
    // check if the next line is end line
    // if not, it does not move match
    private static bool CheckEndLine(Match match, ref int count)
    {
        string line;
        Match tempMatch = match, endMatch;
        int tempCount = count;
        line = ReadNonEmptyLine(ref match, ref count);
        if (line == null) return false;
        endMatch = Regex.Match(line, @"^\s*end\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        if (!endMatch.Success)
        {
            match = tempMatch;
            count = tempCount;
        }
        return endMatch.Success;
    }
}
