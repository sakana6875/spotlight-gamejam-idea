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
        public IEnumerator TerrainWaterController_UsesGroundCoverageAndOnlyDampsOccupiedCells()
        {
            TerrainWaterController controller = CreateController(
                out Tilemap waterTilemap,
                out BoxCollider2D dampingTrigger,
                out Rigidbody2D waterBody,
                out Rigidbody2D dryBody);
            Vector3 sourceWorld = waterTilemap.GetCellCenterWorld(new Vector3Int(20, 5, 0));
            Vector3 supportedRightWorld = waterTilemap.GetCellCenterWorld(new Vector3Int(21, 5, 0));
            Vector3 unsupportedRightWorld = waterTilemap.GetCellCenterWorld(new Vector3Int(23, 5, 0));
            Vector3 dryCandidateWorld = waterTilemap.GetCellCenterWorld(new Vector3Int(20, 6, 0));

            _root.SetActive(true);
            Physics2D.SyncTransforms();
            yield return null;

            Assert.That(controller.IsWaterAtWorldPosition(sourceWorld), Is.True);
            Assert.That(dampingTrigger.bounds.Contains(supportedRightWorld), Is.True);
            Assert.That(dampingTrigger.bounds.Contains(dryCandidateWorld), Is.True);

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(controller.IsWaterAtWorldPosition(supportedRightWorld), Is.True);
            Assert.That(controller.IsWaterAtWorldPosition(unsupportedRightWorld), Is.False);
            Assert.That(controller.IsWaterAtWorldPosition(dryCandidateWorld), Is.False);
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

            GameObject waterObject = CreateTilemapObject("WaterInteriorTilemap");
            waterTilemap = waterObject.GetComponent<Tilemap>();
            waterTilemap.SetTile(new Vector3Int(20, 5, 0), _tile);
            WaterTilemapRenderer renderer = waterObject.AddComponent<WaterTilemapRenderer>();
            TerrainWaterController controller = waterObject.AddComponent<TerrainWaterController>();
            dampingTrigger = waterObject.AddComponent<BoxCollider2D>();
            dampingTrigger.isTrigger = true;
            WaterDampingArea dampingArea = waterObject.AddComponent<WaterDampingArea>();

            Tilemap groundTilemap = CreateTilemapObject("GroundTilemap").GetComponent<Tilemap>();
            groundTilemap.SetTile(new Vector3Int(20, 2, 0), _tile);
            groundTilemap.SetTile(new Vector3Int(21, 2, 0), _tile);
            groundTilemap.SetTile(new Vector3Int(22, 2, 0), _tile);
            groundTilemap.SetTile(new Vector3Int(20, 6, 0), _tile);
            Tilemap sideWallTilemap = CreateTilemapObject("SideWallTilemap").GetComponent<Tilemap>();

            waterBody = CreateBody("WaterBody", waterTilemap.GetCellCenterWorld(new Vector3Int(20, 5, 0)));
            dryBody = CreateBody("DryBody", waterTilemap.GetCellCenterWorld(new Vector3Int(20, 6, 0)));
            ConfigureRenderer(renderer, waterTilemap);
            ConfigureController(controller, sideWallTilemap, groundTilemap, renderer);
            ConfigureDampingArea(dampingArea, controller);
            return controller;
        }

        private GameObject CreateTilemapObject(string name)
        {
            GameObject tilemapObject = new GameObject(name, typeof(Tilemap));
            tilemapObject.transform.SetParent(_root.transform, false);
            return tilemapObject;
        }

        private Rigidbody2D CreateBody(string name, Vector3 worldPosition)
        {
            GameObject bodyObject = new GameObject(name, typeof(Rigidbody2D), typeof(CircleCollider2D));
            bodyObject.transform.SetParent(_root.transform, false);
            bodyObject.transform.position = worldPosition;
            Rigidbody2D body = bodyObject.GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.velocity = new Vector2(10f, 0f);
            return body;
        }

        private static void ConfigureRenderer(WaterTilemapRenderer renderer, Tilemap waterTilemap)
        {
            SerializedObject serializedRenderer = new SerializedObject(renderer);
            serializedRenderer.FindProperty("_waterTilemap").objectReferenceValue = waterTilemap;
            serializedRenderer.FindProperty("_waterTile").objectReferenceValue = waterTilemap.GetTile(new Vector3Int(20, 5, 0));
            serializedRenderer.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureController(
            TerrainWaterController controller,
            Tilemap sideWallTilemap,
            Tilemap groundTilemap,
            WaterTilemapRenderer renderer)
        {
            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("_sideWallTilemap").objectReferenceValue = sideWallTilemap;
            serializedController.FindProperty("_groundTilemap").objectReferenceValue = groundTilemap;
            serializedController.FindProperty("_flowStepSeconds").floatValue = 0.05f;
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
