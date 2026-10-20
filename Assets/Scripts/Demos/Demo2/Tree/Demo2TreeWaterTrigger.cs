using UnityEngine;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 在水首次到达中心检测点时触发树苗发芽。
    /// </summary>
    public sealed class Demo2TreeWaterTrigger : MonoBehaviour
    {
        [SerializeField] private Demo2WaterPresenceSource _waterController;
        [SerializeField] private Transform _waterTarget;
        [SerializeField] private Demo2TreeController _treeController;

        private bool _isConfigured;
        private bool _hasTriggered;

        private void Awake()
        {
            if (_waterController == null || _waterTarget == null || _treeController == null)
            {
                Debug.LogError("Demo2TreeWaterTrigger 缺少水控制器、中心检测点或树控制器引用。", this);
                enabled = false;
                return;
            }

            _isConfigured = true;
        }

        private void FixedUpdate()
        {
            if (!_isConfigured || _hasTriggered ||
                !_waterController.IsWaterAtWorldPosition(_waterTarget.position))
            {
                return;
            }

            Demo2TreeTransitionResult result = _treeController.BeginSprouting();
            if (result == Demo2TreeTransitionResult.Succeeded ||
                _treeController.Phase != Demo2TreePhase.Dormant)
            {
                _hasTriggered = true;
            }
        }
    }
}
