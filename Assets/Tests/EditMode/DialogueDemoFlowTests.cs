using NUnit.Framework;
using Spotlight.Adapters.Demo;
using Spotlight.Adapters.Dialogue;
using Spotlight.Adapters.Session;
using Spotlight.Application.Services.Demo;
using Spotlight.Application.Services.Dialogue;
using Spotlight.Application.Services.Scene;
using Spotlight.Domain.Story;

namespace Spotlight.Tests.EditMode
{
    public sealed class DialogueDemoFlowTests
    {
        [Test]
        public void ContentId_TrimsValueAndRejectsBlankValue()
        {
            ContentId validId = new ContentId("  story_intro  ");
            ContentId invalidId = new ContentId("   ");

            Assert.That(validId.Value, Is.EqualTo("story_intro"));
            Assert.That(validId.IsValid, Is.True);
            Assert.That(invalidId.IsValid, Is.False);
        }

        [Test]
        public void StoryFlag_IsStablePureValue()
        {
            StoryFlag first = new StoryFlag("  story_demo1_complete  ");
            StoryFlag second = new StoryFlag("story_demo1_complete");

            Assert.That(first.IsValid, Is.True);
            Assert.That(first, Is.EqualTo(second));
            Assert.That(first.Value, Is.EqualTo("story_demo1_complete"));
        }

        [Test]
        public void RecordingDialogue_StartAndComplete_TracksMovementLock()
        {
            RecordingDialogueService dialogue = new RecordingDialogueService();
            DialogueRequest request = new DialogueRequest(new ContentId("story_intro"), true);

            DialogueResult started = dialogue.StartDialogue(request);
            DialogueResult completed = dialogue.Complete();

            Assert.That(started.Code, Is.EqualTo(DialogueResultCode.Started));
            Assert.That(dialogue.IsPlaying, Is.False);
            Assert.That(dialogue.IsMovementLocked, Is.False);
            Assert.That(completed.Code, Is.EqualTo(DialogueResultCode.Completed));
            Assert.That(dialogue.LastRequest, Is.SameAs(request));
        }

        [Test]
        public void RecordingDialogue_Skip_EndsPlaybackAndUnlocksMovement()
        {
            RecordingDialogueService dialogue = new RecordingDialogueService();
            dialogue.StartDialogue(new DialogueRequest(new ContentId("story_intro"), true));

            DialogueResult skipped = dialogue.Skip();

            Assert.That(skipped.Code, Is.EqualTo(DialogueResultCode.Skipped));
            Assert.That(dialogue.IsPlaying, Is.False);
            Assert.That(dialogue.IsMovementLocked, Is.False);
        }

        [Test]
        public void RecordingDialogue_InvalidRequest_DoesNotStartPlayback()
        {
            RecordingDialogueService dialogue = new RecordingDialogueService();

            DialogueResult result = dialogue.StartDialogue(
                new DialogueRequest(new ContentId(string.Empty), true));

            Assert.That(result.Code, Is.EqualTo(DialogueResultCode.InvalidContentId));
            Assert.That(dialogue.IsPlaying, Is.False);
            Assert.That(dialogue.IsMovementLocked, Is.False);
        }

        [Test]
        public void RecordingDemo_RecordsContinueAndReplayEntryModes()
        {
            RecordingDemoFlow flow = new RecordingDemoFlow();

            DemoRunResult continued = flow.EnterDemo(DemoId.Demo1, DemoEntryMode.Continue);
            DemoRunResult replayed = flow.EnterDemo(DemoId.Demo1, DemoEntryMode.Replay);

            Assert.That(continued.Code, Is.EqualTo(DemoRunResultCode.Succeeded));
            Assert.That(replayed.Code, Is.EqualTo(DemoRunResultCode.Succeeded));
            Assert.That(flow.LastEntryMode, Is.EqualTo(DemoEntryMode.Replay));
            Assert.That(flow.LastOperation, Is.EqualTo(DemoFlowOperation.Enter));
        }

        [Test]
        public void RecordingDemo_RetryAndExit_AreExplicitOperations()
        {
            RecordingDemoFlow flow = new RecordingDemoFlow();

            flow.RetryDemo(DemoId.Demo2);
            Assert.That(flow.LastOperation, Is.EqualTo(DemoFlowOperation.Retry));
            Assert.That(flow.LastEntryMode, Is.EqualTo(DemoEntryMode.Replay));

            DemoRunResult exited = flow.ExitDemo(DemoId.Demo2);
            Assert.That(flow.LastOperation, Is.EqualTo(DemoFlowOperation.Exit));
            Assert.That(exited.Code, Is.EqualTo(DemoRunResultCode.Succeeded));
            Assert.That(flow.LastEntryMode, Is.Null);
        }

        [Test]
        public void RecordingDemo_DistinguishesSucceededFailedAndAbandoned()
        {
            RecordingDemoFlow flow = new RecordingDemoFlow();

            flow.SetNextResult(DemoRunResult.Succeeded(DemoId.Demo1));
            DemoRunResult succeeded = flow.EnterDemo(DemoId.Demo1, DemoEntryMode.Continue);
            flow.SetNextResult(DemoRunResult.Failed(DemoId.Demo1));
            DemoRunResult failed = flow.EnterDemo(DemoId.Demo1, DemoEntryMode.Replay);
            flow.SetNextResult(DemoRunResult.Abandoned(DemoId.Demo1));
            DemoRunResult abandoned = flow.EnterDemo(DemoId.Demo1, DemoEntryMode.Replay);

            Assert.That(succeeded.Code, Is.EqualTo(DemoRunResultCode.Succeeded));
            Assert.That(failed.Code, Is.EqualTo(DemoRunResultCode.Failed));
            Assert.That(abandoned.Code, Is.EqualTo(DemoRunResultCode.Abandoned));
            Assert.That(failed.Code, Is.Not.EqualTo(DemoRunResultCode.Succeeded));
        }

        [Test]
        public void UnavailableDialogue_ReportsUnavailableForValidContent()
        {
            UnavailableDialogueService dialogue = new UnavailableDialogueService();

            DialogueResult result = dialogue.StartDialogue(
                new DialogueRequest(new ContentId("story_intro"), false));

            Assert.That(result.Code, Is.EqualTo(DialogueResultCode.Unavailable));
            Assert.That(result.IsSuccess, Is.False);
        }
    }
}
