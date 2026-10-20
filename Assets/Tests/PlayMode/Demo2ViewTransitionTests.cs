using Cinemachine;
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
        public IEnumerator ViewTransition_FarPlayerRemainsVisibleAndLandsSafelyInSideView()
        {
            Time.timeScale = 1f;

            Demo2SimulationController simulationController = CreateSimulationController();
            PlayerMoveController playerMoveController = CreatePlayerMoveController(simulationController);
            SetPlayerPosition(playerMoveController, new Vector2(40f, 0f));
            CameraRig cameraRig = CreateCameraRig(playerMoveController.transform);
            CanvasGroup blackoutCanvasGroup = CreateBlackoutCanvasGroup();
            GameObject sideView = CreateObject("SideView");
            GameObject topView = CreateObject("TopView");
            topView.SetActive(false);

            Demo2ViewModeController viewModeController = CreateViewModeController(
                playerMoveController,
                simulationController,
                blackoutCanvasGroup,
                sideView,
                topView,
                cameraRig);

            Assert.That(playerMoveController.TryEnterHitStun(), Is.True);
            Assert.That(viewModeController.TrySwitchView(), Is.True);
            Assert.That(playerMoveController.State, Is.EqualTo(Demo2PlayerState.Idle));

            yield return new WaitForSecondsRealtime(0.75f);

            Assert.That(simulationController.IsGameplayFrozen, Is.True);
            Assert.That(cameraRig.Brain.ActiveVirtualCamera, Is.EqualTo(cameraRig.TransitionCamera));
            Assert.That(sideView.activeSelf, Is.True);
            Assert.That(topView.activeSelf, Is.False);
            Assert.That(blackoutCanvasGroup.alpha, Is.GreaterThan(0f));

            yield return new WaitForSecondsRealtime(0.4f);

            Assert.That(sideView.activeSelf, Is.False);
            Assert.That(topView.activeSelf, Is.True);
            Assert.That(blackoutCanvasGroup.alpha, Is.GreaterThan(0.2f));

            yield return new WaitForSecondsRealtime(0.6f);

            Assert.That(simulationController.IsGameplayFrozen, Is.False);
            Assert.That(cameraRig.Brain.ActiveVirtualCamera, Is.EqualTo(cameraRig.GameplayCamera));
            Assert.That(blackoutCanvasGroup.alpha, Is.EqualTo(0f));
            AssertPlayerIsVisible(cameraRig.OutputCamera, playerMoveController.transform.position);

            SetPlayerPosition(playerMoveController, new Vector2(-30f, -5f));
            yield return null;

            Assert.That(viewModeController.TrySwitchView(), Is.True);
            yield return new WaitForSecondsRealtime(1.7f);

            Rigidbody2D playerBody = playerMoveController.GetComponent<Rigidbody2D>();
            Assert.That(sideView.activeSelf, Is.True);
            Assert.That(topView.activeSelf, Is.False);
            Assert.That(playerBody.position.x, Is.EqualTo(-30f));
            Assert.That(playerBody.position.y, Is.EqualTo(2.5f));
            Assert.That(simulationController.IsGameplayFrozen, Is.False);
            Assert.That(cameraRig.Brain.ActiveVirtualCamera, Is.EqualTo(cameraRig.GameplayCamera));
            Assert.That(blackoutCanvasGroup.alpha, Is.EqualTo(0f));
            AssertPlayerIsVisible(cameraRig.OutputCamera, playerMoveController.transform.position);
        }

        [UnityTest]
        public IEnumerator ViewTransition_DisablingControllerRestoresGameplayCameraAndPause()
        {
            Time.timeScale = 1f;

            Demo2SimulationController simulationController = CreateSimulationController();
            PlayerMoveController playerMoveController = CreatePlayerMoveController(simulationController);
            CameraRig cameraRig = CreateCameraRig(playerMoveController.transform);
            CanvasGroup blackoutCanvasGroup = CreateBlackoutCanvasGroup();
            GameObject sideView = CreateObject("SideView");
            GameObject topView = CreateObject("TopView");

            Demo2ViewModeController viewModeController = CreateViewModeController(
                playerMoveController,
                simulationController,
                blackoutCanvasGroup,
                sideView,
                topView,
                cameraRig);

            Assert.That(viewModeController.TrySwitchView(), Is.True);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(simulationController.IsGameplayFrozen, Is.True);

            viewModeController.enabled = false;

            Assert.That(simulationController.IsGameplayFrozen, Is.False);
            Assert.That(cameraRig.GameplayCamera.Priority, Is.EqualTo(20));
            Assert.That(cameraRig.TransitionCamera.Priority, Is.EqualTo(10));
            Assert.That(blackoutCanvasGroup.alpha, Is.EqualTo(0f));
            Assert.That(blackoutCanvasGroup.blocksRaycasts, Is.False);
        }

        [UnityTest]
        public IEnumerator GameplayFreeze_PausesAndResumesPlayerStateTime()
        {
            Time.timeScale = 1f;
            Demo2SimulationController simulationController = CreateSimulationController();
            PlayerMoveController playerMoveController = CreatePlayerMoveController(simulationController);

            Assert.That(playerMoveController.TryEnterHitStun(), Is.True);
            simulationController.AcquirePause(Demo2PauseReason.Dialogue);

            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(playerMoveController.State, Is.EqualTo(Demo2PlayerState.HitStun));

            simulationController.ReleasePause(Demo2PauseReason.Dialogue);
            yield return new WaitForSeconds(0.51f);
            Assert.That(playerMoveController.State, Is.EqualTo(Demo2PlayerState.Idle));
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
            Rigidbody2D playerBody = player.AddComponent<Rigidbody2D>();
            playerBody.gravityScale = 0f;

            GameObject groundCheck = CreateObject("GroundCheck");
            groundCheck.transform.SetParent(player.transform, false);
            groundCheck.transform.localPosition = Vector3.down;

            GameObject attackPoint = CreateObject("AttackPoint");
            attackPoint.transform.SetParent(player.transform, false);
            attackPoint.transform.localPosition = new Vector3(0.9f, 0f, 0f);

            Demo2PlayerMeleeAttack meleeAttack = player.AddComponent<Demo2PlayerMeleeAttack>();
            SetPrivateField(meleeAttack, "_attackPoint", attackPoint.transform);
            SetPrivateField(meleeAttack, "_monsterLayers", (LayerMask)(1 << LayerMask.NameToLayer("Monster")));
            SetPrivateField(meleeAttack, "_obstacleLayers", (LayerMask)(1 << LayerMask.NameToLayer("SideWall")));

            PlayerMoveController playerMoveController = player.AddComponent<PlayerMoveController>();
            SetPrivateField(playerMoveController, "_groundCheck", groundCheck.transform);
            SetPrivateField(playerMoveController, "_simulationController", simulationController);
            SetPrivateField(playerMoveController, "_meleeAttack", meleeAttack);
            player.SetActive(true);

            return playerMoveController;
        }

        private CameraRig CreateCameraRig(Transform playerTransform)
        {
            GameObject outputCameraObject = CreateObject("Main Camera");
            outputCameraObject.tag = "MainCamera";
            outputCameraObject.transform.position = new Vector3(0f, 0f, -10f);

            Camera outputCamera = outputCameraObject.AddComponent<Camera>();
            outputCamera.orthographic = true;
            outputCamera.orthographicSize = 5.4f;

            CinemachineBrain brain = outputCameraObject.AddComponent<CinemachineBrain>();
            brain.m_IgnoreTimeScale = true;
            brain.m_UpdateMethod = CinemachineBrain.UpdateMethod.SmartUpdate;
            brain.m_BlendUpdateMethod = CinemachineBrain.BrainUpdateMethod.LateUpdate;
            brain.m_DefaultBlend = new CinemachineBlendDefinition(
                CinemachineBlendDefinition.Style.EaseInOut,
                0.5f);

            CinemachineVirtualCamera gameplayCamera =
                CreateObject("CM_Gameplay").AddComponent<CinemachineVirtualCamera>();
            gameplayCamera.Follow = playerTransform;
            gameplayCamera.Priority = 20;
            gameplayCamera.m_Lens.OrthographicSize = 5.4f;
            CinemachineFramingTransposer framing =
                gameplayCamera.AddCinemachineComponent<CinemachineFramingTransposer>();
            framing.m_XDamping = 0f;
            framing.m_YDamping = 0f;
            framing.m_ZDamping = 0f;
            framing.m_DeadZoneWidth = 0f;
            framing.m_DeadZoneHeight = 0f;

            GameObject transitionObject = CreateObject("CM_ViewTransition");
            transitionObject.transform.position = new Vector3(0f, 0f, -10f);
            CinemachineVirtualCamera transitionCamera =
                transitionObject.AddComponent<CinemachineVirtualCamera>();
            transitionCamera.Priority = 10;
            transitionCamera.m_Lens.OrthographicSize = 5.4f;

            return new CameraRig(outputCamera, brain, gameplayCamera, transitionCamera);
        }

        private CanvasGroup CreateBlackoutCanvasGroup()
        {
            return CreateObject("Blackout").AddComponent<CanvasGroup>();
        }

        private Demo2ViewModeController CreateViewModeController(
            PlayerMoveController playerMoveController,
            Demo2SimulationController simulationController,
            CanvasGroup blackoutCanvasGroup,
            GameObject sideView,
            GameObject topView,
            CameraRig cameraRig)
        {
            GameObject viewMode = CreateInactiveObject("ViewMode");
            Demo2ViewModeController viewModeController = viewMode.AddComponent<Demo2ViewModeController>();
            SetPrivateField(viewModeController, "_playerMoveController", playerMoveController);
            SetPrivateField(viewModeController, "_simulationController", simulationController);
            SetPrivateField(viewModeController, "_blackoutCanvasGroup", blackoutCanvasGroup);
            SetPrivateField(viewModeController, "_sideView", sideView);
            SetPrivateField(viewModeController, "_topView", topView);
            SetPrivateField(viewModeController, "_cameraBrain", cameraRig.Brain);
            SetPrivateField(viewModeController, "_gameplayCamera", cameraRig.GameplayCamera);
            SetPrivateField(viewModeController, "_viewTransitionCamera", cameraRig.TransitionCamera);
            SetPrivateField(viewModeController, "_sideLandingY", 2.5f);
            viewMode.SetActive(true);

            return viewModeController;
        }

        private static void SetPlayerPosition(PlayerMoveController playerMoveController, Vector2 position)
        {
            playerMoveController.transform.position = position;
            playerMoveController.GetComponent<Rigidbody2D>().position = position;
            Physics2D.SyncTransforms();
        }

        private static void AssertPlayerIsVisible(Camera camera, Vector3 playerPosition)
        {
            Vector3 viewportPosition = camera.WorldToViewportPoint(playerPosition);
            Assert.That(viewportPosition.x, Is.InRange(0f, 1f));
            Assert.That(viewportPosition.y, Is.InRange(0f, 1f));
            Assert.That(viewportPosition.z, Is.GreaterThan(0f));
        }

        private readonly struct CameraRig
        {
            public CameraRig(
                Camera outputCamera,
                CinemachineBrain brain,
                CinemachineVirtualCamera gameplayCamera,
                CinemachineVirtualCamera transitionCamera)
            {
                OutputCamera = outputCamera;
                Brain = brain;
                GameplayCamera = gameplayCamera;
                TransitionCamera = transitionCamera;
            }

            public Camera OutputCamera { get; }

            public CinemachineBrain Brain { get; }

            public CinemachineVirtualCamera GameplayCamera { get; }

            public CinemachineVirtualCamera TransitionCamera { get; }
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
