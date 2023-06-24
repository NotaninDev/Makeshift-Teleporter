using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    private const int row = 4, column = 8, squareCount = row * column;
    private const string FirstScene = "TitleScene";
    private static GameObject canvas, squareParent;
    private static GameObject[] squareObjects;
    private static Image[] squareImages;

    private static GameObject borderObject;

    private static bool loading = false;
    public static bool Loading { get { return loading; } }

    public class StringEvent : UnityEvent<string> { }
    public static StringEvent sceneEvent;

    void Awake()
    {
        // find canvas
        canvas = GameObject.FindWithTag("Scene Loader");
        if (canvas == null)
        {
            Debug.LogWarning("SceneLoader.Awake: Scene Loader Canvas not found");
            canvas = General.AddChild(gameObject, "Scene Loader Canvas");
        }

        // initialize squares for scene transition
        squareParent = General.AddChild(canvas, "Square Parent");
        RectTransform squareRect = squareParent.AddComponent<RectTransform>();
        squareRect.anchorMin = Vector2.zero;
        squareRect.anchorMax = Vector2.one;
        squareRect.sizeDelta = Vector2.zero;
        squareObjects = new GameObject[squareCount];
        squareImages = new Image[squareCount];
        for (int i = 0; i < row; i++)
        {
            for (int j = 0; j < column; j++)
            {
                int k = i * column + j;
                squareObjects[k] = General.AddChild(squareParent, $"Square {k}");
                squareImages[k] = squareObjects[k].AddComponent<Image>();
            }
        }
        loading = false;

        borderObject = GameObject.Find("Border Manager");
        if (borderObject == null)
        {
            Debug.LogWarning("SceneLoader.Awake: Border Manager not found");
        }

        sceneEvent = new StringEvent();
        sceneEvent.AddListener(LoadNextScene);
        Keyboard.Initialize();
        StartCoroutine(LoadFirstScene());
    }

    void Start()
    {
        for (int i = 0; i < row; i++)
        {
            for (int j = 0; j < column; j++)
            {
                int k = i * column + j;
                squareImages[k].color = Graphics.Brown;
                squareImages[k].rectTransform.anchorMin = squareImages[k].rectTransform.anchorMax = new Vector2(1f / column * (j + .5f), 1f / row * (i + .5f));
            }
        }
        squareParent.SetActive(false);
    }

    private IEnumerator LoadFirstScene()
    {
        loading = true;
        GameManager.currentScene = FirstScene;
        SceneManager.LoadScene(FirstScene, LoadSceneMode.Additive);
        yield return null;
        Camera.main.gameObject.SetActive(false);
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(FirstScene));
        loading = false;
    }

    public void LoadNextScene(string sceneName)
    {
        if (loading)
        {
            Debug.LogWarning("LoadNextScene: loading another scene now");
            return;
        }
        loading = true;
        StartCoroutine(_LoadNextScene(sceneName));
    }
    private IEnumerator _LoadNextScene(string sceneName)
    {
        Coroutine[] coroutines = new Coroutine[squareCount * 2];
        UnityEngine.AsyncOperation asyncLoad, asyncUnload;

        squareParent.SetActive(true);
        for (int i = 0; i < row; i++)
        {
            for (int j = 0; j < column; j++)
            {
                int k = i * column + j;
                float delay = .06f * (row - 1 - i) + .04f * j;
                squareImages[k].rectTransform.sizeDelta = Vector2.one * (float)Math.Max((float)Screen.width / canvas.transform.localScale.x / column, (float)Screen.height / canvas.transform.localScale.y / row) * 1.01f;
                squareObjects[k].transform.localScale = Vector3.zero;
                StartCoroutine(Graphics.Resize(squareObjects[k], Vector3.zero, Vector3.one, duration: .5f, delay: delay));
            }
        }
        yield return new WaitForSeconds(.5f + .06f * row + .04f * column);

        // load and unload scenes
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.GetSceneByName("BorderScene"));
        GameManager.previousScene = GameManager.currentScene;
        asyncUnload = SceneManager.UnloadSceneAsync(activeScene);
        asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        GameManager.currentScene = sceneName;
        while (!asyncLoad.isDone || !asyncUnload.isDone) { yield return null; }
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));

        // (de)activate border manager
        switch (sceneName)
        {
            case "MainScene":
                borderObject.SetActive(false);
                break;

            default:
                borderObject.SetActive(true);
                break;
        }

        for (int i = 0; i < row; i++)
        {
            for (int j = 0; j < column; j++)
            {
                int k = i * column + j;
                float delay = .06f * (row - 1 - i) + .04f * j;
                StartCoroutine(Graphics.Resize(squareObjects[k], Vector3.one, Vector3.zero, duration: .5f, delay: delay));
            }
        }
        yield return new WaitForSeconds(.5f + .06f * row + .04f * column);
        squareParent.SetActive(false);
        loading = false;
    }
}
