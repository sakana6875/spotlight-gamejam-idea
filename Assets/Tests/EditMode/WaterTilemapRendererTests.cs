using NUnit.Framework;
using Spotlight.Demos.Demo2;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace Spotlight.Tests.EditMode
{
    public sealed class WaterTilemapRendererTests
    {
        private GameObject _gridObject;
        private Tile _waterTile;

        [TearDown]
        public void TearDown()
        {
            if (_gridObject != null)
            {
                Object.DestroyImmediate(_gridObject);
            }

            if (_waterTile != null)
            {
                Object.DestroyImmediate(_waterTile);
            }
        }

        [Test]
        public void AuthoredSourceBounds_DiscoversNonContiguousTilesWithoutFillingGap()
        {
            WaterTilemapRenderer renderer = CreateRenderer(out Tilemap waterTilemap);
            Vector3Int lowerWater = new Vector3Int(-4, -8, 0);
            Vector3Int upperWater = new Vector3Int(17, 3, 0);
            Vector3Int gap = new Vector3Int(0, 0, 0);
            waterTilemap.SetTile(lowerWater, _waterTile);
            waterTilemap.SetTile(upperWater, _waterTile);

            bool wasFound = renderer.TryGetAuthoredSourceBounds(out BoundsInt waterBounds);
            TerrainWaterModel model = new TerrainWaterModel(waterBounds.size.x, waterBounds.size.y);
            renderer.CaptureAuthoredSources(model, waterBounds);
            model.ResetToSources();
            renderer.Render(model, waterBounds);
            renderer.UpdateFlowAnimation(model, waterBounds, 10f);

            Assert.That(wasFound, Is.True);
            Assert.That(waterBounds,
                Is.EqualTo(new BoundsInt(new Vector3Int(-4, -8, 0), new Vector3Int(22, 12, 1))));
            Assert.That(waterTilemap.GetTile(lowerWater), Is.SameAs(_waterTile));
            Assert.That(waterTilemap.GetTile(upperWater), Is.SameAs(_waterTile));
            Assert.That(waterTilemap.HasTile(gap), Is.False);
        }

        [Test]
        public void AuthoredSourceBounds_ReturnsFalseForEmptyTilemap()
        {
            WaterTilemapRenderer renderer = CreateRenderer(out Tilemap _);

            bool wasFound = renderer.TryGetAuthoredSourceBounds(out BoundsInt waterBounds);

            Assert.That(wasFound, Is.False);
            Assert.That(waterBounds.size, Is.EqualTo(Vector3Int.zero));
        }

        private WaterTilemapRenderer CreateRenderer(out Tilemap waterTilemap)
        {
            _gridObject = new GameObject("Grid", typeof(Grid));
            GameObject waterObject = new GameObject("WaterInteriorTilemap", typeof(Tilemap));
            waterObject.transform.SetParent(_gridObject.transform, false);
            waterTilemap = waterObject.GetComponent<Tilemap>();
            WaterTilemapRenderer renderer = waterObject.AddComponent<WaterTilemapRenderer>();
            _waterTile = ScriptableObject.CreateInstance<Tile>();

            SerializedObject serializedRenderer = new SerializedObject(renderer);
            serializedRenderer.FindProperty("_waterTilemap").objectReferenceValue = waterTilemap;
            serializedRenderer.FindProperty("_waterTile").objectReferenceValue = _waterTile;
            serializedRenderer.ApplyModifiedPropertiesWithoutUndo();
            return renderer;
        }
    }
}
