using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ProjectOrganizer : EditorWindow
{
    private const string DST_MINE      = "Assets/My_Scripts";
    private const string DST_STORE     = "Assets/Store_Scripts";
    private const string DST_MATERIALS = "Assets/All_Materials";

    private List<string[]> scriptMoves   = new List<string[]>();
    private List<string[]> materialMoves = new List<string[]>();
    private List<string>   emptyFolders  = new List<string>();

    private bool    analyzed;
    private int     currentTab;
    private Vector2 sv1, sv2, sv3;

    [MenuItem("Tools/Project Organizer")]
    public static void Open()
    {
        GetWindow<ProjectOrganizer>("Project Organizer");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Project Organizer", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "My_Scripts   - скрипты из Project_Scripts и Editor\n" +
            "Store_Scripts - все остальные скрипты\n" +
            "All_Materials - все .mat файлы\n" +
            "Пустые папки удаляются автоматически.",
            MessageType.Info);

        GUILayout.Space(6);

        if (GUILayout.Button("Анализировать", GUILayout.Height(28)))
            RunAnalyze();

        if (!analyzed)
            return;

        GUILayout.Space(4);

        string[] tabNames = new string[]
        {
            "Скрипты ("    + scriptMoves.Count   + ")",
            "Материалы ("  + materialMoves.Count + ")",
            "Папки ("      + emptyFolders.Count  + ")"
        };
        currentTab = GUILayout.Toolbar(currentTab, tabNames);

        GUILayout.Space(4);

        if (currentTab == 0)
        {
            sv1 = EditorGUILayout.BeginScrollView(sv1, GUILayout.Height(300));
            foreach (string[] row in scriptMoves)
            {
                EditorGUILayout.LabelField(row[0], EditorStyles.miniLabel);
                EditorGUI.indentLevel = 1;
                EditorGUILayout.LabelField("-> " + row[1], EditorStyles.miniLabel);
                EditorGUI.indentLevel = 0;
            }
            EditorGUILayout.EndScrollView();
        }
        else if (currentTab == 1)
        {
            sv2 = EditorGUILayout.BeginScrollView(sv2, GUILayout.Height(300));
            foreach (string[] row in materialMoves)
            {
                EditorGUILayout.LabelField(row[0], EditorStyles.miniLabel);
                EditorGUI.indentLevel = 1;
                EditorGUILayout.LabelField("-> " + row[1], EditorStyles.miniLabel);
                EditorGUI.indentLevel = 0;
            }
            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.HelpBox("Эти папки будут удалены как пустые.", MessageType.Warning);
            sv3 = EditorGUILayout.BeginScrollView(sv3, GUILayout.Height(280));
            foreach (string f in emptyFolders)
                EditorGUILayout.LabelField(f, EditorStyles.miniLabel);
            EditorGUILayout.EndScrollView();
        }

        GUILayout.Space(8);

        int total = scriptMoves.Count + materialMoves.Count + emptyFolders.Count;
        if (total == 0)
        {
            EditorGUILayout.HelpBox("Всё уже организовано.", MessageType.Info);
            return;
        }

        GUI.color = new Color(0.45f, 0.85f, 1f);
        if (GUILayout.Button("ПРИМЕНИТЬ ВСЁ", GUILayout.Height(34)))
        {
            GUI.color = Color.white;
            bool ok = EditorUtility.DisplayDialog(
                "Применить",
                "Скрипты: "    + scriptMoves.Count   + "\n" +
                "Материалы: "  + materialMoves.Count + "\n" +
                "Папки: "      + emptyFolders.Count  + "\n\nПродолжить?",
                "Да", "Отмена");
            if (ok) RunExecute();
        }
        GUI.color = Color.white;
    }

    // -------------------------------------------------------------------------

    private void RunAnalyze()
    {
        scriptMoves.Clear();
        materialMoves.Clear();
        emptyFolders.Clear();

        EditorUtility.DisplayProgressBar("Анализ", "Скрипты...", 0.2f);
        AnalyzeScripts();

        EditorUtility.DisplayProgressBar("Анализ", "Материалы...", 0.6f);
        AnalyzeMaterials();

        EditorUtility.DisplayProgressBar("Анализ", "Папки...", 0.9f);
        AnalyzeFolders();

        EditorUtility.ClearProgressBar();
        analyzed = true;
        Repaint();
    }

    private void AnalyzeScripts()
    {
        string[] all = AssetDatabase.GetAllAssetPaths();
        foreach (string src in all)
        {
            if (!src.StartsWith("Assets/")) continue;
            if (!src.EndsWith(".cs"))        continue;
            if (IsProtected(src))            continue;

            bool mine   = IsMine(src);
            bool editor = IsEditor(src);

            string root = mine ? DST_MINE : DST_STORE;
            string sub  = editor ? "Editor" : GetSub(src, mine);
            string dir  = string.IsNullOrEmpty(sub) ? root : root + "/" + sub;
            string dst  = dir + "/" + Path.GetFileName(src);
            dst = MakeUnique(dst);

            if (Norm(src) == Norm(dst)) continue;

            scriptMoves.Add(new string[] { src, dst });
        }

        scriptMoves.Sort((a, b) => string.Compare(a[0], b[0], System.StringComparison.Ordinal));
    }

    private void AnalyzeMaterials()
    {
        string[] all = AssetDatabase.GetAllAssetPaths();
        foreach (string src in all)
        {
            if (!src.StartsWith("Assets/")) continue;
            if (!src.EndsWith(".mat"))       continue;
            if (IsProtected(src))            continue;

            string dst = DST_MATERIALS + "/" + Path.GetFileName(src);
            dst = MakeUnique(dst);

            if (Norm(src) == Norm(dst)) continue;

            materialMoves.Add(new string[] { src, dst });
        }
    }

    private void AnalyzeFolders()
    {
        HashSet<string> files = new HashSet<string>();
        string[] all = AssetDatabase.GetAllAssetPaths();
        foreach (string p in all)
        {
            if (p.StartsWith("Assets/") && !AssetDatabase.IsValidFolder(p))
                files.Add(p);
        }

        foreach (string[] op in scriptMoves)   { files.Remove(op[0]); files.Add(op[1]); }
        foreach (string[] op in materialMoves) { files.Remove(op[0]); files.Add(op[1]); }

        HashSet<string> used = new HashSet<string>();
        foreach (string f in files)
        {
            string d = Path.GetDirectoryName(f);
            if (d == null) continue;
            d = d.Replace("\\", "/");
            while (d.Length > "Assets".Length && d.StartsWith("Assets"))
            {
                used.Add(d);
                string p2 = Path.GetDirectoryName(d);
                if (p2 == null) break;
                d = p2.Replace("\\", "/");
            }
        }

        foreach (string p in all)
        {
            if (!p.StartsWith("Assets/"))         continue;
            if (!AssetDatabase.IsValidFolder(p))  continue;
            if (p == "Assets")                    continue;
            if (IsProtected(p))                   continue;
            if (!used.Contains(p))
                emptyFolders.Add(p);
        }

        emptyFolders.Sort((a, b) => b.Length.CompareTo(a.Length));
    }

    // -------------------------------------------------------------------------

    private void RunExecute()
    {
        List<string> failed = new List<string>();
        int total = scriptMoves.Count + materialMoves.Count;

        EditorUtility.DisplayProgressBar("Создание папок", "", 0f);
        List<string[]> allMoves = new List<string[]>(scriptMoves);
        allMoves.AddRange(materialMoves);

        foreach (string[] op in allMoves)
        {
            string d = Path.GetDirectoryName(op[1]);
            if (d != null) MakeFolder(d.Replace("\\", "/"));
        }
        AssetDatabase.Refresh();

        AssetDatabase.StartAssetEditing();
        try
        {
            int i = 0;
            foreach (string[] op in allMoves)
            {
                EditorUtility.DisplayProgressBar("Перемещение", op[0], (float)i / Mathf.Max(1, total));
                i++;
                string err = AssetDatabase.MoveAsset(op[0], op[1]);
                if (!string.IsNullOrEmpty(err))
                {
                    failed.Add(op[0]);
                    Debug.LogWarning("[ProjectOrganizer] " + op[0] + ": " + err);
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        int deleted = 0;
        foreach (string folder in emptyFolders)
        {
            EditorUtility.DisplayProgressBar("Папки", folder, 0.5f);
            string[] g = AssetDatabase.FindAssets("", new string[] { folder });
            if (g.Length == 0)
            {
                if (AssetDatabase.DeleteAsset(folder)) deleted++;
            }
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();

        int moved = total - failed.Count;
        analyzed = false;
        scriptMoves.Clear();
        materialMoves.Clear();
        emptyFolders.Clear();

        EditorUtility.DisplayDialog("Готово",
            "Перемещено: "   + moved   + "\n" +
            "Папок удалено: " + deleted + "\n" +
            "Ошибок: "        + failed.Count,
            "OK");
    }

    // -------------------------------------------------------------------------

    private static bool IsMine(string path)
    {
        string n = Norm(path);
        return n.StartsWith("assets/project_scripts/") || n.StartsWith("assets/editor/");
    }

    private static bool IsEditor(string path)
    {
        string n = Norm(path);
        return n.Contains("/editor/") || n.EndsWith("/editor");
    }

    private static bool IsProtected(string path)
    {
        string n = Norm(path);
        return n.StartsWith("assets/samples/")       ||
               n.StartsWith("assets/_old/")          ||
               n.StartsWith("assets/_recovery/")     ||
               n.StartsWith("assets/project_scene/") ||
               n.StartsWith("assets/my_scripts/")    ||
               n.StartsWith("assets/store_scripts/") ||
               n.StartsWith("assets/all_materials/") ||
               n.StartsWith("assets/textmesh pro/");
    }

    private static string GetSub(string src, bool mine)
    {
        if (mine)
        {
            string[] roots = new string[]
            {
                "assets/project_scripts/",
                "assets/editor/"
            };
            foreach (string r in roots)
            {
                if (Norm(src).StartsWith(r))
                {
                    string rel = src.Substring(r.Length);
                    string sub = Path.GetDirectoryName(rel);
                    return sub != null ? sub.Replace("\\", "/") : "";
                }
            }
        }
        else
        {
            string[] parts = src.Replace("\\", "/").Split('/');
            if (parts.Length > 2) return parts[1];
        }
        return "";
    }

    private static string Norm(string s)
    {
        return s.ToLower().Replace("\\", "/");
    }

    private static string MakeUnique(string dst)
    {
        string full = Path.GetFullPath(dst);
        if (!File.Exists(full)) return dst;

        string dir  = Path.GetDirectoryName(dst).Replace("\\", "/");
        string name = Path.GetFileNameWithoutExtension(dst);
        string ext  = Path.GetExtension(dst);
        int    n    = 2;
        while (true)
        {
            string c = dir + "/" + name + "_" + n + ext;
            if (!File.Exists(Path.GetFullPath(c))) return c;
            n++;
        }
    }

    private static void MakeFolder(string path)
    {
        if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
            return;
        string par = Path.GetDirectoryName(path);
        if (par == null) return;
        par = par.Replace("\\", "/");
        MakeFolder(par);
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(par, Path.GetFileName(path));
    }
}
