using NUnit.Framework;
using Spotlight.Features.DemoOne.Data;

namespace Spotlight.Tests.EditMode
{
    public class ChartJsonParserTests
    {
        [Test]
        public void Parse_ValidJson_ReturnsSuccess()
        {
            string json = @"{
                ""songId"": ""demo1_song"",
                ""bpm"": 120,
                ""offset"": 0.15,
                ""notes"": [
                    { ""time"": 1.5, ""index"": 0 },
                    { ""time"": 2.0, ""index"": 1 }
                ]
            }";

            ChartParseResult result = ChartJsonParser.Parse(json);

            Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
            Assert.AreEqual("demo1_song", result.Data.SongId);
            Assert.AreEqual(120f, result.Data.Bpm, 0.001f);
            Assert.AreEqual(0.15f, result.Data.Offset, 0.001f);
            Assert.AreEqual(2, result.Data.Notes.Length);
            Assert.AreEqual(1.5f, result.Data.Notes[0].Time, 0.001f);
            Assert.AreEqual(0, result.Data.Notes[0].Index);
            Assert.AreEqual(2.0f, result.Data.Notes[1].Time, 0.001f);
            Assert.AreEqual(1, result.Data.Notes[1].Index);
        }

        [Test]
        public void Parse_EmptyString_ReturnsFailure()
        {
            ChartParseResult result = ChartJsonParser.Parse("");
            Assert.IsFalse(result.IsSuccess);
            Assert.IsNotNull(result.ErrorMessage);
        }

        [Test]
        public void Parse_WhitespaceOnly_ReturnsFailure()
        {
            ChartParseResult result = ChartJsonParser.Parse("   \n\t  ");
            Assert.IsFalse(result.IsSuccess);
        }

        [Test]
        public void Parse_MissingSongId_ReturnsFailure()
        {
            string json = @"{ ""bpm"": 120, ""notes"": [{ ""time"": 1, ""index"": 0 }] }";
            ChartParseResult result = ChartJsonParser.Parse(json);
            Assert.IsFalse(result.IsSuccess);
        }

        [Test]
        public void Parse_ZeroBpm_ReturnsFailure()
        {
            string json = @"{ ""songId"": ""s"", ""bpm"": 0, ""notes"": [{ ""time"": 1, ""index"": 0 }] }";
            ChartParseResult result = ChartJsonParser.Parse(json);
            Assert.IsFalse(result.IsSuccess);
        }

        [Test]
        public void Parse_EmptyNotes_ReturnsFailure()
        {
            string json = @"{ ""songId"": ""s"", ""bpm"": 120, ""notes"": [] }";
            ChartParseResult result = ChartJsonParser.Parse(json);
            Assert.IsFalse(result.IsSuccess);
        }

        [Test]
        public void Parse_UnorderedNotes_ReturnsFailure()
        {
            string json = @"{
                ""songId"": ""s"", ""bpm"": 120,
                ""notes"": [
                    { ""time"": 2.0, ""index"": 0 },
                    { ""time"": 1.0, ""index"": 1 }
                ]
            }";
            ChartParseResult result = ChartJsonParser.Parse(json);
            Assert.IsFalse(result.IsSuccess);
        }

        [Test]
        public void Parse_NonSequentialIndex_ReturnsFailure()
        {
            string json = @"{
                ""songId"": ""s"", ""bpm"": 120,
                ""notes"": [
                    { ""time"": 1.0, ""index"": 0 },
                    { ""time"": 2.0, ""index"": 5 }
                ]
            }";
            ChartParseResult result = ChartJsonParser.Parse(json);
            Assert.IsFalse(result.IsSuccess);
        }

        [Test]
        public void Parse_NegativeTime_ReturnsFailure()
        {
            string json = @"{
                ""songId"": ""s"", ""bpm"": 120,
                ""notes"": [{ ""time"": -1, ""index"": 0 }]
            }";
            ChartParseResult result = ChartJsonParser.Parse(json);
            Assert.IsFalse(result.IsSuccess);
        }
    }
}