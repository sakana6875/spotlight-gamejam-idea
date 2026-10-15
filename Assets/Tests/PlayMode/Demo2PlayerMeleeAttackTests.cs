using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Spotlight.Demos.Demo2;
#if UNITY_EDITOR
using UnityEditor.Animations;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Spotlight.Tests.PlayMode
{
    public sealed class Demo2PlayerMeleeAttackTests
    {
        private readonly List<InputDevice> _createdDevices = new List<InputDevice>();
        private readonly List<Object> _createdObjects = new List<Object>();

        [UnityTest]
        public IEnumerator TopVerticalMovement_PreservesMirroredAttackPoint()
        {
            GameObject simulationObject = CreateObject("Simulation");
            Demo2SimulationController simulation = simulationObject.AddComponent<Demo2SimulationController>();
            GameObject playerObject = CreateInactiveObject("Player");
            Rigidbody2D body = playerObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;

            GameObject groundCheck = CreateObject("GroundCheck");
            groundCheck.transform.SetParent(playerObject.transform, false);

            GameObject attackPointObject = CreateObject("AttackPoint");
            attackPointObject.transform.SetParent(playerObject.transform, false);
            attackPointObject.transform.localPosition = new Vector3(0.9f, 0f, 0f);

            Demo2PlayerMeleeAttack melee = playerObject.AddComponent<Demo2PlayerMeleeAttack>();
            SetPrivateField(melee, "_attackPoint", attackPointObject.transform);
            SetPrivateField(
                melee,
                "_monsterLayers",
                (LayerMask)(1 << LayerMask.NameToLayer("Monster")));

            PlayerMoveController player = playerObject.AddComponent<PlayerMoveController>();
            SetPrivateField(player, "_groundCheck", groundCheck.transform);
            SetPrivateField(player, "_simulationController", simulation);
            SetPrivateField(player, "_meleeAttack", melee);
            playerObject.SetActive(true);

            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            _createdDevices.Add(keyboard);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
            InputSystem.Update();
            yield return null;
            Assert.That(player.IsFacingRight, Is.False);

            player.SetViewMode(PlayerMoveController.Demo2ViewMode.Top, Vector2.zero);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            InputSystem.Update();
            yield return null;

            Assert.That(player.IsFacingRight, Is.False);
            Assert.That(attackPointObject.transform.position.x, Is.LessThan(player.transform.position.x));
        }

        [UnityTest]
        public IEnumerator AttackPointAndConfiguredSize_DetermineMirroredAttackArea()
        {
            CreateSceneDependencies(out Demo2TreeController tree, out Demo2SimulationController simulation);
            Demo2PlayerMeleeAttack melee = CreateMelee(new Vector2(0.4f, 0.4f));
            Demo2MonsterController rightMonster = CreateMonster(tree, simulation, new Vector2(1.5f, 0f));
            Demo2MonsterController leftMonster = CreateMonster(tree, simulation, new Vector2(-1.5f, 0f));
            yield return null;

            Physics2D.SyncTransforms();
            melee.ResolveStrike();
            Assert.That(rightMonster.CurrentHealth, Is.EqualTo(30));
            Assert.That(leftMonster.CurrentHealth, Is.EqualTo(30));

            SetPrivateField(melee, "_attackSize", new Vector2(1.5f, 0.8f));
            Physics2D.SyncTransforms();
            melee.ResolveStrike();
            Assert.That(rightMonster.CurrentHealth, Is.EqualTo(20));
            Assert.That(leftMonster.CurrentHealth, Is.EqualTo(30));

            melee.transform.localScale = new Vector3(-1f, 1f, 1f);
            Physics2D.SyncTransforms();
            melee.ResolveStrike();
            Assert.That(rightMonster.CurrentHealth, Is.EqualTo(20));
            Assert.That(leftMonster.CurrentHealth, Is.EqualTo(20));
        }

        [UnityTest]
        public IEnumerator ResolveStrike_DamagesAllTargetsOnceAndDeduplicatesColliders()
        {
            CreateSceneDependencies(out Demo2TreeController tree, out Demo2SimulationController simulation);
            Demo2PlayerMeleeAttack melee = CreateMelee(new Vector2(1.5f, 0.8f));
            Demo2MonsterController firstMonster = CreateMonster(tree, simulation, new Vector2(1f, 0.15f));
            Demo2MonsterController secondMonster = CreateMonster(tree, simulation, new Vector2(1.3f, -0.15f));

            GameObject extraColliderObject = CreateObject("ExtraMonsterCollider");
            extraColliderObject.layer = LayerMask.NameToLayer("Monster");
            extraColliderObject.transform.SetParent(firstMonster.transform, false);
            extraColliderObject.transform.localPosition = new Vector3(0.1f, 0f, 0f);
            extraColliderObject.AddComponent<BoxCollider2D>().size = new Vector2(0.2f, 0.2f);
            yield return null;

            Physics2D.SyncTransforms();
            melee.ResolveStrike();

            Assert.That(firstMonster.CurrentHealth, Is.EqualTo(20));
            Assert.That(secondMonster.CurrentHealth, Is.EqualTo(20));
        }

        [UnityTest]
        public IEnumerator SideWall_BlocksOnlyMonsterBehindWall()
        {
            CreateSceneDependencies(out Demo2TreeController tree, out Demo2SimulationController simulation);
            Demo2PlayerMeleeAttack melee = CreateMelee(new Vector2(1.5f, 0.8f));
            Demo2MonsterController blockedMonster =
                CreateMonster(tree, simulation, new Vector2(1.2f, 0.25f));
            Demo2MonsterController openMonster =
                CreateMonster(tree, simulation, new Vector2(1.2f, -0.25f));

            GameObject wall = CreateObject("SideWall");
            wall.layer = LayerMask.NameToLayer("SideWall");
            wall.transform.position = new Vector2(0.65f, 0.14f);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(0.1f, 0.12f);
            yield return null;

            Physics2D.SyncTransforms();
            melee.ResolveStrike();

            Assert.That(blockedMonster.CurrentHealth, Is.EqualTo(30));
            Assert.That(openMonster.CurrentHealth, Is.EqualTo(20));
        }

        [UnityTest]
        public IEnumerator ThreeStrikes_KillThirtyHealthMonsterThroughExistingDamageFlow()
        {
            CreateSceneDependencies(out Demo2TreeController tree, out Demo2SimulationController simulation);
            Demo2PlayerMeleeAttack melee = CreateMelee(new Vector2(1.5f, 0.8f));
            Demo2MonsterController monster = CreateMonster(tree, simulation, new Vector2(1.2f, 0f));
            yield return null;

            Physics2D.SyncTransforms();
            melee.ResolveStrike();
            melee.ResolveStrike();
            melee.ResolveStrike();

            Assert.That(monster.CurrentHealth, Is.Zero);
            Assert.That(monster.State, Is.EqualTo(Demo2MonsterState.Dead));
            Assert.That(monster.IsAlive, Is.False);
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

            foreach (InputDevice device in _createdDevices)
            {
                if (device != null && device.added)
                {
                    InputSystem.RemoveDevice(device);
                }
            }

            _createdDevices.Clear();
            _createdObjects.Clear();
            yield return null;
        }

        private Demo2PlayerMeleeAttack CreateMelee(Vector2 attackSize)
        {
            int monsterLayer = LayerMask.NameToLayer("Monster");
            int sideWallLayer = LayerMask.NameToLayer("SideWall");
            Assert.That(monsterLayer, Is.GreaterThanOrEqualTo(0));
            Assert.That(sideWallLayer, Is.GreaterThanOrEqualTo(0));

            GameObject player = CreateInactiveObject("Player");
            GameObject attackPointObject = CreateObject("AttackPoint");
            attackPointObject.transform.SetParent(player.transform, false);
            attackPointObject.transform.localPosition = new Vector3(0.9f, 0f, 0f);

            Demo2PlayerMeleeAttack melee = player.AddComponent<Demo2PlayerMeleeAttack>();
            SetPrivateField(melee, "_attackPoint", attackPointObject.transform);
            SetPrivateField(melee, "_attackSize", attackSize);
            SetPrivateField(melee, "_damage", 10);
            SetPrivateField(melee, "_monsterLayers", (LayerMask)(1 << monsterLayer));
            SetPrivateField(melee, "_obstacleLayers", (LayerMask)(1 << sideWallLayer));
            player.SetActive(true);
            return melee;
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
            monsterObject.layer = LayerMask.NameToLayer("Monster");
            monsterObject.transform.position = position;
            Rigidbody2D rigidbody = monsterObject.AddComponent<Rigidbody2D>();
            rigidbody.gravityScale = 0f;
            rigidbody.constraints = RigidbodyConstraints2D.FreezeAll;
            BoxCollider2D bodyCollider = monsterObject.AddComponent<BoxCollider2D>();
            bodyCollider.size = new Vector2(0.2f, 0.2f);

            GameObject visualRoot = CreateObject("Visual");
            visualRoot.layer = monsterObject.layer;
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

        private void CreateSceneDependencies(
            out Demo2TreeController tree,
            out Demo2SimulationController simulation)
        {
            GameObject simulationObject = CreateObject("Simulation");
            simulation = simulationObject.AddComponent<Demo2SimulationController>();

            Sprite sprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f),
                1f);
            Track(sprite);

            GameObject treeObject = CreateInactiveObject("Tree");
            treeObject.transform.position = new Vector2(100f, 0f);
            SpriteRenderer spriteRenderer = treeObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            BoxCollider2D treeCollider = treeObject.AddComponent<BoxCollider2D>();
            treeCollider.enabled = false;

            GameObject visualRoot = CreateObject("TreeVisual");
            visualRoot.transform.SetParent(treeObject.transform, false);

            tree = treeObject.AddComponent<Demo2TreeController>();
            SetPrivateField(tree, "_spriteRenderer", spriteRenderer);
            SetPrivateField(tree, "_visualRoot", visualRoot.transform);
            SetPrivateField(tree, "_treeCollider", treeCollider);
            SetPrivateField(tree, "_saplingSprite", sprite);
            SetPrivateField(tree, "_growthFrames", new[] { sprite, sprite, sprite, sprite });
            SetPrivateField(tree, "_matureSprite", sprite);
            treeObject.SetActive(true);
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
