using UnityEngine;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 牵手跟随（适配层）：跟随者以固定侧向偏移平滑跟随领队。
    /// 牵手不需要玩家操作（系统随剧情决定），第二段男孩领队、第三段女孩领队，
    /// 只换 Leader/Follower 引用即可。
    /// </summary>
    public sealed class HandholdFollower : MonoBehaviour
    {
        [SerializeField] private Transform follower;
        [SerializeField] private Vector2 holdOffset = new Vector2(0.45f, -0.1f);
        [SerializeField] private float followLerp = 5f;
        [SerializeField] private bool active;

        private Vector2 leaderPos;

        public bool Active { get => active; set => active = value; }
        public Vector2 FollowerPos => follower != null ? (Vector2)follower.position : leaderPos;

        public void SetFollower(Transform t) => follower = t;
        public void Snap(Vector2 leader, Vector2 followerPos)
        {
            leaderPos = leader;
            if (follower != null) follower.position = followerPos;
        }

        private void LateUpdate()
        {
            if (!active || follower == null) return;
            Vector2 target = leaderPos + holdOffset;
            follower.position = Vector2.Lerp(follower.position, target, followLerp * Time.deltaTime);
        }

        public void SetLeaderPos(Vector2 p) => leaderPos = p;
    }
}
