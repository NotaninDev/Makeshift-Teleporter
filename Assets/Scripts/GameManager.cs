using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// stores information needed across multiple scenes
public class GameManager : MonoBehaviour
{
    public static string previousScene = "", currentScene = "";
    public static int level, eventNumber;
}
