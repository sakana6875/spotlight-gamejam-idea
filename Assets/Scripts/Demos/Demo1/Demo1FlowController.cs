using UnityEngine;
using Spotlight.Application.Services.Scene;

namespace Spotlight.Demos.Demo1
{
    public sealed class Demo1FlowController : MonoBehaviour
    {
        private ISceneFlow _sceneFlow;

        public void Setup(ISceneFlow sceneFlow)
        {
            _sceneFlow = sceneFlow;
        }

        public void ReturnToHub()
        {
            if (_sceneFlow != null)
            {
                _sceneFlow.LoadHub();
            }
        }
    }
}