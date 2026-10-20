using UnityEngine;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 牵手跟随（适配层）：跟随者以固定侧向偏移平滑跟随领队。
    /// 本组件只维护「平滑后的跟随位置」这个逻辑值（FollowerPos），不直接写跟随者的 transform——
    /// 跟随者的位置由场景根每帧把 FollowerPos 喂给它的视图适配器（rb.MovePosition）驱动。
    /// （直接写 transform 会和 rb.MovePosition 互相覆盖：LateUpdate 刚挪近一点，
    ///   FixedUpdate 又写回旧位置，跟随者永远追不上领队——表现为两人相隔很远。）
    /// 牵手不需要玩家操作（系统随剧情决定），第二段男孩领队、第三段女孩领队，
    /// 只换 Leader/Follower 引用即可。
    /// </summary>
    public sealed class HandholdFollower : MonoBehaviour
    {
        [SerializeField] private Transform follower; // 仅用于开场吸附摆放；运行中不写它
        [SerializeField] private Vector2 holdOffset = new Vector2(0.45f, -0.1f);
        [SerializeField] private float followLerp = 5f;
        [SerializeField] private bool active;

        private Vector2 leaderPos;
        private Vector2 smoothPos; // 平滑后的跟随位置（逻辑值，场景根每帧 Apply 给跟随者）

        public bool Active { get => active; set => active = value; }
        public Vector2 FollowerPos => smoothPos;

        public void SetFollower(Transform t)
        {
            follower = t;
            if (follower != null) smoothPos = follower.position;
        }

        public void Snap(Vector2 leader, Vector2 followerPos)
        {
            leaderPos = leader;
            // 摆得离牵手位太远（>1.2m）说明不是按牵手站位摆的：直接吸附到领队身旁，
            // 避免开场两人相隔很远、跟随者从远处滑行几秒才追上
            Vector2 want = leader + holdOffset;
            smoothPos = Vector2.Distance(followerPos, want) > 1.2f ? want : followerPos;
            if (follower != null) follower.position = smoothPos;
        }

        private void LateUpdate()
        {
            if (!active) return;
            // 只推进逻辑位置；跟随者的 transform 由场景根 Apply（rb.MovePosition）驱动
            smoothPos = Vector2.Lerp(smoothPos, leaderPos + holdOffset, followLerp * Time.deltaTime);
        }

        public void SetLeaderPos(Vector2 p) => leaderPos = p;
    }
}
