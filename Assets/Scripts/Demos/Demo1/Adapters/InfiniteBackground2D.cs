using UnityEngine;

namespace Spotlight.Demos.Demo1.Adapters
{
    /// <summary>
    /// 2D 无限滚动背景。通过修改材质的 UV 偏移来实现，性能极高。
    /// </summary>
    public sealed class InfiniteBackground2D : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField] private Transform _cameraTransform;

        [Header("Parallax Settings")]
        [Tooltip("视差系数：1 表示完全跟随相机（看起来背景不动），0.5 表示背景慢速移动，0.2 表示远景。")]
        [Range(0f, 1f)]
        [SerializeField] private float _parallaxFactorX = 0.5f;
        [Range(0f, 1f)]
        [SerializeField] private float _parallaxFactorY = 0.5f;

        [Tooltip("贴图在世界上实际有多大（如果觉得滚得太快，把这里数值调大）。")]
        [SerializeField] private float _textureWorldSizeX = 10f;
        [SerializeField] private float _textureWorldSizeY = 10f;

        private SpriteRenderer _spriteRenderer;
        private Material _material;
        private Vector3 _startCameraPosition;

        private void Start()
        {
            if (_cameraTransform == null)
            {
                _cameraTransform = Camera.main.transform;
            }

            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer != null)
            {
                // 实例化材质，避免影响场景中其它使用同材质的物体
                _material = _spriteRenderer.material;
            }

            _startCameraPosition = _cameraTransform.position;
        }

        private void LateUpdate()
        {
            if (_material == null || _cameraTransform == null) return;

            // 计算相机相对于起点的位移
            Vector3 deltaPos = _cameraTransform.position - _startCameraPosition;

            // 根据位移和视差系数，计算 UV 偏移量
            float offsetX = (deltaPos.x / _textureWorldSizeX) * _parallaxFactorX;
            float offsetY = (deltaPos.y / _textureWorldSizeY) * _parallaxFactorY;

            // 应用到材质的 Main Texture Offset
            _material.mainTextureOffset = new Vector2(offsetX, offsetY);
        }
    }
}