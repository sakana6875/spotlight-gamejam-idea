using UnityEngine;
using Spotlight.Demos.Demo1.Domain.Rhythm;

namespace Spotlight.Demos.Demo1.Data
{
    /// <summary>
    /// 判定窗口配置资产。默认值与 spec 一致：Perfect ±60ms，Good ±120ms。
    /// </summary>
    [CreateAssetMenu(fileName = "JudgeWindowConfigAsset", menuName = "Spotlight/Demo1/Judge Window Config")]
    public sealed class JudgeWindowConfigAsset : ScriptableObject
    {
        [SerializeField] private float _perfectSeconds = 0.060f;
        [SerializeField] private float _goodSeconds = 0.120f;

        public JudgeWindow ToDomain()
        {
            return new JudgeWindow(_perfectSeconds, _goodSeconds);
        }
    }
}