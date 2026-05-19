using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class MaterialShaderFixer : EditorWindow
{
    private class BrokenMaterial
    {
        public Material material;
        public string path;
        public string originalShaderName;
        public Shader assignedShader;
    }

    private List<BrokenMaterial> brokenMaterials = new();
    private Vector2 scroll;
    private Shader defaultShader;
    private Shader globalOverrideShader;
    private bool analyzed;

    [MenuItem("Tools/Material Shader Fixer")]
    public static void ShowWindow()
    {
        GetWindow<MaterialShaderFixer>("Material Shader Fixer");
    }

    private void OnEnable()
    {
        defaultShader = Shader.Find("Universal Render Pipeline/Lit");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Material Shader Fixer", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Находит все материалы с потерянным шейдером (розовые) и позволяет назначить им рабочий URP шейдер.",
            MessageType.Info);

        EditorGUILayout.Space(4);
        if (GUILayout.Button("Найти сломанные материалы", GUILayout.Height(30)))
            Analyze();

        if (!analyzed)
            return;

        if (brokenMaterials.Count == 0)
        {
            EditorGUILayout.HelpBox("Сломанных материалов не найдено.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField($"Сломанных материалов: {brokenMaterials.Count}", EditorStyles.boldLabel);

        // Глобальная замена шейдера
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Назначить всем один шейдер:", EditorStyles.miniLabel);
        EditorGUILayout.BeginHorizontal();
        globalOverrideShader = (Shader)EditorGUILayout.ObjectField(
            globalOverrideShader, typeof(Shader), false);
        if (GUILayout.Button("Применить ко всем", GUILayout.Width(140)))
        {
            if (globalOverrideShader != null)
                foreach (var bm in brokenMaterials)
                    bm.assignedShader = globalOverrideShader;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);

        // Список материалов
        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(340));
        foreach (var bm in brokenMaterials)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(bm.material, typeof(Material), false, GUILayout.Width(160));
            EditorGUILayout.LabelField(
                $"был: {bm.originalShaderName}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Шейдер:", GUILayout.Width(50));
            bm.assignedShader = (Shader)EditorGUILayout.ObjectField(
                bm.assignedShader, typeof(Shader), false);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(6);

        int readyCount = brokenMaterials.Count(bm => bm.assignedShader != null);
        GUI.enabled = readyCount > 0;
        GUI.color = new Color(0.4f, 1f, 0.5f);
        if (GUILayout.Button($"Применить шейдеры ({readyCount} материалов)", GUILayout.Height(34)))
            ApplyShaders();
        GUI.color = Color.white;
        GUI.enabled = true;
    }

    private void Analyze()
    {
        brokenMaterials = new List<BrokenMaterial>();

        var matPaths = AssetDatabase.GetAllAssetPaths()
            .Where(p => p.StartsWith("Assets/") && p.EndsWith(".mat"));

        foreach (var path in matPaths)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            var shaderName = mat.shader != null ? mat.shader.name : "null";
            bool isBroken = mat.shader == null
                || shaderName == "Hidden/InternalErrorShader"
                || shaderName.Contains("InternalError");

            if (!isBroken) continue;

            // Пытаемся подобрать шейдер по имени оригинала
            Shader suggested = defaultShader;
            if (shaderName.ToLower().Contains("unlit"))
                suggested = Shader.Find("Universal Render Pipeline/Unlit") ?? defaultShader;
            else if (shaderName.ToLower().Contains("simple"))
                suggested = Shader.Find("Universal Render Pipeline/Simple Lit") ?? defaultShader;

            brokenMaterials.Add(new BrokenMaterial
            {
                material = mat,
                path = path,
                originalShaderName = shaderName,
                assignedShader = suggested,
            });
        }

        analyzed = true;
        Repaint();
    }

    private void ApplyShaders()
    {
        int count = 0;
        foreach (var bm in brokenMaterials)
        {
            if (bm.assignedShader == null) continue;
            bm.material.shader = bm.assignedShader;
            EditorUtility.SetDirty(bm.material);
            count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[MaterialShaderFixer] Исправлено {count} материалов.");
        EditorUtility.DisplayDialog("Готово", $"Исправлено материалов: {count}", "OK");

        Analyze();
    }
}
