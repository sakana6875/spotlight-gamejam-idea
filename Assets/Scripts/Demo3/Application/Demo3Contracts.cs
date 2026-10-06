using System;
using Healing.Demo3.Domain;
using UnityEngine;

namespace Healing.Demo3.Application
{
    // ============ 与全局架构对齐的运行结果契约 ============
    // 若全局 Contracts 中已存在 DemoRunResult / IDemoRun，删除此处定义并改用全局版本。

    public enum DemoRunResult { Succeeded = 0, Failed = 1, Abandoned = 2 }

    /// <summary>Demo 只报告结果；成功后的流程由 DemoFlowManager 负责</summary>
    public interface IDemoRun
    {
        void Report(DemoRunResult result);
    }

    // ============ Demo3 的音频端口 ============
    /// <summary>
    /// Demo3 的持续环境音端口（循环音量的交叉淡化）。
    /// 一次性音效仍走全局 IAudioService.PlaySfx(audioCue)；
    /// 本端口只覆盖"人群嘈杂 / 脚步 / 心跳"这类需要连续混音的场景。
    /// 由音频适配层（Demo3AmbienceAudioAdapter）实现，Gameplay 只面向此接口。
    /// </summary>
    public interface IDemo3Audio
    {
        void EnterAmbience();                 // 进入本幕，启动循环组（音量从 0 开始）
        void ApplyMix(in Demo3AudioMix mix);  // 每帧应用混合比例
        void ExitAmbience();                  // 离开本幕，停止循环组
    }

    /// <summary>SessionRoot 注入给 Demo3 场景的服务集合（场景级组合根入参）</summary>
    public interface IDemo3Services
    {
        IInputService Input { get; }
        IEventBus Bus { get; }
        IDemoRun Run { get; }
        IDemo3Audio Audio { get; }
    }
}

// ============================================================
// 以下为全局契约的最小定义，仅为让 Demo3 可独立编译。
// 若项目全局 Contracts 已有 IInputService / IEventBus，请删除本段，改用全局定义。
// ============================================================
namespace Healing.Demo3.Application
{
    /// <summary>统一输入接口（玩法只依赖它，不碰 Keyboard.current / Input.GetKey）</summary>
    public interface IInputService
    {
        Vector2 Move { get; }
        event Action InteractPressed;
        event Action PulsePressed;
        event Action SwitchModePressed;
        event Action PausePressed;
    }

    /// <summary>类型化事件总线：只负责订阅/发布/退订，不做业务判断</summary>
    public interface IEventBus
    {
        void Publish<T>(T evt);
        IDisposable Subscribe<T>(Action<T> handler);
        void Unsubscribe<T>(Action<T> handler);
    }
}
