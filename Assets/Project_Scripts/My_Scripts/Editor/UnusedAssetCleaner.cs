using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class UnusedAssetCleaner : EditorWindow
{
    private const string ArchiveRoot = "Assets/_Old";

    private static readonly string[] TargetScenes =
    {
        "Assets/Project_Scene/LoadingScreen.unity",
        "Assets/Project_Scene/Head.unity",
        "Assets/Project_Scene/ProdolgovatiyMozg.unity",
        "Assets/Project_Scene/lobnaia.unity",
        "Assets/Project_Scene/Mozjechok.unity",
        "Assets/Project_Scene/Temennaya.unity",
        "Assets/Project_Scene/VisochnaDolya.unity",
    };

    // Расширения которые НЕЛЬЗЯ трогать — шейдеры и материалы критичны
    private static readonly string[] SafeExtensions =
    {
        ".shader", ".shadergraph", ".shadersubgraph", ".compute",
        ".hlsl", ".glsl", ".cginc",
        ".mat",         // материалы — Unity теряет ссылку если переместить
        ".lighting",    // Lighting Settings
        ".renderTexture",
        ".mixer",       // Audio Mixer
        ".asmdef",      // Assembly Definition
    };

    // Папки и паттерны, которые НЕЛЬЗЯ трогать никогда
    private static readonly string[] SafePatterns =
    {
        // XR
        "/xr/", "xr/", "/xr", "xrinteraction", "xrhands",
        "openxr", "xrsimulation", "xr interaction toolkit", "xr hands",
        "samples/xr", "handvisualizer",
        // Unity system
        "/editor/", "/editor",
        "resources/", "/resources",
        "streamingassets",
        "textmesh pro", "textmeshpro",
        "samples/",          // все Samples пакетов
        // Проект
        "project_scene",
        "_recovery",
        "_old",
        "plugins/", "/plugins",
    };

    private List<string> unusedAssets = new();
    private List<string> skippedSafe  = new();
    private Vector2 scrollUnused;
    private Vector2 scrollSafe;
    private bool analyzed;
    private bool showSafeList;
    private bool excludeScripts  = true;
    private bool excludeShaders  = true;
    private bool excludeMaterials = true;

    [MenuItem("Tools/Unused Asset Cleaner")]
    public static void ShowWindow() =>
        GetWindow<UnusedAssetCleaner>("Unused Asset Cleaner");

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Unused Asset Cleaner", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Сканирует зависимости всех 7 сцен + всех префабов проекта.\n" +
            "Ассеты перемещаются в Assets/_Old/ — ничего не удаляется.\n" +
            "Чтобы восстановить — перетащи файлы из _Old обратно.",
            MessageType.Info);

        EditorGUILayout.Space(4);
        excludeScripts   = EditorGUILayout.ToggleLeft("Не трогать .cs скрипты (рекомендуется)", excludeScripts);
        excludeShaders   = EditorGUILayout.ToggleLeft("Не трогать шейдеры (рекомендуется)", excludeShaders);
        excludeMaterials = EditorGUILayout.ToggleLeft("Не трогать материалы (рекомендуется)", excludeMaterials);

        EditorGUILayout.Space(4);
        if (GUILayout.Button("Анализировать", GUILayout.Height(30)))
            Analyze();

        if (!analyzed) return;

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField($"Неиспользуемых ассетов: {unusedAssets.Count}", EditorStyles.boldLabel);

        scrollUnused = EditorGUILayout.BeginScrollView(scrollUnused, GUILayout.Height(280));
        foreach (var path in unusedAssets)
            EditorGUILayout.LabelField(path, EditorStyles.miniLabel);
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(4);
        showSafeList = EditorGUILayout.Foldout(showSafeList,
            $"Защищённые (XR / шейдеры / Editor / ...): {skippedSafe.Count}");
        if (showSafeList)
        {
            scrollSafe = EditorGUILayout.BeginScrollView(scrollSafe, GUILayout.Height(100));
            foreach (var path in skippedSafe)
                EditorGUILayout.LabelField(path, EditorStyles.miniLabel);
            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.Space(8);

        if (unusedAssets.Count == 0)
        {
            EditorGUILayout.HelpBox("Неиспользуемых ассетов не найдено.", MessageType.Info);
            return;
        }

        GUI.color = new Color(1f, 0.85f, 0.2f);
        if (GUILayout.Button($"ПЕРЕМЕСТИТЬ {unusedAssets.Count} ассетов в _Old", GUILayout.Height(34)))
        {
            GUI.color = Color.white;
            if (EditorUtility.DisplayDialog(
                    "Переместить в архив",
                    $"Переместить {unusedAssets.Count} ассетов в Assets/_Old/?\n\nНичего не удаляется — всё можно вернуть обратно.",
                    "Переместить", "Отмена"))
                MoveToOld();
        }
        GUI.color = Color.white;
    }

    private void Analyze()
    {
        EditorUtility.DisplayProgressBar("Анализ", "Зависимости сцен...", 0.1f);

        var usedPaths = new HashSet<string>(
            AssetDatabase.GetDependencies(TargetScenes, recursive: true));

        foreach (var s in TargetScenes)
            usedPaths.Add(s);

        // Зависимости ВСЕХ префабов — их могут спавнить скрипты в рантайме
        EditorUtility.DisplayProgressBar("Анализ", "Зависимости префабов...", 0.3f);
        var allPrefabs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => !p.Contains("/_Old/"))
            .ToArray();
        if (allPrefabs.Length > 0)
            foreach (var dep in AssetDatabase.GetDependencies(allPrefabs, recursive: true))
                usedPaths.Add(dep);

        // Зависимости URP / HDRP Render Pipeline Asset
        // (шейдеры, рендерер-фичи, пост-обработка — НЕ видны через сцены)
        EditorUtility.DisplayProgressBar("Анализ", "Зависимости Render Pipeline...", 0.5f);
        AddRenderPipelineDeps(usedPaths);

        // Зависимости всех ScriptableObject-ов в Assets/Settings и Assets/Project_*
        EditorUtility.DisplayProgressBar("Анализ", "Зависимости ScriptableObjects...", 0.65f);
        var allSOs = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => !p.Contains("/_Old/"))
            .ToArray();
        if (allSOs.Length > 0)
            foreach (var dep in AssetDatabase.GetDependencies(allSOs, recursive: true))
                usedPaths.Add(dep);

        EditorUtility.DisplayProgressBar("Анализ", "Фильтрация...", 0.85f);

        unusedAssets = new List<string>();
        skippedSafe  = new List<string>();

        foreach (var path in AssetDatabase.GetAllAssetPaths())
        {
            if (!path.StartsWith("Assets/")) continue;
            if (AssetDatabase.IsValidFolder(path))  continue;
            if (usedPaths.Contains(path))           continue;
            if (excludeScripts   && path.EndsWith(".cs"))  continue;
            if (excludeShaders   && IsShaderFile(path))    continue;
            if (excludeMaterials && path.EndsWith(".mat"))  continue;

            if (IsSafe(path))
                skippedSafe.Add(path);
            else
                unusedAssets.Add(path);
        }

        unusedAssets.Sort();
        skippedSafe.Sort();

        EditorUtility.ClearProgressBar();
        analyzed = true;
        Repaint();

        Debug.Log($"[UnusedAssetCleaner] Анализ завершён. Найдено неиспользуемых: {unusedAssets.Count}. Защищённых: {skippedSafe.Count}.");
    }

    private static void AddRenderPipelineDeps(HashSet<string> usedPaths)
    {
        // Default render pipeline из Graphics Settings (может быть None — тогда null)
        CollectRPAssetDeps(GraphicsSettings.defaultRenderPipeline, usedPaths);

        // Render pipeline из каждого уровня качества
        for (int i = 0; i < QualitySettings.count; i++)
            CollectRPAssetDeps(QualitySettings.GetRenderPipelineAssetAt(i), usedPaths);

        // Ищем URP/HDRP asset-ы напрямую в проекте (на случай если не назначен в Graphics Settings)
        var rpAssets = AssetDatabase.FindAssets("t:RenderPipelineAsset", new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => !p.Contains("/_Old/"))
            .ToArray();
        if (rpAssets.Length > 0)
            foreach (var dep in AssetDatabase.GetDependencies(rpAssets, recursive: true))
                usedPaths.Add(dep);

        // Renderer Data (ForwardRenderer, UniversalRendererData и т.д.)
        var rendererAssets = AssetDatabase.FindAssets("t:ScriptableRendererData", new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => !p.Contains("/_Old/"))
            .ToArray();
        if (rendererAssets.Length > 0)
            foreach (var dep in AssetDatabase.GetDependencies(rendererAssets, recursive: true))
                usedPaths.Add(dep);

        // Все .asset файлы в папках Settings (URP Renderer, Post-processing Volume и т.д.)
        var settingsFolders = new[] { "Assets/Settings", "Assets/Project_Scene" }
            .Where(AssetDatabase.IsValidFolder).ToArray();
        if (settingsFolders.Length > 0)
        {
            var settingsAssets = AssetDatabase.FindAssets("t:Object", settingsFolders)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !p.Contains("/_Old/"))
                .ToArray();
            if (settingsAssets.Length > 0)
                foreach (var dep in AssetDatabase.GetDependencies(settingsAssets, recursive: true))
                    usedPaths.Add(dep);
        }
    }

    private static void CollectRPAssetDeps(RenderPipelineAsset rp, HashSet<string> usedPaths)
    {
        if (rp == null) return;
        string path = AssetDatabase.GetAssetPath(rp);
        if (string.IsNullOrEmpty(path)) return;
        foreach (var dep in AssetDatabase.GetDependencies(path, recursive: true))
            usedPaths.Add(dep);
    }

    private static bool IsSafe(string path)
    {
        var lower = path.ToLower().Replace("\\", "/");
        if (SafeExtensions.Any(e => lower.EndsWith(e))) return true;
        return SafePatterns.Any(p => lower.Contains(p));
    }

    private static bool IsShaderFile(string path)
    {
        var ext = Path.GetExtension(path).ToLower();
        return ext is ".shader" or ".shadergraph" or ".shadersubgraph"
                   or ".compute" or ".hlsl" or ".glsl" or ".cginc";
    }

    private void MoveToOld()
    {
        var failed = new List<string>();
        int total  = unusedAssets.Count;

        // Фаза 1: создаём все нужные папки ДО StartAssetEditing
        EditorUtility.DisplayProgressBar("Подготовка", "Создаём папки в _Old...", 0f);
        foreach (string src in unusedAssets)
        {
            string destFolder = Path.GetDirectoryName(
                ArchiveRoot + "/" + src.Substring("Assets/".Length)
            ).Replace("\\", "/");
            EnsureFolderExists(destFolder);
        }
        AssetDatabase.Refresh();

        // Фаза 2: перемещаем
        AssetDatabase.StartAssetEditing();
        try
        {
            for (int i = 0; i < unusedAssets.Count; i++)
            {
                string src  = unusedAssets[i];
                string dest = ArchiveRoot + "/" + src.Substring("Assets/".Length);

                EditorUtility.DisplayProgressBar("Перемещение в _Old",
                    src, (float)i / unusedAssets.Count);

                string error = AssetDatabase.MoveAsset(src, dest);
                if (!string.IsNullOrEmpty(error))
                {
                    failed.Add(src);
                    Debug.LogWarning($"[UnusedAssetCleaner] Не удалось: '{src}' → {error}");
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }

        int moved = total - failed.Count;
        analyzed = false;
        unusedAssets.Clear();
        skippedSafe.Clear();

        Debug.Log($"[UnusedAssetCleaner] Перемещено {moved} из {total} в {ArchiveRoot}.");
        EditorUtility.DisplayDialog("Готово",
            $"Найдено:    {total}\n" +
            $"Перемещено: {moved}\n" +
            $"Ошибок:     {failed.Count}\n\n" +
            "Чтобы восстановить — перетащи из _Old обратно в нужную папку.",
            "OK");
    }

    private static void EnsureFolderExists(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath) || AssetDatabase.IsValidFolder(folderPath))
            return;

        string parent = Path.GetDirectoryName(folderPath).Replace("\\", "/");
        string child  = Path.GetFileName(folderPath);

        EnsureFolderExists(parent);
        if (!AssetDatabase.IsValidFolder(folderPath))
            AssetDatabase.CreateFolder(parent, child);
    }
}
