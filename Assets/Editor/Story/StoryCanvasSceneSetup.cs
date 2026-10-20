using System.IO;
using Spotlight.Story;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace Spotlight.Editor.Story
{
    /// <summary>
    /// 把 BootStrap 上的对话框做成预制体，放进 Hub 和仍要显示旁白的 Demo 场景。
    /// 剧情宿主留在 SessionRoot，场景卸载时对话框跟着场景走，避免开场叠在启动场景上。
    /// </summary>
    public static class StoryCanvasSceneSetup
    {
        private const string BootstrapScenePath = "Assets/Scenes/BootStrap.unity";
        private const string PrefabPath = "Assets/Prefabs/Story/StoryCanvas.prefab";
        private const string CanvasName = "StoryCanvas";

        private static readonly string[] TargetScenePaths =
        {
            "Assets/Scenes/Hub.unity",
            "Assets/Scenes/demo1.unity",
            "Assets/Scenes/demo2.unity",
            "Assets/Scenes/demo2_act1.unity"
        };

        [MenuItem("Spotlight/Move Story Canvas Into Scenes")]
        public static void Apply()
        {
            EnsurePrefab();
            for (int index = 0; index < TargetScenePaths.Length; index++)
            {
                EnsureInstance(TargetScenePaths[index]);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsurePrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            {
                RemoveBootstrapCanvas();
                return;
            }

            UnityEngine.SceneManagement.Scene loadedScene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
            GameObject canvas = FindCanvas(loadedScene);
            if (canvas == null)
            {
                throw new System.InvalidOperationException("BootStrap 里找不到 " + CanvasName + "，也无法从预制体恢复。");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath) ?? "Assets/Prefabs/Story");
            PrefabUtility.SaveAsPrefabAsset(canvas, PrefabPath);
            Object.DestroyImmediate(canvas);
            if (!EditorSceneManager.SaveScene(loadedScene))
            {
                throw new System.InvalidOperationException("无法保存 BootStrap：" + BootstrapScenePath);
            }
        }

        private static void RemoveBootstrapCanvas()
        {
            UnityEngine.SceneManagement.Scene loadedScene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
            GameObject canvas = FindCanvas(loadedScene);
            if (canvas == null)
            {
                return;
            }

            Object.DestroyImmediate(canvas);
            if (!EditorSceneManager.SaveScene(loadedScene))
            {
                throw new System.InvalidOperationException("无法保存 BootStrap：" + BootstrapScenePath);
            }
        }

        private static void EnsureInstance(string scenePath)
        {
            UnityEngine.SceneManagement.Scene loadedScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (FindCanvas(loadedScene) != null)
            {
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                throw new System.InvalidOperationException("找不到对话框预制体：" + PrefabPath);
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, loadedScene);
            if (instance == null || instance.GetComponent<StoryDialogueView>() == null)
            {
                throw new System.InvalidOperationException("无法把对话框放进场景：" + scenePath);
            }

            if (!EditorSceneManager.SaveScene(loadedScene))
            {
                throw new System.InvalidOperationException("无法保存场景：" + scenePath);
            }
        }

        private static GameObject FindCanvas(UnityEngine.SceneManagement.Scene loadedScene)
        {
            GameObject[] roots = loadedScene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                if (roots[index].name == CanvasName)
                {
                    return roots[index];
                }

                Transform child = roots[index].transform.Find(CanvasName);
                if (child != null)
                {
                    return child.gameObject;
                }
            }

            return null;
        }
    }
}
