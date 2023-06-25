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
    public Vector2Int Target { get; set; }

    public bool[,] Walls;

    public enum BlockShape { None, Corner, Up, Right, UpRight, Crumb }
    public BlockShape[,] BlockShapes;

    public enum Direction { Up, Right, Down, Left }
    private static Vector2Int[] directionDictionary;

    // MicroHistory is to store data about what happens in each execution of game logic within a turn
    public class MicroHistory
    {
        public bool[,] Moved, Broke;
        public MicroHistory(Vector2Int Size)
        {
            Moved = new bool[Size.x, Size.y];
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
                    BlockShapes = new BlockShape[Size.x, Size.y];
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
                    BlockShapes = new BlockShape[Size.x, Size.y];
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
        clone.BlockShapes = (BlockShape[,])BlockShapes.Clone();
        return clone;
    }
    public void Reset(MapData initialState)
    {
        Player = initialState.Player;
        BlockShapes = (BlockShape[,])initialState.BlockShapes.Clone();
    }
    public bool Win()
    {
        return Player == Target;
    }

    public bool Move(Direction direction)
    {
        TurnHistory = new Queue<MicroHistory>();
        Vector2Int targetPosition = Player + directionDictionary[(int)direction];
        if (!InMap(targetPosition) || Walls[targetPosition.x, targetPosition.y]) return false;

        Player = targetPosition;
        for (int i = 0; i < Size.x; i++)
        {
            for (int j = 0; j < Size.y; j++)
            {
                if (BlockShapes[i, j] == BlockShape.Crumb) BlockShapes[i, j] = BlockShape.None;
            }
        }
        return true;
    }
    public bool InMap(Vector2Int coordinates)
    {
        return coordinates.x >= 0 && coordinates.y >= 0 && coordinates.x < Size.x && coordinates.y < Size.y;
    }
    public bool HasBlock(int x, int y)
    {
        return InMap(new Vector2Int(x, y)) && (BlockShapes[x, y] == BlockShape.Corner || BlockShapes[x, y] == BlockShape.Up || BlockShapes[x, y] == BlockShape.Right || BlockShapes[x, y] == BlockShape.UpRight);
    }
    public bool HasBlock(Vector2Int coordinates)
    {
        return HasBlock(coordinates.x, coordinates.y);
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
            BlockShapes = new BlockShape[Size.x, Size.y];
        }
        else
        {
            Debug.LogWarning($"LoadMapData-size: invalid value {data} in level {tag}");
            return false;
        }

        // read map data
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
            for (int i = 0; i < Size.x; i++)
            {
                switch (Char.ToLower(data[i]))
                {
                    case '#':
                        Walls[i, j] = true;
                        break;
                    case '.':
                        Walls[i, j] = false;
                        break;
                    case 'p':
                        if (playerLoaded)
                        {
                            Debug.LogWarning($"LoadMapData: player is defined twice; line {count} in level {tag}");
                            return false;
                        }
                        Walls[i, j] = false;
                        Player = new Vector2Int(i, j);
                        playerLoaded = true;
                        break;
                    case '@':
                        if (targetLoaded)
                        {
                            Debug.LogWarning($"LoadMapData: target is defined twice; line {count} in level {tag}");
                            return false;
                        }
                        Walls[i, j] = false;
                        Target = new Vector2Int(i, j);
                        targetLoaded = true;
                        break;
                    default:
                        Debug.LogWarning($"LoadMapData: invalid card type \'{data[i]}\' ({Size.y - 1 - j}, {i}) at line {count} in level {tag}");
                        return false;
                }
            }
        }

        if (playerLoaded && targetLoaded) return true;
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
