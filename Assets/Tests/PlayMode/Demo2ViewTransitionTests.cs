using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Spotlight.Demos.Demo2;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Spotlight.Tests.PlayMode
{
    public sealed class Demo2ViewTransitionTests
    {
        private readonly List<Object> _createdObjects = new List<Object>();

        [UnityTest]
        public IEnumerator ViewTransition_SwitchesMapUnderBlackoutAndLandsSafelyInSideView()
        {
            Time.timeScale = 1f;

            Demo2SimulationController simulationController = CreateSimulationController();
            PlayerMoveController playerMoveController = CreatePlayerMoveController(simulationController);
            Demo2CameraFollowController cameraFollowController = CreateCameraFollowController(playerMoveController.transform);
            CanvasGroup blackoutCanvasGroup = CreateBlackoutCanvasGroup();
            GameObject sideView = CreateObject("SideView");
            GameObject topView = CreateObject("TopView");
            topView.SetActive(false);

            Demo2ViewModeController viewModeController = CreateViewModeController(
                playerMoveController,
                cameraFollowController,
                simulationController,
                blackoutCanvasGroup,
                sideView,
                topView);

            Assert.That(viewModeController.TrySwitchView(), Is.True);

            yield return new WaitForSecondsRealtime(0.75f);

            Assert.That(simulationController.IsGameplayFrozen, Is.True);
            Assert.That(cameraFollowController.IsFollowing, Is.False);
            Assert.That(sideView.activeSelf, Is.True);
            Assert.That(topView.activeSelf, Is.False);
            Assert.That(blackoutCanvasGroup.alpha, Is.GreaterThan(0f));
            yield return new WaitForSecondsRealtime(0.4f);

            Assert.That(sideView.activeSelf, Is.False);
            Assert.That(topView.activeSelf, Is.True);
            Assert.That(blackoutCanvasGroup.alpha, Is.GreaterThan(0.2f));

            yield return new WaitForSecondsRealtime(0.6f);

            Assert.That(simulationController.IsGameplayFrozen, Is.False);
            Assert.That(cameraFollowController.IsFollowing, Is.True);
            Assert.That(blackoutCanvasGroup.alpha, Is.EqualTo(0f));

            Assert.That(cameraFollowController.transform.position, Is.EqualTo(new Vector3(10f, 0f, -10f)));

            Rigidbody2D playerBody = playerMoveController.GetComponent<Rigidbody2D>();
            playerBody.position = new Vector2(1f, 0f);
            Physics2D.SyncTransforms();
            yield return null;

            Assert.That(cameraFollowController.transform.position, Is.EqualTo(new Vector3(11f, 0f, -10f)));

            playerBody.position = new Vector2(4f, -5f);

            Assert.That(viewModeController.TrySwitchView(), Is.True);

            yield return new WaitForSecondsRealtime(1.1f);

            Assert.That(sideView.activeSelf, Is.True);
            Assert.That(topView.activeSelf, Is.False);
            Assert.That(playerBody.position.x, Is.EqualTo(4f));
            Assert.That(playerBody.position.y, Is.EqualTo(2.5f));

            yield return new WaitForSecondsRealtime(0.6f);

            Assert.That(simulationController.IsGameplayFrozen, Is.False);
            Assert.That(cameraFollowController.IsFollowing, Is.True);
            Assert.That(blackoutCanvasGroup.alpha, Is.EqualTo(0f));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;

            foreach (Object createdObject in _createdObjects)
            {
                if (createdObject != null)
                {
                    Object.Destroy(createdObject);
                }
            }

            _createdObjects.Clear();
            yield return null;
        }

        private Demo2SimulationController CreateSimulationController()
        {
            return CreateObject("Simulation").AddComponent<Demo2SimulationController>();
        }

        private PlayerMoveController CreatePlayerMoveController(Demo2SimulationController simulationController)
        {
            GameObject player = CreateInactiveObject("Player");
            player.AddComponent<Rigidbody2D>();

            GameObject groundCheck = CreateObject("GroundCheck");
            groundCheck.transform.SetParent(player.transform, false);
            groundCheck.transform.localPosition = Vector3.down;

            PlayerMoveController playerMoveController = player.AddComponent<PlayerMoveController>();
            SetPrivateField(playerMoveController, "_groundCheck", groundCheck.transform);
            SetPrivateField(playerMoveController, "_simulationController", simulationController);
            player.SetActive(true);

            return playerMoveController;
        }

        private Demo2CameraFollowController CreateCameraFollowController(Transform playerTransform)
        {
            GameObject camera = CreateInactiveObject("Camera");
            Demo2CameraFollowController cameraFollowController = camera.AddComponent<Demo2CameraFollowController>();
            SetPrivateField(cameraFollowController, "_target", playerTransform);
            camera.SetActive(true);

            return cameraFollowController;
        }

        private CanvasGroup CreateBlackoutCanvasGroup()
        {
            return CreateObject("Blackout").AddComponent<CanvasGroup>();
        }

        private Demo2ViewModeController CreateViewModeController(
            PlayerMoveController playerMoveController,
            Demo2CameraFollowController cameraFollowController,
            Demo2SimulationController simulationController,
            CanvasGroup blackoutCanvasGroup,
            GameObject sideView,
            GameObject topView)
        {
            Transform sideGameplayAnchor = CreateAnchor("SideGameplay", new Vector3(0f, 0f, -10f));
            Transform sideCloseupAnchor = CreateAnchor("SideCloseup", new Vector3(2f, 0f, -10f));
            Transform topGameplayAnchor = CreateAnchor("TopGameplay", new Vector3(10f, 0f, -10f));
            Transform topCloseupAnchor = CreateAnchor("TopCloseup", new Vector3(8f, 0f, -10f));

            GameObject viewMode = CreateInactiveObject("ViewMode");
            Demo2ViewModeController viewModeController = viewMode.AddComponent<Demo2ViewModeController>();
            SetPrivateField(viewModeController, "_playerMoveController", playerMoveController);
            SetPrivateField(viewModeController, "_cameraFollowController", cameraFollowController);
            SetPrivateField(viewModeController, "_simulationController", simulationController);
            SetPrivateField(viewModeController, "_blackoutCanvasGroup", blackoutCanvasGroup);
            SetPrivateField(viewModeController, "_sideView", sideView);
            SetPrivateField(viewModeController, "_topView", topView);
            SetPrivateField(viewModeController, "_sideGameplayAnchor", sideGameplayAnchor);
            SetPrivateField(viewModeController, "_sideCloseupAnchor", sideCloseupAnchor);
            SetPrivateField(viewModeController, "_topGameplayAnchor", topGameplayAnchor);
            SetPrivateField(viewModeController, "_topCloseupAnchor", topCloseupAnchor);
            SetPrivateField(viewModeController, "_sideLandingY", 2.5f);
            viewMode.SetActive(true);

            return viewModeController;
        }

        private Transform CreateAnchor(string name, Vector3 position)
        {
            GameObject anchor = CreateObject(name);
            anchor.transform.position = position;
            return anchor.transform;
        }

        private GameObject CreateObject(string name)
        {
            GameObject gameObject = new GameObject(name);
            _createdObjects.Add(gameObject);
            return gameObject;
        }

        private GameObject CreateInactiveObject(string name)
        {
            GameObject gameObject = CreateObject(name);
            gameObject.SetActive(false);
            return gameObject;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (field == null)
            {
                throw new MissingFieldException(target.GetType().FullName, fieldName);
            }

            field.SetValue(target, value);
        }
    }
}
