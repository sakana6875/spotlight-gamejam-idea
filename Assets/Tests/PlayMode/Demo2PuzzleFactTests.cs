using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Spotlight.Demos.Demo2;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace Spotlight.Tests.PlayMode
{
    public sealed class Demo2PuzzleFactTests
    {
        private readonly List<Object> _createdObjects = new List<Object>();

        [UnityTest]
        public IEnumerator PartialSlots_DoNotPublishUntilEverySlotIsOccupiedAndWet()
        {
            Demo2PipeWaterController water = CreateWater();
            Demo2PipeShape shape = CreateShape();
            Demo2PipeSegment sourcePipe = CreatePipe(water, shape, Vector3Int.zero);
            Demo2Act1PipePuzzleController partial = CreatePuzzle(
                water,
                new[] { sourcePipe },
                new[]
                {
                    new Demo2Act1PipeSlotDefinition("source", Vector3Int.zero, shape, 0),
                    new Demo2Act1PipeSlotDefinition("dry", new Vector3Int(1, 0, 0), shape, 0)
                });
            Demo2Act1PipePuzzleController complete = CreatePuzzle(
                water,
                new[] { sourcePipe },
                new[]
                {
                    new Demo2Act1PipeSlotDefinition("filled", Vector3Int.zero, shape, 0)
                });
            int partialCount = 0;
            int completeCount = 0;
            partial.PuzzleCompleted += () => partialCount++;
            complete.PuzzleCompleted += () => completeCount++;

            partial.gameObject.SetActive(true);
            complete.gameObject.SetActive(true);
            yield return null;
            yield return new WaitForFixedUpdate();

            Assert.That(partial.IsConfigured, Is.True);
            Assert.That(complete.IsConfigured, Is.True);
            Assert.That(partial.TryGetSlotState("source", out bool sourceOccupied, out bool sourceWet), Is.True);
            Assert.That(sourceOccupied, Is.True);
            Assert.That(sourceWet, Is.True);
            Assert.That(partial.TryGetSlotState("dry", out bool dryOccupied, out bool dryWet), Is.True);
            Assert.That(dryOccupied, Is.False);
            Assert.That(dryWet, Is.False);
            Assert.That(partialCount, Is.Zero);
            Assert.That(completeCount, Is.EqualTo(1));

            yield return new WaitForFixedUpdate();
            Assert.That(completeCount, Is.EqualTo(1));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
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

        private Demo2PipeWaterController CreateWater()
        {
            Tile marker = ScriptableObject.CreateInstance<Tile>();
            Tile waterTile = ScriptableObject.CreateInstance<Tile>();
            Track(marker);
            Track(waterTile);

            GameObject gridObject = CreateObject("Grid");
            gridObject.AddComponent<Grid>();
            GameObject routeObject = CreateObject("Route");
            routeObject.transform.SetParent(gridObject.transform, false);
            Tilemap route = routeObject.AddComponent<Tilemap>();
            route.SetTile(Vector3Int.zero, marker);
            route.SetTile(Vector3Int.right, marker);

            GameObject waterMapObject = CreateObject("WaterMap");
            waterMapObject.transform.SetParent(gridObject.transform, false);
            Tilemap waterMap = waterMapObject.AddComponent<Tilemap>();

            GameObject waterObject = CreateInactiveObject("Water");
            Demo2PipeWaterController water = waterObject.AddComponent<Demo2PipeWaterController>();
            SetPrivateField(water, "_routeTilemap", route);
            SetPrivateField(water, "_routeMarkerTile", marker);
            SetPrivateField(water, "_sourceCell", Vector3Int.zero);
            SetPrivateField(water, "_waterTilemap", waterMap);
            SetPrivateField(water, "_waterTile", waterTile);
            SetPrivateField(water, "_flowStepSeconds", 1f);
            waterObject.SetActive(true);
            Assert.That(water.IsConfigured, Is.True);
            return water;
        }

        private Demo2PipeShape CreateShape()
        {
            Demo2PipeShape shape = ScriptableObject.CreateInstance<Demo2PipeShape>();
            Track(shape);
            SetPrivateField(
                shape,
                "_cells",
                new[] { new Demo2PipeCellDefinition(Vector2Int.zero, Demo2WaterPort.Right) });
            return shape;
        }

        private Demo2PipeSegment CreatePipe(
            Demo2PipeWaterController water,
            Demo2PipeShape shape,
            Vector3Int cell)
        {
            GameObject pipeObject = CreateInactiveObject("Pipe");
            pipeObject.transform.position = water.GetCellCenterWorld(cell);
            BoxCollider2D collider = pipeObject.AddComponent<BoxCollider2D>();
            Demo2PipeSegment pipe = pipeObject.AddComponent<Demo2PipeSegment>();
            SetPrivateField(pipe, "_shape", shape);
            SetPrivateField(pipe, "_waterController", water);
            SetPrivateField(pipe, "_solidColliders", new[] { collider });
            return pipe;
        }

        private Demo2Act1PipePuzzleController CreatePuzzle(
            Demo2PipeWaterController water,
            Demo2PipeSegment[] pipes,
            Demo2Act1PipeSlotDefinition[] slots)
        {
            GameObject puzzleObject = CreateInactiveObject("Puzzle");
            Demo2Act1PipePuzzleController puzzle = puzzleObject.AddComponent<Demo2Act1PipePuzzleController>();
            SetPrivateField(puzzle, "_waterController", water);
            SetPrivateField(puzzle, "_topViewPipeSegments", pipes);
            SetPrivateField(puzzle, "_slots", slots);
            return puzzle;
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
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "未找到字段 " + fieldName);
            field.SetValue(target, value);
        }
    }
}
