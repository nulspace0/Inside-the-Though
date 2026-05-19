// Assets/Editor/URPToHDRPMaterialConverter.cs
// Запуск: Tools → Convert URP Materials to HDRP
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class URPToHDRPMaterialConverter : EditorWindow
{
    [MenuItem("Tools/Revert HDRP Materials to URP")]
    static void RevertHDRPToURP()
    {
        Shader urpLit   = Shader.Find("Universal Render Pipeline/Lit");
        Shader urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");

        if (urpLit == null) { Debug.LogError("URP/Lit не найден! Убедись что URP пакет установлен."); return; }

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
        int reverted = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null || mat.shader == null) continue;

            string sName = mat.shader.name;
            // Все HDRP шейдеры → URP аналоги
            if (sName.StartsWith("HDRP/") || sName.Contains("HD Render Pipeline") || sName == "Hidden/InternalErrorShader")
            {
                bool likelyUnlit = mat.name.ToLower().Contains("unlit")
                                || mat.name.ToLower().Contains("emiss")
                                || mat.name.ToLower().Contains("glow");
                mat.shader = likelyUnlit ? urpUnlit : urpLit;
                EditorUtility.SetDirty(mat);
                reverted++;
                Debug.Log($"[Revert] {mat.name}: {sName} → {mat.shader.name}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Готово", $"Возвращено на URP: {reverted} материалов", "OK");
    }

    [MenuItem("Tools/Fix InternalErrorShader Materials")]
    static void FixErrorShaders()
    {
        Shader hdrpLit   = Shader.Find("HDRP/Lit");
        Shader hdrpUnlit = Shader.Find("HDRP/Unlit");

        if (hdrpLit == null) { Debug.LogError("HDRP/Lit не найден!"); return; }

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
        int fixed_count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null || mat.shader == null) continue;

            string sName = mat.shader.name;
            if (sName == "Hidden/InternalErrorShader" || sName.Contains("ErrorShader"))
            {
                // Эвристика по имени материала
                bool likelyUnlit = mat.name.ToLower().Contains("unlit")
                                || mat.name.ToLower().Contains("emiss")
                                || mat.name.ToLower().Contains("glow")
                                || mat.name.ToLower().Contains("vfx");

                mat.shader = likelyUnlit ? hdrpUnlit : hdrpLit;
                EditorUtility.SetDirty(mat);
                fixed_count++;
                Debug.Log($"[Fix] {mat.name} → {mat.shader.name}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Готово", $"Исправлено материалов: {fixed_count}", "OK");
    }

    [MenuItem("Tools/Convert URP Materials to HDRP")]
    static void Run()
    {
        // URP shader name → HDRP shader name
        var map = new Dictionary<string, string>
        {
            { "Universal Render Pipeline/Lit",                        "HDRP/Lit" },
            { "Universal Render Pipeline/Simple Lit",                 "HDRP/Lit" },
            { "Universal Render Pipeline/Complex Lit",                "HDRP/Lit" },
            { "Universal Render Pipeline/Unlit",                      "HDRP/Unlit" },
            { "Universal Render Pipeline/Particles/Lit",              "HDRP/Lit" },
            { "Universal Render Pipeline/Particles/Simple Lit",       "HDRP/Lit" },
            { "Universal Render Pipeline/Particles/Unlit",            "HDRP/Unlit" },
            { "Universal Render Pipeline/Baked Lit",                  "HDRP/Lit" },
            { "Universal Render Pipeline/Nature/SpeedTree7",          "HDRP/Lit" },
            { "Universal Render Pipeline/Nature/SpeedTree8_PBRLit",   "HDRP/Lit" },
            { "Universal Render Pipeline/Terrain/Lit",                "HDRP/TerrainLit" },
            { "Sprites/Default",                                      "HDRP/Unlit" },
            // TextMeshPro
            { "TextMeshPro/Distance Field",                           "HDRP/TextMeshPro/Distance Field" },
            { "TextMeshPro/Sprite",                                   "HDRP/TextMeshPro/Sprite" },
            { "TextMeshPro/Mobile/Distance Field",                    "HDRP/TextMeshPro/Distance Field" },
            { "TextMeshPro/Bitmap",                                   "HDRP/TextMeshPro/Bitmap" },
        };

        // Найти все .mat файлы в Assets
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
        int converted = 0;
        int skipped   = 0;
        int errors    = 0;

        EditorUtility.DisplayProgressBar("Конвертация материалов", "Начинаю...", 0f);

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat == null || mat.shader == null) { errors++; continue; }

            EditorUtility.DisplayProgressBar("Конвертация материалов",
                $"{i + 1}/{guids.Length}: {mat.name}", (float)i / guids.Length);

            string shaderName = mat.shader.name;

            if (map.TryGetValue(shaderName, out string hdrpShaderName))
            {
                Shader hdrpShader = Shader.Find(hdrpShaderName);
                if (hdrpShader == null)
                {
                    Debug.LogWarning($"[Converter] Не найден HDRP шейдер: {hdrpShaderName} для материала {mat.name}");
                    errors++;
                    continue;
                }
                mat.shader = hdrpShader;
                EditorUtility.SetDirty(mat);
                converted++;
                Debug.Log($"[Converter] {mat.name}: {shaderName} → {hdrpShaderName}");
            }
            else if (shaderName.StartsWith("Universal Render Pipeline"))
            {
                // Неизвестный URP шейдер — ставим HDRP/Lit как дефолт
                Shader fallback = Shader.Find("HDRP/Lit");
                if (fallback != null)
                {
                    mat.shader = fallback;
                    EditorUtility.SetDirty(mat);
                    converted++;
                    Debug.LogWarning($"[Converter] {mat.name}: {shaderName} → HDRP/Lit (fallback)");
                }
            }
            else
            {
                skipped++;
            }
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Конвертация завершена",
            $"Конвертировано: {converted}\nПропущено (не URP): {skipped}\nОшибок: {errors}", "OK");
    }
}
