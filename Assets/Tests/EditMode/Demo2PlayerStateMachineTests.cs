using NUnit.Framework;
using Spotlight.Demos.Demo2;

namespace Spotlight.Tests.EditMode
{
    public sealed class Demo2PlayerStateMachineTests
    {
        private const float AttackDuration = 31f / 60f;

        [Test]
        public void Locomotion_TracksMovementAndIdle()
        {
            Demo2PlayerStateMachine stateMachine = new Demo2PlayerStateMachine();

            stateMachine.Advance(0f, true, false);
            Assert.That(stateMachine.State, Is.EqualTo(Demo2PlayerState.Moving));
            Assert.That(stateMachine.CanMove, Is.True);
            Assert.That(stateMachine.CanPush, Is.True);

            stateMachine.Advance(0f, false, false);
            Assert.That(stateMachine.State, Is.EqualTo(Demo2PlayerState.Idle));
        }

        [Test]
        public void FirstAttack_InputBeforeQueueWindowDoesNotStartSecondAttack()
        {
            Demo2PlayerStateMachine stateMachine = new Demo2PlayerStateMachine();

            stateMachine.Advance(0f, false, true);
            stateMachine.Advance(0.249f, false, true);
            stateMachine.Advance(AttackDuration - 0.249f, false, false);

            Assert.That(stateMachine.State, Is.EqualTo(Demo2PlayerState.Idle));
        }

        [Test]
        public void FirstAttack_InputAtQueueWindowStartsSecondAttackAndRestartsCooldown()
        {
            Demo2PlayerStateMachine stateMachine = new Demo2PlayerStateMachine();

            stateMachine.Advance(0f, false, true);
            stateMachine.Advance(0.25f, false, true);
            stateMachine.Advance(AttackDuration - 0.25f, false, false);

            Assert.That(stateMachine.State, Is.EqualTo(Demo2PlayerState.Attack2));
            Assert.That(stateMachine.CanMove, Is.False);
            Assert.That(stateMachine.CanPush, Is.False);

            stateMachine.Advance(AttackDuration, false, true);
            Assert.That(stateMachine.State, Is.EqualTo(Demo2PlayerState.Idle));

            stateMachine.Advance(0.28f, false, true);
            Assert.That(stateMachine.State, Is.EqualTo(Demo2PlayerState.Idle));

            stateMachine.Advance(0.01f, false, true);
            Assert.That(stateMachine.State, Is.EqualTo(Demo2PlayerState.Attack1));
        }

        [Test]
        public void HitStun_InterruptsQueuedAttackAndRejectsRepeatedHit()
        {
            Demo2PlayerStateMachine stateMachine = new Demo2PlayerStateMachine();
            stateMachine.Advance(0f, false, true);
            stateMachine.Advance(0.25f, false, true);

            Assert.That(stateMachine.TryEnterHitStun(), Is.True);
            stateMachine.Advance(0.49f, true, false);
            Assert.That(stateMachine.TryEnterHitStun(), Is.False);
            Assert.That(stateMachine.State, Is.EqualTo(Demo2PlayerState.HitStun));

            stateMachine.Advance(0.01f, true, false);
            Assert.That(stateMachine.State, Is.EqualTo(Demo2PlayerState.Moving));
        }

        [Test]
        public void CancelCurrentAction_RemovesQueuedSecondAttackAndReturnsToIdle()
        {
            Demo2PlayerStateMachine stateMachine = new Demo2PlayerStateMachine();
            stateMachine.Advance(0f, false, true);
            stateMachine.Advance(0.25f, false, true);

            stateMachine.CancelCurrentAction();
            stateMachine.Advance(AttackDuration, false, false);

            Assert.That(stateMachine.State, Is.EqualTo(Demo2PlayerState.Idle));
            Assert.That(stateMachine.CanMove, Is.True);
        }
        [Test]
        public void Attack1_CrossingStrikeTimeReportsExactlyOnce()
        {
            Demo2PlayerStateMachine stateMachine = new Demo2PlayerStateMachine();
            stateMachine.Advance(0f, false, true);

            Assert.That(
                stateMachine.Advance(0.249f, false, false),
                Is.EqualTo(Demo2PlayerAdvanceResult.None));
            Assert.That(
                stateMachine.Advance(0.001f, false, false),
                Is.EqualTo(Demo2PlayerAdvanceResult.Attack1Strike));
            Assert.That(
                stateMachine.Advance(0.1f, false, false),
                Is.EqualTo(Demo2PlayerAdvanceResult.None));
        }

        [Test]
        public void Attack2_ReportsItsOwnStrikeExactlyOnce()
        {
            Demo2PlayerStateMachine stateMachine = new Demo2PlayerStateMachine();
            stateMachine.Advance(0f, false, true);
            stateMachine.Advance(0.25f, false, true);
            stateMachine.Advance(AttackDuration - 0.25f, false, false);

            Assert.That(stateMachine.State, Is.EqualTo(Demo2PlayerState.Attack2));
            Assert.That(
                stateMachine.Advance(0.3f, false, false),
                Is.EqualTo(Demo2PlayerAdvanceResult.Attack2Strike));
            Assert.That(
                stateMachine.Advance(0.1f, false, false),
                Is.EqualTo(Demo2PlayerAdvanceResult.None));
        }

        [Test]
        public void CancelBeforeStrike_DoesNotReportDelayedStrike()
        {
            Demo2PlayerStateMachine stateMachine = new Demo2PlayerStateMachine();
            stateMachine.Advance(0f, false, true);
            stateMachine.Advance(0.2f, false, false);

            stateMachine.CancelCurrentAction();

            Assert.That(
                stateMachine.Advance(0.1f, false, false),
                Is.EqualTo(Demo2PlayerAdvanceResult.None));
            Assert.That(stateMachine.State, Is.EqualTo(Demo2PlayerState.Idle));
        }

    }
}
