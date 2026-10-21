using UnityEngine;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 把俯视小 Boss 接到动画、位移和预警上。
    /// 追击用网格寻路绕开 WorldTree，身体和树之间留出空隙，避免碰撞箱卡在一起。
    /// 冲刺第一次撞到 WorldTree 就死亡并通知剧情。玩家攻击只闪红，不会杀死 Boss。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Demo2BossController : MonoBehaviour
    {
        private const int OverlapCapacity = 8;
        private const int TreeCapacity = 16;
        private const float PlayerKnockbackSpeed = 6f;
        private const float HitFlashSeconds = 0.15f;
        private const float TreeHitFlashSeconds = 0.45f;
        private const float PathClearance = Demo2BossStateMachine.BodySize * 0.5f + 0.05f;

        [SerializeField] private Transform _player;
        [SerializeField] private Animator _animator;

        private readonly Collider2D[] _overlaps = new Collider2D[OverlapCapacity];
        private readonly Demo2BossPathfinder _pathfinder = new Demo2BossPathfinder();
        private readonly float[] _treeMinX = new float[TreeCapacity];
        private readonly float[] _treeMinY = new float[TreeCapacity];
        private readonly float[] _treeMaxX = new float[TreeCapacity];
        private readonly float[] _treeMaxY = new float[TreeCapacity];
        private Demo2BossStateMachine _rules;
        private SpriteRenderer _sweepTelegraph;
        private SpriteRenderer _chargeTelegraph;
        private SpriteRenderer _bodyRenderer;
        private Color _bodyColor = Color.white;
        private string _animationState = string.Empty;
        private float _hitFlashRemaining;
        private float _walkFacingX = 1f;
        private float _nextTreeScan;
        private Demo2WorldTree[] _trees = new Demo2WorldTree[0];
        private Demo2BossPhase _previousPhase = Demo2BossPhase.Chase;
        private bool _chargeKnockedPlayer;
        private bool _defeatPublished;

        /// <summary>
        /// 当前规则阶段，供场景占位和后续对话框读取。
        /// </summary>
        public Demo2BossPhase Phase => _rules == null ? Demo2BossPhase.Chase : _rules.Phase;

        /// <summary>
        /// 已经叠上的撞树伤害层数。玩家攻击不会增加。
        /// </summary>
        public int StunStacks => _rules == null ? 0 : _rules.StunStacks;

        /// <summary>
        /// 是否已经因冲撞树木死亡。
        /// </summary>
        public bool IsDefeated => _defeatPublished ||
            (_rules != null && _rules.Phase == Demo2BossPhase.PermanentlyStunned);

        /// <summary>
        /// 第一次冲撞树木并死亡后发布一次。
        /// </summary>
        public event System.Action Defeated;

        private void Awake()
        {
            _rules = new Demo2BossStateMachine();
            _bodyRenderer = GetComponent<SpriteRenderer>();
            if (_bodyRenderer != null)
            {
                _bodyColor = _bodyRenderer.color;
            }

            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }

            EnsureBody();
            _sweepTelegraph = CreateTelegraph("SweepTelegraph", Demo2BossTelegraph.Circle);
            _chargeTelegraph = CreateTelegraph("ChargeTelegraph", Demo2BossTelegraph.Square);
            if (_player == null)
            {
                PlayerMoveController player = FindFirstObjectByType<PlayerMoveController>();
                if (player != null)
                {
                    _player = player.transform;
                }
            }
        }

        private void OnDestroy()
        {
            if (_sweepTelegraph != null)
            {
                Destroy(_sweepTelegraph.gameObject);
            }

            if (_chargeTelegraph != null)
            {
                Destroy(_chargeTelegraph.gameObject);
            }
        }

        /// <summary>
        /// 玩家攻击命中。只闪一下，不扣撞树伤害，也不能打死。
        /// </summary>
        public void ReceivePlayerStrike()
        {
            if (_rules == null || _rules.Phase == Demo2BossPhase.PermanentlyStunned)
            {
                return;
            }

            Flash(HitFlashSeconds);
        }

        private void Update()
        {
            if (_rules == null || _defeatPublished)
            {
                return;
            }

            Vector2 offset = Vector2.zero;
            if (_player != null)
            {
                offset = (Vector2)_player.position - (Vector2)transform.position;
            }

            bool chargeHitsTree = _rules.Phase == Demo2BossPhase.Charging && OverlapsTree(transform.position);
            _rules.Tick(Time.deltaTime, offset.x, offset.y, chargeHitsTree);
            if (_rules.Phase != Demo2BossPhase.Charging)
            {
                _chargeKnockedPlayer = false;
            }
            else if (_previousPhase != Demo2BossPhase.Charging)
            {
                _chargeKnockedPlayer = false;
            }

            _previousPhase = _rules.Phase;
            Vector2 step = new Vector2(_rules.StepX, _rules.StepY);
            if (_rules.Phase == Demo2BossPhase.Chase)
            {
                step = FollowPath(step);
            }
            else if (_rules.Phase == Demo2BossPhase.Charging && step.sqrMagnitude > 0f &&
                OverlapsTree((Vector2)transform.position + step))
            {
                _rules.NotifyTreeHit();
                step = Vector2.zero;
            }

            if (_rules.TreeStunThisTick)
            {
                Flash(TreeHitFlashSeconds);
                if (_rules.Phase == Demo2BossPhase.PermanentlyStunned)
                {
                    PublishDefeat();
                }
            }

            transform.position += (Vector3)step;
            TryKnockPlayer();
            FaceBody();
            UpdateHitFlash();
            UpdateTelegraphs();
            PlayAnimation();
        }

        private Vector2 FollowPath(Vector2 fallback)
        {
            if (_player == null)
            {
                return fallback;
            }

            int treeCount = CollectTrees();
            float maxDistance = fallback.magnitude;
            if (maxDistance < 0.0001f)
            {
                maxDistance = Demo2BossStateMachine.MoveSpeed * Time.deltaTime;
            }

            Vector2 position = transform.position;
            if (!_pathfinder.TryStep(
                position.x,
                position.y,
                _player.position.x,
                _player.position.y,
                maxDistance,
                treeCount,
                _treeMinX,
                _treeMinY,
                _treeMaxX,
                _treeMaxY,
                PathClearance,
                out float stepX,
                out float stepY))
            {
                return SlideWithoutEntering(fallback);
            }

            Vector2 step = new Vector2(stepX, stepY);
            if (Mathf.Abs(step.x) >= 0.05f)
            {
                _walkFacingX = step.x;
            }

            if (!OverlapsTree(position) && OverlapsTree(position + step))
            {
                return SlideWithoutEntering(step);
            }

            return step;
        }

        private Vector2 SlideWithoutEntering(Vector2 step)
        {
            Vector2 position = transform.position;
            bool fullBlocked = OverlapsTree(position + step);
            bool xBlocked = OverlapsTree(position + new Vector2(step.x, 0f));
            bool yBlocked = OverlapsTree(position + new Vector2(0f, step.y));
            Demo2BossSteering.ChooseStep(
                step.x,
                step.y,
                fullBlocked,
                xBlocked,
                yBlocked,
                out float stepX,
                out float stepY);
            Vector2 slid = new Vector2(stepX, stepY);
            if (slid.sqrMagnitude > 0.0001f && !OverlapsTree(position) && OverlapsTree(position + slid))
            {
                return Vector2.zero;
            }

            if (Mathf.Abs(slid.x) >= 0.05f)
            {
                _walkFacingX = slid.x;
            }

            return slid;
        }

        private int CollectTrees()
        {
            if (Time.time >= _nextTreeScan || HasMissingTree())
            {
                _trees = FindObjectsByType<Demo2WorldTree>(FindObjectsSortMode.None);
                _nextTreeScan = Time.time + 0.5f;
            }

            int count = _trees.Length < TreeCapacity ? _trees.Length : TreeCapacity;
            int written = 0;
            for (int index = 0; index < count; index++)
            {
                Demo2WorldTree tree = _trees[index];
                if (tree == null)
                {
                    continue;
                }

                Collider2D body = tree.GetComponent<Collider2D>();
                if (body != null)
                {
                    Bounds bounds = body.bounds;
                    _treeMinX[written] = bounds.min.x;
                    _treeMinY[written] = bounds.min.y;
                    _treeMaxX[written] = bounds.max.x;
                    _treeMaxY[written] = bounds.max.y;
                }
                else
                {
                    Vector2 center = tree.transform.position;
                    _treeMinX[written] = center.x - Demo2WorldTree.Size * 0.5f;
                    _treeMinY[written] = center.y - Demo2WorldTree.Size * 0.5f;
                    _treeMaxX[written] = center.x + Demo2WorldTree.Size * 0.5f;
                    _treeMaxY[written] = center.y + Demo2WorldTree.Size * 0.5f;
                }

                written++;
            }

            return written;
        }

        private bool HasMissingTree()
        {
            for (int index = 0; index < _trees.Length; index++)
            {
                if (_trees[index] == null)
                {
                    return true;
                }
            }

            return false;
        }

        private void Flash(float seconds)
        {
            if (_hitFlashRemaining < seconds)
            {
                _hitFlashRemaining = seconds;
            }

            if (_bodyRenderer != null)
            {
                _bodyRenderer.color = new Color(1f, 0.45f, 0.45f, _bodyColor.a);
            }
        }

        private void PublishDefeat()
        {
            _defeatPublished = true;
            _sweepTelegraph.gameObject.SetActive(false);
            _chargeTelegraph.gameObject.SetActive(false);
            if (_animator != null)
            {
                _animator.enabled = false;
            }

            if (_bodyRenderer != null)
            {
                _bodyRenderer.color = new Color(0.45f, 0.08f, 0.08f, _bodyColor.a);
            }

            Defeated?.Invoke();
        }

        private void TryKnockPlayer()
        {
            if (_player == null || _rules.Phase == Demo2BossPhase.PermanentlyStunned)
            {
                return;
            }

            PlayerMoveController player = _player.GetComponent<PlayerMoveController>();
            if (player == null)
            {
                return;
            }

            if (_rules.SweepResolvedThisTick && IsPlayerInSweep())
            {
                Vector2 away = (Vector2)_player.position - (Vector2)transform.position;
                Knock(player, away);
                return;
            }

            if (_rules.Phase == Demo2BossPhase.Charging && !_chargeKnockedPlayer && OverlapsPlayer())
            {
                _chargeKnockedPlayer = true;
                Knock(player, new Vector2(_rules.LockedChargeX, _rules.LockedChargeY));
            }
        }

        private static void Knock(PlayerMoveController player, Vector2 direction)
        {
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector2.right;
            }

            player.ApplyKnockback(direction.normalized * PlayerKnockbackSpeed);
        }

        private bool IsPlayerInSweep()
        {
            Collider2D body = _player.GetComponent<Collider2D>();
            Vector2 point = body != null
                ? body.ClosestPoint(transform.position)
                : (Vector2)_player.position;
            Vector2 delta = point - (Vector2)transform.position;
            float radius = Demo2BossStateMachine.SweepRadius;
            return delta.sqrMagnitude <= radius * radius;
        }

        private bool OverlapsPlayer()
        {
            Collider2D playerBody = _player.GetComponent<Collider2D>();
            if (playerBody == null)
            {
                return false;
            }

            int count = Physics2D.OverlapBoxNonAlloc(
                transform.position,
                new Vector2(Demo2BossStateMachine.BodySize, Demo2BossStateMachine.BodySize),
                0f,
                _overlaps);
            for (int index = 0; index < count; index++)
            {
                if (_overlaps[index] == playerBody)
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateHitFlash()
        {
            if (_hitFlashRemaining <= 0f || _bodyRenderer == null)
            {
                return;
            }

            _hitFlashRemaining -= Time.deltaTime;
            if (_hitFlashRemaining <= 0f)
            {
                _bodyRenderer.color = _bodyColor;
            }
        }

        private void EnsureBody()
        {
            Rigidbody2D body = GetComponent<Rigidbody2D>();
            if (body == null)
            {
                body = gameObject.AddComponent<Rigidbody2D>();
            }

            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;

            BoxCollider2D collider = GetComponent<BoxCollider2D>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<BoxCollider2D>();
            }

            collider.isTrigger = true;
            collider.offset = Vector2.zero;
            Vector3 scale = transform.lossyScale;
            float scaleX = Mathf.Abs(scale.x) < 0.0001f ? 1f : Mathf.Abs(scale.x);
            float scaleY = Mathf.Abs(scale.y) < 0.0001f ? 1f : Mathf.Abs(scale.y);
            collider.size = new Vector2(
                Demo2BossStateMachine.BodySize / scaleX,
                Demo2BossStateMachine.BodySize / scaleY);
        }

        private bool OverlapsTree(Vector2 center)
        {
            int count = Physics2D.OverlapBoxNonAlloc(
                center,
                new Vector2(Demo2BossStateMachine.BodySize, Demo2BossStateMachine.BodySize),
                0f,
                _overlaps);
            for (int index = 0; index < count; index++)
            {
                Collider2D overlap = _overlaps[index];
                if (overlap != null && overlap.GetComponent<Demo2WorldTree>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void FaceBody()
        {
            if (_bodyRenderer == null)
            {
                return;
            }

            float facing = _rules.Phase == Demo2BossPhase.Chase ? _walkFacingX : _rules.FacingX;
            _bodyRenderer.flipX = facing < 0f;
        }

        private void UpdateTelegraphs()
        {
            _sweepTelegraph.gameObject.SetActive(_rules.IsSweepTelegraphVisible);
            if (_rules.IsSweepTelegraphVisible)
            {
                _sweepTelegraph.transform.position = transform.position;
            }

            _chargeTelegraph.gameObject.SetActive(_rules.IsChargeTelegraphVisible);
            if (!_rules.IsChargeTelegraphVisible)
            {
                return;
            }

            Vector2 direction = new Vector2(_rules.LockedChargeX, _rules.LockedChargeY);
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector2.right;
            }

            direction.Normalize();
            float front = Demo2BossStateMachine.BodySize * 0.5f;
            Vector2 center = (Vector2)transform.position +
                direction * (front + Demo2BossStateMachine.ChargeDistance * 0.5f);
            _chargeTelegraph.transform.SetPositionAndRotation(
                center,
                Quaternion.FromToRotation(Vector3.right, direction));
        }

        private void PlayAnimation()
        {
            if (_animator == null)
            {
                return;
            }

            string state = _rules.Phase == Demo2BossPhase.SweepWindup
                ? "BossAttack1"
                : _rules.Phase == Demo2BossPhase.ChargeWindup || _rules.Phase == Demo2BossPhase.Charging
                    ? "BossAttack2"
                    : "BossWalking";
            if (state == _animationState)
            {
                return;
            }

            _animationState = state;
            _animator.Play(state, 0, 0f);
        }

        private static SpriteRenderer CreateTelegraph(string name, Sprite sprite)
        {
            GameObject marker = new GameObject(name);
            SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(1f, 0.15f, 0.1f, 0.4f);
            renderer.sortingOrder = 20;
            if (name == "SweepTelegraph")
            {
                marker.transform.localScale = new Vector3(
                    Demo2BossStateMachine.SweepRadius * 2f,
                    Demo2BossStateMachine.SweepRadius * 2f,
                    1f);
            }
            else
            {
                marker.transform.localScale = new Vector3(
                    Demo2BossStateMachine.ChargeDistance,
                    Demo2BossStateMachine.BodySize,
                    1f);
            }

            marker.SetActive(false);
            return renderer;
        }
    }
}
