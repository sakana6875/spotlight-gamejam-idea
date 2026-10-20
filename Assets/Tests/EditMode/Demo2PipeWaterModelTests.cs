using System.Reflection;
using NUnit.Framework;
using Spotlight.Demos.Demo2;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Spotlight.Tests.EditMode
{
    public sealed class Demo2PipeWaterModelTests
    {
        [Test]
        public void PortRotation_RotatesFlagsAndOffsetsClockwise()
        {
            Assert.That(
                Demo2WaterPortUtility.RotateClockwise(Demo2WaterPort.Up | Demo2WaterPort.Left, 1),
                Is.EqualTo(Demo2WaterPort.Right | Demo2WaterPort.Up));
            Assert.That(
                Demo2WaterPortUtility.RotateClockwise(new Vector2Int(2, 1), 1),
                Is.EqualTo(new Vector2Int(1, -2)));
            Assert.That(
                Demo2WaterPortUtility.Opposite(Demo2WaterPort.Left),
                Is.EqualTo(Demo2WaterPort.Right));
        }

        [Test]
        public void Act1Slot_RequiresExactShapeOriginAndQuarterTurn()
        {
            Demo2PipeShape shape = ScriptableObject.CreateInstance<Demo2PipeShape>();
            Demo2PipeShape otherShape = ScriptableObject.CreateInstance<Demo2PipeShape>();
            try
            {
                Demo2PipeCellDefinition[] cells =
                {
                    new Demo2PipeCellDefinition(Vector2Int.zero, Demo2WaterPort.Left | Demo2WaterPort.Right),
                    new Demo2PipeCellDefinition(Vector2Int.right, Demo2WaterPort.Left | Demo2WaterPort.Right),
                    new Demo2PipeCellDefinition(Vector2Int.right * 2, Demo2WaterPort.Left | Demo2WaterPort.Right)
                };
                SetPrivateField(shape, "_cells", cells);
                SetPrivateField(otherShape, "_cells", cells);
                Vector3Int origin = new Vector3Int(4, -2, 0);
                Demo2Act1PipeSlotDefinition slot = new Demo2Act1PipeSlotDefinition(
                    "act1_gap_near_tree",
                    origin,
                    shape,
                    0);

                Assert.That(slot.TryValidate(out string error), Is.True, error);
                Assert.That(slot.MatchesPlacement(shape, origin, 0), Is.True);
                Assert.That(slot.MatchesPlacement(otherShape, origin, 0), Is.False);
                Assert.That(slot.MatchesPlacement(shape, origin + Vector3Int.right, 0), Is.False);
                Assert.That(slot.MatchesPlacement(shape, origin, 1), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(shape);
                Object.DestroyImmediate(otherShape);
            }
        }

        [Test]
        public void PlacementPreview_UsesExactCellsAndValidityColor()
        {
            GameObject gridObject = new GameObject("Grid");
            GameObject routeObject = new GameObject("Route");
            GameObject controllerObject = new GameObject("Controller");
            GameObject previewObject = new GameObject("Preview");
            Demo2PipeShape shape = ScriptableObject.CreateInstance<Demo2PipeShape>();

            try
            {
                Grid grid = gridObject.AddComponent<Grid>();
                routeObject.transform.SetParent(grid.transform, false);
                Tilemap routeTilemap = routeObject.AddComponent<Tilemap>();
                routeObject.AddComponent<TilemapRenderer>();

                controllerObject.SetActive(false);
                Demo2PipeWaterController controller = controllerObject.AddComponent<Demo2PipeWaterController>();
                SetPrivateField(controller, "_routeTilemap", routeTilemap);

                SetPrivateField(
                    shape,
                    "_cells",
                    new[]
                    {
                        new Demo2PipeCellDefinition(Vector2Int.zero, Demo2WaterPort.Left | Demo2WaterPort.Right),
                        new Demo2PipeCellDefinition(Vector2Int.right, Demo2WaterPort.Left | Demo2WaterPort.Right)
                    });

                previewObject.SetActive(false);
                SpriteRenderer first = new GameObject("First").AddComponent<SpriteRenderer>();
                SpriteRenderer second = new GameObject("Second").AddComponent<SpriteRenderer>();
                first.transform.SetParent(previewObject.transform, false);
                second.transform.SetParent(previewObject.transform, false);
                Demo2PipePlacementPreview preview = previewObject.AddComponent<Demo2PipePlacementPreview>();
                SetPrivateField(preview, "_cellRenderers", new[] { first, second });
                previewObject.SetActive(true);

                Assert.That(preview.Show(shape, controller, Vector3Int.zero, 0, true), Is.True);
                Assert.That(first.enabled, Is.True);
                Assert.That(second.enabled, Is.True);
                Assert.That(first.transform.position, Is.EqualTo(routeTilemap.GetCellCenterWorld(Vector3Int.zero)));
                Assert.That(second.transform.position, Is.EqualTo(routeTilemap.GetCellCenterWorld(Vector3Int.right)));
                Assert.That(first.color, Is.EqualTo(new Color(0.2f, 1f, 0.2f, 0.5f)));

                Assert.That(preview.Show(shape, controller, Vector3Int.zero, 0, false), Is.True);
                Assert.That(first.color, Is.EqualTo(new Color(1f, 0.2f, 0.2f, 0.5f)));
                Assert.That(second.color, Is.EqualTo(first.color));
            }
            finally
            {
                Object.DestroyImmediate(shape);
                Object.DestroyImmediate(previewObject);
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(routeObject);
                Object.DestroyImmediate(gridObject);
            }
        }

        [Test]
        public void MarkerRoute_WithMatchingPipes_FillsOneCellPerStep()
        {
            Demo2PipeWaterModel model = CreateHorizontalModel();
            model.SetPipe(1, 0, true, Demo2WaterPort.Left | Demo2WaterPort.Right);
            model.SetPipe(2, 0, true, Demo2WaterPort.Left | Demo2WaterPort.Right);
            model.SetPipe(3, 0, true, Demo2WaterPort.Left);

            Assert.That(model.IsWater(0, 0), Is.True);
            Assert.That(model.IsWater(1, 0), Is.False);

            Assert.That(model.Step(), Is.True);
            Assert.That(model.IsWater(1, 0), Is.True);
            Assert.That(model.IsWater(2, 0), Is.False);

            Assert.That(model.Step(), Is.True);
            Assert.That(model.IsWater(2, 0), Is.True);
            Assert.That(model.IsWater(3, 0), Is.False);

            Assert.That(model.Step(), Is.True);
            Assert.That(model.IsWater(3, 0), Is.True);
            Assert.That(model.Step(), Is.False);
        }

        [Test]
        public void MissingOrWrongPipePort_StopsAtLastConnectedCell()
        {
            Demo2PipeWaterModel model = CreateHorizontalModel();
            model.SetPipe(1, 0, true, Demo2WaterPort.Up | Demo2WaterPort.Down);

            Assert.That(model.Step(), Is.False);
            Assert.That(model.IsWater(0, 0), Is.True);
            Assert.That(model.IsWater(1, 0), Is.False);

            model.SetPipe(1, 0, true, Demo2WaterPort.Left | Demo2WaterPort.Right);
            Assert.That(model.Step(), Is.True);
            Assert.That(model.IsWater(1, 0), Is.True);
            Assert.That(model.Step(), Is.False);
            Assert.That(model.IsWater(2, 0), Is.False);
        }

        [Test]
        public void RemovingConnectedPipe_ClearsDownstreamAndAllowsReflow()
        {
            Demo2PipeWaterModel model = CreateHorizontalModel();
            Demo2WaterPort horizontal = Demo2WaterPort.Left | Demo2WaterPort.Right;
            model.SetPipe(1, 0, true, horizontal);
            model.SetPipe(2, 0, true, horizontal);
            model.SetPipe(3, 0, true, Demo2WaterPort.Left);
            model.Step();
            model.Step();
            model.Step();

            Assert.That(model.IsWater(3, 0), Is.True);

            model.SetPipe(2, 0, false, Demo2WaterPort.None);

            Assert.That(model.IsWater(0, 0), Is.True);
            Assert.That(model.IsWater(1, 0), Is.False);
            Assert.That(model.IsWater(2, 0), Is.False);
            Assert.That(model.IsWater(3, 0), Is.False);

            Assert.That(model.Step(), Is.True);
            Assert.That(model.IsWater(1, 0), Is.True);
            Assert.That(model.Step(), Is.False);

            model.SetPipe(2, 0, true, horizontal);
            model.Step();
            model.Step();
            model.Step();
            Assert.That(model.IsWater(3, 0), Is.True);
        }

        [Test]
        public void TurnAndBranchRoute_FillsEachReachableBranchByDistance()
        {
            Demo2PipeWaterModel model = new Demo2PipeWaterModel(3, 2);
            model.SetRoute(0, 0, Demo2WaterPort.Right);
            model.SetRoute(1, 0, Demo2WaterPort.Left | Demo2WaterPort.Right | Demo2WaterPort.Up);
            model.SetRoute(2, 0, Demo2WaterPort.Left);
            model.SetRoute(1, 1, Demo2WaterPort.Down);
            model.SetSource(0, 0);
            model.SetPipe(1, 0, true, Demo2WaterPort.Left | Demo2WaterPort.Right | Demo2WaterPort.Up);
            model.SetPipe(2, 0, true, Demo2WaterPort.Left);
            model.SetPipe(1, 1, true, Demo2WaterPort.Down);

            Assert.That(model.Step(), Is.True);
            Assert.That(model.IsWater(1, 0), Is.True);
            Assert.That(model.IsWater(2, 0), Is.False);
            Assert.That(model.IsWater(1, 1), Is.False);

            Assert.That(model.Step(), Is.True);
            Assert.That(model.IsWater(2, 0), Is.True);
            Assert.That(model.IsWater(1, 1), Is.True);
            Assert.That(model.Step(), Is.False);
        }

        [Test]
        public void Controller_UsesOnlyConfiguredMarkerAsPlacementRoute()
        {
            GameObject gridObject = new GameObject("Grid", typeof(Grid));
            GameObject routeObject = new GameObject("Route", typeof(Tilemap), typeof(TilemapRenderer));
            GameObject waterObject = new GameObject("Water", typeof(Tilemap), typeof(TilemapRenderer));
            GameObject controllerObject = new GameObject("Controller");
            Tile markerTile = ScriptableObject.CreateInstance<Tile>();
            Tile waterTile = ScriptableObject.CreateInstance<Tile>();

            try
            {
                routeObject.transform.SetParent(gridObject.transform, false);
                waterObject.transform.SetParent(gridObject.transform, false);
                Tilemap routeTilemap = routeObject.GetComponent<Tilemap>();
                Tilemap waterTilemap = waterObject.GetComponent<Tilemap>();
                Vector3Int sourceCell = new Vector3Int(-1, 2, 0);
                Vector3Int routeCell = sourceCell + Vector3Int.right;
                routeTilemap.SetTile(sourceCell, markerTile);
                routeTilemap.SetTile(routeCell, markerTile);

                controllerObject.SetActive(false);
                Demo2PipeWaterController controller = controllerObject.AddComponent<Demo2PipeWaterController>();
                SetPrivateField(controller, "_routeTilemap", routeTilemap);
                SetPrivateField(controller, "_routeMarkerTile", markerTile);
                SetPrivateField(controller, "_sourceCell", sourceCell);
                SetPrivateField(controller, "_waterTilemap", waterTilemap);
                SetPrivateField(controller, "_waterTile", waterTile);
                controllerObject.SetActive(true);

                Assert.That(controller.IsConfigured, Is.True);
                Assert.That(controller.IsPlacementCellAllowed(sourceCell), Is.True);
                Assert.That(controller.IsPlacementCellAllowed(routeCell), Is.True);
                Assert.That(controller.IsPlacementCellAllowed(routeCell + Vector3Int.right), Is.False);
                Assert.That(controller.IsWaterAtCell(sourceCell), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(waterObject);
                Object.DestroyImmediate(routeObject);
                Object.DestroyImmediate(gridObject);
                Object.DestroyImmediate(waterTile);
                Object.DestroyImmediate(markerTile);
            }
        }

        private static Demo2PipeWaterModel CreateHorizontalModel()
        {
            Demo2PipeWaterModel model = new Demo2PipeWaterModel(4, 1);
            model.SetRoute(0, 0, Demo2WaterPort.Right);
            model.SetRoute(1, 0, Demo2WaterPort.Left | Demo2WaterPort.Right);
            model.SetRoute(2, 0, Demo2WaterPort.Left | Demo2WaterPort.Right);
            model.SetRoute(3, 0, Demo2WaterPort.Left);
            model.SetSource(0, 0);
            return model;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }
    }
}
