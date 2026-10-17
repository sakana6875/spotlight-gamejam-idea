using UnityEngine;
using Spotlight.Domain.Save;

namespace Spotlight.Demos.Demo1
{
    public sealed class Demo1CheckpointController : MonoBehaviour
    {
        private ISaveService _saveService;

        public void Setup(ISaveService saveService)
        {
            _saveService = saveService;
        }
    }
}