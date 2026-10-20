using Spotlight.Scene;
using UnityEngine;

namespace Spotlight.Bootstrap
{
    /// <summary>
    /// 在唯一组合根完成初始化后，通过稳定场景ID进入正式初始场景。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SessionRoot))]
    [DefaultExecutionOrder(100)]
    public sealed class BootstrapEntry : MonoBehaviour
    {
        [SerializeField] private string _initialSceneId = SceneCatalog.Demo1;

        private SessionRoot _sessionRoot;

        private void Awake()
        {
            _sessionRoot = GetComponent<SessionRoot>();
            if (_sessionRoot != null)
            {
                return;
            }

            Debug.LogError("BootstrapEntry必须与SessionRoot位于同一GameObject。", this);
            enabled = false;
        }

        private void Start()
        {
            if (!enabled)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_initialSceneId))
            {
                Debug.LogError("BootstrapEntry的Initial Scene Id不能为空。", this);
                return;
            }

            if (!_sessionRoot.Initialize() || _sessionRoot.SceneLoader == null)
            {
                Debug.LogError("BootstrapEntry无法使用已初始化的SessionRoot。", this);
                return;
            }

            SceneLoadResult result = _sessionRoot.SceneLoader.LoadScene(_initialSceneId);
            if (result == SceneLoadResult.Started || result == SceneLoadResult.AlreadyActive)
            {
                return;
            }

            Debug.LogError(
                $"BootstrapEntry加载初始场景失败：ID={_initialSceneId}，结果={result}。",
                this);
        }
    }
}
