using System;
using System.Reflection;
using NUnit.Framework;
using Spotlight.Demos.Demo2;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Spotlight.Tests.EditMode
{
    public sealed class Demo2MonsterConfigTests
    {
        private Demo2MonsterConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<Demo2MonsterConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void DefaultJumpConfiguration_IsValidAndUsesConfirmedValues()
        {
            Assert.That(_config.TryValidate(out string error), Is.True, error);
            Assert.That(_config.JumpHeight, Is.EqualTo(3.5f));
            Assert.That(_config.ObstacleDetectionDistance, Is.EqualTo(0.35f));
            Assert.That(_config.LandingRetrySeconds, Is.EqualTo(0.3f));
        }

        [TestCase("_jumpHeight", 0f)]
        [TestCase("_obstacleDetectionDistance", float.NaN)]
        [TestCase("_landingRetrySeconds", -0.01f)]
        public void InvalidJumpConfiguration_IsRejected(string fieldName, float value)
        {
            SetPrivateField(_config, fieldName, value);

            Assert.That(_config.TryValidate(out string error), Is.False);
            Assert.That(error, Is.Not.Empty);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new MissingFieldException(target.GetType().FullName, fieldName);
            }

            field.SetValue(target, value);
        }
    }
}
