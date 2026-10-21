using UnityEngine;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 把正交摄像机的画面夹在场景里已摆好的俯视背景内。
    /// 挂到你创建的摄像机上。背景物体是 Act2Background，不要由脚本再生成。
    /// 勾选跟随时，镜头追玩家；不勾选时只限制你自己驱动的摄像机，不超出背景。
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class Demo2Act2CameraRig : MonoBehaviour
    {
        private const float ReferenceAspect = 16f / 9f;
        private const string BackgroundObjectName = "Act2Background";

        [SerializeField] private Camera _camera;
        [SerializeField] private Transform _target;
        [SerializeField] private SpriteRenderer _background;
        [SerializeField] private bool _followTarget = true;

        private float _minX;
        private float _maxX;
        private float _minY;
        private float _maxY;
        private bool _hasBounds;

        private void Awake()
        {
            if (_camera == null)
            {
                _camera = GetComponent<Camera>();
            }

            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_camera == null || !_camera.orthographic)
            {
                Debug.LogError("第二幕镜头脚本需要一台正交摄像机。", this);
                enabled = false;
                return;
            }

            if (!TryReadBackground())
            {
                enabled = false;
            }
        }

        private void Start()
        {
            if (_target == null)
            {
                PlayerMoveController player = FindFirstObjectByType<PlayerMoveController>();
                if (player != null)
                {
                    _target = player.transform;
                }
            }

            ApplyFrame();
        }

        private void LateUpdate()
        {
            ApplyFrame();
        }

        private bool TryReadBackground()
        {
            if (_background == null)
            {
                GameObject plate = GameObject.Find(BackgroundObjectName);
                if (plate != null)
                {
                    _background = plate.GetComponent<SpriteRenderer>();
                }
            }

            if (_background == null || _background.sprite == null)
            {
                Debug.LogError("第二幕找不到场景背景 Act2Background。", this);
                return false;
            }

            Bounds bounds = _background.bounds;
            if (bounds.size.x <= 0f || bounds.size.y <= 0f)
            {
                Debug.LogError("第二幕背景尺寸无效。", _background);
                return false;
            }

            _minX = bounds.min.x;
            _maxX = bounds.max.x;
            _minY = bounds.min.y;
            _maxY = bounds.max.y;
            _hasBounds = true;
            return true;
        }

        private void ApplyFrame()
        {
            if (!_hasBounds || _camera == null)
            {
                return;
            }

            Vector3 position = _camera.transform.position;
            if (_followTarget && _target != null)
            {
                position.x = _target.position.x;
                position.y = _target.position.y;
            }

            float aspect = _camera.aspect > 0.01f ? _camera.aspect : ReferenceAspect;
            float halfHeight = _camera.orthographicSize;
            float halfWidth = halfHeight * aspect;
            Demo2CameraFrame.ClampCenter(
                position.x,
                position.y,
                _minX,
                _maxX,
                _minY,
                _maxY,
                halfWidth,
                halfHeight,
                out float clampedX,
                out float clampedY);
            position.x = clampedX;
            position.y = clampedY;
            _camera.transform.position = position;
        }
    }
}
