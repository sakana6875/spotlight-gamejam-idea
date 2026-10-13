using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Spotlight.Demos.Demo2;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Spotlight.Tests.PlayMode
{
    public sealed class Demo2MonsterSpawnDeviceTests
    {
        private const string MonsterPrefabPath = "Assets/Prefabs/Demo2/SlimeMonster.prefab";

        private readonly List<Object> _createdObjects = new List<Object>();

        [UnityTest]
        public IEnumerator SproutingStarted_PublishesOnceBeforeAnimationCompletes()
        {
            Demo2TreeController tree = CreateTree(new Vector2(5f, 0f));
            int startedCount = 0;
            int completedCount = 0;
            tree.SproutingStarted += () => startedCount++;
            tree.SproutingCompleted += () => completedCount++;

            Assert.That(tree.BeginSprouting(), Is.EqualTo(Demo2TreeTransitionResult.Succeeded));
            Assert.That(startedCount, Is.EqualTo(1));
            Assert.That(completedCount, Is.Zero);
            Assert.That(tree.BeginSprouting(), Is.EqualTo(Demo2TreeTransitionResult.InvalidState));
            Assert.That(startedCount, Is.EqualTo(1));

            yield return new WaitForSecondsRealtime(0.03f);
            Assert.That(completedCount, Is.EqualTo(1));
            Assert.That(startedCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator BlockedSpawn_WaitsWithoutConsumingCountThenCompletesOnce()
        {
            Demo2SimulationController simulation = CreateObject("Simulation")
                .AddComponent<Demo2SimulationController>();
            Demo2TreeController tree = CreateTree(new Vector2(5f, 0f));
            Transform spawnedRoot;
            Demo2MonsterSpawnDevice device = CreateDevice(
                "Device",
                Vector2.zero,
                1,
                2f,
                tree,
                simulation,
                out spawnedRoot);
            BoxCollider2D blocker = CreateObject("SpawnBlocker").AddComponent<BoxCollider2D>();
            blocker.transform.position = Vector2.zero;
            Physics2D.SyncTransforms();

            Assert.That(tree.BeginSprouting(), Is.EqualTo(Demo2TreeTransitionResult.Succeeded));
            yield return null;
            Assert.That(device.SpawnedCount, Is.Zero);

            Object.Destroy(blocker.gameObject);
            yield return null;
            yield return null;
            Assert.That(device.SpawnedCount, Is.EqualTo(1));
            Assert.That(device.ActiveMonsterCount, Is.EqualTo(1));

            int completedCount = 0;
            device.WaveCompleted += () => completedCount++;
            Demo2MonsterController monster = spawnedRoot.GetComponentInChildren<Demo2MonsterController>();
            Assert.That(monster, Is.Not.Null);
            Assert.That(monster.TryTakeDamage(30), Is.EqualTo(MonsterDamageResult.Killed));
            Assert.That(completedCount, Is.EqualTo(1));
            Assert.That(device.ActiveMonsterCount, Is.Zero);
            Assert.That(monster.TryTakeDamage(10), Is.EqualTo(MonsterDamageResult.AlreadyDead));
            Assert.That(completedCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator MultipleDevices_CountIndependentlyAndPauseTheirIntervals()
        {
            Demo2SimulationController simulation = CreateObject("Simulation")
                .AddComponent<Demo2SimulationController>();
            Demo2TreeController tree = CreateTree(new Vector2(5f, 0f));
            Transform firstRoot;
            Transform secondRoot;
            Demo2MonsterSpawnDevice firstDevice = CreateDevice(
                "FirstDevice",
                new Vector2(-2f, 0f),
                1,
                0.05f,
                tree,
                simulation,
                out firstRoot);
            Demo2MonsterSpawnDevice secondDevice = CreateDevice(
                "SecondDevice",
                new Vector2(2f, 0f),
                2,
                0.05f,
                tree,
                simulation,
                out secondRoot);

            Assert.That(tree.BeginSprouting(), Is.EqualTo(Demo2TreeTransitionResult.Succeeded));
            yield return null;
            Assert.That(firstDevice.SpawnedCount, Is.EqualTo(1));
            Assert.That(secondDevice.SpawnedCount, Is.EqualTo(1));

            Demo2MonsterController firstSpawn = secondRoot.GetComponentInChildren<Demo2MonsterController>();
            firstSpawn.transform.position = Vector2.zero;
            firstSpawn.GetComponent<Rigidbody2D>().position = Vector2.zero;
            Physics2D.SyncTransforms();

            simulation.AcquirePause(Demo2PauseReason.Dialogue);
            yield return new WaitForSecondsRealtime(0.08f);
            Assert.That(secondDevice.SpawnedCount, Is.EqualTo(1));

            simulation.ReleasePause(Demo2PauseReason.Dialogue);
            yield return new WaitForSeconds(0.07f);
            Assert.That(firstDevice.SpawnedCount, Is.EqualTo(1));
            Assert.That(secondDevice.SpawnedCount, Is.EqualTo(2));
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

        private Demo2MonsterSpawnDevice CreateDevice(
            string name,
            Vector2 position,
            int spawnCount,
            float intervalSeconds,
            Demo2TreeController tree,
            Demo2SimulationController simulation,
            out Transform spawnedRoot)
        {
            GameObject monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath);
            Assert.That(monsterPrefab, Is.Not.Null);

            GameObject deviceObject = CreateInactiveObject(name);
            deviceObject.transform.position = position;
            Transform spawnPoint = CreateObject(name + "SpawnPoint").transform;
            spawnPoint.SetParent(deviceObject.transform, false);
            spawnedRoot = CreateObject(name + "SpawnedMonsters").transform;
            spawnedRoot.SetParent(deviceObject.transform, false);

            Demo2MonsterSpawnDevice device = deviceObject.AddComponent<Demo2MonsterSpawnDevice>();
            SetPrivateField(device, "_treeController", tree);
            SetPrivateField(device, "_simulationController", simulation);
            SetPrivateField(device, "_monsterPrefab", monsterPrefab.GetComponent<Demo2MonsterController>());
            SetPrivateField(device, "_spawnPoint", spawnPoint);
            SetPrivateField(device, "_spawnedMonstersRoot", spawnedRoot);
            SetPrivateField(device, "_spawnCount", spawnCount);
            SetPrivateField(device, "_spawnIntervalSeconds", intervalSeconds);
            SetPrivateField(device, "_spawnBlockingLayers", (LayerMask)1);
            SetPrivateField(device, "_spawnClearanceSize", new Vector2(0.9f, 1.5f));
            deviceObject.SetActive(true);
            return device;
        }

        private Demo2TreeController CreateTree(Vector2 position)
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
            Transform visualRoot = CreateObject("TreeVisual").transform;
            visualRoot.SetParent(treeObject.transform, false);

            Demo2TreeController tree = treeObject.AddComponent<Demo2TreeController>();
            SetPrivateField(tree, "_spriteRenderer", spriteRenderer);
            SetPrivateField(tree, "_visualRoot", visualRoot);
            SetPrivateField(tree, "_treeCollider", treeCollider);
            SetPrivateField(tree, "_saplingSprite", sprite);
            SetPrivateField(tree, "_growthFrames", new[] { sprite, sprite, sprite, sprite });
            SetPrivateField(tree, "_matureSprite", sprite);
            SetPrivateField(tree, "_sproutingDurationSeconds", 0.01f);
            treeObject.SetActive(true);
            return tree;
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
