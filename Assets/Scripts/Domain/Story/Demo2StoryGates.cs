namespace Spotlight.Domain.Story
{
    /// <summary>
    /// 进化之地 2.0 文案的切幕边界。对白可以点过去，只有这些节点才换场景或等待玩法。
    /// </summary>
    public static class Demo2StoryGates
    {
        /// <summary>
        /// 进入进化之地后要留在画面上的第一句。加载场景时不能把它跳掉。
        /// </summary>
        public const string Demo2EntryLineId = "evolution_005";

        /// <summary>
        /// 侧视感谢句。水管关已删除，确认后进入第二幕，下一句在新场景出现。
        /// </summary>
        public const string Act1ExitLineId = "evolution_013";


        /// <summary>
        /// 树长成后要在第二幕播放的灌溉句。
        /// </summary>
        public const string Act2EntryLineId = "evolution_019";

        /// <summary>
        /// 音游说明的最后一句。确认后先进入音游，回来才显示选项。
        /// </summary>
        public const string Demo1LeadInLineId = "music_007";

        /// <summary>
        /// 推动教学句。确认后要等玩家开始推石，才播放下一句鼓励。
        /// </summary>
        public const string PushNarrationGateId = "evolution_006";

        /// <summary>
        /// 鼓励句。确认后要等坑被填上或树发芽，才播放第一抹绿。
        /// </summary>
        public const string PitNarrationGateId = "evolution_007";

        /// <summary>
        /// 发芽感谢句。确认后要等第一只怪物出现，才播放虚无兽涌来。
        /// </summary>
        public const string MonsterNarrationGateId = "evolution_008";

        /// <summary>
        /// 攻击教学句。确认后要等玩家打出攻击，才播放坚持住。
        /// </summary>
        public const string AttackNarrationGateId = "evolution_011";

        /// <summary>
        /// 战斗中的鼓励句。确认后要等这一波清空，才播放再次感谢。
        /// </summary>
        public const string WaveNarrationGateId = "evolution_012";

        /// <summary>
        /// 第二幕灌溉句。确认后要等啃食开始，才播放什么声音。
        /// </summary>
        public const string BiteNarrationGateId = "evolution_019";

        /// <summary>
        /// 兽王提示句。确认后等待兽王撞树死亡，再播放胜利旁白。
        /// </summary>
        public const string BossNarrationGateId = "evolution_023";

        /// <summary>
        /// 这句确认后不能直接点进下一句，要等对应的玩法事实。第二幕事实不接受提前完成。
        /// </summary>
        public static bool TryGetAdvanceGate(string nodeId, out bool rememberEarly)
        {
            switch (nodeId)
            {
                case PushNarrationGateId:
                case PitNarrationGateId:
                case MonsterNarrationGateId:
                case AttackNarrationGateId:
                case WaveNarrationGateId:
                    rememberEarly = true;
                    return true;
                case BiteNarrationGateId:
                case BossNarrationGateId:
                    rememberEarly = false;
                    return true;
                default:
                    rememberEarly = false;
                    return false;
            }
        }

    }
}
