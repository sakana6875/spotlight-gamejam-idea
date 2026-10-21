using NUnit.Framework;
using Spotlight.Demos.Demo2;

namespace Spotlight.Tests.EditMode
{
    public sealed class Demo2BossStateMachineTests
    {
        [Test]
        public void OpeningChase_WaitsFiveSecondsThenSweeps()
        {
            Demo2BossStateMachine boss = new Demo2BossStateMachine();

            boss.Tick(4.9f, -1f, -1f, false);
            Assert.That(boss.Phase, Is.EqualTo(Demo2BossPhase.Chase));
            Assert.That(boss.StepX, Is.LessThan(0f));
            Assert.That(boss.StepY, Is.LessThan(0f));

            boss.Tick(0.1f, -1f, 0f, false);
            Assert.That(boss.Phase, Is.EqualTo(Demo2BossPhase.SweepWindup));
            Assert.That(boss.IsSweepTelegraphVisible, Is.False);
        }
        [Test]
        public void InvalidDelta_DoesNotMoveOrAttack()
        {
            Demo2BossStateMachine boss = new Demo2BossStateMachine();
            boss.Tick(float.NaN, 1f, 0f, false);
            boss.Tick(-1f, 1f, 0f, false);
            Assert.That(boss.Phase, Is.EqualTo(Demo2BossPhase.Chase));
            Assert.That(boss.StepX, Is.EqualTo(0f));
            Assert.That(boss.StepY, Is.EqualTo(0f));
        }


        [Test]
        public void Sweep_ShowsCircleOnlyDuringTheLastSecond()
        {
            Demo2BossStateMachine boss = AdvanceToSweep();

            boss.Tick(0.99f, 1f, 0f, false);
            Assert.That(boss.IsSweepTelegraphVisible, Is.False);

            boss.Tick(0.01f, 1f, 0f, false);
            Assert.That(boss.IsSweepTelegraphVisible, Is.True);
            Assert.That(boss.SweepResolvedThisTick, Is.False);

            boss.Tick(1f, 1f, 0f, false);
            Assert.That(boss.SweepResolvedThisTick, Is.True);
            Assert.That(boss.Phase, Is.EqualTo(Demo2BossPhase.Chase));
            Assert.That(boss.IsSweepTelegraphVisible, Is.False);
        }

        [Test]
        public void Charge_LocksDirectionWhenTheRedZoneAppears()
        {
            Demo2BossStateMachine boss = AdvanceToCharge();

            boss.Tick(3.99f, 0f, 1f, false);
            Assert.That(boss.IsChargeTelegraphVisible, Is.False);
            Assert.That(boss.ChargeLockedThisTick, Is.False);

            boss.Tick(0.01f, 0f, 1f, false);
            Assert.That(boss.ChargeLockedThisTick, Is.True);
            Assert.That(boss.IsChargeTelegraphVisible, Is.True);
            Assert.That(boss.LockedChargeY, Is.GreaterThan(0.9f));

            boss.Tick(0.5f, -1f, 0f, false);
            Assert.That(boss.LockedChargeY, Is.GreaterThan(0.9f));
            Assert.That(boss.Phase, Is.EqualTo(Demo2BossPhase.ChargeWindup));
        }

        [Test]
        public void Charge_TravelsEightMetersUnlessItHitsATree()
        {
            Demo2BossStateMachine boss = AdvanceToCharge();
            boss.Tick(5f, 1f, 0f, false);
            Assert.That(boss.Phase, Is.EqualTo(Demo2BossPhase.Charging));

            float traveled = 0f;
            for (int step = 0; step < 20 && boss.Phase == Demo2BossPhase.Charging; step++)
            {
                boss.Tick(0.05f, -1f, 0f, false);
                traveled += boss.StepX;
            }

            Assert.That(traveled, Is.EqualTo(8f).Within(0.001f));
            Assert.That(boss.Phase, Is.EqualTo(Demo2BossPhase.Chase));
        }

        [Test]
        public void FirstTreeHit_DefeatsBoss()
        {
            Demo2BossStateMachine boss = new Demo2BossStateMachine();

            ChargeIntoTree(boss);

            Assert.That(boss.StunStacks, Is.EqualTo(1));
            Assert.That(boss.Phase, Is.EqualTo(Demo2BossPhase.PermanentlyStunned));
            boss.Tick(10f, 1f, 0f, true);
            Assert.That(boss.Phase, Is.EqualTo(Demo2BossPhase.PermanentlyStunned));
            Assert.That(boss.StepX, Is.EqualTo(0f));
        }

        private static Demo2BossStateMachine AdvanceToSweep()
        {
            Demo2BossStateMachine boss = new Demo2BossStateMachine();
            boss.Tick(5f, 1f, 0f, false);
            Assert.That(boss.Phase, Is.EqualTo(Demo2BossPhase.SweepWindup));
            return boss;
        }

        private static Demo2BossStateMachine AdvanceToCharge()
        {
            Demo2BossStateMachine boss = AdvanceToSweep();
            boss.Tick(2f, 1f, 0f, false);
            boss.Tick(5f, 0f, 1f, false);
            Assert.That(boss.Phase, Is.EqualTo(Demo2BossPhase.ChargeWindup));
            return boss;
        }

        private static void ChargeIntoTree(Demo2BossStateMachine boss)
        {
            if (boss.Phase != Demo2BossPhase.ChargeWindup)
            {
                boss.Tick(5f, 1f, 0f, false);
            }

            if (boss.Phase == Demo2BossPhase.SweepWindup)
            {
                boss.Tick(2f, 1f, 0f, false);
                boss.Tick(5f, 1f, 0f, false);
            }

            if (boss.Phase == Demo2BossPhase.ChargeWindup)
            {
                boss.Tick(5f, 1f, 0f, false);
            }

            Assert.That(boss.Phase, Is.EqualTo(Demo2BossPhase.Charging));
            boss.Tick(0.1f, 1f, 0f, true);
        }
    }
}
