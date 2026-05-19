#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TRAIBAG
{
    public class IndiDolls_custom_shader_UI : ShaderGUI
    {
        enum RenderingMode { Opaque = 0, Transparent = 1 }

        // Foldouts
        static bool sShowBase = true;
        static bool sShowMask = true;
        static bool sShowTrans = true;
        static bool sShowRim = true;
        static bool sShowRefl = true;
        static bool sShowEmiss = true;
        static bool sShowSpec = true;
        static bool sShowVtxTint = true;

        // Helpers
        static void SetKeyword(Material m, string kw, bool on)
        { if (on) m.EnableKeyword(kw); else m.DisableKeyword(kw); }

        static bool IsKeywordOn(Material m, string kw)
        { return m != null && m.IsKeywordEnabled(kw); }

        static bool Any(Material[] mats, string kw)
        {
            if (mats == null) return false;
            foreach (var m in mats)
                if (IsKeywordOn(m, kw)) return true;
            return false;
        }

        static void ApplyKeywordToAll(Material[] mats, string kw, bool on)
        {
            if (mats == null) return;
            foreach (var m in mats)
            {
                if (!m) continue;
                SetKeyword(m, kw, on);
                EditorUtility.SetDirty(m);
            }
        }

        static bool GetFloatToggle(Material m, string propName, float threshold = 0.5f)
        {
            if (!m || !m.HasProperty(propName)) return false;
            return m.GetFloat(propName) > threshold;
        }

        static void SetFloatToggleAll(Material[] mats, string propName, bool on)
        {
            if (mats == null) return;
            foreach (var m in mats)
            {
                if (!m || !m.HasProperty(propName)) continue;
                m.SetFloat(propName, on ? 1f : 0f);
                EditorUtility.SetDirty(m);
            }
        }

        static void ApplyRenderState(Material m, RenderingMode mode)
        {
            if (!m) return;
            if (mode == RenderingMode.Opaque)
            {
                m.SetOverrideTag("RenderType", "Opaque");
                m.renderQueue = -1;
                m.SetFloat("_SrcBlend", (float)BlendMode.One);
                m.SetFloat("_DstBlend", (float)BlendMode.Zero);
                m.SetFloat("_ZWrite", 1f);
            }
            else
            {
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = (int)RenderQueue.Transparent;
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
            }
        }

        static bool Prop(MaterialEditor me, MaterialProperty p, string label)
        {
            if (p == null) return false;
            me.ShaderProperty(p, label);
            return false;
        }

        static bool Vec(MaterialEditor me, MaterialProperty p, string label)
        {
            if (p == null) return false;
            me.VectorProperty(p, label);
            return false;
        }

        static bool TexLine(MaterialEditor me, string label, MaterialProperty tex, MaterialProperty col = null)
        {
            if (tex == null) return false;
            me.TexturePropertySingleLine(new GUIContent(label), tex, col);
            return false;
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            var mats = new Material[materialEditor.targets.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = materialEditor.targets[i] as Material;
            var m0 = mats.Length > 0 ? mats[0] : null;

            // Rendering Mode
            var pRenderMode = FindProperty("_RenderMode", props, false);
            int mode = pRenderMode != null ? Mathf.Clamp(Mathf.RoundToInt(pRenderMode.floatValue), 0, 1) : 0;
            int newMode = EditorGUILayout.Popup("Rendering Mode", mode, new[] { "Opaque", "Transparent" });
            if (pRenderMode != null && newMode != mode) pRenderMode.floatValue = newMode;
            if (m0) ApplyRenderState(m0, (RenderingMode)newMode);

            // Base
            sShowBase = EditorGUILayout.Foldout(sShowBase, "Base", true);
            if (sShowBase)
            {
                var _MainTex = FindProperty("_MainTex", props, false);
                var _Color = FindProperty("_Color", props, false);
                var _MainUV_ST = FindProperty("_MainUV_ST", props, false);
                var _Metallic = FindProperty("_Metallic", props, false);
                var _Smooth = FindProperty("_Smoothness", props, false);

                TexLine(materialEditor, "Main Texture", _MainTex, _Color);
                Prop(materialEditor, _Metallic, "Metallic");
                Prop(materialEditor, _Smooth, "Smoothness");
                Vec(materialEditor, _MainUV_ST, "Main Tiling(XY) Offset(ZW)");
            }

            // Mask
            sShowMask = EditorGUILayout.Foldout(sShowMask, "Mask (R=Emiss, G=Refl, B=Spec)", true);
            if (sShowMask)
            {
                var _MaskTex = FindProperty("_MaskTex", props, false);
                var _MaskUV = FindProperty("_MaskUV_ST", props, false);
                var _HasMask = FindProperty("_HasMaskTex", props, false);

                TexLine(materialEditor, "Mask Texture", _MaskTex, null);
                Vec(materialEditor, _MaskUV, "Mask Tiling(XY) Offset(ZW)");
                if (_HasMask != null)
                {
                    float has = (_MaskTex != null && _MaskTex.textureValue != null) ? 1f : 0f;
                    _HasMask.floatValue = has;
                }
            }

            // Transparency
            if (newMode == (int)RenderingMode.Transparent)
            {
                sShowTrans = EditorGUILayout.Foldout(sShowTrans, "Transparency", true);
                if (sShowTrans)
                {
                    var _Opacity = FindProperty("_Opacity", props, false);
                    var _AlphaMode = FindProperty("_AlphaMode", props, false);
                    Prop(materialEditor, _Opacity, "Opacity");
                    if (_AlphaMode != null)
                    {
                        int aMode = Mathf.Clamp(Mathf.RoundToInt(_AlphaMode.floatValue), 0, 3);
                        int aNew = EditorGUILayout.Popup("Alpha Combine",
                            aMode, new[] { "OpacityOnly", "Multiply (Opacity × A)", "Min", "Max" });
                        if (aNew != aMode) _AlphaMode.floatValue = aNew;
                    }
                }
            }

            // Rim Light
            sShowRim = EditorGUILayout.Foldout(sShowRim, "Rim Light", true);
            if (sShowRim)
            {
                bool rim1On = m0 && GetFloatToggle(m0, "_Rim1Toggle");
                bool rim2On = m0 && GetFloatToggle(m0, "_Rim2Toggle");

                bool rim1New = EditorGUILayout.Toggle("Enable Rim 1", rim1On);
                SetFloatToggleAll(mats, "_Rim1Toggle", rim1New);

                bool rim2New = EditorGUILayout.Toggle("Enable Rim 2", rim2On);
                SetFloatToggleAll(mats, "_Rim2Toggle", rim2New);

                if (rim1New)
                {
                    var _Rim1Color = FindProperty("_Rim1Color", props, false);
                    var _Rim1Power = FindProperty("_Rim1Power", props, false);
                    var _Rim1Int = FindProperty("_Rim1Intensity", props, false);
                    var _Rim1Amb = FindProperty("_Rim1AmbientInfluence", props, false);

                    Prop(materialEditor, _Rim1Color, "Rim 1 Color");
                    Prop(materialEditor, _Rim1Power, "Rim 1 Power");
                    Prop(materialEditor, _Rim1Int, "Rim 1 Intensity");
                    Prop(materialEditor, _Rim1Amb, "Rim 1 Ambient Influence");
                }

                if (rim2New)
                {
                    var _Rim2Color = FindProperty("_Rim2Color", props, false);
                    var _Rim2Power = FindProperty("_Rim2Power", props, false);
                    var _Rim2Int = FindProperty("_Rim2Intensity", props, false);
                    var _Rim2Amb = FindProperty("_Rim2AmbientInfluence", props, false);

                    Prop(materialEditor, _Rim2Color, "Rim 2 Color");
                    Prop(materialEditor, _Rim2Power, "Rim 2 Power");
                    Prop(materialEditor, _Rim2Int, "Rim 2 Intensity");
                    Prop(materialEditor, _Rim2Amb, "Rim 2 Ambient Influence");
                }

                if (m0) SetKeyword(m0, "_RIM_ON", rim1New || rim2New);
            }

            // Reflection
            sShowRefl = EditorGUILayout.Foldout(sShowRefl, "Reflection", true);
            if (sShowRefl)
            {
                bool reflOn = Any(mats, "_REFL_ON");
                bool reflNew = EditorGUILayout.Toggle("Enable Reflection", reflOn);
                ApplyKeywordToAll(mats, "_REFL_ON", reflNew);

                if (reflNew)
                {
                    var _ReflCube = FindProperty("_ReflCube", props, false);
                    var _ReflColor = FindProperty("_ReflColor", props, false);
                    var _ReflInt = FindProperty("_ReflIntensity", props, false);
                    var _ReflFre = FindProperty("_ReflFresnel", props, false);
                    var _ReflRgh = FindProperty("_ReflRoughness", props, false);
                    var _HasRefl = FindProperty("_HasReflCube", props, false);

                    TexLine(materialEditor, "User Cube", _ReflCube, null);
                    if (_HasRefl != null && _ReflCube != null)
                    {
                        float has = (_ReflCube.textureValue != null) ? 1f : 0f;
                        _HasRefl.floatValue = has;
                    }
                    Prop(materialEditor, _ReflColor, "Color");
                    Prop(materialEditor, _ReflInt, "Intensity");
                    Prop(materialEditor, _ReflFre, "Fresnel Power");
                    Prop(materialEditor, _ReflRgh, "Roughness");
                }
            }

            // Emission
            sShowEmiss = EditorGUILayout.Foldout(sShowEmiss, "Emission", true);
            if (sShowEmiss)
            {
                bool emissOn = Any(mats, "_EMISS_ON");
                bool emissNew = EditorGUILayout.Toggle("Enable Emission", emissOn);
                ApplyKeywordToAll(mats, "_EMISS_ON", emissNew);

                if (emissNew)
                {
                    var _EmTex = FindProperty("_EmissiveTex", props, false);
                    var _HasEm = FindProperty("_HasEmissiveTex", props, false);
                    var _EmCol = FindProperty("_EmissiveColor", props, false);
                    var _EmInt = FindProperty("_EmissiveIntensity", props, false);
                    var _UVScale = FindProperty("_EmissUVScale", props, false);
                    var _ScrDir = FindProperty("_EmissScrollDir", props, false);
                    var _ScrSpd = FindProperty("_EmissScrollSpeed", props, false);
                    var _BSpd = FindProperty("_BlinkSpeed", props, false);
                    var _BPh = FindProperty("_BlinkPhase", props, false);
                    var _BMin = FindProperty("_BlinkMin", props, false);
                    var _BMax = FindProperty("_BlinkMax", props, false);

                    TexLine(materialEditor, "Emissive Tex (Optional)", _EmTex, _EmCol);
                    if (_HasEm != null)
                    {
                        float has = (_EmTex != null && _EmTex.textureValue != null) ? 1f : 0f;
                        _HasEm.floatValue = has;
                    }
                    Prop(materialEditor, _EmInt, "Intensity");

                    bool localOn = Any(mats, "_EMISS_LOCALUV_ON");
                    bool localNew = EditorGUILayout.Toggle("Use Local UV (XY)", localOn);
                    ApplyKeywordToAll(mats, "_EMISS_LOCALUV_ON", localNew);
                    if (localNew) Vec(materialEditor, _UVScale, "Local UV Scale (XY)");

                    bool scrollOn = Any(mats, "_EMISS_SCROLL_ON");
                    bool scrollNew = EditorGUILayout.Toggle("UV Scroll", scrollOn);
                    ApplyKeywordToAll(mats, "_EMISS_SCROLL_ON", scrollNew);
                    if (scrollNew)
                    {
                        Vec(materialEditor, _ScrDir, "Scroll Dir (XY)");
                        Prop(materialEditor, _ScrSpd, "Scroll Speed");
                    }

                    bool blinkOn = Any(mats, "_EMISS_BLINK_ON");
                    bool blinkNew = EditorGUILayout.Toggle("Blink", blinkOn);
                    ApplyKeywordToAll(mats, "_EMISS_BLINK_ON", blinkNew);
                    if (blinkNew)
                    {
                        Prop(materialEditor, _BSpd, "Blink Speed");
                        Prop(materialEditor, _BPh, "Blink Phase");
                        Prop(materialEditor, _BMin, "Blink Min");
                        Prop(materialEditor, _BMax, "Blink Max");
                    }
                }
            }

            // Spec Extra
            sShowSpec = EditorGUILayout.Foldout(sShowSpec, "Spec Extra", true);
            if (sShowSpec)
            {
                bool specOn = Any(mats, "_SPEC_EXTRA_ON");
                bool specNew = EditorGUILayout.Toggle("Enable Spec Extra", specOn);
                ApplyKeywordToAll(mats, "_SPEC_EXTRA_ON", specNew);

                if (specNew)
                {
                    var _SpecCol = FindProperty("_SpecExtraColor", props, false);
                    var _SpecInt = FindProperty("_SpecIntensity", props, false);
                    var _SpecMin = FindProperty("_SpecMinPower", props, false);
                    var _SpecMax = FindProperty("_SpecMaxPower", props, false);

                    Prop(materialEditor, _SpecCol, "Color");
                    Prop(materialEditor, _SpecInt, "Intensity");
                    Prop(materialEditor, _SpecMin, "Min Power");
                    Prop(materialEditor, _SpecMax, "Max Power");
                }
            }

            // Vertex Tint
            sShowVtxTint = EditorGUILayout.Foldout(sShowVtxTint, "Vertex Tint (RGBA Masks)", true);
            if (sShowVtxTint)
            {
                var RCol = FindProperty("_VtxTintRColor", props, false);
                var RStr = FindProperty("_VtxTintRStrength", props, false);
                var GCol = FindProperty("_VtxTintGColor", props, false);
                var GStr = FindProperty("_VtxTintGStrength", props, false);
                var BCol = FindProperty("_VtxTintBColor", props, false);
                var BStr = FindProperty("_VtxTintBStrength", props, false);
                var ACol = FindProperty("_VtxTintAColor", props, false);
                var AStr = FindProperty("_VtxTintAStrength", props, false);

                Prop(materialEditor, RCol, "Tint R Color (Vtx R)");
                Prop(materialEditor, RStr, "Tint R Strength");
                Prop(materialEditor, GCol, "Tint G Color (Vtx G)");
                Prop(materialEditor, GStr, "Tint G Strength");
                Prop(materialEditor, BCol, "Tint B Color (Vtx B)");
                Prop(materialEditor, BStr, "Tint B Strength");
                Prop(materialEditor, ACol, "Tint A Color (Vtx A)");
                Prop(materialEditor, AStr, "Tint A Strength");
            }

            if (m0 != null) EditorUtility.SetDirty(m0);
        }
    }
}
#endif
