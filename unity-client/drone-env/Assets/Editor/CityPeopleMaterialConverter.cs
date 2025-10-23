#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Utility to upgrade the City People material set to URP.
/// </summary>
public static class CityPeopleMaterialConverter
{
    private const string TargetFolder = "Assets/DenysAlmaral/CityPeople-FREE";
    private const string ShaderName = "Universal Render Pipeline/Lit";

    [MenuItem("Tools/CityPeople/Convert Materials To URP" , priority = 10)]
    private static void ConvertMaterials()
    {
        if (!AssetDatabase.IsValidFolder(TargetFolder))
        {
            Debug.LogError($"[CityPeopleMaterialConverter] Folder not found: {TargetFolder}");
            return;
        }

        var shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            Debug.LogError($"[CityPeopleMaterialConverter] Shader not found: {ShaderName}. Ensure URP is installed.");
            return;
        }

        var guids = AssetDatabase.FindAssets("t:Material", new[] { TargetFolder });
        if (guids.Length == 0)
        {
            Debug.LogWarning("[CityPeopleMaterialConverter] No materials found to convert.");
            return;
        }

        var converted = new List<string>();
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".mat")) continue;
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) continue;

            if (material.shader != null && material.shader == shader)
            {
                continue; // already converted
            }

            var originalShaderName = material.shader != null ? material.shader.name : "<null>";

            // Cache Standard shader values before switching to URP
            var mainTex = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
            var mainScale = material.HasProperty("_MainTex") ? material.GetTextureScale("_MainTex") : Vector2.one;
            var mainOffset = material.HasProperty("_MainTex") ? material.GetTextureOffset("_MainTex") : Vector2.zero;
            var color = material.HasProperty("_Color") ? (Color?)material.GetColor("_Color") : null;
            var emissionColor = material.HasProperty("_EmissionColor") ? (Color?)material.GetColor("_EmissionColor") : null;
            var emissionMap = material.HasProperty("_EmissionMap") ? material.GetTexture("_EmissionMap") : null;
            var emissionScale = material.HasProperty("_EmissionMap") ? material.GetTextureScale("_EmissionMap") : Vector2.one;
            var emissionOffset = material.HasProperty("_EmissionMap") ? material.GetTextureOffset("_EmissionMap") : Vector2.zero;
            var metallicGlossMap = material.HasProperty("_MetallicGlossMap") ? material.GetTexture("_MetallicGlossMap") : null;
            var metallicGlossScale = material.HasProperty("_MetallicGlossMap") ? material.GetTextureScale("_MetallicGlossMap") : Vector2.one;
            var metallicGlossOffset = material.HasProperty("_MetallicGlossMap") ? material.GetTextureOffset("_MetallicGlossMap") : Vector2.zero;
            var metallic = material.HasProperty("_Metallic") ? (float?)material.GetFloat("_Metallic") : null;
            var glossiness = material.HasProperty("_Glossiness") ? (float?)material.GetFloat("_Glossiness") : null;
            var bumpMap = material.HasProperty("_BumpMap") ? material.GetTexture("_BumpMap") : null;
            var bumpScale = material.HasProperty("_BumpMap") ? material.GetTextureScale("_BumpMap") : Vector2.one;
            var bumpOffset = material.HasProperty("_BumpMap") ? material.GetTextureOffset("_BumpMap") : Vector2.zero;
            var bumpStrength = material.HasProperty("_BumpScale") ? (float?)material.GetFloat("_BumpScale") : null;
            var occlusionMap = material.HasProperty("_OcclusionMap") ? material.GetTexture("_OcclusionMap") : null;
            var occlusionScale = material.HasProperty("_OcclusionMap") ? material.GetTextureScale("_OcclusionMap") : Vector2.one;
            var occlusionOffset = material.HasProperty("_OcclusionMap") ? material.GetTextureOffset("_OcclusionMap") : Vector2.zero;
            var occlusionStrength = material.HasProperty("_OcclusionStrength") ? (float?)material.GetFloat("_OcclusionStrength") : null;
            var cutoff = material.HasProperty("_Cutoff") ? (float?)material.GetFloat("_Cutoff") : null;
            bool alphaTest = material.IsKeywordEnabled("_ALPHATEST_ON");
            bool alphaBlend = material.IsKeywordEnabled("_ALPHABLEND_ON") || material.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON");

            material.shader = shader;

            if (mainTex != null)
            {
                material.SetTexture("_BaseMap", mainTex);
                material.SetTextureScale("_BaseMap", mainScale);
                material.SetTextureOffset("_BaseMap", mainOffset);
            }
            if (color.HasValue)
            {
                material.SetColor("_BaseColor", color.Value);
            }
            if (emissionMap != null)
            {
                material.SetTexture("_EmissionMap", emissionMap);
                material.SetTextureScale("_EmissionMap", emissionScale);
                material.SetTextureOffset("_EmissionMap", emissionOffset);
            }
            if (emissionColor.HasValue)
            {
                material.SetColor("_EmissionColor", emissionColor.Value);
                if (emissionColor.Value.maxColorComponent > 0f)
                {
                    material.EnableKeyword("_EMISSION");
                }
            }
            if (metallicGlossMap != null)
            {
                material.SetTexture("_MetallicGlossMap", metallicGlossMap);
                material.SetTextureScale("_MetallicGlossMap", metallicGlossScale);
                material.SetTextureOffset("_MetallicGlossMap", metallicGlossOffset);
            }
            if (metallic.HasValue)
            {
                material.SetFloat("_Metallic", metallic.Value);
            }
            if (glossiness.HasValue)
            {
                material.SetFloat("_Smoothness", glossiness.Value);
            }
            if (bumpMap != null)
            {
                material.SetTexture("_BumpMap", bumpMap);
                material.SetTextureScale("_BumpMap", bumpScale);
                material.SetTextureOffset("_BumpMap", bumpOffset);
            }
            if (bumpStrength.HasValue)
            {
                material.SetFloat("_BumpScale", bumpStrength.Value);
            }
            if (occlusionMap != null)
            {
                material.SetTexture("_OcclusionMap", occlusionMap);
                material.SetTextureScale("_OcclusionMap", occlusionScale);
                material.SetTextureOffset("_OcclusionMap", occlusionOffset);
            }
            if (occlusionStrength.HasValue)
            {
                material.SetFloat("_OcclusionStrength", occlusionStrength.Value);
            }
            if (cutoff.HasValue)
            {
                material.SetFloat("_Cutoff", cutoff.Value);
            }

            if (alphaTest)
            {
                material.SetFloat("_AlphaClip", 1f);
            }

            if (alphaBlend)
            {
                material.SetFloat("_Surface", 1f); // Transparent
            }

            EditorUtility.SetDirty(material);
            converted.Add(Path.GetFileName(path) + $" ({originalShaderName} -> {ShaderName})");
        }

        if (converted.Count > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[CityPeopleMaterialConverter] Converted {converted.Count} materials to URP.\n" + string.Join("\n", converted));
        }
        else
        {
            Debug.Log("[CityPeopleMaterialConverter] All CityPeople materials are already using URP.");
        }
    }
}
#endif
