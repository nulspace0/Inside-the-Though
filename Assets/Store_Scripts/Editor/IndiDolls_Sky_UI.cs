#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TRAIBAG
{
    public class IndiDolls_SkyGUI : ShaderGUI
    {
        public override void OnGUI(MaterialEditor me, MaterialProperty[] props)
        {
            // Preset
            var pPreset = FindProperty("_SkyPreset", props, false);
            var pTop = FindProperty("_SkyGradientTop", props, false);
            var pBot = FindProperty("_SkyGradientBottom", props, false);
            var pExp = FindProperty("_SkyGradientExponent", props, false);
            var pHorzCol = FindProperty("_HorizonLineColor", props, false);
            var pHorzExp = FindProperty("_HorizonLineExponent", props, false);

            if (pPreset == null) return;

            EditorGUILayout.LabelField("Sky Preset", EditorStyles.boldLabel);

            string[] labels = {
                "Custom","Dawn","Sunrise","Clear Noon","Summer Zenith","Twilight",
                "Sunset","Afterglow","Clear Night","Misty Morning","Hazy"
            };

            int cur = Mathf.Clamp(Mathf.RoundToInt(pPreset.floatValue), 0, labels.Length - 1);
            int sel = EditorGUILayout.Popup("Preset", cur, labels);

            // 값 저장
            if (sel != cur)
            {
                pPreset.floatValue = sel;

                foreach (var t in me.targets)
                {
                    var mat = t as Material;
                    if (!mat) continue;
                    Undo.RecordObject(mat, "Apply Sky Preset");

                    ApplyPreset(mat, sel);
                    EditorUtility.SetDirty(mat);
                }
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Custom Settings", EditorStyles.boldLabel);

            // 직접 조절
            if (pTop != null) me.ColorProperty(pTop, "Top Color");
            if (pBot != null) me.ColorProperty(pBot, "Bottom Color");
            if (pExp != null) me.FloatProperty(pExp, "Exponent");
            if (pHorzCol != null) me.ColorProperty(pHorzCol, "Horizon Color");
            if (pHorzExp != null) me.FloatProperty(pHorzExp, "Horizon Exponent");
        }

        void ApplyPreset(Material mat, int sel)
        {
            Color top = Color.white;
            Color bot = Color.white;
            float exp = 2.5f;
            Color horz = Color.white;
            float horzExp = 4f;

            switch (sel)
            {
                case 1: top = new Color(0.22f, 0.36f, 0.60f); bot = new Color(0.90f, 0.78f, 0.82f); exp = 2.2f; horz = new Color(0.95f,0.82f,0.70f); horzExp = 3f; break;
                case 2: top = new Color(0.45f,0.60f,0.85f); bot = new Color(1.00f,0.82f,0.60f); exp = 2.0f; horz = new Color(1.00f,0.78f,0.50f); horzExp = 2.5f; break;
                case 3: top = new Color(0.20f,0.60f,0.90f); bot = new Color(0.75f,0.85f,0.95f); exp = 3.0f; horz = new Color(0.90f,0.93f,0.97f); horzExp = 6f; break;
                case 4: top = new Color(0.08f,0.45f,0.95f); bot = new Color(0.68f,0.82f,0.98f); exp = 3.6f; horz = new Color(0.88f,0.93f,0.98f); horzExp = 8f; break;
                case 5: top = new Color(0.35f,0.15f,0.55f); bot = new Color(0.95f,0.68f,0.55f); exp = 1.8f; horz = new Color(0.98f,0.76f,0.60f); horzExp = 2f; break;
                case 6: top = new Color(0.55f,0.28f,0.15f); bot = new Color(0.98f,0.64f,0.38f); exp = 1.6f; horz = new Color(1.0f,0.70f,0.45f); horzExp = 2f; break;
                case 7: top = new Color(0.28f,0.18f,0.42f); bot = new Color(0.92f,0.56f,0.50f); exp = 1.9f; horz = new Color(0.96f,0.66f,0.52f); horzExp = 2.2f; break;
                case 8: top = new Color(0.02f,0.08f,0.20f); bot = new Color(0.08f,0.16f,0.26f); exp = 2.8f; horz = new Color(0.20f,0.24f,0.32f); horzExp = 4f; break;
                case 9: top = new Color(0.52f,0.65f,0.74f); bot = new Color(0.78f,0.84f,0.88f); exp = 3.4f; horz = new Color(0.86f,0.88f,0.90f); horzExp = 7f; break;
                case 10: top = new Color(0.58f,0.58f,0.52f); bot = new Color(0.88f,0.84f,0.78f); exp = 2.1f; horz = new Color(0.92f,0.88f,0.78f); horzExp = 3.5f; break;
            }

            if (mat.HasProperty("_SkyGradientTop")) mat.SetColor("_SkyGradientTop", top);
            if (mat.HasProperty("_SkyGradientBottom")) mat.SetColor("_SkyGradientBottom", bot);
            if (mat.HasProperty("_SkyGradientExponent")) mat.SetFloat("_SkyGradientExponent", exp);
            if (mat.HasProperty("_HorizonLineColor")) mat.SetColor("_HorizonLineColor", horz);
            if (mat.HasProperty("_HorizonLineExponent")) mat.SetFloat("_HorizonLineExponent", horzExp);
        }
    }
}
#endif
