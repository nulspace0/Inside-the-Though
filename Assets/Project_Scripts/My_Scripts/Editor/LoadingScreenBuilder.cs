// Assets/Editor/LoadingScreenBuilder.cs
// Запуск: Tools → Build Loading Screen UI
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public static class LoadingScreenBuilder
{
    [MenuItem("Tools/Build Loading Screen UI")]
    static void Build()
    {
        // ── Найти Canvas в сцене ──────────────────────────────────────────
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Canvas не найден в сцене! Создай Canvas сначала.");
            return;
        }
        Transform root = canvas.transform;

        // Убираем старые дочерние объекты (Image, Text) если есть
        for (int i = root.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(root.GetChild(i).gameObject);

        // ── 1. Background ─────────────────────────────────────────────────
        var bg = MakeImage(root, "Background", new Color(0.06f, 0.06f, 0.12f));
        Stretch(bg);
        // Назначь splash.png вручную в инспекторе → поле Source Image

        // ── 2. Тёмный оверлей снизу (gradient vignette эффект) ───────────
        var overlay = MakeImage(root, "BottomOverlay", new Color(0,0,0,0.55f));
        var oRT = overlay.GetComponent<RectTransform>();
        oRT.anchorMin = new Vector2(0, 0);
        oRT.anchorMax = new Vector2(1, 0.35f);
        oRT.offsetMin = oRT.offsetMax = Vector2.zero;

        // ── 3. ProgressBar_BG ─────────────────────────────────────────────
        var barBG = MakeImage(root, "ProgressBar_BG", new Color(0.1f, 0.12f, 0.25f, 0.85f));
        var barBGRT = barBG.GetComponent<RectTransform>();
        barBGRT.anchorMin        = new Vector2(0.5f, 0f);
        barBGRT.anchorMax        = new Vector2(0.5f, 0f);
        barBGRT.pivot            = new Vector2(0.5f, 0f);
        barBGRT.anchoredPosition = new Vector2(0, 72);
        barBGRT.sizeDelta        = new Vector2(820, 10);

        // Скруглённые углы (если есть спрайт — иначе просто Image)
        barBG.type = Image.Type.Sliced;

        // ── 4. ProgressBar_Fill (дочерний к BG) ──────────────────────────
        var fill = MakeImage(barBG.transform, "ProgressBar_Fill",
                             new Color(0.45f, 0.72f, 1f, 1f));
        fill.type       = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillAmount = 0f;
        Stretch(fill);

        // ── 5. StatusText — строка загрузки ──────────────────────────────
        var statusRT = MakeTMP(root, "StatusText",
                               "Инициализация...", 18,
                               new Color(0.85f, 0.92f, 1f),
                               FontStyles.Normal);
        statusRT.anchorMin        = new Vector2(0.5f, 0f);
        statusRT.anchorMax        = new Vector2(0.5f, 0f);
        statusRT.pivot            = new Vector2(0.5f, 0f);
        statusRT.anchoredPosition = new Vector2(0, 92);
        statusRT.sizeDelta        = new Vector2(700, 28);
        statusRT.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // ── 6. PercentText ────────────────────────────────────────────────
        var pctRT = MakeTMP(root, "PercentText",
                            "0%", 15,
                            new Color(0.6f, 0.78f, 1f),
                            FontStyles.Normal);
        pctRT.anchorMin        = new Vector2(0.5f, 0f);
        pctRT.anchorMax        = new Vector2(0.5f, 0f);
        pctRT.pivot            = new Vector2(0.5f, 0f);
        pctRT.anchoredPosition = new Vector2(430, 64);
        pctRT.sizeDelta        = new Vector2(80, 24);
        pctRT.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Right;

        // ── 7. TipText — факт о мозге ─────────────────────────────────────
        var tipRT = MakeTMP(root, "TipText",
                            "Твой мозг обрабатывает 11 миллионов бит информации в секунду.",
                            13,
                            new Color(0.65f, 0.7f, 0.85f),
                            FontStyles.Italic);
        tipRT.anchorMin        = new Vector2(0.5f, 0f);
        tipRT.anchorMax        = new Vector2(0.5f, 0f);
        tipRT.pivot            = new Vector2(0.5f, 0f);
        tipRT.anchoredPosition = new Vector2(0, 32);
        tipRT.sizeDelta        = new Vector2(820, 30);
        tipRT.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // ── 8. Назначить компонент LoadingScreenController ───────────────
        GameObject lm = GameObject.Find("LoadingManager");
        if (lm == null)
        {
            lm = new GameObject("LoadingManager");
            Undo.RegisterCreatedObjectUndo(lm, "Create LoadingManager");
        }

        var ctrl = lm.GetComponent<LoadingScreenController>()
                   ?? Undo.AddComponent<LoadingScreenController>(lm);

        // Назначяем через SerializedObject чтобы Undo работал
        var so = new SerializedObject(ctrl);
        so.FindProperty("progressBarFill").objectReferenceValue = fill;
        so.FindProperty("statusText")     .objectReferenceValue = statusRT.GetComponent<TextMeshProUGUI>();
        so.FindProperty("percentText")    .objectReferenceValue = pctRT.GetComponent<TextMeshProUGUI>();
        so.FindProperty("tipText")        .objectReferenceValue = tipRT.GetComponent<TextMeshProUGUI>();
        so.ApplyModifiedProperties();

        // ── 9. Splash.png → Background ───────────────────────────────────
        var splashTex = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/splash.png");
        if (splashTex != null)
        {
            bg.sprite = splashTex;
            bg.color  = Color.white;
            bg.preserveAspect = false;
        }
        else
        {
            Debug.LogWarning("splash.png не найден — назначь вручную в Background → Source Image");
        }

        EditorUtility.SetDirty(canvas.gameObject);
        Debug.Log("[LoadingScreenBuilder] UI создан успешно!");
        EditorUtility.DisplayDialog("Готово!",
            "Загрузочный экран создан.\n\n" +
            "Осталось:\n" +
            "1. Убедись что splash.png имеет тип Sprite (2D)\n" +
            "2. В Build Settings добавь LoadingScreen первой сценой\n" +
            "3. Укажи имя следующей сцены в LoadingManager → Next Scene Name", "OK");
    }

    // ── Хелперы ───────────────────────────────────────────────────────────
    static Image MakeImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static RectTransform MakeTMP(Transform parent, string name,
                                  string text, float size, Color color,
                                  FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text          = text;
        tmp.fontSize      = size;
        tmp.color         = color;
        tmp.fontStyle     = style;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = true;
        return go.GetComponent<RectTransform>();
    }

    static void Stretch(Component c)
    {
        var rt = c.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
