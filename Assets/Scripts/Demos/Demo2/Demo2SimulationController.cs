using System.Collections.Generic;
using UnityEngine;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 协调场景内必须同时冻结的玩法状态，并保留冻结前的全局时间倍率。
    /// </summary>
    public sealed class Demo2SimulationController : MonoBehaviour
    {
        private readonly HashSet<Demo2PauseReason> _pauseReasons = new HashSet<Demo2PauseReason>();

        private float _resumeTimeScale = 1f;

        /// <summary>
        /// 是否存在阻止玩法继续推进的暂停理由。
        /// </summary>
        public bool IsGameplayFrozen => _pauseReasons.Count > 0;

        /// <summary>
        /// 增加一个暂停理由；首个理由冻结 Unity 的缩放时间。
        /// </summary>
        public void AcquirePause(Demo2PauseReason reason)
        {
            if (!_pauseReasons.Add(reason))
            {
                return;
            }

            if (_pauseReasons.Count != 1)
            {
                return;
            }

            _resumeTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        /// <summary>
        /// 移除一个暂停理由；最后一个理由释放后恢复先前的时间倍率。
        /// </summary>
        public void ReleasePause(Demo2PauseReason reason)
        {
            if (!_pauseReasons.Remove(reason) || _pauseReasons.Count != 0)
            {
                return;
            }

            Time.timeScale = _resumeTimeScale;
        }

        private void OnDisable()
        {
            if (_pauseReasons.Count == 0)
            {
                return;
            }

            _pauseReasons.Clear();
            Time.timeScale = _resumeTimeScale;
        }
    }

    /// <summary>
    /// Demo2 中会冻结全部玩法推进的原因。
    /// </summary>
    public enum Demo2PauseReason
    {
        Dialogue,
        ViewTransition,
        PlayerDead
    }
}
