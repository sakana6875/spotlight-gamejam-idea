using UnityEngine;

namespace Spotlight.Story
{
    /// <summary>
    /// SessionRoot 不是 RectTransform。Unity 会把其子 Canvas 的 Scale 写成 0，界面就消失。
    /// 编辑器和运行时都拉回 1，避免只能手改、一存盘又归零。
    /// </summary>
    [ExecuteAlways]
    public sealed class StoryCanvasScaleFix : MonoBehaviour
    {
        private void OnEnable()
        {
            RestoreScale();
        }

        private void LateUpdate()
        {
            RestoreScale();
        }

        private void RestoreScale()
        {
            if (transform.localScale == Vector3.one)
            {
                return;
            }

            transform.localScale = Vector3.one;
        }
    }
}
