using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Spotlight.Domain.Story;
using Spotlight.Story;
using UnityEditor;
using UnityEngine;

namespace Spotlight.Editor.Story
{
    /// <summary>
    /// 从文案 CSV 生成运行时剧情资产。校验失败时不覆盖旧资产。
    /// </summary>
    public static class StoryCatalogImporter
    {
        private const string CsvPath = "Assets/Resources/剧情对话配置_2.0.csv";
        private const string OutputDirectory = "Assets/Resources/Story";
        private const string CatalogPath = OutputDirectory + "/StoryCatalog.asset";
        private const string SpeakerPath = OutputDirectory + "/StorySpeakerCatalog.asset";

        [MenuItem("Spotlight/Import Story Catalog")]
        public static void ImportFromMenu()
        {
            Import(logResult: true);
        }

        [InitializeOnLoadMethod]
        private static void ImportWhenCsvIsNewer()
        {
            EditorApplication.delayCall += () => Import(logResult: false);
        }

        public static bool Import(bool logResult)
        {
            if (!File.Exists(CsvPath))
            {
                Debug.LogError("剧情导入失败：找不到 " + CsvPath);
                return false;
            }

            if (!NeedsImport() && !logResult)
            {
                return true;
            }

            if (!TryParse(ReadCsv(), out List<StoryNodeRecord> records, out string error))
            {
                Debug.LogError("剧情导入失败，未覆盖旧资产：" + error);
                return false;
            }

            Directory.CreateDirectory(OutputDirectory);
            StoryCatalogAsset catalog = AssetDatabase.LoadAssetAtPath<StoryCatalogAsset>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<StoryCatalogAsset>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            SerializedObject serialized = new SerializedObject(catalog);
            SerializedProperty nodes = serialized.FindProperty("_nodes");
            nodes.arraySize = records.Count;
            for (int index = 0; index < records.Count; index++)
            {
                WriteRecord(nodes.GetArrayElementAtIndex(index), records[index]);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            WriteSpeakerCatalog();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            if (logResult)
            {
                Debug.Log("剧情资产已更新：" + records.Count + " 个节点。");
            }

            return true;
        }

        private static string ReadCsv()
        {
            byte[] bytes = File.ReadAllBytes(CsvPath);
            try
            {
                return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                return Encoding.GetEncoding(936).GetString(bytes);
            }
        }

        private static bool NeedsImport()
        {
            if (!File.Exists(CatalogPath) || !File.Exists(SpeakerPath))
            {
                return true;
            }

            return File.GetLastWriteTimeUtc(CsvPath) > File.GetLastWriteTimeUtc(CatalogPath);
        }

        private static bool TryParse(string csv, out List<StoryNodeRecord> records, out string error)
        {
            records = new List<StoryNodeRecord>();
            string[] lines = csv.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            HashSet<string> ids = new HashSet<string>();
            for (int lineIndex = 1; lineIndex < lines.Length; lineIndex++)
            {
                if (string.IsNullOrWhiteSpace(lines[lineIndex]))
                {
                    continue;
                }

                string[] fields = SplitCsv(lines[lineIndex]);
                string id = Field(fields, 1);
                if (id.Length == 0)
                {
                    continue;
                }

                if (!ids.Add(id))
                {
                    records = null;
                    error = CsvPath + " 第 " + (lineIndex + 1) + " 行 ID 重复：" + id;
                    return false;
                }

                if (!TryKind(Field(fields, 0), out StoryNodeKind kind))
                {
                    records = null;
                    error = CsvPath + " 第 " + (lineIndex + 1) + " 行标志无法识别：" + Field(fields, 0);
                    return false;
                }

                records.Add(new StoryNodeRecord
                {
                    id = id,
                    kind = kind,
                    speakerIds = StorySpeakerIds.FromCsvSpeaker(Field(fields, 2)),
                    text = Field(fields, 4),
                    nextId = EmptySentinel(Field(fields, 5)),
                    sceneLabel = Field(fields, 8),
                    failureNextId = ParseFailure(EmptySentinel(Field(fields, 9))),
                    choiceGroup = EmptySentinel(Field(fields, 10))
                });
            }

            for (int index = 0; index < records.Count; index++)
            {
                StoryNodeRecord record = records[index];
                if (!JumpExists(ids, record.nextId) || !JumpExists(ids, record.failureNextId))
                {
                    records = null;
                    error = CsvPath + " 节点 " + record.id + " 的跳转目标不存在。";
                    return false;
                }
            }

            if (!ids.Contains(StorySession.StartNodeId))
            {
                records = null;
                error = CsvPath + " 缺少起始节点 " + StorySession.StartNodeId;
                return false;
            }

            error = null;
            return true;
        }

        private static bool JumpExists(HashSet<string> ids, string jump)
        {
            return string.IsNullOrWhiteSpace(jump) || ids.Contains(jump);
        }

        private static string EmptySentinel(string value)
        {
            return value == "21" ? string.Empty : value;
        }

        private static string ParseFailure(string condition)
        {
            const string prefix = "失败时:";
            if (string.IsNullOrWhiteSpace(condition) || !condition.StartsWith(prefix, StringComparison.Ordinal))
            {
                return string.Empty;
            }

            return condition.Substring(prefix.Length).Trim();
        }

        private static bool TryKind(string flag, out StoryNodeKind kind)
        {
            switch (flag)
            {
                case "#":
                    kind = StoryNodeKind.Line;
                    return true;
                case "EVENT":
                    kind = StoryNodeKind.Event;
                    return true;
                case "&":
                    kind = StoryNodeKind.Choice;
                    return true;
                case "END":
                    kind = StoryNodeKind.End;
                    return true;
                default:
                    kind = StoryNodeKind.Line;
                    return false;
            }
        }

        private static void WriteRecord(SerializedProperty property, StoryNodeRecord record)
        {
            property.FindPropertyRelative("id").stringValue = record.id;
            property.FindPropertyRelative("kind").enumValueIndex = (int)record.kind;
            SerializedProperty speakers = property.FindPropertyRelative("speakerIds");
            string[] speakerIds = record.speakerIds ?? Array.Empty<string>();
            speakers.arraySize = speakerIds.Length;
            for (int index = 0; index < speakerIds.Length; index++)
            {
                speakers.GetArrayElementAtIndex(index).stringValue = speakerIds[index];
            }

            property.FindPropertyRelative("text").stringValue = record.text;
            property.FindPropertyRelative("nextId").stringValue = record.nextId;
            property.FindPropertyRelative("sceneLabel").stringValue = record.sceneLabel;
            property.FindPropertyRelative("failureNextId").stringValue = record.failureNextId;
            property.FindPropertyRelative("choiceGroup").stringValue = record.choiceGroup;
        }

        private static void WriteSpeakerCatalog()
        {
            StorySpeakerCatalog catalog = AssetDatabase.LoadAssetAtPath<StorySpeakerCatalog>(SpeakerPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<StorySpeakerCatalog>();
                AssetDatabase.CreateAsset(catalog, SpeakerPath);
            }

            SerializedObject serialized = new SerializedObject(catalog);
            serialized.FindProperty("_hallBackground").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/HALL_backgrond.png");
            SerializedProperty speakers = serialized.FindProperty("_speakers");
            SpeakerBinding[] bindings =
            {
                new SpeakerBinding(StorySpeakerIds.AXian, "阿闲", "Assets/Resources/Sprites/闲.png"),
                new SpeakerBinding(StorySpeakerIds.BeiJie, "北街", "Assets/Resources/Sprites/贝姐.png"),
                new SpeakerBinding(StorySpeakerIds.GuiYun, "归云", "Assets/Resources/Sprites/归云.png"),
                new SpeakerBinding(StorySpeakerIds.DaoXia, "刀夏", "Assets/Resources/Sprites/刀夏.png"),
                new SpeakerBinding(StorySpeakerIds.MiaoMiao, "喵喵", "Assets/Resources/Sprites/喵喵.png"),
                new SpeakerBinding(StorySpeakerIds.LiangFeng, "凉风", "Assets/Resources/Sprites/凉风.png"),
                new SpeakerBinding(StorySpeakerIds.MoLe, "摸了", "Assets/Resources/Sprites/摸了.png"),
                new SpeakerBinding(StorySpeakerIds.Light, "light", "Assets/Resources/Sprites/light.png"),
                new SpeakerBinding(StorySpeakerIds.YangMeiHua, "杨梅花", string.Empty),
                new SpeakerBinding(StorySpeakerIds.Player, "玩家", string.Empty),
                new SpeakerBinding(StorySpeakerIds.Everyone, "全员", string.Empty)
            };
            speakers.arraySize = bindings.Length;
            for (int index = 0; index < bindings.Length; index++)
            {
                SerializedProperty entry = speakers.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("_speakerId").stringValue = bindings[index].Id;
                entry.FindPropertyRelative("_displayName").stringValue = bindings[index].DisplayName;
                entry.FindPropertyRelative("_portrait").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Sprite>(bindings[index].SpritePath);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        private static string Field(string[] fields, int index)
        {
            return index < fields.Length ? fields[index].Trim() : string.Empty;
        }

        private static string[] SplitCsv(string line)
        {
            List<string> fields = new List<string>();
            StringBuilder current = new StringBuilder();
            bool quoted = false;
            for (int index = 0; index < line.Length; index++)
            {
                char character = line[index];
                if (character == '"')
                {
                    quoted = !quoted;
                    continue;
                }

                if (character == ',' && !quoted)
                {
                    fields.Add(current.ToString());
                    current.Clear();
                    continue;
                }

                current.Append(character);
            }

            fields.Add(current.ToString());
            return fields.ToArray();
        }

        private readonly struct SpeakerBinding
        {
            public SpeakerBinding(string id, string displayName, string spritePath)
            {
                Id = id;
                DisplayName = displayName;
                SpritePath = spritePath;
            }

            public string Id { get; }
            public string DisplayName { get; }
            public string SpritePath { get; }
        }
    }
}
