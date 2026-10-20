using System;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 伤害请求的明确执行结果。
    /// </summary>
    public enum MonsterDamageResult
    {
        InvalidAmount,
        Applied,
        Killed,
        AlreadyDead
    }

    /// <summary>
    /// 怪物向伤害来源和波次统计公开的最小战斗边界。
    /// </summary>
    public interface IMonster
    {
        /// <summary>
        /// 怪物是否仍可接受伤害并参与玩法。
        /// </summary>
        bool IsAlive { get; }

        /// <summary>
        /// 当前剩余生命值。
        /// </summary>
        int CurrentHealth { get; }

        /// <summary>
        /// 生命首次降至零时发布的事实通知。
        /// </summary>
        event Action<IMonster> Died;

        /// <summary>
        /// 尝试施加正整数伤害，并返回伤害或死亡结果。
        /// </summary>
        MonsterDamageResult TryTakeDamage(int damage);
    }
}
