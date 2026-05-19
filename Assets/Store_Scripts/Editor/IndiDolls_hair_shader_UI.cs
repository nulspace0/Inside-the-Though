// Assets/Editor/HairShaderGUI.cs
// Shader "Custom/Hair_Standard_Rim_AngelRing_v10" custom inspector (clean titles, EN only, no help/separators)
using UnityEditor;
using UnityEngine;

namespace TRAIBAG
{
    public class HairShaderGUI : ShaderGUI
    {
        // Foldout states
        static bool sShowBase = true;
        static bool sShowPBR = true;
        static bool sShowEmission = true;
        static bool sShowRim = true;
        static bool sShowRing = true;
        static bool sShowRingView = false;
        static bool sShowAdvanced = false;

        static bool Foldout(string title, bool state)
        {
            return EditorGUILayout.Foldout(state, title, true);
        }

        static bool Toggle(MaterialProperty prop, string label)
        {
            bool v = prop != null && prop.floatValue > 0.5f;
            EditorGUI.BeginChangeCheck();
            v = EditorGUILayout.Toggle(label, v);
            if (EditorGUI.EndChangeCheck() && prop != null)
                prop.floatValue = v ? 1f : 0f;
            return v;
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            // Find properties (v10)
            var _Color = FindProperty("_Color", props, false);
            var _MainTex = FindProperty("_MainTex", props, false);
            var _AlphaClip = FindProperty("_AlphaClip", props, false);
            var _Cutoff = FindProperty("_Cutoff", props, false);

            var _Metallic = FindProperty("_Metallic", props, false);
            var _Glossiness = FindProperty("_Glossiness", props, false);

            var _EmissionMap = FindProperty("_EmissionMap", props, false);
            var _EmissionColor = FindProperty("_EmissionColor", props, false);

            var _RimColor = FindProperty("_RimColor", props, false);
            var _RimIntensity = FindProperty("_RimIntensity", props, false);
            var _RimPower = FindProperty("_RimPower", props, false);
            var _RimAmbientInfluence = FindProperty("_RimAmbientInfluence", props, false);
            var _RimUseViewSpace = FindProperty("_RimUseViewSpace", props, false);

            var _RingUseLocal = FindProperty("_RingUseLocal", props, false);
            var _RingColor = FindProperty("_RingColor", props, false);
            var _RingIntensity = FindProperty("_RingIntensity", props, false);
            var _RingCenter = FindProperty("_RingCenter", props, false);
            var _RingWidth = FindProperty("_RingWidth", props, false);
            var _RingSoftness = FindProperty("_RingSoftness", props, false);

            var _RingCenterViewInfluence = FindProperty("_RingCenterViewInfluence", props, false);
            var _RingCenterViewMaxShift = FindProperty("_RingCenterViewMaxShift", props, false);

            var _LocalMinY = FindProperty("_LocalMinY", props, false);
            var _LocalMaxY = FindProperty("_LocalMaxY", props, false);

            // Header (simple, EN only)
            // EditorGUILayout.LabelField("Hair Shader (Built-in) – Material UI", EditorStyles.boldLabel);

            // Base
            sShowBase = Foldout("Base", sShowBase);
            if (sShowBase)
            {
                if (_MainTex != null && _Color != null)
                    materialEditor.TexturePropertySingleLine(new GUIContent("Albedo"), _MainTex, _Color);

                using (new EditorGUI.IndentLevelScope())
                {
                    if (_AlphaClip != null)
                    {
                        bool clip = Toggle(_AlphaClip, "Alpha Clip");
                        if (_Cutoff != null)
                        {
                            using (new EditorGUI.DisabledScope(!clip))
                                materialEditor.ShaderProperty(_Cutoff, "Alpha Cutoff");
                        }
                    }
                }
            }

            // PBR
            sShowPBR = Foldout("PBR", sShowPBR);
            if (sShowPBR)
            {
                if (_Metallic != null) materialEditor.ShaderProperty(_Metallic, "Metallic");
                if (_Glossiness != null) materialEditor.ShaderProperty(_Glossiness, "Smoothness");
            }

            // Emission (Global)
            sShowEmission = Foldout("Emission", sShowEmission);
            if (sShowEmission)
            {
                if (_EmissionMap != null && _EmissionColor != null)
                    materialEditor.TexturePropertySingleLine(new GUIContent("Emission Map (Optional)"), _EmissionMap, _EmissionColor);
            }

            // Rim
            sShowRim = Foldout("Rim", sShowRim);
            if (sShowRim)
            {
                if (_RimColor != null) materialEditor.ShaderProperty(_RimColor, "Rim Color");
                if (_RimIntensity != null) materialEditor.ShaderProperty(_RimIntensity, "Rim Intensity");
                if (_RimPower != null) materialEditor.ShaderProperty(_RimPower, "Rim Power");
                if (_RimAmbientInfluence != null) materialEditor.ShaderProperty(_RimAmbientInfluence, "Ambient Influence");

                if (_RimUseViewSpace != null)
                {
                    bool vs = _RimUseViewSpace.floatValue > 0.5f;
                    EditorGUI.BeginChangeCheck();
                    vs = EditorGUILayout.Toggle("Use ViewSpace Edge", vs);
                    if (EditorGUI.EndChangeCheck())
                        _RimUseViewSpace.floatValue = vs ? 1f : 0f;
                }
            }

            // Angel Ring
            sShowRing = Foldout("Angel Ring", sShowRing);
            if (sShowRing)
            {
                if (_RingColor != null) materialEditor.ShaderProperty(_RingColor, "Ring Color");
                if (_RingIntensity != null) materialEditor.ShaderProperty(_RingIntensity, "Ring Intensity");

                EditorGUILayout.Space(2);
                if (_RingCenter != null) materialEditor.ShaderProperty(_RingCenter, "Ring Center");
                if (_RingWidth != null) materialEditor.ShaderProperty(_RingWidth, "Ring Width");
                if (_RingSoftness != null) materialEditor.ShaderProperty(_RingSoftness, "Ring Softness");

                if (_RingUseLocal != null)
                {
                    bool useLocal = _RingUseLocal.floatValue > 0.5f;
                    EditorGUI.BeginChangeCheck();
                    useLocal = EditorGUILayout.Toggle("Use Local Space (Off=UV)", useLocal);
                    if (EditorGUI.EndChangeCheck())
                        _RingUseLocal.floatValue = useLocal ? 1f : 0f;
                }

                // Camera-responsive center
                sShowRingView = Foldout("View-driven Center", sShowRingView);
                if (sShowRingView)
                {
                    if (_RingCenterViewInfluence != null) materialEditor.ShaderProperty(_RingCenterViewInfluence, "Center View Influence");
                    if (_RingCenterViewMaxShift != null) materialEditor.ShaderProperty(_RingCenterViewMaxShift, "Center Max Shift");
                }

                // Local Y normalization
                sShowAdvanced = Foldout("Local Space Normalize", sShowAdvanced);
                if (sShowAdvanced)
                {
                    if (_LocalMinY != null) materialEditor.ShaderProperty(_LocalMinY, "Local Min Y");
                    if (_LocalMaxY != null) materialEditor.ShaderProperty(_LocalMaxY, "Local Max Y");
                }
            }
        }
    }
}