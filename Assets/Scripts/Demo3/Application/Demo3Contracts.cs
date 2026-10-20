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
    /// 本 Demo 运行期需要跨幕携带的纯数据：石碑点亮记录（影响结局判定）。
    /// 只存稳定 ID，不存任何 Unity 对象引用。
    /// 正式接入全局架构后，"石碑点亮"应迁移为 ProgressManager 的永久收集物，本端口随之删除。
    /// </summary>
    public interface IDemo3RunFlags
    {
        bool IsLit(string tabletId);
        void MarkLit(string tabletId);
    }

    // ============ 跨场景进度端口（场景进入计数 + 隐藏区域访问记录）============
    /// <summary>
    /// 结局分支与场景变体用的全局参数（本次运行累计；只存字符串 ID 与计数，不存 Unity 引用）：
    ///  - EntryCount：进入某场景的次数——"根据进入次数区分现在进行到哪一幕"
    ///    （如 Act2 第 1 次=营救、第 2 次=追寻；大树/结局场景按次数推进剧情）；
    ///  - HasVisitedHidden：是否进入过隐藏区域（L1/L2 隐藏场景）——触发不同结局；
    ///  - LastSceneId：来源场景——场景变体按"从哪来"切换物体 失活/出现。
    /// 实现：跨场景载体（DontDestroyOnLoad）。正式接入全局架构后迁移到 ProgressManager。
    /// </summary>
    public interface IDemo3Progress
    {
        string CurrentSceneId { get; }          // 当前场景（Build Settings 名）
        string LastSceneId { get; }             // 上一个场景（没有则 null）
        int EntryCount(string sceneId);         // 进入某场景的次数（含当前这次）
        bool HasVisitedHidden(string hiddenId); // 是否进入过隐藏区域（如 "L1"）
        void MarkHiddenVisited(string hiddenId);

        /// <summary>记录"离开某场景去隐藏区"时的站位与行走范围（返回时原地复活，如回到 L1 平台上）</summary>
        void SetReturnPoint(string sceneId, Vector2 pos, Rect walkArea);
        /// <summary>取走该场景的返回点（取出即消费，只生效一次）；没有记录返回 false</summary>
        bool TryConsumeReturnPoint(string sceneId, out Vector2 pos, out Rect walkArea);
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
        IDemo3Progress Progress { get; }
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
