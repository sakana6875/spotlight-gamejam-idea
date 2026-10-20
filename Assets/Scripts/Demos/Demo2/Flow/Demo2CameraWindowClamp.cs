using Cinemachine;
using UnityEngine;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 把玩法镜头的可视窗口夹在给定世界矩形内。
    /// CinemachineConfiner2D 在某一轴被镜头吃满时会放掉该轴，所以竖直限制不能只靠它。
    /// </summary>
    public sealed class Demo2CameraWindowClamp : CinemachineExtension
    {
        private float _minX;
        private float _maxX;
        private float _minY;
        private float _maxY;

        /// <summary>
        /// 设置画面必须留在其中的世界范围。底边和顶边都是画面边缘，不是镜头中心。
        /// </summary>
        public void SetWindow(float minX, float maxX, float minY, float maxY)
        {
            _minX = minX;
            _maxX = maxX;
            _minY = minY;
            _maxY = maxY;
        }

        /// <inheritdoc />
        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage,
            ref CameraState state,
            float deltaTime)
        {
            if (stage != CinemachineCore.Stage.Finalize)
            {
                return;
            }

            float halfHeight = state.Lens.OrthographicSize;
            float aspect = state.Lens.Aspect > 0.01f ? state.Lens.Aspect : 16f / 9f;
            float halfWidth = halfHeight * aspect;
            Vector3 position = state.CorrectedPosition;
            float x = ClampCenter(position.x, _minX, _maxX, halfWidth);
            float y = ClampCenter(position.y, _minY, _maxY, halfHeight);
            state.PositionCorrection += new Vector3(x - position.x, y - position.y, 0f);
        }

        private static float ClampCenter(float center, float min, float max, float halfSize)
        {
            float minCenter = min + halfSize;
            float maxCenter = max - halfSize;
            if (minCenter > maxCenter)
            {
                return (min + max) * 0.5f;
            }

            return Mathf.Clamp(center, minCenter, maxCenter);
        }
    }
}
