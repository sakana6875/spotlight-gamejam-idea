using System;
using UnityEngine;
using Spotlight.Demos.Demo1.Adapters;
using Spotlight.Demos.Demo1.Application;
using Spotlight.Demos.Demo1.Data;
using Spotlight.Demos.Demo1.Domain;
using Spotlight.Demos.Demo1.Domain.Chart;
using Spotlight.Demos.Demo1.Domain.Rhythm;

namespace Spotlight.Demos.Demo1
{
    /// <summary>
    /// 使用场景配置和组合根服务装配一局 Demo1。
    /// </summary>
    public sealed class Demo1GameplayAssembler : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private DemoOneChartAsset _chartAsset;
        [SerializeField] private JudgeWindowConfigAsset _judgeWindowConfig;
        [SerializeField] private HeightRuleConfigAsset _heightRuleConfig;
        [SerializeField] private AudioClip _bgmClip;

        [Header("Adapters")]
        [SerializeField] private PulseInputAdapter _pulseInput;
        [SerializeField] private DemoOneRunnerBehaviour _runner;
        [SerializeField] private PlayerVisualAdapter _playerVisual;
        [SerializeField] private JudgmentLineVisual _judgmentLineVisual;
        [SerializeField] private DemoOneUIAdapter _uiAdapter;

        [Header("Config")]
        [SerializeField] private float _leadInSeconds = 0.5f;

        public bool IsInitialized { get; private set; }

        /// <summary>
        /// 创建本局规则对象并以实际 DSP 起点启动歌曲时钟。
        /// </summary>
        public bool Initialize(IMusicPlaybackService musicPlaybackService, Action<int> onReturn)
        {
            if (IsInitialized)
            {
                return true;
            }

            if (musicPlaybackService == null || onReturn == null)
            {
                Debug.LogError("Demo1 玩法初始化失败：缺少音乐服务或返回操作。", this);
                return false;
            }

            if (_chartAsset == null
                || _judgeWindowConfig == null
                || _heightRuleConfig == null
                || _bgmClip == null)
            {
                Debug.LogError("Demo1 玩法初始化失败：缺少谱面、规则或 BGM 配置。", this);
                return false;
            }

            if (_pulseInput == null
                || _runner == null
                || _playerVisual == null
                || _judgmentLineVisual == null
                || _uiAdapter == null)
            {
                Debug.LogError("Demo1 玩法初始化失败：缺少场景适配器引用。", this);
                return false;
            }

            try
            {
                Demo1EventBus eventBus = new Demo1EventBus();
                ChartData chart = _chartAsset.ToDomain();
                RhythmJudgeEngine judgeEngine = new RhythmJudgeEngine(_judgeWindowConfig.ToDomain());
                HeightCalculator heightCalculator = new HeightCalculator(_heightRuleConfig.ToDomain());
                DemoOneRunController controller = new DemoOneRunController(
                    chart,
                    judgeEngine,
                    heightCalculator,
                    eventBus);

                int finalHeight = 0;
                eventBus.Subscribe<DemoOneCompletedEvent>(completed => finalHeight = completed.FinalHeight);
                _playerVisual.Initialize(eventBus);
                _judgmentLineVisual.Initialize(eventBus);
                _uiAdapter.Initialize(eventBus, controller.State, () => onReturn(finalHeight));

                double scheduledDspTime = AudioSettings.dspTime + _leadInSeconds;
                double actualStartDspTime = musicPlaybackService.PlayMusicAtDspTime(
                    _bgmClip,
                    scheduledDspTime);
                _runner.Initialize(
                    controller,
                    new SongClockAdapter(actualStartDspTime),
                    _pulseInput);

                IsInitialized = true;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError("Demo1 玩法初始化失败：" + exception.Message, this);
                return false;
            }
        }
    }
}
