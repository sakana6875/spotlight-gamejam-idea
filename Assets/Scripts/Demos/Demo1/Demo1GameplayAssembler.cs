using UnityEngine;
using Spotlight.Application.Services;
using Spotlight.Demos.Demo1.Adapters;
using Spotlight.Demos.Demo1.Application;
using Spotlight.Demos.Demo1.Data;
using Spotlight.Demos.Demo1.Domain;
using Spotlight.Demos.Demo1.Domain.Rhythm;

namespace Spotlight.Demos.Demo1
{
    public sealed class Demo1GameplayAssembler : MonoBehaviour
    {
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

        [Header("Config")]
        [SerializeField] private float _leadInSeconds = 0.5f;

        private IEventBus _localEventBus;

        private void Start()
        {
            if (_chartAsset == null || _judgeWindowConfig == null || _heightRuleConfig == null)
            {
                Debug.LogError("[Demo1GameplayAssembler] 缺少 Data 资产引用，无法启动玩法！", this);
                return;
            }

            // 使用 Demo1 内部极简事件总线，彻底解耦
            _localEventBus = new Demo1LocalEventBus();

            var chart = _chartAsset.ToDomain();
            var judgeEngine = new RhythmJudgeEngine(_judgeWindowConfig.ToDomain());
            var heightCalc = new HeightCalculator(_heightRuleConfig.ToDomain());

            var controller = new DemoOneRunController(chart, judgeEngine, heightCalc, _localEventBus);

            _playerVisual.Initialize(_localEventBus);
            _judgmentLineVisual.Initialize(_localEventBus);
            _uiAdapter.Initialize(_localEventBus, controller.State, onReturnToHub: () => {
                Debug.Log("[Demo1GameplayAssembler] UI 点击了返回 Hub。");
            });

            double scheduleDsp = AudioSettings.dspTime + _leadInSeconds;
            double actualStartDsp = _musicSource.PlayMusicAtDspTime(chart.SongId, scheduleDsp);
            var clock = new SongClockAdapter(actualStartDsp);

            _runner.Initialize(controller, clock, _pulseInput);

            Debug.Log("[Demo1GameplayAssembler] Demo1 玩法组装完毕，开始播放音乐。");
        }
    }
}