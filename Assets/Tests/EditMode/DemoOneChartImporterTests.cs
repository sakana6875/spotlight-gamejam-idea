using System.IO;
using NUnit.Framework;
using UnityEngine;
using Spotlight.Demos.Demo1.Data;
using Spotlight.Demos.Demo1.Data.Editor;

namespace Spotlight.Tests.EditMode
{
    public sealed class DemoOneChartImporterTests
    {
        [Test]
        public void TryImportFromJson_InvalidSource_PreservesExistingAsset()
        {
            DemoOneChartAsset chartAsset = ScriptableObject.CreateInstance<DemoOneChartAsset>();
            chartAsset.SetData(
                "existing_song",
                120f,
                0.25f,
                new[] { new NoteEntry { time = 1f, index = 0 } });
            string sourcePath = Path.GetTempFileName();

            try
            {
                File.WriteAllText(sourcePath, "{ \"songId\": \"\", \"bpm\": 120, \"notes\": [] }");

                bool succeeded = DemoOneChartImporter.TryImportFromJson(
                    chartAsset,
                    sourcePath,
                    out string errorMessage);

                Assert.That(succeeded, Is.False);
                Assert.That(errorMessage, Does.Contain(sourcePath));
                Assert.That(errorMessage, Does.Contain("songId"));
                Assert.That(chartAsset.SongId, Is.EqualTo("existing_song"));
                Assert.That(chartAsset.Bpm, Is.EqualTo(120f));
                Assert.That(chartAsset.Offset, Is.EqualTo(0.25f));
                Assert.That(chartAsset.NoteCount, Is.EqualTo(1));
            }
            finally
            {
                File.Delete(sourcePath);
                Object.DestroyImmediate(chartAsset);
            }
        }
    }
}
