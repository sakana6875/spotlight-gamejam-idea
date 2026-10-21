using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Spotlight.Editor.Bootstrap
{
    /// <summary>
    /// 让编辑器 Play 从 BootStrap 进入，保证 SessionRoot 先创建再加载正式场景。
    /// </summary>
    [InitializeOnLoad]
    public static class PlayFromBootstrap
    {
        private const string BootstrapScenePath = "Assets/Scenes/BootStrap.unity";

        static PlayFromBootstrap()
        {
            EditorApplication.delayCall += Apply;
        }

        private static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            SceneAsset bootstrapScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath);
            if (bootstrapScene == null)
            {
                Debug.LogError("找不到 BootStrap 场景：" + BootstrapScenePath);
                return;
            }

            if (EditorSceneManager.playModeStartScene != bootstrapScene)
            {
                EditorSceneManager.playModeStartScene = bootstrapScene;
            }
        }
    }
}
