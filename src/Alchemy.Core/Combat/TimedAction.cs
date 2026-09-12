using System;
using Alchemy.Core.Entities;

namespace Alchemy.Core.Combat;

/// <summary>
/// 一次正在倒计时的战斗动作（敌人行动/炼药操作等）。
/// 由 CombatState.AdvanceTime(delta) 统一推进；时长已被迟钝/专注等修饰过。
/// </summary>
public sealed class TimedAction
{
    public string Id { get; }

    public Creature? Actor { get; }

    /// <summary>被修饰后的总时长（秒）。</summary>
    public float Duration { get; }

    /// <summary>预兆提前量（秒）：在动作执行前这个时刻触发一次 ActionTelegraphed（0=不触发）。</summary>
    public float TelegraphSeconds { get; }

    /// <summary>是否已发出预兆（保证每个动作最多触发一次）。</summary>
    internal bool Telegraphed { get; set; }

    public float Remaining { get; internal set; }

    /// <summary>到点触发；参数为所属战斗。</summary>
    public Action<CombatState> OnComplete { get; }

    public TimedAction(string id, Creature? actor, float duration, Action<CombatState> onComplete, float telegraphSeconds = 0f)
    {
        Id = id;
        Actor = actor;
        Duration = duration;
        Remaining = duration;
        OnComplete = onComplete;
        TelegraphSeconds = Math.Max(0f, telegraphSeconds);
    }
}
