using System.Collections.Generic;
using NUnit.Framework;
using Spotlight.Adapters.Save;
using Spotlight.Domain.Save;

namespace Spotlight.Tests.EditMode
{
    public sealed class SaveServiceTests
    {
        [Test]
        public void EmptySave_ReturnsEmptyResult()
        {
            InMemorySaveService saveService = new InMemorySaveService();

            SnapshotResult result = saveService.LoadLatestSnapshot();

            Assert.That(saveService.HasSave, Is.False);
            Assert.That(result.Code, Is.EqualTo(SaveResultCode.Empty));
            Assert.That(result.Snapshot, Is.Null);
        }

        [Test]
        public void SavingSameCheckpointId_ReplacesPreviousSnapshot()
        {
            InMemorySaveService saveService = new InMemorySaveService();
            saveService.SaveCheckpoint(CreateSnapshot("checkpoint_a", "hub", 10));
            saveService.SaveCheckpoint(CreateSnapshot("checkpoint_a", "demo1", 20));

            SnapshotResult result = saveService.LoadSnapshot("checkpoint_a");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Snapshot.Checkpoint.SceneId, Is.EqualTo("demo1"));
            Assert.That(result.Snapshot.SavedAtUnixSeconds, Is.EqualTo(20));
            Assert.That(saveService.ListSnapshots().Snapshots, Has.Count.EqualTo(1));
        }

        [Test]
        public void LoadingOlderSnapshot_DoesNotRemovePermanentProgress()
        {
            InMemorySaveService saveService = new InMemorySaveService();
            InMemoryProgressService progressService =
                new InMemoryProgressService(saveService.PermanentProgress);
            progressService.UnlockDemo("demo2");
            progressService.SetStoryFlag("story_demo1_complete");
            saveService.SaveCheckpoint(CreateSnapshot("checkpoint_a", "hub", 10));
            saveService.SaveCheckpoint(CreateSnapshot("checkpoint_b", "demo1", 20));

            SnapshotResult result = saveService.LoadSnapshot("checkpoint_a");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Snapshot.Checkpoint.SceneId, Is.EqualTo("hub"));
            Assert.That(progressService.IsDemoUnlocked("demo2"), Is.True);
            Assert.That(progressService.HasStoryFlag("story_demo1_complete"), Is.True);
        }

        [Test]
        public void UnknownSnapshot_ReturnsNotFound()
        {
            InMemorySaveService saveService = new InMemorySaveService();

            SnapshotResult result = saveService.LoadSnapshot("missing");

            Assert.That(result.Code, Is.EqualTo(SaveResultCode.NotFound));
            Assert.That(result.Snapshot, Is.Null);
        }

        [Test]
        public void ResetSave_ClearsSnapshotsProgressAndRestoresSettings()
        {
            InMemorySaveService saveService = new InMemorySaveService();
            InMemoryProgressService progressService =
                new InMemoryProgressService(saveService.PermanentProgress);
            saveService.SaveCheckpoint(CreateSnapshot("checkpoint_a", "hub", 10));
            progressService.UnlockDemo("demo1");
            saveService.SaveSettings(new SettingsData(0.1f, 0.2f, 0.3f, 0.4f));

            SaveResult result = saveService.ResetSave();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(saveService.HasSave, Is.False);
            Assert.That(progressService.IsDemoUnlocked("demo1"), Is.False);
            Assert.That(saveService.Settings.MasterVolume, Is.EqualTo(1f));
        }

        [Test]
        public void SaveSettings_PreservesStableAudioSettingValues()
        {
            InMemorySaveService saveService = new InMemorySaveService();
            SettingsData settings = new SettingsData(0.1f, 0.2f, 0.3f, 0.4f);

            SaveResult result = saveService.SaveSettings(settings);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(saveService.Settings.BgmVolume, Is.EqualTo(0.2f));
            Assert.That(SettingsData.NarrationVolumeKey, Is.EqualTo("narrationVolume"));
        }

        private static SaveSnapshot CreateSnapshot(
            string checkpointId,
            string sceneId,
            long savedAtUnixSeconds)
        {
            CheckpointData checkpoint = new CheckpointData(
                checkpointId,
                sceneId,
                checkpointId,
                1f,
                2f,
                0f);
            return new SaveSnapshot(
                checkpoint,
                new Dictionary<string, string>(),
                savedAtUnixSeconds,
                60);
        }
    }
}
