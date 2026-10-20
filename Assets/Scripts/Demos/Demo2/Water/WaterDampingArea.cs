using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 仅对实际被水格占据位置内的刚体施加速度阻尼。
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class WaterDampingArea : MonoBehaviour
    {
        [SerializeField] private TerrainWaterController _terrainWaterController;
        [SerializeField] private float _dampingCoefficient = 2f;

        private readonly HashSet<Rigidbody2D> _overlappingBodies = new HashSet<Rigidbody2D>();
        private readonly List<Rigidbody2D> _staleBodies = new List<Rigidbody2D>();
        private BoxCollider2D _triggerCollider;

        private void Awake()
        {
            _triggerCollider = GetComponent<BoxCollider2D>();
            if (_triggerCollider == null)
            {
                DisableForInvalidConfiguration("缺少同根 BoxCollider2D。");
                return;
            }

            if (!_triggerCollider.isTrigger)
            {
                DisableForInvalidConfiguration("BoxCollider2D 的 Is Trigger 必须勾选。");
                return;
            }

            if (_terrainWaterController == null)
            {
                DisableForInvalidConfiguration("未指定 TerrainWaterController。");
                return;
            }

            if (_dampingCoefficient < 0f)
            {
                DisableForInvalidConfiguration("Damping Coefficient 不能小于零。");
            }
        }

        /// <summary>
        /// 使用水 Tilemap 的推导域设置 Trigger 的本地范围。
        /// </summary>
        /// <remarks>角点先转换到当前根节点局部坐标，避免依赖父子原点或缩放。</remarks>
        public bool TryConfigureWaterBounds(Tilemap waterTilemap, BoundsInt waterBounds)
        {
            if (waterTilemap == null || waterBounds.size.x < 1 || waterBounds.size.y < 1)
            {
                return false;
            }

            if (_triggerCollider == null)
            {
                _triggerCollider = GetComponent<BoxCollider2D>();
            }

            if (_triggerCollider == null || !_triggerCollider.isTrigger)
            {
                return false;
            }

            Vector3Int minimum = waterBounds.min;
            Vector3Int lowerRight = new Vector3Int(waterBounds.xMax, waterBounds.yMin, waterBounds.zMin);
            Vector3Int upperLeft = new Vector3Int(waterBounds.xMin, waterBounds.yMax, waterBounds.zMin);
            Vector3Int maximum = waterBounds.max;
            Vector3 localMinimum = transform.InverseTransformPoint(waterTilemap.CellToWorld(minimum));
            Vector3 localLowerRight = transform.InverseTransformPoint(waterTilemap.CellToWorld(lowerRight));
            Vector3 localUpperLeft = transform.InverseTransformPoint(waterTilemap.CellToWorld(upperLeft));
            Vector3 localMaximum = transform.InverseTransformPoint(waterTilemap.CellToWorld(maximum));
            float minimumX = Mathf.Min(Mathf.Min(localMinimum.x, localLowerRight.x),
                Mathf.Min(localUpperLeft.x, localMaximum.x));
            float maximumX = Mathf.Max(Mathf.Max(localMinimum.x, localLowerRight.x),
                Mathf.Max(localUpperLeft.x, localMaximum.x));
            float minimumY = Mathf.Min(Mathf.Min(localMinimum.y, localLowerRight.y),
                Mathf.Min(localUpperLeft.y, localMaximum.y));
            float maximumY = Mathf.Max(Mathf.Max(localMinimum.y, localLowerRight.y),
                Mathf.Max(localUpperLeft.y, localMaximum.y));

            _triggerCollider.offset = new Vector2((minimumX + maximumX) * 0.5f, (minimumY + maximumY) * 0.5f);
            _triggerCollider.size = new Vector2(maximumX - minimumX, maximumY - minimumY);
            return _triggerCollider.size.x > 0f && _triggerCollider.size.y > 0f;
        }

        private void DisableForInvalidConfiguration(string reason)
        {
            Debug.LogError("水域阻尼区配置无效：" + reason, this);
            enabled = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Rigidbody2D body = other.attachedRigidbody;
            if (body != null)
            {
                _overlappingBodies.Add(body);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            Rigidbody2D body = other.attachedRigidbody;
            if (body != null)
            {
                _overlappingBodies.Remove(body);
            }
        }

        private void FixedUpdate()
        {
            foreach (Rigidbody2D body in _overlappingBodies)
            {
                if (body == null)
                {
                    _staleBodies.Add(body);
                    continue;
                }

                if (!_terrainWaterController.IsWaterAtWorldPosition(body.position))
                {
                    continue;
                }

                Vector2 velocity = body.velocity;
                if (_dampingCoefficient > 0f && velocity.sqrMagnitude > 0f)
                {
                    body.AddForce(-velocity * _dampingCoefficient, ForceMode2D.Force);
                }
            }

            for (int index = 0; index < _staleBodies.Count; index++)
            {
                _overlappingBodies.Remove(_staleBodies[index]);
            }

            _staleBodies.Clear();
        }

        private void OnDisable()
        {
            _overlappingBodies.Clear();
            _staleBodies.Clear();
        }
    }
}
