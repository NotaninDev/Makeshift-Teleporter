using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class Graphics : MonoBehaviour
{

    private static Graphics instance = null;
    [SerializeField]
    private bool isBorderGraphics = false;

    private static float width = 0, height = 0;
    public static float Width { get { return width; } }
    public static float Height { get { return height; } }
    public static Color32 Red, Blue, Brown, LightBrown, DarkBrown, WhiteBrown, BlackBrown, Green, LightGreen, DarkGreen, WhiteGreen,
        Pink, LightPink, DarkPink, WhitePink, BlackPink, CheckGreen, WhitePurple, White, Gray, DarkGray, SemiTransparent;

    public const float ScreenRatio = 9f / 16f;
    private static GameObject masterArea;
    private static GameObject background;
    private static SpriteRenderer backgroundSpriteRenderer;

    public enum Background
    {
        Dark,
        Debug
    }
    public enum FontName
    {
        Recurso,
        Mops,
        Sniglet
    }

    public static Sprite[] optionBox, player, tile, arrow, checkbox;
    public static Sprite plainWhite, credits, logo;
    public static Sprite[] backgroundSprites;

    public static TMP_FontAsset[] fonts;
    public static Font[] rawFonts;

    private static AsyncOperationHandle<GameObject>[] prefabOperations, mapPrefabOperations;
    public static GameObject ButtonPrefab { get; private set; }
    public static GameObject TextUIPrefab { get; private set; }
    public static GameObject[] MapPrefabs;

    void Awake()
    {
        if (isBorderGraphics)
        {
            if (instance == null) { instance = this; }
        }

        if (!isBorderGraphics)
        {
            masterArea = GameObject.FindWithTag("Master Area");
            if (masterArea == null)
            {
                throw new Exception("Master Area not found.");
            }
        }

        // set screen size
        if (!isBorderGraphics)
        {
            height = Camera.main.orthographicSize * 2;
            width = height / ScreenRatio;
            if ((float)Screen.height / (float)Screen.width > ScreenRatio)
            {
                masterArea.transform.localScale = Vector3.one * ((float)Screen.width / Screen.height * ScreenRatio);
            }
            else masterArea.transform.localScale = Vector3.one;
        }

        // load resources and such
        if (isBorderGraphics)
        {
            // define colors
            Red = new Color32(236, 62, 118, 255);
            Blue = new Color32(59, 98, 167, 255);
            Brown = new Color32(210, 122, 47, 255);
            LightBrown = new Color32(209, 161, 116, 255);
            DarkBrown = new Color32(156, 89, 31, 255);
            WhiteBrown = new Color32(232, 209, 189, 255);
            BlackBrown = new Color32(64, 43, 24, 255);
            Green = new Color32(66, 128, 34, 255);
            LightGreen = new Color32(167, 219, 107, 255);
            DarkGreen = new Color32(62, 114, 36, 255);
            WhiteGreen = new Color32(195, 224, 162, 255);
            Pink = new Color32(239, 57, 142, 255);
            LightPink = new Color32(230, 207, 230, 255);
            DarkPink = new Color32(196, 125, 196, 255);
            WhitePink = new Color32(239, 222, 239, 255);
            BlackPink = new Color32(114, 14, 114, 255);
            CheckGreen = new Color32(73, 183, 38, 255);
            WhitePurple = new Color32(173, 131, 201, 255);
            White = new Color32(209, 209, 209, 255);
            Gray = new Color32(159, 159, 159, 255);
            DarkGray = new Color32(94, 94, 94, 255);
            SemiTransparent = new Color32(17, 32, 53, 172);

            // load all sprites
            optionBox = LoadSprites("Option Box", 3);
            player = LoadSprites("Player", 2);
            tile = LoadSprites("Tile", 5);
            arrow = LoadSprites("Arrow", 4);
            checkbox = LoadSprites("Checkbox", 4);
            plainWhite = LoadSprite("Plain White");
            credits = LoadSprite("Credits");
            logo = LoadSprite("Logo");
            backgroundSprites = new Sprite[Enum.GetNames(typeof(Background)).Length];
            for (int i = 0; i < backgroundSprites.Length; i++) { backgroundSprites[i] = LoadSprite("BG_" + Enum.GetNames(typeof(Background))[i]); }

            // load fonts
            fonts = new TMP_FontAsset[Enum.GetNames(typeof(FontName)).Length];
            rawFonts = new Font[Enum.GetNames(typeof(FontName)).Length];
            for (int i = 0; i < fonts.Length; i++)
            {
                fonts[i] = Resources.Load<TMP_FontAsset>($"Fonts/{Enum.GetNames(typeof(FontName))[i]}");
                if (fonts[i] == null)
                {
                    Debug.LogWarning($"Awake: The font {Enum.GetNames(typeof(FontName))[i]} not found.");
                }
                rawFonts[i] = Resources.Load<Font>($"Fonts/{Enum.GetNames(typeof(FontName))[i]}_raw");
                if (rawFonts[i] == null)
                {
                    Debug.LogWarning($"Awake: The raw font {Enum.GetNames(typeof(FontName))[i]} not found.");
                }
            }

            // load TMPButton prefab
            prefabOperations = new AsyncOperationHandle<GameObject>[2];
            prefabOperations[0] = Addressables.LoadAssetAsync<GameObject>("Assets/AddressableAssets/Prefabs/TMPButton.prefab");
            prefabOperations[0].Completed += SetButtonPrefab;
            prefabOperations[1] = Addressables.LoadAssetAsync<GameObject>("Assets/AddressableAssets/Prefabs/TMPUI.prefab");
            prefabOperations[1].Completed += SetTextUIPrefab;

            // load map prefabs
            string[] mapPrefabLabels =
            {
                //"player"
            };
            int mapPrefabCount = mapPrefabLabels.Length;
            MapPrefabs = new GameObject[mapPrefabCount];
            mapPrefabOperations = new AsyncOperationHandle<GameObject>[mapPrefabCount];
            for (int i = 0; i < mapPrefabCount; i++)
            {
                mapPrefabOperations[i] = Addressables.LoadAssetAsync<GameObject>(mapPrefabLabels[i]);
                mapPrefabOperations[i].Completed += GetAdressableOperationHandler(i);
            }
        }

        // initialize the background
        if (!isBorderGraphics)
        {
            background = General.AddChild(gameObject, "Background");
            backgroundSpriteRenderer = background.AddComponent<SpriteRenderer>();
            backgroundSpriteRenderer.sortingLayerName = "Background";
        }
    }
    // load a sprite in Resources/Sprites/'path'
    // returns null if failed
    private static Sprite LoadSprite(string path)
    {
        Sprite sprite = Resources.Load<Sprite>("Sprites/" + path);
        if (sprite == null)
        {
            Debug.LogWarning($"LoadSprite: The sprite {path} not found.");
        }
        return sprite;
    }
    // load all sprites in Resources/Sprites/'path'
    // returns a null-filled array if failed
    private static Sprite[] LoadSprites(string path, int size)
    {
        bool loadingFailed = false;
        Sprite[] sprite = Resources.LoadAll<Sprite>("Sprites/" + path);
        if (sprite.Length != size)
        {
            Debug.LogWarning($"LoadSprites: The required sprite size was {size}, but the sprite {path} was size {sprite.Length}.");
            loadingFailed = true;
        }
        if (loadingFailed)
        {
            sprite = new Sprite[size];
        }
        return sprite;
    }
    // load a Material in Resources/Materials/'path'
    // returns null if failed
    private static Material LoadMaterial(string path)
    {
        Material material = Resources.Load<Material>("Materials/" + path);
        if (material == null)
        {
            Debug.LogWarning($"LoadMaterial: The material {path} not found.");
        }
        return material;
    }

    private void SetButtonPrefab(AsyncOperationHandle<GameObject> obj)
    {
        if (obj.Status == AsyncOperationStatus.Succeeded)
        {
            ButtonPrefab = obj.Result;
        }
        else
        {
            Debug.LogError($"Graphics.SetButtonPrefab: failed to load TMPButton prefab");
        }
    }

    private void SetTextUIPrefab(AsyncOperationHandle<GameObject> obj)
    {
        if (obj.Status == AsyncOperationStatus.Succeeded)
        {
            TextUIPrefab = obj.Result;
        }
        else
        {
            Debug.LogError($"Graphics.SetTextUIPrefab: failed to load TMPUI prefab");
        }
    }

    private Action<AsyncOperationHandle<GameObject>> GetAdressableOperationHandler(int i)
    {
        return delegate (AsyncOperationHandle<GameObject> obj)
        {
            if (obj.Status == AsyncOperationStatus.Succeeded)
            {
                MapPrefabs[i] = obj.Result;
            }
            else
            {
                Debug.LogError($"Graphics.GetAdressableOperationHandler-delegate: failed to load mapPrefab[{i}]");
            }
        };
    }

    void Start()
    {
        if (!isBorderGraphics)
        {
            background.SetActive(true);
            switch (GameManager.currentScene)
            {
                case "TitleScene":
                    SetBackground(Background.Dark);
                    break;
                case "MainScene":
                    SetBackground(Background.Dark);
                    background.SetActive(false);
                    break;
                case "TestScene":
                    SetBackground(Background.Dark);
                    break;
                default:
                    Debug.LogWarning($"Start: background is not defined for scene {GameManager.currentScene}");
                    SetBackground(Background.Dark);
                    break;
            }
        }
    }

    void OnDestroy()
    {
        if (isBorderGraphics)
        {
            Addressables.Release(prefabOperations[0]);
            Addressables.Release(prefabOperations[1]);
            for (int i = 0; i < mapPrefabOperations.Length; i++)
            {
                Addressables.Release(mapPrefabOperations[i]);
            }
        }
    }

    public static void SetResolution(int width, int height, bool fullscreen, RefreshRate refreshRate)
    {
        Screen.SetResolution(width, height, fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed, refreshRate);

        // change master area size
        if ((float)height / width > ScreenRatio)
        {
            masterArea.transform.localScale = Vector3.one * ((float)width / height * ScreenRatio);
        }
        else masterArea.transform.localScale = Vector3.one;

        // resize border
        Border.Resize(width, height);

        // resize background
        float spriteWidth = backgroundSpriteRenderer.sprite.bounds.size.x, spriteHeight = backgroundSpriteRenderer.sprite.bounds.size.y;
        if ((float)height / width > ScreenRatio)
        {
            if (spriteHeight / spriteWidth > ScreenRatio)
            {
                background.transform.localScale = new Vector3(Graphics.height * width / height / spriteWidth,
                    Graphics.height * width / height / spriteWidth, 1);
            }
            else
            {
                background.transform.localScale = new Vector3(Graphics.height * width / height * ScreenRatio / spriteHeight,
                    Graphics.height * width / height * ScreenRatio / spriteHeight, 1);
            }
        }
        else
        {
            if (spriteHeight / spriteWidth > ScreenRatio)
            {
                background.transform.localScale = new Vector3(Graphics.width / spriteWidth, Graphics.width / spriteWidth, 1);
            }
            else
            {
                background.transform.localScale = new Vector3(Graphics.height / spriteHeight, Graphics.height / spriteHeight, 1);
            }
        }
    }

    public static void SetBackground(Background bg)
    {
        backgroundSpriteRenderer.sprite = backgroundSprites[(int)bg];
        float spriteWidth = backgroundSpriteRenderer.sprite.bounds.size.x, spriteHeight = backgroundSpriteRenderer.sprite.bounds.size.y;
        if ((float)Screen.height / (float)Screen.width > ScreenRatio)
        {
            if (spriteHeight / spriteWidth > ScreenRatio)
            {
                background.transform.localScale = new Vector3(height * Screen.width / Screen.height / spriteWidth,
                    height * Screen.width / Screen.height / spriteWidth, 1);
            }
            else
            {
                background.transform.localScale = new Vector3(height * Screen.width / Screen.height * ScreenRatio / spriteHeight,
                    height * Screen.width / Screen.height * ScreenRatio / spriteHeight, 1);
            }
        }
        else
        {
            if (spriteHeight / spriteWidth > ScreenRatio)
            {
                background.transform.localScale = new Vector3(width / spriteWidth, width / spriteWidth, 1);
            }
            else
            {
                background.transform.localScale = new Vector3(height / spriteHeight, height / spriteHeight, 1);
            }
        }
    }

    // flipping animation
    // destroy indicates if beforeFlip is destroyed after flip
    public static IEnumerator Flip(GameObject beforeFlip, GameObject afterFlip, bool horizontal = true, float duration = .8f, float interval = .05f,
        float delay = 0, bool destroy = false)
    {
        if (beforeFlip == null && afterFlip == null)
        {
            Debug.LogWarning($"Flip: Either of 2 GameObjects in the arguments should be non-null.");
            yield break;
        }

        if (beforeFlip != null)
        {
            beforeFlip.SetActive(true);
            beforeFlip.transform.localScale = new Vector3(1, 1, 1);
        }
        if (afterFlip != null)
        {
            afterFlip.SetActive(false);
        }
        if (delay > 0) yield return new WaitForSeconds(delay);

        if (beforeFlip != null)
        {
            beforeFlip.SetActive(true);
            for (float time = 0; time < duration / 2; time += (interval > 0 ? interval : Time.deltaTime))
            {
                beforeFlip.transform.localScale = new Vector3(horizontal ? 1 - time / duration * 2 : 1,
                    horizontal ? 1 : 1 - time / duration * 2, 1);
                yield return new WaitForSeconds(interval);
            }
            beforeFlip.SetActive(false);
            if (destroy)
            {
                Destroy(beforeFlip);
            }
        }

        if (afterFlip != null)
        {
            afterFlip.SetActive(true);
            for (float time = 0; time < duration / 2; time += interval)
            {
                afterFlip.transform.localScale = new Vector3(horizontal ? time / duration * 2 : 1, horizontal ? 1 : time / duration * 2, 1);
                yield return new WaitForSeconds(interval);
            }
            afterFlip.transform.localScale = new Vector3(1, 1, 1);
        }
    }

    // linear move animation
    // don't use this on an already moving GameObject
    public static IEnumerator Move(GameObject target, Vector3 start, Vector3 end, float duration = 1.2f, float interval = 0, float delay = 0)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);
        if (target == null)
        {
            Debug.LogWarning($"Move: The GameObject in the arguments should not be null.");
            yield break;
        }

        for (float time = 0; time < duration; time += (interval > 0 ? interval : Time.deltaTime))
        {
            target.transform.localPosition = new Vector3(start.x + (end.x - start.x) * time / duration,
                start.y + (end.y - start.y) * time / duration, start.z + (end.z - start.z) * time / duration);
                yield return new WaitForSeconds(interval);
        }
        target.transform.localPosition = end;
    }
    public static IEnumerator SlowDownMove(GameObject target, Vector3 start, Vector3 end, float totalDuration, float slowDuration, float delay = 0)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);
        if (target == null)
        {
            Debug.LogWarning($"SlowDownMove: The GameObject in the arguments should not be null.");
            yield break;
        }
        if (totalDuration <= 0)
        {
            Debug.LogWarning($"SlowDownMove: totalDuration {totalDuration} should be positive.");
            yield break;
        }
        if (slowDuration <= 0)
        {
            Debug.LogWarning($"SlowDownMove: slowDuration {slowDuration} should be positive.");
            yield break;
        }
        if (slowDuration > totalDuration)
        {
            Debug.LogWarning($"SlowDownMove: slowDuration {slowDuration} should not be longer than totalDuration {totalDuration}.");
            slowDuration = totalDuration;
        }

        float timeLeft = totalDuration;
        for (; timeLeft < totalDuration - slowDuration; timeLeft -= Time.deltaTime)
        {
            target.transform.localPosition = Vector3.Lerp(start, end, 1 - (timeLeft - slowDuration / 2) / (totalDuration - slowDuration / 2));
            yield return null;
        }
        for (; timeLeft > 0; timeLeft -= Time.deltaTime)
        {
            target.transform.localPosition = Vector3.Lerp(start, end, 1 - timeLeft * timeLeft / (2 * totalDuration - slowDuration) / slowDuration);
            yield return null;
        }
        target.transform.localPosition = end;
    }

    // resizing animation
    public static IEnumerator Resize(GameObject target, Vector3 start, Vector3 end, float duration = 1.2f, float interval = 0, float delay = 0)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);
        if (target == null)
        {
            Debug.LogWarning($"Resize: The GameObject in the arguments should not be null.");
            yield break;
        }

        for (float time = 0; time < duration; time += (interval > 0 ? interval : Time.deltaTime))
        {
            target.transform.localScale = Vector3.Lerp(start, end, time / duration);
            yield return new WaitForSeconds(interval);
        }
        target.transform.localScale = end;
    }

    // rotating animation
    // lap means how many laps target rotates
    // if lap is -1, the target rotates endlessly
    public static IEnumerator Rotate(GameObject target, float start, float end, float speed, int lap = 0, float delay = 0)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);
        if (target == null)
        {
            Debug.LogWarning($"Rotate: The GameObject in the arguments should not be null.");
            yield break;
        }
        if (lap < -1)
        {
            Debug.LogWarning($"Rotate: lap should not be less than -1: {lap}");
            yield break;
        }
        if (speed == 0)
        {
            Debug.LogWarning($"Rotate: speed should be non-zero: {speed}");
            yield break;
        }
        if (Math.Abs(speed) >= 360 * 10)
        {
            Debug.LogWarning($"Rotate: the absolute value of speed should be less than 360 * 10: {speed}");
            yield break;
        }

        if (lap == 0 && (end - start > 0 && speed > 0 || end - start < 0 && speed < 0))
        {
            TruncateAngle(Math.Abs(end - start), out lap);
        }

        start = TruncateAngle(start);
        end = TruncateAngle(end);
        int counterClockwise = (speed > 0 ? 1 : -1);
        speed *= counterClockwise;

        if (counterClockwise < 0)
        {
            start = TruncateAngle(-start);
            end = TruncateAngle(-end);
        }

        // adjust start so that 0 <= end - start < 360
        if (start > end)
        {
            if (start > 0) start -= 360;
            else end += 360;
        }

        for (float angle = start; !(angle >= end && lap == 0); angle += speed * Time.deltaTime)
        {
            int n;
            float originalAngle = angle;
            angle = TruncateAngle(angle, out n, allowNegative: true);
            if (n < 0)
            {
                Debug.LogWarning($"Rotate: impossible angle: {originalAngle}");
                yield break;
            }
            if (lap != -1)
            {
                lap -= n;
                if (angle >= end && lap == 0 || lap < 0)
                {
                    break;
                }
            }
            target.transform.eulerAngles = new Vector3(0, 0, angle * counterClockwise);

            yield return null;
        }
        target.transform.eulerAngles = new Vector3(0, 0, end * counterClockwise);
    }

    // circular move animation
    // don't use this on an already moving GameObject
    // if duration is negative, the target moves around endlessly
    public static IEnumerator MoveAround(GameObject target, float speed, float maxRadius, float duration = -1, float resizingDuration = 0,
        float startAngle = 0, float delay = 0)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);
        if (target == null)
        {
            Debug.LogWarning($"MoveAround: the target GameObject should not be null.");
            yield break;
        }

        float radius, angle = startAngle;
        if (resizingDuration > 0)
        {
            for (float time = 0; time < resizingDuration; time += Time.deltaTime)
            {
                radius = maxRadius * time / resizingDuration;
                target.transform.localPosition = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * radius, Mathf.Sin(angle * Mathf.Deg2Rad) * radius, 0);
                yield return null;
                angle = TruncateAngle(angle + speed * Time.deltaTime);
            }
        }
        radius = maxRadius;
        for (float time = 0; time < duration || duration < 0; time += Time.deltaTime)
        {
            target.transform.localPosition = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * radius, Mathf.Sin(angle * Mathf.Deg2Rad) * radius, 0);
            yield return null;
            angle = TruncateAngle(angle + speed * Time.deltaTime);
        }
        if (resizingDuration > 0)
        {
            for (float time = 0; time < resizingDuration; time += Time.deltaTime)
            {
                radius = maxRadius * (1 - time / resizingDuration);
                target.transform.localPosition = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * radius, Mathf.Sin(angle * Mathf.Deg2Rad) * radius, 0);
                yield return null;
                angle = TruncateAngle(angle + speed * Time.deltaTime);
            }
            target.transform.localPosition = Vector3.zero;
        }
    }

    public static IEnumerator ChangeSortingOrder(SpriteRenderer renderer, int order, float delay)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);
        renderer.sortingOrder = order;
    }

    public static IEnumerator ChangeColor(SpriteRenderer renderer, Color32 start, Color32 end,
        float duration = .5f, float interval = 0, float delay = 0)
    {
        renderer.color = start;
        if (delay > 0) yield return new WaitForSeconds(delay);
        if (renderer == null)
        {
            Debug.LogWarning($"ChangeColor: The SpriteRenderer in the arguments should not be null.");
            yield break;
        }

        for (float time = 0; time < duration; time += (interval > 0 ? interval : Time.deltaTime))
        {
            renderer.color = Color32.Lerp(start, end, time / duration);
            yield return new WaitForSeconds(interval);
        }
        renderer.color = end;
    }

    // truncate the argument into a range [0, 360)
    // if allowNegative is true, the result can be in a range (-360, 0) too
    public static float TruncateAngle(float angle, bool allowNegative = false)
    {
        if (angle >= 360)
        {
            angle -= (int)(angle / 360) * 360;
        }
        else if (angle < 0)
        {
            angle -= (int)(angle / 360) * 360;
            if (!allowNegative)
            {
                angle += 360;
            }
        }
        return angle;
    }
    // n indicates the number cut off
    private static float TruncateAngle(float angle, out int n, bool allowNegative = false)
    {
        if (angle >= 360)
        {
            n = (int)(angle / 360);
            angle -= (int)(angle / 360) * 360;
        }
        else if (angle < 0)
        {
            n = (int)(angle / 360);
            angle -= (int)(angle / 360) * 360;
            if (!allowNegative)
            {
                angle += 360;
                n--;
            }
        }
        else
        {
            n = 0;
        }
        return angle;
    }
}
