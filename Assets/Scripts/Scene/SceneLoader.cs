using UnityEngine.SceneManagement;

namespace Spotlight.Scene
{
    /// <summary>
    /// 当前项目唯一的 Unity 场景加载边界。
    /// </summary>
    public sealed class SceneLoader
    {
        /// <summary>
        /// 请求加载当前唯一可用的 SampleScene；已在该场景时返回失败。
        /// </summary>
        public bool LoadSampleScene()
        {
            if (SceneManager.GetActiveScene().name == "SampleScene")
            {
                return false;
            }

            try
            {
                SceneManager.LoadSceneAsync("SampleScene");
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }
    }
}
