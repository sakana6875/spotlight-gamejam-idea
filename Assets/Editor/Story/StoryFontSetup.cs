using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Spotlight.Editor.Story
{
    /// <summary>
    /// 用已经导入的 Font 和 Shader 资产生成动态字体。
    /// Font Asset Creator 和 TMP 的 CreateFontAsset 都走 Shader.Find，这个工程里会返回空，于是存出没有材质的空资产。
    /// </summary>
    public static class StoryFontSetup
    {
        private const string SourceFontPath = "Assets/Resources/Fonts/FZshui.TTF";
        private const string ShaderPath = "Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile.shader";
        private const string FontAssetPath = "Assets/Resources/Fonts/StoryFont.asset";

        [MenuItem("Spotlight/Create Story Font")]
        public static void CreateFromMenu()
        {
            CreateStoryFont();
        }

        [InitializeOnLoadMethod]
        private static void CreateWhenMissing()
        {
            EditorApplication.delayCall += () =>
            {
                TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
                if (existing != null && existing.material != null && existing.atlasTexture != null)
                {
                    return;
                }

                CreateStoryFont();
            };
        }

        public static void CreateStoryFont()
        {
            Font source = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (source == null)
            {
                Debug.LogError("找不到字体文件：" + SourceFontPath);
                return;
            }

            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null)
            {
                Debug.LogError("找不到 SDF 着色器：" + ShaderPath);
                return;
            }

            ShaderUtilities.GetShaderPropertyIDs();
            FieldInfo shaderField = typeof(ShaderUtilities).GetField(
                "k_ShaderRef_MobileSDF",
                BindingFlags.Static | BindingFlags.NonPublic);
            if (shaderField == null)
            {
                Debug.LogError("TMP 着色器缓存字段不存在，无法绕过 Shader.Find。");
                return;
            }

            shaderField.SetValue(null, shader);
            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                source,
                48,
                5,
                GlyphRenderMode.SDFAA,
                2048,
                2048,
                AtlasPopulationMode.Dynamic,
                true);
            if (fontAsset == null || fontAsset.material == null || fontAsset.atlasTexture == null)
            {
                Debug.LogError("字体图集没有生成。FZshui.TTF 能导入，失败出在 TMP 创建材质这一步。");
                return;
            }

            fontAsset.name = "StoryFont";
            fontAsset.atlasTexture.name = "StoryFont Atlas";
            fontAsset.material.name = "StoryFont Material";
            fontAsset.material.shader = shader;
            AssetDatabase.DeleteAsset(FontAssetPath);
            AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(FontAssetPath);
            Debug.Log("剧情字体已生成，拖 StoryFont，不要拖 FZshui SDF：" + FontAssetPath, fontAsset);
        }
    }
}
