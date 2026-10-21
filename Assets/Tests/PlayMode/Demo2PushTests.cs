using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Spotlight.Demos.Demo2;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Spotlight.Tests.PlayMode
{
    public sealed class Demo2PushTests
    {
        private readonly List<Object> _createdObjects = new List<Object>();
        private readonly List<InputDevice> _createdDevices = new List<InputDevice>();

        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            Time.timeScale = 1f;

            for (int index = _createdDevices.Count - 1; index >= 0; index--)
            {
                if (_createdDevices[index] != null)
                {
                    InputSystem.RemoveDevice(_createdDevices[index]);
                }
            }

            _createdDevices.Clear();

            for (int index = _createdObjects.Count - 1; index >= 0; index--)
            {
                if (_createdObjects[index] != null)
                {
                    Object.Destroy(_createdObjects[index]);
                }
            }

            _createdObjects.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator AuthorizedPushMovesAtConfiguredSpeedAndStopsWithoutFurtherRequests()
        {
            Demo2SimulationController simulationController = CreateSimulationController();
            PlayerMoveController player = CreatePlayer(simulationController);
            Demo2PushController pushController = player.GetComponent<Demo2PushController>();
            Demo2PushableObstacle obstacle = CreatePushableObstacle(new Vector2(1.05f, 0f));
            Physics2D.SyncTransforms();
            float startX = obstacle.Rigidbody.position.x;

            Assert.That(pushController.TryPush(), Is.True);
            yield return new WaitForFixedUpdate();
            Assert.That(pushController.TryPush(), Is.True);
            yield return new WaitForFixedUpdate();

            float expectedDistance = 1f * Time.fixedDeltaTime * 2f;
            Assert.That(obstacle.Rigidbody.position.x, Is.EqualTo(startX + expectedDistance).Within(0.005f));
            Assert.That(obstacle.Rigidbody.position.y, Is.EqualTo(0f).Within(0.001f));

            float releasedX = obstacle.Rigidbody.position.x;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(obstacle.Rigidbody.position.x, Is.EqualTo(releasedX).Within(0.001f));
            Assert.That(player.ViewMode, Is.EqualTo(PlayerMoveController.Demo2ViewMode.Side));
        }

        [UnityTest]
        public IEnumerator PlayerAndMonsterCollisions_CannotMoveObstacleWithoutPushRequest()
        {
            Demo2PushableObstacle obstacle = CreatePushableObstacle(new Vector2(1.1f, 0f));
            float startX = obstacle.Rigidbody.position.x;
            Rigidbody2D playerBody = CreateBody("PlayerCollisionBody", Vector2.zero);
            playerBody.velocity = new Vector2(3f, 0f);
            Physics2D.SyncTransforms();

            for (int step = 0; step < 5; step++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(obstacle.Rigidbody.position.x, Is.EqualTo(startX).Within(0.001f));

            Object.Destroy(playerBody.gameObject);
            yield return null;
            Rigidbody2D monsterBody = CreateBody("MonsterCollisionBody", new Vector2(2.2f, 0f));
            monsterBody.velocity = new Vector2(-3f, 0f);
            Physics2D.SyncTransforms();

            for (int step = 0; step < 5; step++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(obstacle.Rigidbody.position.x, Is.EqualTo(startX).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator GravityFallsWhileHorizontalPositionRemainsLocked()
        {
            Demo2PushableObstacle obstacle = CreatePushableObstacle(new Vector2(1.1f, 2f));
            obstacle.Rigidbody.gravityScale = 1f;
            float startX = obstacle.Rigidbody.position.x;
            float startY = obstacle.Rigidbody.position.y;

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(obstacle.Rigidbody.position.x, Is.EqualTo(startX).Within(0.001f));
            Assert.That(obstacle.Rigidbody.position.y, Is.LessThan(startY));
            Assert.That(obstacle.Rigidbody.velocity.y, Is.LessThan(0f));
        }

        [UnityTest]
        public IEnumerator TopView_BackFacingAndUnmarkedBody_DoNotReceivePush()
        {
            Demo2SimulationController simulationController = CreateSimulationController();
            PlayerMoveController player = CreatePlayer(simulationController);
            Demo2PushController pushController = player.GetComponent<Demo2PushController>();
            Demo2PushableObstacle obstacle = CreatePushableObstacle(new Vector2(1.05f, 0f));
            Physics2D.SyncTransforms();
            float startX = obstacle.Rigidbody.position.x;

            player.SetViewMode(PlayerMoveController.Demo2ViewMode.Top, Vector2.zero);
            Assert.That(pushController.TryPush(), Is.False);
            yield return new WaitForFixedUpdate();
            Assert.That(obstacle.Rigidbody.position.x, Is.EqualTo(startX).Within(0.001f));

            player.SetViewMode(PlayerMoveController.Demo2ViewMode.Side, Vector2.zero);
            obstacle.Rigidbody.position = new Vector2(-1.05f, 0f);
            Physics2D.SyncTransforms();
            Assert.That(player.IsFacingRight, Is.True);
            Assert.That(pushController.TryPush(), Is.False);
            yield return new WaitForFixedUpdate();
            Assert.That(obstacle.Rigidbody.position.x, Is.EqualTo(-1.05f).Within(0.001f));

            Object.Destroy(obstacle.gameObject);
            yield return null;

            Rigidbody2D ordinaryBody = CreateBody("OrdinaryBody", new Vector2(1.05f, 0f));
            Assert.That(pushController.TryPush(), Is.False);
            yield return new WaitForFixedUpdate();
            Assert.That(ordinaryBody.position.x, Is.EqualTo(1.05f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator HitStun_PreventsPushFromReachingObstacle()
        {
            Demo2SimulationController simulationController = CreateSimulationController();
            PlayerMoveController player = CreatePlayer(simulationController);
            Demo2PushController pushController = player.GetComponent<Demo2PushController>();
            Demo2PushableObstacle obstacle = CreatePushableObstacle(new Vector2(1.05f, 0f));
            Physics2D.SyncTransforms();

            Assert.That(player.TryEnterHitStun(), Is.True);
            Assert.That(pushController.TryPush(), Is.False);
            yield return new WaitForFixedUpdate();

            Assert.That(obstacle.Rigidbody.position, Is.EqualTo(new Vector2(1.05f, 0f)));
        }

        [UnityTest]
        public IEnumerator AirborneActionLock_StopsHorizontalControlButPreservesGravity()
        {
            Demo2SimulationController simulationController = CreateSimulationController();
            PlayerMoveController player = CreatePlayer(simulationController);
            Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
            playerBody.gravityScale = 1f;
            playerBody.velocity = new Vector2(2f, -1f);

            Assert.That(player.TryEnterHitStun(), Is.True);
            yield return new WaitForFixedUpdate();

            Assert.That(playerBody.velocity.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(playerBody.velocity.y, Is.LessThan(-1f));
        }

        [UnityTest]
        public IEnumerator PushableObstacle_RemainsBlockedByWall()
        {
            Demo2SimulationController simulationController = CreateSimulationController();
            PlayerMoveController player = CreatePlayer(simulationController);
            Demo2PushController pushController = player.GetComponent<Demo2PushController>();
            Demo2PushableObstacle obstacle = CreatePushableObstacle(new Vector2(1.05f, 0f));
            BoxCollider2D wall = CreateStaticBox("Wall", new Vector2(2.1f, 0f));
            BoxCollider2D obstacleCollider = obstacle.GetComponent<BoxCollider2D>();
            Physics2D.SyncTransforms();
            for (int step = 0; step < 30; step++)
            {
                pushController.TryPush();
                yield return new WaitForFixedUpdate();
            }

            Assert.That(obstacleCollider.bounds.max.x, Is.LessThanOrEqualTo(wall.bounds.min.x + 0.02f));
        }
        [UnityTest]
        public IEnumerator FrozenOrMissedPush_DoesNotPublishUntilFirstSuccess()
        {
            Demo2SimulationController simulationController = CreateSimulationController();
            PlayerMoveController player = CreatePlayer(simulationController);
            Demo2PushController pushController = player.GetComponent<Demo2PushController>();
            int startedCount = 0;
            pushController.PushStarted += () => startedCount++;

            Assert.That(pushController.TryPush(), Is.False);
            Assert.That(startedCount, Is.Zero);

            CreatePushableObstacle(new Vector2(1.05f, 0f));
            Physics2D.SyncTransforms();
            simulationController.AcquirePause(Demo2PauseReason.Dialogue);
            Assert.That(pushController.TryPush(), Is.False);
            Assert.That(startedCount, Is.Zero);

            simulationController.ReleasePause(Demo2PauseReason.Dialogue);
            Assert.That(pushController.TryPush(), Is.True);
            Assert.That(startedCount, Is.EqualTo(1));
            Assert.That(pushController.TryPush(), Is.True);
            Assert.That(startedCount, Is.EqualTo(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ObstacleEnteringPit_PublishesPitFilledOnce()
        {
            GameObject invalidPit = CreateObject("InvalidPit");
            invalidPit.AddComponent<BoxCollider2D>();
            LogAssert.Expect(LogType.Error, "Demo2PitFillTrigger 的 Collider2D 必须是 Trigger。");
            Demo2PitFillTrigger invalidTrigger = invalidPit.AddComponent<Demo2PitFillTrigger>();
            Assert.That(invalidTrigger.enabled, Is.False);

            GameObject pitObject = CreateObject("Pit");
            BoxCollider2D pitCollider = pitObject.AddComponent<BoxCollider2D>();
            pitCollider.isTrigger = true;
            pitCollider.size = new Vector2(1f, 1f);
            Demo2PitFillTrigger pit = pitObject.AddComponent<Demo2PitFillTrigger>();
            int filledCount = 0;
            pit.PitFilled += () => filledCount++;

            Rigidbody2D otherBody = CreateBody("NotObstacle", new Vector2(0f, 2f));
            yield return MoveBodyInto(otherBody, new Vector2(0f, -8f), 20);
            Assert.That(filledCount, Is.Zero);

            Demo2PushableObstacle obstacle = CreatePushableObstacle(new Vector2(0f, 3f));
            yield return MoveBodyInto(obstacle.Rigidbody, new Vector2(0f, -8f), 30);
            Assert.That(filledCount, Is.EqualTo(1));

            obstacle.Rigidbody.position = new Vector2(0f, 3f);
            obstacle.Rigidbody.velocity = Vector2.zero;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return MoveBodyInto(obstacle.Rigidbody, new Vector2(0f, -8f), 30);
            Assert.That(filledCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator FirstAttackStrike_PublishesOnceAcrossLaterStrikes()
        {
            Demo2SimulationController simulationController = CreateSimulationController();
            PlayerMoveController player = CreatePlayer(simulationController);
            int strikeCount = 0;
            player.AttackStrikeStarted += () => strikeCount++;
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            _createdDevices.Add(mouse);
            yield return null;

            InputSystem.QueueStateEvent(mouse, default(MouseState).WithButton(MouseButton.Left));
            float elapsed = 0f;
            while (elapsed < 0.45f && strikeCount == 0)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }

            Assert.That(strikeCount, Is.EqualTo(1));
            InputSystem.QueueStateEvent(mouse, default(MouseState));
            yield return new WaitForSeconds(0.9f);

            InputSystem.QueueStateEvent(mouse, default(MouseState).WithButton(MouseButton.Left));
            yield return new WaitForSeconds(0.4f);
            Assert.That(strikeCount, Is.EqualTo(1));
        }



        private Demo2SimulationController CreateSimulationController()
        {
            return CreateObject("Simulation").AddComponent<Demo2SimulationController>();
        }

        private PlayerMoveController CreatePlayer(Demo2SimulationController simulationController)
        {
            GameObject playerObject = CreateInactiveObject("Player");
            Rigidbody2D body = playerObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            playerObject.AddComponent<BoxCollider2D>();

            GameObject groundCheck = CreateObject("GroundCheck");
            groundCheck.transform.SetParent(playerObject.transform, false);
            groundCheck.transform.localPosition = Vector3.down;

            GameObject attackPoint = CreateObject("AttackPoint");
            attackPoint.transform.SetParent(playerObject.transform, false);
            attackPoint.transform.localPosition = new Vector3(0.9f, 0f, 0f);

            Demo2PlayerMeleeAttack meleeAttack = playerObject.AddComponent<Demo2PlayerMeleeAttack>();
            SetPrivateField(meleeAttack, "_attackPoint", attackPoint.transform);
            SetPrivateField(meleeAttack, "_monsterLayers", (LayerMask)(1 << LayerMask.NameToLayer("Monster")));
            SetPrivateField(meleeAttack, "_obstacleLayers", (LayerMask)(1 << LayerMask.NameToLayer("SideWall")));

            PlayerMoveController player = playerObject.AddComponent<PlayerMoveController>();
            Demo2PushController pushController = playerObject.AddComponent<Demo2PushController>();
            SetPrivateField(player, "_groundCheck", groundCheck.transform);
            SetPrivateField(player, "_simulationController", simulationController);
            SetPrivateField(player, "_meleeAttack", meleeAttack);
            SetPrivateField(pushController, "_simulationController", simulationController);
            SetPrivateField(pushController, "_castDistance", 0.1f);
            playerObject.SetActive(true);
            return player;
        }

        private Demo2PushableObstacle CreatePushableObstacle(Vector2 position)
        {
            GameObject obstacleObject = CreateInactiveObject("PushableObstacle");
            obstacleObject.transform.position = position;
            Rigidbody2D body = obstacleObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            obstacleObject.AddComponent<BoxCollider2D>();
            Demo2PushableObstacle obstacle = obstacleObject.AddComponent<Demo2PushableObstacle>();
            obstacleObject.SetActive(true);
            return obstacle;
        }

        private Rigidbody2D CreateBody(string name, Vector2 position)
        {
            GameObject bodyObject = CreateObject(name);
            bodyObject.transform.position = position;
            Rigidbody2D body = bodyObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            bodyObject.AddComponent<BoxCollider2D>();
            return body;
        }

        private BoxCollider2D CreateStaticBox(string name, Vector2 position)
        {
            GameObject boxObject = CreateObject(name);
            boxObject.transform.position = position;
            return boxObject.AddComponent<BoxCollider2D>();
        }
        private static IEnumerator MoveBodyInto(Rigidbody2D body, Vector2 velocity, int steps)
        {
            body.velocity = velocity;
            for (int step = 0; step < steps; step++)
            {
                yield return new WaitForFixedUpdate();
            }

            body.velocity = Vector2.zero;
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
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"未找到字段 {fieldName}");
            field.SetValue(target, value);
        }
    }
}
