namespace Healing.Demo3.Application
{
    /// <summary>
    /// 各幕场景的稳定 ID（即 Build Settings 中的场景名，必须与实际场景文件名逐字一致）。
    /// 当前工程场景地图（Scenes/Demo3/）：
    ///   Act1（看客压迫）→ Act2（营救，末菇落地）→ Act3（大树枢纽，第1次：男孩退场）
    ///   → Act2-1（第三幕：追寻蘑菇+隐藏路线）→ Act4（看客区迷失晕倒）→ Act2-2（第四幕：重演蘑菇）
    ///   → Act3（大树枢纽，第2次起：按进入次数触发结局）；Hidden1/Hidden2 为隐藏区域独立场景。
    /// </summary>
    public static class Demo3SceneIds
    {
        public const string Act1Crowd   = "Act1";
        public const string Act2Rescue  = "Act2";
        public const string Act3Pursue  = "Act3";     // 大树枢纽场景（结局按进入次数在此触发）
        public const string Act4Reenact = "Act4";     // 看客区域场景（迷失晕倒）
        public const string Act2Pursue  = "Act2-1";   // 第三幕：从大树第一次回来跳蘑菇（追寻）
        public const string Act2Reenact = "Act2-2";   // 第四幕：从看客区回来重演蘑菇（→回大树）
        public const string HiddenL1    = "Hidden1";  // 隐藏区域 L1（独立场景）
        public const string HiddenL2    = "Hidden2";  // 隐藏区域 L2（独立场景）
    }
}
