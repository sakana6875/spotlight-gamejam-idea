using System;
using UnityEngine;
using Spotlight.Domain.Save;
using Spotlight.Application.Services.Scene;

namespace Spotlight.Demos.Demo1
{
    public sealed class Demo1SceneEntry : MonoBehaviour
    {
        [Header("Local Controllers")]
        [SerializeField] private Demo1FlowController _flowController;
        [SerializeField] private Demo1CheckpointController _checkpointController;

        public bool IsInitialized { get; private set; }
        public ISaveService SaveService { get; private set; }
        public ISceneFlow SceneFlow { get; private set; }

        public bool Initialize(ISaveService saveService, ISceneFlow sceneFlow)
        {
            if (saveService == null)
            {
                Debug.LogError("[Demo1SceneEntry] 初始化失败：saveService 为 null。", this);
                return false;
            }

            if (sceneFlow == null)
            {
                Debug.LogError("[Demo1SceneEntry] 初始化失败：sceneFlow 为 null。", this);
                return false;
            }

            if (IsInitialized)
            {
                if (SaveService == saveService && SceneFlow == sceneFlow)
                {
                    return true;
                }

                Debug.LogError("[Demo1SceneEntry] 初始化失败：尝试重复注入不同的服务实例。", this);
                return false;
            }

            if (_flowController == null || _checkpointController == null)
            {
                Debug.LogError("[Demo1SceneEntry] 初始化失败：场景 Hierarchy 中未绑定 _flowController 或 _checkpointController。", this);
                return false;
            }

            SaveService = saveService;
            SceneFlow = sceneFlow;

            _checkpointController.Setup(SaveService);
            _flowController.Setup(SceneFlow);

            IsInitialized = true;
            return true;
        }
    }
}