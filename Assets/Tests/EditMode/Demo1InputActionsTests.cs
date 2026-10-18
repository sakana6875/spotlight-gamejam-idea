using NUnit.Framework;
using UnityEditor;
using UnityEngine.InputSystem;

namespace Spotlight.Tests.EditMode
{
    public sealed class Demo1InputActionsTests : InputTestFixture
    {
        [Test]
        public void Pulse_SpaceAndLeftClick_PerformAction()
        {
            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/Input_Actions.inputactions");
            Assert.That(inputActions, Is.Not.Null);

            InputAction pulse = inputActions.FindAction("Gameplay/Pulse", true);
            pulse.Enable();

            try
            {
                Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
                Mouse mouse = InputSystem.AddDevice<Mouse>();

                Press(keyboard.spaceKey);
                Assert.That(pulse.WasPerformedThisFrame(), Is.True);
                Release(keyboard.spaceKey);

                Press(mouse.leftButton);
                Assert.That(pulse.WasPerformedThisFrame(), Is.True);
                Release(mouse.leftButton);
            }
            finally
            {
                pulse.Disable();
            }
        }
    }
}
