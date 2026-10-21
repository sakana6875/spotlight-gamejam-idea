using Cinemachine;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 把一屏大小的背景铺到地面范围，并用 CinemachineConfiner2D 把玩法镜头限制在同一范围内。
    /// 侧视和俯视使用不同精灵，分别挂在对应视图根下，随视图停用一起隐藏。
    /// 素材按当前正交镜头高度对齐，只横向拼接，不拉伸成整张关卡。
    /// </summary>
    [ExecuteAlways]
    public sealed class Demo2LevelFrame : MonoBehaviour
    {
        private const float ReferenceAspect = 16f / 9f;
        private const int BackgroundSortingOrder = -20;
        private const int ForegroundSortingOrder = -2;
        private const string PlatesName = "SidePlates";
        private const string TopPlatesName = "TopPlates";
        private const string BoundsName = "CameraBounds";

        [SerializeField] private CinemachineVirtualCamera _gameplayCamera;
        [SerializeField] private Tilemap _groundTilemap;
        [SerializeField] private Transform _sideView;
        [SerializeField] private Sprite _background;
        [SerializeField] private Sprite _foreground;
        [SerializeField] private int _backgroundSortingOrder = BackgroundSortingOrder;
        [SerializeField] private int _foregroundSortingOrder = ForegroundSortingOrder;
        [SerializeField] private Transform _topView;
        [SerializeField] private Sprite _topBackground;
        [SerializeField] private int _topBackgroundSortingOrder = BackgroundSortingOrder;

        private void OnEnable()
        {
            Apply();
        }

        /// <summary>
        /// 按当前地面瓦片重建背景拼接和镜头边界。地面范围变化后可再次调用。
        /// </summary>
        public void Apply()
        {
            if (_gameplayCamera == null || _groundTilemap == null || _sideView == null)
            {
                Debug.LogError("Demo2LevelFrame 缺少玩法镜头、地面 Tilemap 或 SideView。", this);
                return;
            }

            if (!TryGetGroundBounds(out float minX, out float maxX, out float minY, out float maxY))
            {
                Debug.LogError("Demo2LevelFrame 的地面 Tilemap 没有可显示的格子。", _groundTilemap);
                return;
            }

            ExpandToCamera(ref minX, ref maxX, ref minY, ref maxY);
            ApplyConfiner(minX, maxX, minY, maxY);
            ApplyPlates(minX, maxX, minY, maxY);
            ApplyTopPlates(minX, maxX, minY, maxY);
        }

        private bool TryGetGroundBounds(out float minX, out float maxX, out float minY, out float maxY)
        {
            BoundsInt cells = _groundTilemap.cellBounds;
            minX = maxX = minY = maxY = 0f;
            if (cells.size.x <= 0 || cells.size.y <= 0)
            {
                return false;
            }

            Vector3 min = _groundTilemap.CellToWorld(cells.min);
            Vector3 max = _groundTilemap.CellToWorld(cells.max);
            minX = Mathf.Min(min.x, max.x);
            maxX = Mathf.Max(min.x, max.x);
            minY = Mathf.Min(min.y, max.y);
            maxY = Mathf.Max(min.y, max.y);
            return maxX > minX && maxY > minY;
        }

        private void ExpandToCamera(ref float minX, ref float maxX, ref float minY, ref float maxY)
        {
            float orthoSize = _gameplayCamera.m_Lens.OrthographicSize;
            float viewHeight = orthoSize * 2f;
            float viewWidth = viewHeight * ReferenceAspect;
            ExpandAxis(ref minX, ref maxX, viewWidth);
            ExpandVertical(ref minY, ref maxY, orthoSize);
        }

        private static void ExpandAxis(ref float min, ref float max, float requiredSize)
        {
            float size = max - min;
            if (size >= requiredSize)
            {
                return;
            }

            float extra = (requiredSize - size) * 0.5f;
            min -= extra;
            max += extra;
        }

        private static void ExpandVertical(ref float min, ref float max, float orthoSize)
        {
            // 画面底边停在最低地面，顶边最高到最高地面之上一整屏。
            // 边界必须高于镜头，否则 Confiner2D 会把竖直轴放掉。
            max += orthoSize * 2f;
        }

        private void ApplyConfiner(float minX, float maxX, float minY, float maxY)
        {
            PolygonCollider2D bounds = GetOrCreateBounds();
            bounds.SetPath(0, new[]
            {
                new Vector2(minX, minY),
                new Vector2(maxX, minY),
                new Vector2(maxX, maxY),
                new Vector2(minX, maxY)
            });

            CinemachineConfiner2D confiner = _gameplayCamera.GetComponent<CinemachineConfiner2D>();
            if (confiner == null)
            {
                confiner = _gameplayCamera.gameObject.AddComponent<CinemachineConfiner2D>();
            }

            confiner.m_BoundingShape2D = bounds;
            confiner.m_Damping = 0f;
            confiner.m_MaxWindowSize = 0f;
            confiner.InvalidateCache();

            Demo2CameraWindowClamp clamp = _gameplayCamera.GetComponent<Demo2CameraWindowClamp>();
            if (clamp == null)
            {
                clamp = _gameplayCamera.gameObject.AddComponent<Demo2CameraWindowClamp>();
            }

            clamp.SetWindow(minX, maxX, minY, maxY);
        }

        private PolygonCollider2D GetOrCreateBounds()
        {
            Transform existing = transform.Find(BoundsName);
            GameObject boundsObject = existing != null ? existing.gameObject : new GameObject(BoundsName);
            if (existing == null)
            {
                boundsObject.transform.SetParent(transform, false);
            }

            boundsObject.layer = 2;
            PolygonCollider2D polygon = boundsObject.GetComponent<PolygonCollider2D>();
            if (polygon == null)
            {
                polygon = boundsObject.AddComponent<PolygonCollider2D>();
            }

            polygon.isTrigger = true;
            return polygon;
        }

        private void ApplyPlates(float minX, float maxX, float minY, float maxY)
        {
            Transform plates = _sideView.Find(PlatesName);
            if (plates == null)
            {
                GameObject platesObject = new GameObject(PlatesName);
                plates = platesObject.transform;
                plates.SetParent(_sideView, false);
            }

            ClearChildren(plates);
            float centerY = (minY + maxY) * 0.5f;
            PlaceRow(
                plates,
                "Background",
                _background,
                _backgroundSortingOrder,
                centerY,
                minX,
                maxX,
                1f,
                minY,
                maxY);
            PlaceRow(
                plates,
                "Foreground",
                _foreground,
                _foregroundSortingOrder,
                centerY,
                minX,
                maxX,
                1f,
                minY,
                maxY);
        }

        private void ApplyTopPlates(float minX, float maxX, float minY, float maxY)
        {
            if (_topView == null && _topBackground == null)
            {
                return;
            }

            if (_topView == null || _topBackground == null)
            {
                Debug.LogError("Demo2LevelFrame 的俯视背景必须同时配置 TopView 和俯视精灵。", this);
                return;
            }

            float spriteHeight = _topBackground.bounds.size.y;
            if (spriteHeight <= 0f)
            {
                Debug.LogError("Demo2LevelFrame 的俯视背景高度无效。", this);
                return;
            }

            Transform plates = _topView.Find(TopPlatesName);
            if (plates == null)
            {
                GameObject platesObject = new GameObject(TopPlatesName);
                plates = platesObject.transform;
                plates.SetParent(_topView, false);
            }

            ClearChildren(plates);
            float viewHeight = _gameplayCamera.m_Lens.OrthographicSize * 2f;
            PlaceRow(
                plates,
                "TopBackground",
                _topBackground,
                _topBackgroundSortingOrder,
                (minY + maxY) * 0.5f,
                minX,
                maxX,
                viewHeight / spriteHeight,
                minY,
                maxY);
        }


        private static void PlaceRow(
            Transform plates,
            string name,
            Sprite sprite,
            int sortingOrder,
            float centerY,
            float minX,
            float maxX,
            float scale = 1f,
            float minY = 0f,
            float maxY = 0f)
        {
            if (sprite == null)
            {
                Debug.LogError("Demo2LevelFrame 缺少" + name + "精灵。", plates);
                return;
            }

            float width = sprite.bounds.size.x * scale;
            if (width <= 0f)
            {
                Debug.LogError("Demo2LevelFrame 的" + name + "精灵宽度无效。", plates);
                return;
            }

            int count = Mathf.Max(1, Mathf.CeilToInt((maxX - minX) / width));
            float height = sprite.bounds.size.y * scale;
            int rowCount = maxY > minY
                ? Mathf.Max(1, Mathf.CeilToInt((maxY - minY) / height))
                : 1;
            float startX = minX + width * 0.5f;
            float startY = rowCount == 1 ? centerY : minY + height * 0.5f;
            for (int row = 0; row < rowCount; row++)
            {
                for (int index = 0; index < count; index++)
                {
                    GameObject plate = new GameObject(name + " " + index + " " + row);
                    plate.transform.SetParent(plates, false);
                    plate.transform.localScale = new Vector3(scale, scale, 1f);
                    Vector3 local = plates.InverseTransformPoint(
                        new Vector3(startX + width * index, startY + height * row, 0f));
                    local.z = 0f;
                    plate.transform.localPosition = local;
                    SpriteRenderer renderer = plate.AddComponent<SpriteRenderer>();
                    renderer.sprite = sprite;
                    renderer.sortingOrder = sortingOrder;
                }
            }
        }

        private static void ClearChildren(Transform parent)
        {
            for (int index = parent.childCount - 1; index >= 0; index--)
            {
                Transform child = parent.GetChild(index);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
    }
}
