using System.Collections.Generic;
using NUnit.Framework;
using Spotlight.Domain.Story;
using Spotlight.Story;
using Spotlight.Scene;
using UnityEngine;

namespace Spotlight.Tests.EditMode
{
    public sealed class StorySessionTests
    {
        [Test]
        public void Advance_FollowsNextLine()
        {
            StorySession session = CreateSession(
                Line("intro_001", "你好", "intro_002"),
                Line("intro_002", "下一句", string.Empty));

            Assert.That(session.Start(StorySession.StartNodeId), Is.True);
            Assert.That(session.Advance(), Is.True);

            Assert.That(session.WaitKind, Is.EqualTo(StoryWaitKind.Line));
            Assert.That(session.Current.Id, Is.EqualTo("intro_002"));
        }

        [Test]
        public void Advance_EndsBranchWhenNextIsMissingFromData()
        {
            StorySession session = CreateSession(Line("greet_1_001", "你好", string.Empty));

            session.Start("greet_1_001");

            Assert.That(session.Advance(), Is.True);
            Assert.That(session.WaitKind, Is.EqualTo(StoryWaitKind.Ended));
            Assert.That(session.BlockReason, Is.Empty);
        }

        [Test]
        public void SelectChoice_PresentsWholeGroupAndFollowsSelectedJump()
        {
            StorySession session = CreateSession(
                Line("music_010", "结束", "music_011"),
                Choice("music_011", "再玩一次", "music_002", "music_replay"),
                Choice("music_012", "回到讨论", "music_013", "music_replay"),
                Line("music_013", "讨论", string.Empty));

            session.Start("music_010");
            session.Advance();

            Assert.That(session.WaitKind, Is.EqualTo(StoryWaitKind.Choices));
            Assert.That(session.CurrentChoices, Has.Count.EqualTo(2));
            Assert.That(session.SelectChoice("music_012"), Is.True);
            Assert.That(session.Current.Id, Is.EqualTo("music_013"));
        }

        [Test]
        public void FailEvent_UsesFailureJumpAndCompleteEventUsesSuccessJump()
        {
            StorySession session = CreateSession(
                Event("evolution_030", "完成", "evolution_031", "evolution_028"),
                Event("evolution_028", "失败", "evolution_029", string.Empty),
                Line("evolution_029", "辅助", string.Empty),
                Line("evolution_031", "灌溉完成", string.Empty));

            session.Start("evolution_030");
            Assert.That(session.WaitKind, Is.EqualTo(StoryWaitKind.Event));
            Assert.That(session.FailEvent(), Is.True);
            Assert.That(session.Current.Id, Is.EqualTo("evolution_028"));

            session.Start("evolution_030");
            Assert.That(session.CompleteEvent(), Is.True);
            Assert.That(session.Current.Id, Is.EqualTo("evolution_031"));
        }

        [Test]
        public void SetPlayerName_ReplacesTokenWithoutChangingStoredCopy()
        {
            StorySession session = CreateSession(Line("intro_003", "欢迎xxx！", string.Empty));
            session.Start("intro_003");

            Assert.That(session.DisplayText(session.Current), Is.EqualTo("欢迎xxx！"));
            session.SetPlayerName("阿闲");

            Assert.That(session.DisplayText(session.Current), Is.EqualTo("欢迎阿闲！"));
            Assert.That(session.Current.Text, Is.EqualTo("欢迎xxx！"));
        }

        [Test]
        public void TryCreate_RejectsDuplicateId()
        {
            bool created = StoryGraph.TryCreate(
                new List<StoryNode>
                {
                    Line("intro_001", "甲", string.Empty),
                    Line("intro_001", "乙", string.Empty)
                },
                out StoryGraph graph,
                out string error);

            Assert.That(created, Is.False);
            Assert.That(graph, Is.Null);
            Assert.That(error, Does.Contain("intro_001"));
        }

        [Test]
        public void GeneratedCatalog_ReachesPlayableEventsAndStopsUnimplementedOnes()
        {
            StoryCatalogAsset catalog = UnityEngine.Resources.Load<StoryCatalogAsset>("Story/StoryCatalog");
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.TryCreateGraph(out StoryGraph graph, out string error), Is.True, error);

