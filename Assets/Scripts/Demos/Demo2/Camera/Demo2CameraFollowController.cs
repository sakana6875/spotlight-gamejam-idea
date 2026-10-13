using UnityEngine;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 在正常玩法期间按玩家位移跟随；视图转场临时接管相机位置时暂停跟随。
    /// </summary>
    public sealed class Demo2CameraFollowController : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _offset = new Vector3(0f, 0f, -10f);

        private bool _isFollowing = true;
        private Vector3 _lastTargetPosition;

        /// <summary>
        /// 当前相机是否正在根据玩家位置更新。
        /// </summary>
        public bool IsFollowing => _isFollowing;

        private void Awake()
        {
            if (_target == null)
            {
                Debug.LogError("Demo2CameraFollowController 缺少玩家引用。", this);
                enabled = false;
                return;
            }

            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (!_isFollowing)
            {
                return;
            }

            Vector3 targetPosition = _target.position;
            transform.position += targetPosition - _lastTargetPosition;
            _lastTargetPosition = targetPosition;
        }

        /// <summary>
        /// 设置正常玩法中的相机跟随状态。恢复跟随时保留当前构图，直到玩家实际移动。
        /// </summary>
        public void SetFollowing(bool isFollowing)
        {
            _isFollowing = isFollowing;

            if (_isFollowing)
            {
                _lastTargetPosition = _target.position;
            }
        }

        /// <summary>
        /// 立即将相机对齐到当前玩家位置，并将该位置作为后续位移跟随的基准。
        /// </summary>
        public void SnapToTarget()
        {
            transform.position = _target.position + _offset;
            _lastTargetPosition = _target.position;
        }
    }
}
