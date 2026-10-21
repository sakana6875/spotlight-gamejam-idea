using UnityEngine;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 保存单类 Demo2 怪物可由策划调整的运行时数值。
    /// </summary>
    [CreateAssetMenu(fileName = "Demo2MonsterConfig", menuName = "Spotlight/Demo2/Monster Config")]
    public sealed class Demo2MonsterConfig : ScriptableObject
    {
        [SerializeField] private int _maxHealth = 30;
        [SerializeField] private float _moveSpeed = 1f;
        [SerializeField] private float _biteDistance = 0.5f;
        [SerializeField] private float _hitStunDurationSeconds = 0.5f;
        [SerializeField] private float _deathDurationSeconds = 0.5f;

        [Header("跳跃")]
        [SerializeField] private float _jumpHeight = 3.5f;
        [SerializeField] private float _obstacleDetectionDistance = 0.35f;
        [SerializeField] private float _landingRetrySeconds = 0.3f;

        /// <summary>
        /// 初始及最大生命值。
        /// </summary>
        public int MaxHealth => _maxHealth;

        /// <summary>
        /// 侧视水平移动速度，单位为米每秒。
        /// </summary>
        public float MoveSpeed => _moveSpeed;

        /// <summary>
        /// 与树苗中心的水平距离不大于该值时开始啃食。
        /// </summary>
        public float BiteDistance => _biteDistance;

        /// <summary>
        /// 每次未致死受击产生的硬直秒数。
        /// </summary>
        public float HitStunDurationSeconds => _hitStunDurationSeconds;

        /// <summary>
        /// 生命归零后保留死亡表现的秒数。
        /// </summary>
        public float DeathDurationSeconds => _deathDurationSeconds;

        /// <summary>
        /// 自动跳跃的目标最高点相对起跳点高度，单位为米。
        /// </summary>
        public float JumpHeight => _jumpHeight;

        /// <summary>
        /// 身体沿朝树方向检测前方实体障碍的距离，单位为米。
        /// </summary>
        public float ObstacleDetectionDistance => _obstacleDetectionDistance;

        /// <summary>
        /// 自动跳跃落地后再次允许起跳前的等待秒数。
        /// </summary>
        public float LandingRetrySeconds => _landingRetrySeconds;

        /// <summary>
        /// 验证所有数值能否安全创建怪物状态模型。
        /// </summary>
        public bool TryValidate(out string error)
        {
            if (_maxHealth <= 0)
            {
                error = "最大生命必须是正整数。";
                return false;
            }

            if (!IsPositiveFinite(_moveSpeed))
            {
                error = "移动速度必须是有限正数。";
                return false;
            }

            if (!IsPositiveFinite(_biteDistance))
            {
                error = "啃食距离必须是有限正数。";
                return false;
            }

            if (!IsPositiveFinite(_hitStunDurationSeconds))
            {
                error = "受击硬直时长必须是有限正数。";
                return false;
            }

            if (!IsPositiveFinite(_deathDurationSeconds))
            {
                error = "死亡表现时长必须是有限正数。";
                return false;
            }

            if (!IsPositiveFinite(_jumpHeight))
            {
                error = "跳跃高度必须是有限正数。";
                return false;
            }

            if (!IsPositiveFinite(_obstacleDetectionDistance))
            {
                error = "障碍检测距离必须是有限正数。";
                return false;
            }

            if (!IsNonNegativeFinite(_landingRetrySeconds))
            {
                error = "落地重试间隔必须是有限非负数。";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static bool IsPositiveFinite(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsNonNegativeFinite(float value)
        {
            return value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
