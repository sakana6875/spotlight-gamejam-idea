using System;
using System.Collections.Generic;
using Healing.Demo3.Domain;
using UnityEngine;

namespace Healing.Demo3.Application
{
    // ============ 与全局架构对齐的运行结果契约 ============
    // 若全局 Contracts 中已存在同名定义，删除此处并改用全局版本。

    public enum DemoRunResult { Succeeded = 0, Failed = 1, Abandoned = 2 }

    /// <summary>Demo 只报告结果；成功后的流程由 DemoFlowManager 负责</summary>
    public interface IDemoRun
    {
        void Report(DemoRunResult result);
    }

    // ============ 场景切换端口 ============
    /// <summary>
    /// 幕与幕之间的场景切换端口（等价于架构文档的 ISceneFlow）。
    /// Gameplay 只调用此端口，永不直接调用 SceneManager；
    /// 唯一包装 SceneManager 的实现是适配层的 DemoSceneFlowAdapter。
    /// </summary>
    public interface IDemoSceneFlow
    {
        void GoTo(string sceneId);
    }

    // ============ 跨场景运行旗标端口 ============
    /// <summary>
    /// 本 Demo 运行期需要跨幕携带的纯数据：石碑点亮记录、旧脚印。
    /// 只存稳定 ID 与纯数据，不存任何 Unity 对象引用。
    /// 正式接入全局架构后，"石碑点亮"应迁移为 ProgressManager 的永久收集物，
    /// 旧脚印归入检查点临时状态，本端口随之删除。
    /// </summary>
    public interface IDemo3RunFlags
    {
        bool IsLit(string tabletId);
        void MarkLit(string tabletId);
        IReadOnlyList<TrailMark> GetOldMarks(string areaId);
        void SaveOldMarks(string areaId, List<TrailMark> marks);
    }

    // ============ Demo3 的音频端口（连续混音）============
    public interface IDemo3Audio
    {
        void EnterAmbience();
        void ApplyMix(in Demo3AudioMix mix);
        void ExitAmbience();
    }

    /// <summary>SessionRoot / 跨场景载体注入给各幕场景的服务集合</summary>
    public interface IDemo3Services
    {
        IInputService Input { get; }
        IEventBus Bus { get; }
        IDemoRun Run { get; }
        IDemo3Audio Audio { get; }
        IDemoSceneFlow Flow { get; }
        IDemo3RunFlags Flags { get; }
    }
}

// ============================================================
// 以下为全局契约的最小定义，仅为让 Demo3 可独立编译。
// 若项目全局 Contracts 已有 IInputService / IEventBus，请删除本段。
// ============================================================
namespace Healing.Demo3.Application
{
    /// <summary>统一输入接口（玩法只依赖它，不读设备）</summary>
    public interface IInputService
    {
        Vector2 Move { get; }
        event Action InteractPressed;  // E：场景交互（石碑等）
        event Action JumpPressed;      // 空格：交互式跳跃
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
