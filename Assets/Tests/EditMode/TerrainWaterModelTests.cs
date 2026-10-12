using System;
using NUnit.Framework;
using Spotlight.Demos.Demo2;

namespace Spotlight.Tests.EditMode
{
    public sealed class TerrainWaterModelTests
    {
        [Test]
        public void ResetToSources_PreservesOnlyAuthoredSourceCells()
        {
            TerrainWaterModel model = new TerrainWaterModel(5, 2);
            model.SetSource(1, 0, true);
            model.SetSource(2, 1, true);

            model.ResetToSources();

            Assert.That(model.IsWater(1, 0), Is.True);
            Assert.That(model.IsWater(2, 1), Is.True);
            Assert.That(model.IsWater(0, 0), Is.False);
        }

        [Test]
        public void SteppingModel_ExtendsRightwardFromAuthoredSource()
        {
            TerrainWaterModel model = CreateSourceModel(5, 2, 0, 0);

            Step(model, 4);

            Assert.That(model.IsWater(4, 0), Is.True);
        }

        [Test]
        public void SteppingModel_StopsAtBlockedWallAndAccumulatesBeforeIt()
        {
            TerrainWaterModel model = CreateSourceModel(5, 3, 0, 0);
            model.SetBlocked(2, 0, true);

            Step(model, 4);

            Assert.That(model.IsWater(2, 0), Is.False);
            Assert.That(model.IsWater(1, 1), Is.True);
            Assert.That(model.IsWater(3, 0), Is.False);
        }

        [Test]
        public void SteppingModel_CrossesAboveFiniteBarrierAfterAccumulating()
        {
            TerrainWaterModel model = CreateSourceModel(5, 2, 0, 0);
            model.SetBlocked(2, 0, true);

            Step(model, 6);

            Assert.That(model.IsWater(3, 1), Is.True);
            Assert.That(model.IsWater(2, 0), Is.False);
        }

        [Test]
        public void SteppingModel_ContinuesRightwardAfterMovingBlockerIsRemoved()
        {
            TerrainWaterModel model = CreateSourceModel(5, 2, 0, 0);
            model.SetBlocked(2, 0, true);
            Step(model, 3);

            model.SetBlocked(2, 0, false);
            Step(model, 3);

            Assert.That(model.IsWater(4, 0), Is.True);
        }

        [Test]
        public void SteppingModel_DoesNotFlowDownFromAuthoredSource()
        {
            TerrainWaterModel model = CreateSourceModel(4, 2, 0, 1);

            Step(model, 4);

            Assert.That(model.IsWater(3, 1), Is.True);
            Assert.That(model.IsWater(3, 0), Is.False);
        }

        [Test]
        public void SourceCount_TracksAuthoredCellsWithoutDoubleCounting()
        {
            TerrainWaterModel model = new TerrainWaterModel(3, 1);
            model.SetSource(0, 0, true);
            model.SetSource(0, 0, true);
            model.SetSource(1, 0, true);
            model.SetSource(0, 0, false);

            Assert.That(model.SourceCount, Is.EqualTo(1));
        }

        [TestCase(0, 1)]
        [TestCase(1, 0)]
        public void CreatingInvalidModel_ThrowsArgumentOutOfRangeException(int width, int height)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TerrainWaterModel(width, height));
        }

        private static TerrainWaterModel CreateSourceModel(int width, int height, int sourceColumn, int sourceRow)
        {
            TerrainWaterModel model = new TerrainWaterModel(width, height);
            model.SetSource(sourceColumn, sourceRow, true);
            model.ResetToSources();
            return model;
        }

        private static void Step(TerrainWaterModel model, int count)
        {
            for (int index = 0; index < count; index++)
            {
                model.Step();
            }
        }
    }
}
