namespace Spotlight.Domain.Story
{
    /// <summary>
    /// 文案人物名对应的稳定立绘 ID。UI 只用这些 ID 查图，不解析中文名。
    /// </summary>
    public static class StorySpeakerIds
    {
        public const string AXian = "a_xian";
        public const string BeiJie = "bei_jie";
        public const string GuiYun = "gui_yun";
        public const string DaoXia = "dao_xia";
        public const string MiaoMiao = "miao_miao";
        public const string LiangFeng = "liang_feng";
        public const string MoLe = "mo_le";

        /// <summary>
        /// 把文案里的人物字段转成稳定 ID。无法识别的片段会原样保留，方便发现漏配。
        /// </summary>
        public static string[] FromCsvSpeaker(string speakerField)
        {
            if (string.IsNullOrWhiteSpace(speakerField) || speakerField.StartsWith("（"))
            {
                return new string[0];
            }

            string[] parts = speakerField.Split('/');
            string[] ids = new string[parts.Length];
            int count = 0;
            for (int index = 0; index < parts.Length; index++)
            {
                string name = parts[index].Trim();
                if (name.Length == 0)
                {
                    continue;
                }

                ids[count] = ToId(name);
                count++;
            }

            if (count == ids.Length)
            {
                return ids;
            }

            string[] trimmed = new string[count];
            for (int index = 0; index < count; index++)
            {
                trimmed[index] = ids[index];
            }

            return trimmed;
        }

        private static string ToId(string name)
        {
            switch (name)
            {
                case "阿闲":
                    return AXian;
                case "北街":
                    return BeiJie;
                case "归云":
                    return GuiYun;
                case "刀夏":
                    return DaoXia;
                case "喵喵":
                    return MiaoMiao;
                case "凉风":
                    return LiangFeng;
                case "摸了":
                    return MoLe;
                default:
                    return name;
            }
        }
    }
}
