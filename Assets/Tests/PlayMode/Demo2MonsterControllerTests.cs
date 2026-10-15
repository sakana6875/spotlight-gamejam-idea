using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Spotlight.Demos.Demo2;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Spotlight.Tests.PlayMode
{
    public sealed class Demo2MonsterControllerTests
    {
        private readonly List<Object> _createdObjects = new List<Object>();

        [UnityTest]
        public IEnumerator MovementAndBiting_PreserveVerticalVelocityAndCleanTreeRegistration()
        {
            Demo2SimulationController simulation = CreateObject("Simulation")
                .AddComponent<Demo2SimulationController>();
            Demo2TreeController tree = CreateReadyTree(new Vector2(2f, 0f));
            yield return WaitForTreeReady(tree);

            Demo2MonsterController monster = CreateMonster(tree, simulation, Vector2.zero);
            Rigidbody2D monsterBody = monster.GetComponent<Rigidbody2D>();
            monsterBody.velocity = new Vector2(0f, -2f);

            yield return new WaitForFixedUpdate();

            Assert.That(monsterBody.velocity.x, Is.EqualTo(1f).Within(0.01f));
            Assert.That(monsterBody.velocity.y, Is.LessThan(-2f));

            monster.transform.position = new Vector2(1.6f, 0f);
            monsterBody.position = monster.transform.position;
            Physics2D.SyncTransforms();
            yield return null;
            yield return new WaitForSeconds(0.08f);

            Assert.That(monster.State, Is.EqualTo(Demo2MonsterState.Biting));
            Assert.That(tree.AccumulatedBiteSeconds, Is.GreaterThan(0f));

            Assert.That(monster.TryTakeDamage(10), Is.EqualTo(MonsterDamageResult.Applied));
            yield return null;
            SpriteRenderer monsterRenderer = monster.GetComponentInChildren<SpriteRenderer>();
            Assert.That(monsterRenderer.color, Is.EqualTo(new Color(1f, 0.55f, 0.55f, 1f)));
            float biteSecondsAfterHit = tree.AccumulatedBiteSeconds;
            yield return new WaitForSeconds(0.08f);

            Assert.That(monster.State, Is.EqualTo(Demo2MonsterState.HitStun));
            Assert.That(tree.AccumulatedBiteSeconds, Is.EqualTo(biteSecondsAfterHit).Within(0.025f));

            yield return new WaitForSeconds(0.43f);
            Assert.That(monster.State, Is.EqualTo(Demo2MonsterState.Biting));
            Assert.That(monsterRenderer.color, Is.EqualTo(Color.white));
            monster.gameObject.SetActive(false);
            yield return null;
            float biteSecondsAfterDisable = tree.AccumulatedBiteSeconds;
            yield return new WaitForSeconds(0.08f);

            Assert.That(tree.AccumulatedBiteSeconds, Is.EqualTo(biteSecondsAfterDisable).Within(0.025f));
        }

        [UnityTest]
        public IEnumerator SideRootDisable_PausesHitStunThenDeathPublishesOnceAndDestroysObject()
        {
            Demo2SimulationController simulation = CreateObject("Simulation")
                .AddComponent<Demo2SimulationController>();
            Demo2TreeController tree = CreateReadyTree(new Vector2(2f, 0f));
            yield return WaitForTreeReady(tree);

            Demo2MonsterController monster = CreateMonster(tree, simulation, Vector2.zero);
            Collider2D bodyCollider = monster.GetComponent<Collider2D>();
            Rigidbody2D monsterBody = monster.GetComponent<Rigidbody2D>();
            GameObject visualRoot = GetPrivateField<GameObject>(monster, "_visualRoot");

            Assert.That(monster.TryTakeDamage(10), Is.EqualTo(MonsterDamageResult.Applied));
            yield return new WaitForSeconds(0.2f);

            GameObject monsterObject = monster.gameObject;
            monsterObject.SetActive(false);
            Assert.That(monsterObject.activeInHierarchy, Is.False);
            Assert.That(visualRoot.activeInHierarchy, Is.False);

            yield return new WaitForSeconds(0.45f);
            Assert.That(monster.State, Is.EqualTo(Demo2MonsterState.HitStun));

            monsterObject.SetActive(true);
            Assert.That(visualRoot.activeInHierarchy, Is.True);
            Assert.That(bodyCollider.enabled, Is.True);
            Assert.That(monsterBody.simulated, Is.True);

            yield return new WaitForSeconds(0.31f);
            Assert.That(monster.State, Is.EqualTo(Demo2MonsterState.Moving));

            int deathEventCount = 0;
            monster.Died += _ => deathEventCount++;
            Assert.That(monster.TryTakeDamage(10), Is.EqualTo(MonsterDamageResult.Applied));
            Assert.That(monster.TryTakeDamage(10), Is.EqualTo(MonsterDamageResult.Killed));
            Assert.That(monster.TryTakeDamage(10), Is.EqualTo(MonsterDamageResult.AlreadyDead));
            Assert.That(deathEventCount, Is.EqualTo(1));
            Assert.That(bodyCollider.enabled, Is.False);
            Assert.That(monsterBody.simulated, Is.False);

            yield return new WaitForSeconds(0.35f);
            Assert.That(monsterObject, Is.Not.Null);
            yield return new WaitForSeconds(0.2f);
            Assert.That(monsterObject, Is.Null);
        }

        [UnityTest]
        public IEnumerator MissingConfiguration_ReportsReasonAndDisablesController()
        {
            GameObject monsterObject = CreateInactiveObject("InvalidMonster");
            Demo2MonsterController monster = monsterObject.AddComponent<Demo2MonsterController>();
            LogAssert.Expect(LogType.Error, "Demo2MonsterController 配置无效：必须配置 Demo2MonsterConfig。");

            monsterObject.SetActive(true);
            yield return null;

            Assert.That(monster.enabled, Is.False);
            Assert.That(monster.IsAlive, Is.False);
        }

        [UnityTest]
        public IEnumerator MovingMonster_JumpsOverForwardBoxAndKeepsHorizontalSpeed()
        {
            Demo2SimulationController simulation = CreateObject("Simulation")
                .AddComponent<Demo2SimulationController>();
            Demo2TreeController tree = CreateReadyTree(new Vector2(10f, 0f));
            yield return WaitForTreeReady(tree);
            CreateStaticBox("Ground", new Vector2(0f, -0.8f), new Vector2(20f, 0.1f), false);
            CreateStaticBox("Obstacle", new Vector2(0.95f, -0.25f), new Vector2(0.5f, 1f), false);
            Demo2MonsterController monster = CreateMonster(tree, simulation, Vector2.zero);
            Rigidbody2D body = monster.GetComponent<Rigidbody2D>();
            Physics2D.SyncTransforms();

            float timeout = 1f;
            while (body.velocity.y <= 0.5f && timeout > 0f)
            {
                timeout -= Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            Assert.That(body.velocity.y, Is.GreaterThan(0.5f));
            Assert.That(body.velocity.x, Is.EqualTo(1f).Within(0.05f));
            Assert.That(monster.State, Is.EqualTo(Demo2MonsterState.Moving));
        }

        [UnityTest]
        public IEnumerator TargetTreeAndTrigger_DoNotCauseJump()
        {
            Demo2SimulationController simulation = CreateObject("Simulation")
                .AddComponent<Demo2SimulationController>();
            Demo2TreeController tree = CreateReadyTree(new Vector2(1.2f, 0f));
            yield return WaitForTreeReady(tree);
            CreateStaticBox("Ground", new Vector2(0f, -0.8f), new Vector2(20f, 0.1f), false);
            CreateStaticBox("Trigger", new Vector2(0.65f, -0.25f), new Vector2(0.2f, 1f), true);
            Demo2MonsterController monster = CreateMonster(tree, simulation, Vector2.zero);
            Rigidbody2D body = monster.GetComponent<Rigidbody2D>();
            Physics2D.SyncTransforms();

            float maximumYVelocity = float.MinValue;
            for (int step = 0; step < 30; step++)
            {
                yield return new WaitForFixedUpdate();
                maximumYVelocity = Mathf.Max(maximumYVelocity, body.velocity.y);
            }

            Assert.That(maximumYVelocity, Is.LessThan(0.5f));
            Assert.That(monster.State, Is.EqualTo(Demo2MonsterState.Moving));
        }

        [UnityTest]
        public IEnumerator HitStun_BlocksJumpButPreservesGravity()
        {
            Demo2SimulationController simulation = CreateObject("Simulation")
                .AddComponent<Demo2SimulationController>();
            Demo2TreeController tree = CreateReadyTree(new Vector2(10f, 0f));
            yield return WaitForTreeReady(tree);
            CreateStaticBox("Ground", new Vector2(0f, -0.8f), new Vector2(20f, 0.1f), false);
            CreateStaticBox("Obstacle", new Vector2(0.65f, -0.25f), new Vector2(0.2f, 1f), false);
            Demo2MonsterController monster = CreateMonster(tree, simulation, Vector2.zero);
            Rigidbody2D body = monster.GetComponent<Rigidbody2D>();
            Assert.That(monster.TryTakeDamage(10), Is.EqualTo(MonsterDamageResult.Applied));
            Physics2D.SyncTransforms();

            for (int step = 0; step < 10; step++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(body.velocity.x, Is.EqualTo(0f).Within(0.01f));
            Assert.That(body.velocity.y, Is.LessThanOrEqualTo(0.1f));
            Assert.That(monster.State, Is.EqualTo(Demo2MonsterState.HitStun));
        }

        [UnityTest]
        public IEnumerator FailedWallJump_WaitsAfterLandingBeforeRetry()
        {
            Demo2SimulationController simulation = CreateObject("Simulation")
                .AddComponent<Demo2SimulationController>();
            Demo2TreeController tree = CreateReadyTree(new Vector2(10f, 0f));
            yield return WaitForTreeReady(tree);
            CreateStaticBox("Ground", new Vector2(0f, -0.8f), new Vector2(20f, 0.1f), false);
            CreateStaticBox("HighWall", new Vector2(0.65f, 1.25f), new Vector2(0.2f, 4f), false);
            Demo2MonsterController monster = CreateMonster(tree, simulation, Vector2.zero);
            Rigidbody2D body = monster.GetComponent<Rigidbody2D>();
            Physics2D.SyncTransforms();

            float timeout = 1f;
            while (body.velocity.y <= 0.5f && timeout > 0f)
            {
                timeout -= Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            Assert.That(body.velocity.y, Is.GreaterThan(0.5f));
            timeout = 2f;
            while ((body.position.y > 0.05f || Mathf.Abs(body.velocity.y) > 0.2f) && timeout > 0f)
            {
                timeout -= Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            Assert.That(timeout, Is.GreaterThan(0f));
            for (int step = 0; step < 10; step++)
            {
                yield return new WaitForFixedUpdate();
                Assert.That(body.velocity.y, Is.LessThan(0.5f));
            }

            bool retried = false;
            for (int step = 0; step < 10; step++)
            {
                yield return new WaitForFixedUpdate();
                retried |= body.velocity.y > 0.5f;
            }

            Assert.That(retried, Is.True);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            for (int index = _createdObjects.Count - 1; index >= 0; index--)
            {
                Object createdObject = _createdObjects[index];
                if (createdObject != null)
                {
                    Object.Destroy(createdObject);
                }
            }

            _createdObjects.Clear();
            yield return null;
        }

        private Demo2MonsterController CreateMonster(
            Demo2TreeController tree,
            Demo2SimulationController simulation,
            Vector2 position)
        {
            Demo2MonsterConfig config = ScriptableObject.CreateInstance<Demo2MonsterConfig>();
            Track(config);

            AnimatorController animatorController = new AnimatorController();
            animatorController.AddParameter("State", AnimatorControllerParameterType.Int);
            Track(animatorController);

            GameObject monsterObject = CreateInactiveObject("Monster");
            monsterObject.transform.position = position;
            Rigidbody2D rigidbody = monsterObject.AddComponent<Rigidbody2D>();
            rigidbody.gravityScale = 1f;
            BoxCollider2D bodyCollider = monsterObject.AddComponent<BoxCollider2D>();
            bodyCollider.size = new Vector2(0.9f, 1.5f);

            GameObject visualRoot = CreateObject("Visual");
            visualRoot.transform.SetParent(monsterObject.transform, false);
            SpriteRenderer spriteRenderer = visualRoot.AddComponent<SpriteRenderer>();
            Animator animator = visualRoot.AddComponent<Animator>();
            animator.runtimeAnimatorController = animatorController;

            Demo2MonsterController monster = monsterObject.AddComponent<Demo2MonsterController>();
            SetPrivateField(monster, "_config", config);
            SetPrivateField(monster, "_rigidbody", rigidbody);
            SetPrivateField(monster, "_bodyCollider", bodyCollider);
            SetPrivateField(monster, "_spriteRenderer", spriteRenderer);
            SetPrivateField(monster, "_animator", animator);
            SetPrivateField(monster, "_visualRoot", visualRoot);
            monsterObject.SetActive(true);
            Assert.That(monster.Initialize(tree, simulation), Is.True);
            return monster;
        }

        private Demo2TreeController CreateReadyTree(Vector2 position)
        {
            Sprite sprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f),
                1f);
            Track(sprite);

            GameObject treeObject = CreateInactiveObject("Tree");
            treeObject.transform.position = position;
            SpriteRenderer spriteRenderer = treeObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            BoxCollider2D treeCollider = treeObject.AddComponent<BoxCollider2D>();
            treeCollider.enabled = false;

            GameObject visualRoot = CreateObject("TreeVisual");
            visualRoot.transform.SetParent(treeObject.transform, false);

            Demo2TreeController tree = treeObject.AddComponent<Demo2TreeController>();
            SetPrivateField(tree, "_spriteRenderer", spriteRenderer);
            SetPrivateField(tree, "_visualRoot", visualRoot.transform);
            SetPrivateField(tree, "_treeCollider", treeCollider);
            SetPrivateField(tree, "_saplingSprite", sprite);
            SetPrivateField(tree, "_growthFrames", new[] { sprite, sprite, sprite, sprite });
            SetPrivateField(tree, "_matureSprite", sprite);
            SetPrivateField(tree, "_sproutingDurationSeconds", 0.01f);
            treeObject.SetActive(true);
            Assert.That(tree.BeginSprouting(), Is.EqualTo(Demo2TreeTransitionResult.Succeeded));
            return tree;
        }


        private static IEnumerator WaitForTreeReady(Demo2TreeController tree)
        {
            float timeout = 1f;
            while (tree.Phase != Demo2TreePhase.AwaitingGrowth && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }


            Assert.That(tree.Phase, Is.EqualTo(Demo2TreePhase.AwaitingGrowth));
        }
        private BoxCollider2D CreateStaticBox(
            string name,
            Vector2 position,
            Vector2 size,
            bool isTrigger)
        {
            GameObject box = CreateObject(name);
            box.transform.position = position;
            BoxCollider2D collider = box.AddComponent<BoxCollider2D>();
            collider.size = size;
            collider.isTrigger = isTrigger;
            return collider;
        }

        private GameObject CreateObject(string name)
        {
            GameObject gameObject = new GameObject(name);
            Track(gameObject);
            return gameObject;
        }

        private GameObject CreateInactiveObject(string name)
        {
            GameObject gameObject = CreateObject(name);
            gameObject.SetActive(false);
            return gameObject;
        }

        private void Track(Object createdObject)
        {
            _createdObjects.Add(createdObject);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new MissingFieldException(target.GetType().FullName, fieldName);
            }

            return (T)field.GetValue(target);
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
#endif
