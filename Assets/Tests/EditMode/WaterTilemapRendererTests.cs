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
        private Tile _sourceTile;

        [TearDown]
        public void TearDown()
        {
            if (_gridObject != null)
            {
                Object.DestroyImmediate(_gridObject);
            }

            if (_sourceTile != null)
            {
                Object.DestroyImmediate(_sourceTile);
            }
        }

        [Test]
        public void TryGetAuthoredSourceBounds_DiscoversAllNonContiguousTilesOutsideLegacyRange()
        {
            WaterTilemapRenderer renderer = CreateRenderer(out Tilemap waterTilemap);
            Vector3Int lowerSource = new Vector3Int(-4, -8, 0);
            Vector3Int upperSource = new Vector3Int(17, 3, 0);
            waterTilemap.SetTile(lowerSource, _sourceTile);
            waterTilemap.SetTile(upperSource, _sourceTile);

            bool wasFound = renderer.TryGetAuthoredSourceBounds(out BoundsInt sourceBounds);
            TerrainWaterModel model = new TerrainWaterModel(sourceBounds.size.x, sourceBounds.size.y);
            renderer.CaptureAuthoredSources(model, sourceBounds);
            model.ResetToSources();
            waterTilemap.SetTile(lowerSource, null);
            waterTilemap.SetTile(upperSource, null);
            renderer.Render(model, sourceBounds);

            Assert.That(wasFound, Is.True);
            Assert.That(sourceBounds, Is.EqualTo(new BoundsInt(new Vector3Int(-4, -8, 0), new Vector3Int(22, 12, 1))));
            Assert.That(model.SourceCount, Is.EqualTo(2));
            Assert.That(model.IsSource(0, 0), Is.True);
            Assert.That(model.IsSource(21, 11), Is.True);
            Assert.That(waterTilemap.GetTile(lowerSource), Is.SameAs(_sourceTile));
            Assert.That(waterTilemap.GetTile(upperSource), Is.SameAs(_sourceTile));
        }

        [Test]
        public void TryGetAuthoredSourceBounds_ReturnsFalseForEmptyTilemap()
        {
            WaterTilemapRenderer renderer = CreateRenderer(out Tilemap _);

            bool wasFound = renderer.TryGetAuthoredSourceBounds(out BoundsInt sourceBounds);

            Assert.That(wasFound, Is.False);
            Assert.That(sourceBounds.size, Is.EqualTo(Vector3Int.zero));
        }

        private WaterTilemapRenderer CreateRenderer(out Tilemap waterTilemap)
        {
            _gridObject = new GameObject("Grid", typeof(Grid));
            GameObject waterObject = new GameObject("WaterInteriorTilemap", typeof(Tilemap));
            waterObject.transform.SetParent(_gridObject.transform, false);
            waterTilemap = waterObject.GetComponent<Tilemap>();
            WaterTilemapRenderer renderer = waterObject.AddComponent<WaterTilemapRenderer>();
            _sourceTile = ScriptableObject.CreateInstance<Tile>();

            SerializedObject serializedRenderer = new SerializedObject(renderer);
            serializedRenderer.FindProperty("_waterTilemap").objectReferenceValue = waterTilemap;
            serializedRenderer.FindProperty("_waterTile").objectReferenceValue = _sourceTile;
            serializedRenderer.ApplyModifiedPropertiesWithoutUndo();
            return renderer;
        }
    }
}
