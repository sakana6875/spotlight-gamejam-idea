using NUnit.Framework;
using Spotlight.Demos.Demo2;
using UnityEditor;
using UnityEngine;

namespace Spotlight.Tests.EditMode
{
    public sealed class Demo2MonsterStateMachineTests
    {
        [Test]
        public void Config_DefaultSlimeValuesAreValid()
        {
            Demo2MonsterConfig config = ScriptableObject.CreateInstance<Demo2MonsterConfig>();

            try
            {
                Assert.That(config.TryValidate(out string error), Is.True, error);
                Assert.That(config.MaxHealth, Is.EqualTo(30));
                Assert.That(config.MoveSpeed, Is.EqualTo(1f));
                Assert.That(config.BiteDistance, Is.EqualTo(0.5f));
                Assert.That(config.HitStunDurationSeconds, Is.EqualTo(0.5f));
                Assert.That(config.DeathDurationSeconds, Is.EqualTo(0.5f));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void Config_NonFiniteMoveSpeedIsRejectedWithReason()
        {
            Demo2MonsterConfig config = ScriptableObject.CreateInstance<Demo2MonsterConfig>();

            try
            {
                SerializedObject serializedConfig = new SerializedObject(config);
                serializedConfig.FindProperty("_moveSpeed").floatValue = float.NaN;
                serializedConfig.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(config.TryValidate(out string error), Is.False);
                Assert.That(error, Does.Contain("移动速度"));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void Movement_EntersAndLeavesBitingByDistance()
        {
            Demo2MonsterStateMachine stateMachine = CreateStateMachine();

            stateMachine.Advance(0f, true);
            Assert.That(stateMachine.State, Is.EqualTo(Demo2MonsterState.Biting));

            stateMachine.Advance(0f, false);
            Assert.That(stateMachine.State, Is.EqualTo(Demo2MonsterState.Moving));
        }

        [Test]
        public void Damage_InvalidAmountDoesNotChangeHealthOrState()
        {
            Demo2MonsterStateMachine stateMachine = CreateStateMachine();

            Assert.That(stateMachine.TryTakeDamage(0), Is.EqualTo(MonsterDamageResult.InvalidAmount));
            Assert.That(stateMachine.TryTakeDamage(-10), Is.EqualTo(MonsterDamageResult.InvalidAmount));
            Assert.That(stateMachine.CurrentHealth, Is.EqualTo(30));
            Assert.That(stateMachine.State, Is.EqualTo(Demo2MonsterState.Moving));
        }

        [Test]
        public void HitStun_InterruptsBitingAndLatestHitRestartsDuration()
        {
            Demo2MonsterStateMachine stateMachine = CreateStateMachine();
            stateMachine.Advance(0f, true);

            Assert.That(stateMachine.TryTakeDamage(10), Is.EqualTo(MonsterDamageResult.Applied));
            Assert.That(stateMachine.State, Is.EqualTo(Demo2MonsterState.HitStun));
            Assert.That(stateMachine.CurrentHealth, Is.EqualTo(20));

            stateMachine.Advance(0.3f, true);
            Assert.That(stateMachine.TryTakeDamage(10), Is.EqualTo(MonsterDamageResult.Applied));
            stateMachine.Advance(0.49f, true);
            Assert.That(stateMachine.State, Is.EqualTo(Demo2MonsterState.HitStun));

            stateMachine.Advance(0.01f, true);
            Assert.That(stateMachine.State, Is.EqualTo(Demo2MonsterState.Biting));
            Assert.That(stateMachine.CurrentHealth, Is.EqualTo(10));
        }

        [Test]
        public void ThirdTenDamageHit_KillsOnceAndCompletesDeathDuration()
        {
            Demo2MonsterStateMachine stateMachine = CreateStateMachine();

            Assert.That(stateMachine.TryTakeDamage(10), Is.EqualTo(MonsterDamageResult.Applied));
            Assert.That(stateMachine.TryTakeDamage(10), Is.EqualTo(MonsterDamageResult.Applied));
            Assert.That(stateMachine.TryTakeDamage(10), Is.EqualTo(MonsterDamageResult.Killed));
            Assert.That(stateMachine.CurrentHealth, Is.Zero);
            Assert.That(stateMachine.IsAlive, Is.False);
            Assert.That(stateMachine.State, Is.EqualTo(Demo2MonsterState.Dead));
            Assert.That(stateMachine.TryTakeDamage(10), Is.EqualTo(MonsterDamageResult.AlreadyDead));

            stateMachine.Advance(0.49f, false);
            Assert.That(stateMachine.ShouldDisappear, Is.False);
            stateMachine.Advance(0.01f, false);
            Assert.That(stateMachine.ShouldDisappear, Is.True);
        }

        private static Demo2MonsterStateMachine CreateStateMachine()
        {
            return new Demo2MonsterStateMachine(30, 0.5f, 0.5f);
        }
    }
}
