using System;
using UnityEngine;
using Spotlight.Application.Services;
using Spotlight.Application.Services.Scene;
using Spotlight.Features.DemoOne.Application;
using Spotlight.Features.DemoOne.Data;
using Spotlight.Features.DemoOne.Domain;
using Spotlight.Features.DemoOne.Domain.Rhythm;

namespace Spotlight.Features.DemoOne.Adapters
{
    /// <summary>
    /// Demo1 场景组装入口。
    /// 从全局 SessionRoot 获取服务，创建 Controller、MusicSource、Clock、
    /// PulseInputAdapter、表现层脚本，并把它们串联起来。
    /// 不允许在玩法层使用 FindObjectOfType；本类是唯一的组装点。
    /// </summary>
    public sealed class DemoOneSceneInstaller : MonoBehaviour
    {
        [Header("Session")]
        [Tooltip("由 Bootstrap 场景跨场景保留的 SessionRoot 引用。")]
        [SerializeField] private MonoBehaviour _sessionRootBehaviour;

        [Header("Data")]
        [SerializeField] private DemoOneChartAsset _chartAsset;
        [SerializeField] private JudgeWindowConfigAsset _judgeWindowConfig;
        [SerializeField] private HeightRuleConfigAsset _heightRuleConfig;

        [Header("Adapters")]
        [SerializeField] private DemoOneMusicSource _musicSource;
        [SerializeField] private PulseInputAdapter _pulseInput;
        [SerializeField] private DemoOneRunnerBehaviour _runner;
        [SerializeField] private PlayerVisualAdapter _playerVisual;
        [SerializeField] private JudgmentLineVisual _judgmentLineVisual;
        [SerializeField] private DemoOneUIAdapter _uiAdapter;

        [Header("Timing")]
        [Tooltip("音乐开始前的准备时间（秒）。")]
        [SerializeField] private float _leadInSeconds = 0.5f;

        private void Start()
        {
            if (_sessionRootBehaviour == null)
            {
                throw new InvalidOperationException(
                    "DemoOneSceneInstaller 未指定 _sessionRootBehaviour。");
            }
            if (_chartAsset == null || _judgeWindowConfig == null || _heightRuleConfig == null)
            {
                throw new InvalidOperationException(
                    "DemoOneSceneInstaller 缺少 Data 资产引用。");
            }

            // 从 SessionRoot 通过反射或接口取 SessionServices。
            // 按现有架构，SessionRoot 通过一个属性暴露 SessionServices；
            // 若实际属性名不同，请按项目现状调整下面这两行。
            var sessionRootType = _sessionRootBehaviour.GetType();
            var servicesProperty = sessionRootType.GetProperty("Services");
            if (servicesProperty == null)
            {
                throw new InvalidOperationException(
                    "SessionRoot 上找不到 Services 属性。");
            }
            var services = (SessionServices)servicesProperty.GetValue(_sessionRootBehaviour);

            IEventBus eventBus = services.EventBus;
            ISceneFlow sceneFlow = services.SceneFlow;

            // 组装 Domain 与 Controller
            var chart = _chartAsset.ToDomain();
            var judgeEngine = new RhythmJudgeEngine(_judgeWindowConfig.ToDomain());
            var heightCalc = new HeightCalculator(_heightRuleConfig.ToDomain());
            var controller = new DemoOneRunController(chart, judgeEngine, heightCalc, eventBus);

            // 播放音乐并拿到起始 DSP 时间
            double scheduleDsp = AudioSettings.dspTime + _leadInSeconds;
            double actualStartDsp = _musicSource.PlayMusicAtDspTime(chart.SongId, scheduleDsp);
            var clock = new SongClockAdapter(actualStartDsp);

            // 组装表现层
            _playerVisual.Initialize(eventBus);
            _judgmentLineVisual.Initialize(eventBus);
            _uiAdapter.Initialize(
                eventBus,
                controller.State,
                onReturnToHub: () => sceneFlow.LoadHub());

            // 组装主驱动
            _runner.Initialize(controller, clock, _pulseInput);
        }
    }
}