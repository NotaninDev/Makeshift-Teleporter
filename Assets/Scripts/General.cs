using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;
using String = System.String;
using Random = System.Random;

public class General : MonoBehaviour
{
    public static Random rand;

    // Progress is a list of level tags of all solved puzzles
    public static List<string> Progress { get; private set; }
    public static string LastPlayedLevel { get; private set; }

    void Awake()
    {
        rand = new Random();
        MapData.LoadLevelNames();

        List<string> progress;
        string lastPlayedLevel;
        SaveFile.LoadProgress(out progress, out lastPlayedLevel);
        Progress = progress;
        LastPlayedLevel = lastPlayedLevel;
    }

    public static GameObject AddChild(GameObject parent, string name = null)
    {
        GameObject child = new GameObject();
        child.transform.parent = parent.transform;
        child.transform.localScale = Vector3.one;
        child.transform.localPosition = Vector3.zero;
        if (name != null && name.Length > 0)
        {
            child.name = name;
        }
        return child;
    }
    public static GameObject InstantiateChild(GameObject prefab, GameObject parent, string name = null)
    {
        GameObject child = Instantiate(prefab, parent.transform);
        child.transform.localScale = Vector3.one;
        child.transform.localPosition = Vector3.zero;
        if (name != null && name.Length > 0)
        {
            child.name = name;
        }
        return child;
    }

    public static void Shuffle<T>(T[] array, int start = 0, int end = -1)
    {
        int originalEnd = end;
        if (end == -1)
        {
            end = array.Length;
        }
        if (start < 0 || start > end || end > array.Length)
        {
            Debug.LogWarning("Shuffle: index out of range");
            Debug.LogWarning(String.Format("length: {0}, start: {1}, end: {2}", array.Length, start, originalEnd));
            return;
        }
        for (int i = start; i < end; i++)
        {
            int n = rand.Next(i, end);
            T temp = array[n];
            array[n] = array[i];
            array[i] = temp;
        }
    }

    public static bool GetMouseHover(Transform transform)
    {
        Vector2 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D[] hits = Physics2D.RaycastAll(pos, Vector2.zero, 0.01f);
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.transform == transform) { return true; }
        }
        return false;
    }

    public static IEnumerator WaitEvent(UnityEvent unityEvent, float delay)
    {
        yield return new WaitForSeconds(delay);
        unityEvent.Invoke();
    }

    public static void AddSolvedLevel(string tag)
    {
        if (!Progress.Contains(tag))
        {
            Progress.Add(tag);
            SaveFile.SaveProgress(Progress, LastPlayedLevel);
        }
    }

    public static void UpdateLastPlayedLevel(string tag)
    {
        if (LastPlayedLevel != tag)
        {
            LastPlayedLevel = tag;
            SaveFile.SaveProgress(Progress, LastPlayedLevel);
        }
    }
}
