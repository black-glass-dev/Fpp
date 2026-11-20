using UnityEngine;
using UnityEngine.Rendering;

namespace Fpp
{
    /// <summary>
    /// Resolves a shader that the *player* can actually guarantee, instead of trusting
    /// Shader.Find in a build. Shader.Find returns null whenever no built scene references the
    /// shader and it is not in Always Included Shaders, and new Material(null) throws
    /// ArgumentNullException("shader") - the exact crash reported from GunplayFeedback.Awake.
    /// The last fallback is a URP-internal error shader that is always present with URP, so a
    /// missing cosmetic shader degrades one effect instead of killing the whole player.
    /// </summary>
    public static class RuntimeShader
    {
        public static Shader Find(string name)
        {
            var shader = Shader.Find(name);
            if (shader != null) return shader;
            shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null)
            {
                Debug.LogWarning("Runtime shader missing in this player build, using URP Unlit instead: " + name);
                return shader;
            }
            shader = Shader.Find("Hidden/InternalErrorShader");
            if (shader == null) Debug.LogError("No usable fallback shader in this player build: " + name);
            return shader;
        }

        /// <summary>Material that never throws on a stripped shader; null only if the player has no shader at all.</summary>
        public static Material Material(string shaderName, Color colour)
        {
            var shader = Find(shaderName);
            if (shader == null) return null;
            return new Material(shader) { color = colour, hideFlags = HideFlags.HideAndDontSave };
        }
    }
}
