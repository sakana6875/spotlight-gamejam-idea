using System;
using System.Collections.Generic;

namespace Healing.Demo3.Domain
{
    public enum Demo3Ending
    {
        StillYou = 1,  // 结局一：还是你（未点亮全部石碑）
        IsItYou  = 2,  // 结局二：是你吗？（点亮全部 L 区域石碑）
    }

    /// <summary>结局判定（纯规则）：所有必需石碑均已点亮 → 结局二，否则结局一</summary>
    public static class EndingRules
    {
        public static Demo3Ending Decide(IReadOnlyCollection<string> requiredTablets,
                                         Func<string, bool> isLit)
        {
            foreach (var id in requiredTablets)
                if (!isLit(id)) return Demo3Ending.StillYou;
            return Demo3Ending.IsItYou;
        }
    }
}