            StorySession greeting = new StorySession(graph);
            Assert.That(greeting.Start("greet_001"), Is.True);
            Assert.That(greeting.Advance(), Is.True);
            Assert.That(greeting.WaitKind, Is.EqualTo(StoryWaitKind.Ended));

            StorySession session = new StorySession(graph);
            Assert.That(session.Start(StorySession.StartNodeId), Is.True);
            int guard = 0;
            while (session.Current.Id != "music_008" && guard < 80)
            {
                Assert.That(session.WaitKind, Is.EqualTo(StoryWaitKind.Event).Or.EqualTo(StoryWaitKind.Line));
                if (session.WaitKind == StoryWaitKind.Event)
                {
                    Assert.That(session.CompleteEvent(), Is.True, session.Current.Id);
                }
                else
                {
                    Assert.That(session.Advance(), Is.True, session.Current.Id);
                }

                guard++;
            }

            Assert.That(session.Current.Id, Is.EqualTo("music_008"));
            Assert.That(session.WaitKind, Is.EqualTo(StoryWaitKind.Choices));
            Assert.That(graph.TryGet("evolution_014", out _), Is.False);
            Assert.That(graph.TryGet("evolution_013", out StoryNode thanks), Is.True);
            Assert.That(thanks.NextId, Is.EqualTo("evolution_019"));
            Assert.That(graph.TryGet("music_008", out StoryNode replay), Is.True);
            Assert.That(graph.GetChoiceGroup(replay.ChoiceGroup), Has.Count.EqualTo(2));
        }

        [Test]
        public void BossDefeat_AdvancesHeldHintToVictoryLine()
        {
            GameObject root = new GameObject("StoryHostTest");
            try
            {
                StorySessionHost host = CreateHost(root);
                Assert.That(host.StartBranch(Demo2StoryGates.BossNarrationGateId), Is.True);
                Assert.That(host.Advance(), Is.True);
                Assert.That(host.IsNarrationHeld, Is.True);

                host.NotifyDemo2BossDefeated();

                Assert.That(host.Current.NodeId, Is.EqualTo("evolution_025"));
                Assert.That(host.IsNarrationHeld, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BossDefeat_BeforeHintIsRememberedUntilHintAdvances()
        {
            GameObject root = new GameObject("StoryHostTest");
            try
            {
                StorySessionHost host = CreateHost(root);
                Assert.That(host.StartBranch("evolution_022"), Is.True);

                host.NotifyDemo2BossDefeated();

                Assert.That(host.Advance(), Is.True);
                Assert.That(host.Current.NodeId, Is.EqualTo(Demo2StoryGates.BossNarrationGateId));
                Assert.That(host.Advance(), Is.True);
                Assert.That(host.Current.NodeId, Is.EqualTo("evolution_025"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }



        private static StorySessionHost CreateHost(GameObject root)
        {
            StorySessionHost host = root.AddComponent<StorySessionHost>();
            host.Initialize(new SceneLoader(new SceneCatalog()), new StoryDemoBridge());
            return host;
        }

        private static StorySession CreateSession(params StoryNode[] nodes)
        {
            Assert.That(StoryGraph.TryCreate(nodes, out StoryGraph graph, out string error), Is.True, error);
            return new StorySession(graph);
        }

        private static StoryNode Line(string id, string text, string nextId)
        {
            return new StoryNode(id, StoryNodeKind.Line, new string[0], text, nextId, string.Empty, string.Empty, string.Empty);
        }

        private static StoryNode Event(string id, string text, string nextId, string failureNextId)
        {
            return new StoryNode(id, StoryNodeKind.Event, new string[0], text, nextId, string.Empty, failureNextId, string.Empty);
        }

        private static StoryNode Choice(string id, string text, string nextId, string group)
        {
            return new StoryNode(id, StoryNodeKind.Choice, new string[0], text, nextId, string.Empty, string.Empty, group);
        }
    }
}
