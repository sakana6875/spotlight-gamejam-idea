using UnityEngine;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 使用预接线SpriteRenderer显示水管精确占格及红绿物理合法性。
    /// </summary>
    public sealed class Demo2PipePlacementPreview : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] _cellRenderers;
        [SerializeField] private Color _validColor = new Color(0.2f, 1f, 0.2f, 0.5f);
        [SerializeField] private Color _invalidColor = new Color(1f, 0.2f, 0.2f, 0.5f);

        private bool _isConfigured;

        private void Awake()
        {
            if (_cellRenderers == null || _cellRenderers.Length == 0)
            {
                Debug.LogError("Demo2PipePlacementPreview至少需要一个SpriteRenderer。", this);
                enabled = false;
                return;
            }

            for (int index = 0; index < _cellRenderers.Length; index++)
            {
                if (_cellRenderers[index] == null)
                {
                    Debug.LogError("Demo2PipePlacementPreview包含空SpriteRenderer引用。", this);
                    enabled = false;
                    return;
                }
            }

            _isConfigured = true;
            Hide();
        }

        /// <summary>
        /// 在指定管网格显示Shape全部占格；端口是否匹配不影响颜色。
        /// </summary>
        public bool Show(
            Demo2PipeShape shape,
            Demo2PipeWaterController waterController,
            Vector3Int origin,
            int quarterTurns,
            bool isValid)
        {
            if (!_isConfigured || shape == null || waterController == null ||
                shape.CellCount > _cellRenderers.Length)
            {
                Hide();
                return false;
            }

            Color color = isValid ? _validColor : _invalidColor;
            for (int index = 0; index < _cellRenderers.Length; index++)
            {
                bool isVisible = index < shape.CellCount;
                SpriteRenderer renderer = _cellRenderers[index];
                if (renderer.enabled != isVisible)
                {
                    renderer.enabled = isVisible;
                }
                if (!isVisible)
                {
                    continue;
                }

                Demo2PipeCellDefinition cellDefinition = shape.GetRotatedCell(index, quarterTurns);
                Vector3Int cell = origin + new Vector3Int(cellDefinition.Offset.x, cellDefinition.Offset.y, 0);
                Vector3 position = waterController.GetCellCenterWorld(cell);
                position.z = renderer.transform.position.z;
                if (renderer.transform.position != position)
                {
                    renderer.transform.position = position;
                }

                if (renderer.color != color)
                {
                    renderer.color = color;
                }
            }

            return true;
        }

        /// <summary>
        /// 隐藏全部占格预览。
        /// </summary>
        public void Hide()
        {
            if (_cellRenderers == null)
            {
                return;
            }

            for (int index = 0; index < _cellRenderers.Length; index++)
            {
                if (_cellRenderers[index] != null && _cellRenderers[index].enabled)
                {
                    _cellRenderers[index].enabled = false;
                }
            }
        }
    }
}
