using UnityEngine;

namespace com.albol.TiltingMaze
{
    /// <summary>
    /// Material helpers that work in both the Built-in render pipeline (Standard shader)
    /// and URP (Lit shader). New materials are copies of the pipeline's default material,
    /// so no shader names have to be hard-coded.
    /// </summary>
    public static class MazeMaterials
    {
        static Material _defaultLit;

        public static Material DefaultLit
        {
            get
            {
                if (_defaultLit == null)
                {
                    var probe = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    _defaultLit = probe.GetComponent<Renderer>().sharedMaterial;
                    Object.DestroyImmediate(probe);
                }
                return _defaultLit;
            }
        }

        public static Material Create(string name) => new Material(DefaultLit) { name = name };

        /// <summary>
        /// Sets colour, texture, metallic and smoothness on Standard, URP Lit or HDRP Lit.
        /// specularHighlights = false gives a fully matte, non-specular surface.
        /// </summary>
        public static void Configure(Material m, Color color, float metallic, float smoothness,
            Texture texture = null, bool specularHighlights = true)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            if (texture != null)
            {
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", texture);
                if (m.HasProperty("_BaseColorMap")) m.SetTexture("_BaseColorMap", texture);
                if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", texture);
            }
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);

            float on = specularHighlights ? 1f : 0f;
            if (m.HasProperty("_SpecularHighlights")) m.SetFloat("_SpecularHighlights", on);
            if (m.HasProperty("_GlossyReflections")) m.SetFloat("_GlossyReflections", on);
            if (m.HasProperty("_EnvironmentReflections")) m.SetFloat("_EnvironmentReflections", on);
            // Keywords: the first is shared by Standard and URP, then Standard, then URP.
            SetKeyword(m, "_SPECULARHIGHLIGHTS_OFF", !specularHighlights);
            SetKeyword(m, "_GLOSSYREFLECTIONS_OFF", !specularHighlights);
            SetKeyword(m, "_ENVIRONMENTREFLECTIONS_OFF", !specularHighlights);
        }

        public static void SetEmission(Material m, Color emission)
        {
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", emission);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        static void SetKeyword(Material m, string keyword, bool on)
        {
            if (on) m.EnableKeyword(keyword);
            else m.DisableKeyword(keyword);
        }
    }
}
