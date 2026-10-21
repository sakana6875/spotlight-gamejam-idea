using UnityEngine;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 第二幕占位：玩家在左下，Boss 在右上，中间放几棵能挡住冲撞的树。
    /// 这轮不接对话框，也不给玩家生命。
    /// </summary>
    public sealed class Demo2Act2Layout : MonoBehaviour
    {
        private static readonly Vector2[] TreePositions =
        {
            new Vector2(0f, 0f),
            new Vector2(-1.2f, 1.4f),
            new Vector2(1.2f, -1.4f)
        };

        [SerializeField] private Vector2 _playerPosition = new Vector2(-4.6f, -2.4f);
        [SerializeField] private Vector2 _bossPosition = new Vector2(2.6f, 1.3f);

        private void Awake()
        {
            transform.position = _bossPosition;
            if (FindObjectsByType<Demo2WorldTree>(FindObjectsSortMode.None).Length == 0)
            {
                CreateTrees();
            }
        }

        private void Start()
        {
            PlayerMoveController player = FindFirstObjectByType<PlayerMoveController>();
            if (player == null)
            {
                Debug.LogError("第二幕找不到玩家，Boss 只能朝上次朝向攻击。", this);
                return;
            }

            // Start 晚于玩家 Awake，刚体已经缓存，切俯视不会空引用。
            player.SetViewMode(PlayerMoveController.Demo2ViewMode.Top, _playerPosition);
            player.transform.position = _playerPosition;
        }

        private static void CreateTrees()
        {
            for (int index = 0; index < TreePositions.Length; index++)
            {
                GameObject tree = new GameObject("WorldTree" + (index + 1));
                tree.transform.position = TreePositions[index];
                tree.AddComponent<Demo2WorldTree>();
            }
        }
    }
}
