using System;
using UnityEngine;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 在石头进入坑的 Trigger 时发布一次填充事实。场景必须把该体积放在坑上。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class Demo2PitFillTrigger : MonoBehaviour
    {
        private bool _isConfigured;
        private bool _hasFilled;

        /// <summary>
        /// 可推动障碍物第一次进入本 Trigger 的事实。
        /// </summary>
        public event Action PitFilled;

        private void Awake()
        {
            Collider2D pitCollider = GetComponent<Collider2D>();
            if (!pitCollider.isTrigger)
            {
                Debug.LogError("Demo2PitFillTrigger 的 Collider2D 必须是 Trigger。", this);
                enabled = false;
                return;
            }

            _isConfigured = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isConfigured || _hasFilled ||
                other.GetComponentInParent<Demo2PushableObstacle>() == null)
            {
                return;
            }

            _hasFilled = true;
            PitFilled?.Invoke();
        }
    }
}
