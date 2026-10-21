using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Spotlight.Demos.Demo1.Domain.Chart;

namespace Spotlight.Demos.Demo1.Data.Editor
{
    /// <summary>
    /// 从外部 JSON 校验并更新 Demo1 运行时谱面资产。
    /// </summary>
    [CustomEditor(typeof(DemoOneChartAsset))]
    public sealed class DemoOneChartImporter : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();

            if (!GUILayout.Button("Import from JSON..."))
            {
                return;
            }

            string sourcePath = EditorUtility.OpenFilePanel("选择谱面 JSON", string.Empty, "json");
            if (string.IsNullOrEmpty(sourcePath))
            {
                return;
            }

            DemoOneChartAsset chartAsset = (DemoOneChartAsset)target;
            if (!TryImportFromJson(chartAsset, sourcePath, out string errorMessage))
            {
                EditorUtility.DisplayDialog("导入失败", errorMessage, "确定");
                return;
            }

            EditorUtility.DisplayDialog("导入成功", "谱面已更新：" + sourcePath, "确定");
        }

        /// <summary>
        /// 校验源文件后一次性更新目标资产；读取或校验失败时不修改目标。
        /// </summary>
        public static bool TryImportFromJson(
            DemoOneChartAsset chartAsset,
            string sourcePath,
            out string errorMessage)
        {
            if (chartAsset == null)
            {
                errorMessage = "目标谱面资产不能为空。";
                return false;
            }

            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                errorMessage = "谱面源文件路径不能为空。";
                return false;
            }

            string json;
            try
            {
                json = File.ReadAllText(sourcePath);
            }
            catch (Exception exception)
            {
                errorMessage = sourcePath + "：读取失败：" + exception.Message;
                return false;
            }

            ChartParseResult parseResult = ChartJsonParser.Parse(json);
            if (!parseResult.IsSuccess)
            {
                errorMessage = sourcePath + "：" + parseResult.ErrorMessage;
                return false;
            }

            ChartData chart = parseResult.Data;
            NoteEntry[] entries = new NoteEntry[chart.Notes.Length];
            for (int index = 0; index < chart.Notes.Length; index++)
            {
                entries[index] = new NoteEntry
                {
                    time = chart.Notes[index].Time,
                    index = chart.Notes[index].Index
                };
            }

            Undo.RecordObject(chartAsset, "Import Demo1 Chart");
            chartAsset.SetData(chart.SongId, chart.Bpm, chart.Offset, entries);
            EditorUtility.SetDirty(chartAsset);
            AssetDatabase.SaveAssets();
            errorMessage = null;
            return true;
        }
    }
}
