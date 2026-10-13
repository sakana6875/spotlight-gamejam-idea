using System.Collections;
using NUnit.Framework;
using Spotlight.Demos.Demo2;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace Spotlight.Tests.PlayMode
{
    public sealed class TerrainWaterControllerTests
    {
        private GameObject _root;
        private Tile _tile;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_root != null)
            {
                Object.Destroy(_root);
            }

            if (_tile != null)
            {
                Object.Destroy(_tile);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator TerrainWaterController_OnlyDampsAuthoredWaterAndNeverFillsGap()
        {
            TerrainWaterController controller = CreateController(
                out Tilemap waterTilemap,
                out BoxCollider2D dampingTrigger,
                out Rigidbody2D waterBody,
                out Rigidbody2D dryBody);
            Vector3Int waterCell = new Vector3Int(20, 5, 0);
            Vector3Int gapCell = new Vector3Int(21, 5, 0);

            _root.SetActive(true);
            Vector3 waterWorld = waterTilemap.GetCellCenterWorld(waterCell);
            Vector3 gapWorld = waterTilemap.GetCellCenterWorld(gapCell);
            waterBody.position = waterWorld;
            dryBody.position = gapWorld;
            waterBody.velocity = new Vector2(10f, 0f);
            dryBody.velocity = new Vector2(10f, 0f);
            Physics2D.SyncTransforms();
            yield return null;

            Assert.That(controller.IsWaterAtWorldPosition(waterWorld), Is.True);
            Assert.That(controller.IsWaterAtWorldPosition(gapWorld), Is.False);
            Assert.That(dampingTrigger.bounds.Contains(waterWorld), Is.True);
            Assert.That(dampingTrigger.bounds.Contains(gapWorld), Is.True);

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(waterTilemap.HasTile(waterCell), Is.True);
            Assert.That(waterTilemap.HasTile(gapCell), Is.False);
            Assert.That(waterBody.velocity.x, Is.LessThan(10f));
            Assert.That(dryBody.velocity.x, Is.EqualTo(10f).Within(0.001f));
        }

        private TerrainWaterController CreateController(
            out Tilemap waterTilemap,
            out BoxCollider2D dampingTrigger,
            out Rigidbody2D waterBody,
            out Rigidbody2D dryBody)
        {
            _root = new GameObject("TerrainWaterTestRoot");
            _root.SetActive(false);
            _root.AddComponent<Grid>();
            _tile = ScriptableObject.CreateInstance<Tile>();

            GameObject waterObject = new GameObject("WaterInteriorTilemap", typeof(Tilemap));
            waterObject.transform.SetParent(_root.transform, false);
            waterTilemap = waterObject.GetComponent<Tilemap>();
            waterTilemap.SetTile(new Vector3Int(20, 5, 0), _tile);
            waterTilemap.SetTile(new Vector3Int(22, 5, 0), _tile);

            WaterTilemapRenderer renderer = waterObject.AddComponent<WaterTilemapRenderer>();
            TerrainWaterController controller = waterObject.AddComponent<TerrainWaterController>();
            dampingTrigger = waterObject.AddComponent<BoxCollider2D>();
            dampingTrigger.isTrigger = true;
            WaterDampingArea dampingArea = waterObject.AddComponent<WaterDampingArea>();

            waterBody = CreateBody("WaterBody");
            dryBody = CreateBody("DryBody");
            ConfigureRenderer(renderer, waterTilemap);
            ConfigureController(controller, renderer);
            ConfigureDampingArea(dampingArea, controller);
            return controller;
        }

        private Rigidbody2D CreateBody(string name)
        {
            GameObject bodyObject = new GameObject(name, typeof(Rigidbody2D), typeof(CircleCollider2D));
            bodyObject.transform.SetParent(_root.transform, false);
            Rigidbody2D body = bodyObject.GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            return body;
        }

        private static void ConfigureRenderer(WaterTilemapRenderer renderer, Tilemap waterTilemap)
        {
            SerializedObject serializedRenderer = new SerializedObject(renderer);
            serializedRenderer.FindProperty("_waterTilemap").objectReferenceValue = waterTilemap;
            serializedRenderer.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureController(
            TerrainWaterController controller,
            WaterTilemapRenderer renderer)
        {
            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("_waterRenderer").objectReferenceValue = renderer;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureDampingArea(WaterDampingArea dampingArea, TerrainWaterController controller)
        {
            SerializedObject serializedDampingArea = new SerializedObject(dampingArea);
            serializedDampingArea.FindProperty("_terrainWaterController").objectReferenceValue = controller;
            serializedDampingArea.FindProperty("_dampingCoefficient").floatValue = 20f;
            serializedDampingArea.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
