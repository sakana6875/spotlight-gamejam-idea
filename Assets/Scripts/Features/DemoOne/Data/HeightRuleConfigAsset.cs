using UnityEngine;
using Spotlight.Features.DemoOne.Domain;

namespace Spotlight.Features.DemoOne.Data
{
    /// <summary>
    /// 高度规则配置资产。默认值与 spec 一致：Base=1000，Min=100，Good扣10，Miss扣20。
    /// </summary>
    [CreateAssetMenu(fileName = "HeightRuleConfigAsset", menuName = "Spotlight/Demo1/Height Rule Config")]
    public sealed class HeightRuleConfigAsset : ScriptableObject
    {
        [SerializeField] private float _baseHeight = 1000f;
        [SerializeField] private float _minHeight = 100f;
        [SerializeField] private float _goodPenalty = 10f;
        [SerializeField] private float _missPenalty = 20f;

        public HeightRuleConfig ToDomain()
        {
            return new HeightRuleConfig(_baseHeight, _minHeight, _goodPenalty, _missPenalty);
        }
    }
}