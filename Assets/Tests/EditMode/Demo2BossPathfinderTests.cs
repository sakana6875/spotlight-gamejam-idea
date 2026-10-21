using NUnit.Framework;
using Spotlight.Demos.Demo2;

namespace Spotlight.Tests.EditMode
{
    public sealed class Demo2BossPathfinderTests
    {
        [Test]
        public void OpenGround_StepsStraightTowardThePlayer()
        {
            Demo2BossPathfinder pathfinder = new Demo2BossPathfinder();
            bool moved = pathfinder.TryStep(
                2f,
                1f,
                -2f,
                -1f,
                0.2f,
                0,
                new float[1],
                new float[1],
                new float[1],
                new float[1],
                0.9f,
                out float stepX,
                out float stepY);

            Assert.That(moved, Is.True);
            Assert.That(stepX, Is.LessThan(0f));
            Assert.That(stepY, Is.LessThan(0f));
            Assert.That(stepX * stepX + stepY * stepY, Is.EqualTo(0.04f).Within(0.0001f));
        }

        [Test]
        public void TreeCluster_ReachesThePlayerWithoutEnteringTrees()
        {
            Demo2BossPathfinder pathfinder = new Demo2BossPathfinder();
            float[] minX = { -1f, -2.2f, 0.2f };
            float[] minY = { -1f, 0.4f, -2.4f };
            float[] maxX = { 1f, -0.2f, 2.2f };
            float[] maxY = { 1f, 2.4f, -0.4f };
            float x = 2.6f;
            float y = 1.3f;
            const float GoalX = -4.6f;
            const float GoalY = -2.4f;
            const float Clearance = 0.95f;
            bool reached = false;
            for (int step = 0; step < 500; step++)
            {
                bool moved = pathfinder.TryStep(
                    x,
                    y,
                    GoalX,
                    GoalY,
                    0.05f,
                    3,
                    minX,
                    minY,
                    maxX,
                    maxY,
                    Clearance,
                    out float stepX,
                    out float stepY);
                Assert.That(moved, Is.True, "第 " + step + " 步无法绕开树群");
                x += stepX;
                y += stepY;
                Assert.That(Overlaps(x, y, minX, minY, maxX, maxY, Clearance), Is.False);
                float remainX = x - GoalX;
                float remainY = y - GoalY;
                if (remainX * remainX + remainY * remainY < 0.09f)
                {
                    reached = true;
                    break;
                }
            }

            Assert.That(reached, Is.True);
        }

        [Test]
        public void StartedInsideTree_StepsBackOut()
        {
            Demo2BossPathfinder pathfinder = new Demo2BossPathfinder();
            float[] minX = { -1f };
            float[] minY = { -1f };
            float[] maxX = { 1f };
            float[] maxY = { 1f };
            float x = 0f;
            float y = 0f;
            bool left = false;
            for (int step = 0; step < 80; step++)
            {
                Assert.That(pathfinder.TryStep(
                    x,
                    y,
                    4f,
                    0f,
                    0.2f,
                    1,
                    minX,
                    minY,
                    maxX,
                    maxY,
                    0.9f,
                    out float stepX,
                    out float stepY), Is.True);
                float nextX = x + stepX;
                float nextY = y + stepY;
                float before = Penetration(x, y, minX[0], minY[0], maxX[0], maxY[0], 0.9f);
                float after = Penetration(nextX, nextY, minX[0], minY[0], maxX[0], maxY[0], 0.9f);
                Assert.That(after, Is.LessThanOrEqualTo(before + 0.0001f));
                x = nextX;
                y = nextY;
                if (!Overlaps(x, y, minX, minY, maxX, maxY, 0.9f))
                {
                    left = true;
                    break;
                }
            }

            Assert.That(left, Is.True);
        }

        private static bool Overlaps(
            float x,
            float y,
            float[] minX,
            float[] minY,
            float[] maxX,
            float[] maxY,
            float clearance)
        {
            for (int index = 0; index < minX.Length; index++)
            {
                if (x + clearance > minX[index] && x - clearance < maxX[index] &&
                    y + clearance > minY[index] && y - clearance < maxY[index])
                {
                    return true;
                }
            }

            return false;
        }

        private static float Penetration(
            float x,
            float y,
            float minX,
            float minY,
            float maxX,
            float maxY,
            float clearance)
        {
            float overlapX = System.Math.Min(x + clearance - minX, maxX - (x - clearance));
            float overlapY = System.Math.Min(y + clearance - minY, maxY - (y - clearance));
            if (overlapX <= 0f || overlapY <= 0f)
            {
                return 0f;
            }

            return System.Math.Min(overlapX, overlapY);
        }
    }
}
