using UnityEngine;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 第二幕可挡住小 Boss 冲撞的成熟大树。
    /// 场景里已经摆好的碰撞体和贴图保持原样；只有缺组件时才补占位。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Demo2WorldTree : MonoBehaviour
    {
        public const float Size = 2f;

        private void Awake()
        {
            if (GetComponent<BoxCollider2D>() == null)
            {
                BoxCollider2D body = gameObject.AddComponent<BoxCollider2D>();
                body.isTrigger = false;
                body.size = new Vector2(Size, Size);
            }

            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            if (renderer != null && renderer.sprite != null)
            {
                return;
            }

            if (renderer == null)
            {
                renderer = gameObject.AddComponent<SpriteRenderer>();
            }

            renderer.sprite = Demo2BossTelegraph.Square;
            renderer.color = new Color(0.2f, 0.55f, 0.22f, 1f);
            float spriteSize = renderer.sprite.bounds.size.x;
            float scale = spriteSize <= 0.0001f ? Size : Size / spriteSize;
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
