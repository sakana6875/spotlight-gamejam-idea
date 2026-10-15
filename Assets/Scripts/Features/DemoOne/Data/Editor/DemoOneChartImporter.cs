using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Spotlight.Features.DemoOne.Data;
using Spotlight.Features.DemoOne.Domain.Chart;

namespace Spotlight.Features.DemoOne.Data.Editor
{
    /// <summary>
    /// DemoOneChartAsset 的 Inspector 扩展。
    /// 提供「Import from JSON...」按钮，从外部 JSON 校验并导入谱面。
    /// 校验失败时不覆盖资产（规范 §9）。
    /// </summary>
    [CustomEditor(typeof(DemoOneChartAsset))]
    public sealed class DemoOneChartImporter : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            var asset = (DemoOneChartAsset)target;

            if (GUILayout.Button("Import from JSON..."))
            {
                ImportFromJson(asset);
            }
        }

        private static void ImportFromJson(DemoOneChartAsset asset)
        {
            string path = EditorUtility.OpenFilePanel("选择谱面 JSON", "", "json");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            string json;
            try
            {
                json = File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("导入失败", $"读取文件失败：{ex.Message}", "确定");
                return;
            }

            ChartParseResult result = ChartJsonParser.Parse(json);
            if (!result.IsSuccess)
            {
                // 校验失败时不修改资产。
                EditorUtility.DisplayDialog("导入失败", result.ErrorMessage, "确定");
                return;
            }

            ChartData domain = result.Data;
            var entries = new NoteEntry[domain.Notes.Length];
            for (int i = 0; i < domain.Notes.Length; i++)
            {
                entries[i] = new NoteEntry
                {
                    time = domain.Notes[i].Time,
                    index = domain.Notes[i].Index
                };
            }

            Undo.RecordObject(asset, "Import DemoOne Chart");
            asset.SetData(domain.SongId, domain.Bpm, domain.Offset, entries);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                "导入成功",
                $"谱面已导入：{domain.Notes.Length} 个 note。\n歌曲：{domain.SongId}\n来源：{Path.GetFileName(path)}",
                "确定");
        }
    }
}