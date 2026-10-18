using UnityEngine;
using Spotlight.Demos.Demo1.Domain.Rhythm;

namespace Spotlight.Demos.Demo1.Adapters
{
    /// <summary>
    /// 判定反馈粒子对象池。
    /// 预创建固定数量的 ParticleSystem，避免运行时 Instantiate 造成 GC 峰值。
    /// 三种判定对应三种颜色，由 Inspector 配置。
    /// </summary>
    public sealed class NoteFeedbackParticlePool : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _particlePrefab;
        [SerializeField] private int _poolSize = 8;
        [SerializeField] private Color _perfectColor = new Color(1f, 0.9f, 0.4f);
        [SerializeField] private Color _goodColor = new Color(0.4f, 0.8f, 1f);
        [SerializeField] private Color _missColor = new Color(1f, 0.4f, 0.4f);

        private ParticleSystem[] _pool;
        private int _cursor;

        private void Awake()
        {
            if (_particlePrefab == null)
            {
                throw new System.InvalidOperationException(
                    "NoteFeedbackParticlePool 未指定 _particlePrefab。");
            }
            _pool = new ParticleSystem[_poolSize];
            for (int i = 0; i < _poolSize; i++)
            {
                var ps = Instantiate(_particlePrefab, transform);
                ps.gameObject.SetActive(false);
                _pool[i] = ps;
            }
        }

        /// <summary>在指定世界坐标播放一次判定反馈粒子。</summary>
        public void Play(Vector3 worldPosition, JudgeResult result)
        {
            var ps = _pool[_cursor];
            _cursor = (_cursor + 1) % _pool.Length;

            ps.transform.position = worldPosition;
            var main = ps.main;
            main.startColor = GetColor(result);
            ps.gameObject.SetActive(true);
            ps.Clear();
            ps.Play();
        }

        private Color GetColor(JudgeResult result)
        {
            switch (result)
            {
                case JudgeResult.Perfect: return _perfectColor;
                case JudgeResult.Good: return _goodColor;
                default: return _missColor;
            }
        }
    }
}