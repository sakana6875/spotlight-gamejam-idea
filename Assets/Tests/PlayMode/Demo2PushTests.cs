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
    public sealed class Demo2PushTests
    {
        private readonly List<Object> _createdObjects = new List<Object>();

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
        public IEnumerator SideView_HeldPushMovesObstacleHorizontallyAndReleaseStopsApplyingForce()
        {
            Demo2SimulationController simulationController = CreateSimulationController();
            PlayerMoveController player = CreatePlayer(simulationController);
            Demo2PushController pushController = player.GetComponent<Demo2PushController>();
            Demo2PushableObstacle obstacle = CreatePushableObstacle(new Vector2(1.05f, 0f));
            Physics2D.SyncTransforms();

            Assert.That(pushController.TryPush(), Is.True);
            yield return new WaitForFixedUpdate();
            Assert.That(pushController.TryPush(), Is.True);
            yield return new WaitForFixedUpdate();

            Assert.That(obstacle.Rigidbody.velocity.x, Is.GreaterThan(0f));
            Assert.That(obstacle.Rigidbody.velocity.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(obstacle.Rigidbody.velocity.x, Is.LessThanOrEqualTo(2.501f));

            obstacle.Rigidbody.velocity = Vector2.zero;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(obstacle.Rigidbody.velocity.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(obstacle.Rigidbody.velocity.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(player.ViewMode, Is.EqualTo(PlayerMoveController.Demo2ViewMode.Side));
        }

        [UnityTest]
        public IEnumerator TopView_BackFacingAndUnmarkedBody_DoNotReceivePush()
        {
            Demo2SimulationController simulationController = CreateSimulationController();
            PlayerMoveController player = CreatePlayer(simulationController);
            Demo2PushController pushController = player.GetComponent<Demo2PushController>();
            Demo2PushableObstacle obstacle = CreatePushableObstacle(new Vector2(1.05f, 0f));
            Physics2D.SyncTransforms();

            player.SetViewMode(PlayerMoveController.Demo2ViewMode.Top, Vector2.zero);
            Assert.That(pushController.TryPush(), Is.False);
            yield return new WaitForFixedUpdate();
            Assert.That(obstacle.Rigidbody.velocity.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(obstacle.Rigidbody.velocity.y, Is.EqualTo(0f).Within(0.001f));

            player.SetViewMode(PlayerMoveController.Demo2ViewMode.Side, Vector2.zero);
            obstacle.Rigidbody.position = new Vector2(-1.05f, 0f);
            obstacle.Rigidbody.velocity = Vector2.zero;
            Physics2D.SyncTransforms();
            Assert.That(player.IsFacingRight, Is.True);
            Assert.That(pushController.TryPush(), Is.False);
            yield return new WaitForFixedUpdate();
            Assert.That(obstacle.Rigidbody.velocity.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(obstacle.Rigidbody.velocity.y, Is.EqualTo(0f).Within(0.001f));

            Object.Destroy(obstacle.gameObject);
            yield return null;

            Rigidbody2D ordinaryBody = CreateBody("OrdinaryBody", new Vector2(1.05f, 0f));
            Assert.That(pushController.TryPush(), Is.False);
            yield return new WaitForFixedUpdate();
            Assert.That(ordinaryBody.velocity.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(ordinaryBody.velocity.y, Is.EqualTo(0f).Within(0.001f));
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

            Assert.That(obstacle.Rigidbody.velocity, Is.EqualTo(Vector2.zero));
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

            PlayerMoveController player = playerObject.AddComponent<PlayerMoveController>();
            Demo2PushController pushController = playerObject.AddComponent<Demo2PushController>();
            SetPrivateField(player, "_groundCheck", groundCheck.transform);
            SetPrivateField(player, "_simulationController", simulationController);
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
