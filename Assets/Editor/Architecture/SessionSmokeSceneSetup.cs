using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Spotlight.Composition;

namespace Spotlight.Editor.Architecture
{
    /// <summary>
    /// 通过 Unity Editor 幂等装配 SampleScene 的架构烟测入口。
    /// </summary>
    public static class SessionSmokeSceneSetup
    {
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
        private const string SmokeObjectName = "ArchitectureSmoke";

        [MenuItem("Tools/Architecture/Apply Session Smoke Scene Setup")]
        public static void Apply()
        {
            Scene scene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
            GameObject smokeObject = FindOrCreateSmokeObject(scene);
            EnsureSingleComponent<SessionRoot>(smokeObject);
            EnsureSingleComponent<SessionSmokeEntry>(smokeObject);

            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new System.InvalidOperationException("无法保存 SampleScene：" + SampleScenePath);
            }

            EditorSceneManager.CloseScene(scene, true);
            AssetDatabase.SaveAssets();
        }

        private static GameObject FindOrCreateSmokeObject(Scene scene)
        {
            GameObject selectedObject = null;
            GameObject[] rootObjects = scene.GetRootGameObjects();
            for (int index = 0; index < rootObjects.Length; index++)
            {
                if (rootObjects[index].name != SmokeObjectName)
                {
                    continue;
                }

                if (selectedObject == null)
                {
                    selectedObject = rootObjects[index];
                }
                else
                {
                    Object.DestroyImmediate(rootObjects[index]);
                }
            }

            if (selectedObject != null)
            {
                return selectedObject;
            }

            GameObject createdObject = new GameObject(SmokeObjectName);
            SceneManager.MoveGameObjectToScene(createdObject, scene);
            return createdObject;
        }

        private static void EnsureSingleComponent<TComponent>(GameObject gameObject)
            where TComponent : Component
        {
            TComponent[] components = gameObject.GetComponents<TComponent>();
            if (components.Length == 0)
            {
                gameObject.AddComponent<TComponent>();
                return;
            }

            for (int index = 1; index < components.Length; index++)
            {
                Object.DestroyImmediate(components[index]);
            }
        }
    }
}
