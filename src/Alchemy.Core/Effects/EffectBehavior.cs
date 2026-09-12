using System;
using Alchemy.Core.Entities;
using Alchemy.Core.Hooks;

namespace Alchemy.Core.Effects;

/// <summary>
/// 单个效果的行为定义（无状态、可共享）：可选钩子回调 + 计时到期逻辑。
/// 具体效果在 <see cref="EffectBehaviors"/> 注册表中配置。
/// </summary>
public sealed class EffectBehavior
{
    public EffectId Id { get; init; }

    /// <summary>是否计时类效果（需要 DurationRemaining，到期触发 OnExpire）。</summary>
    public bool IsTimed { get; init; }

    /// <summary>计时类效果的默认时长（秒）。</summary>
    public float DefaultDurationSeconds { get; init; }

    /// <summary>伤害结算前（可修改 Amount）。</summary>
    public Action<DamageContext, AppliedEffect>? BeforeDamage { get; init; }

    /// <summary>伤害结算后（读 ActualDealt，可产生反弹/夹持等）。</summary>
    public Action<DamageContext, AppliedEffect>? AfterDamage { get; init; }

    /// <summary>治疗结算前。</summary>
    public Action<HealContext, AppliedEffect>? BeforeHeal { get; init; }

    /// <summary>治疗结算后。</summary>
    public Action<HealContext, AppliedEffect>? AfterHeal { get; init; }

    /// <summary>获得格挡前。</summary>
    public Action<BlockContext, AppliedEffect>? BeforeBlockGained { get; init; }

    /// <summary>获得格挡后。</summary>
    public Action<BlockContext, AppliedEffect>? AfterBlockGained { get; init; }

    /// <summary>动作开始前（致幻失败/淤伤自伤/击穿标记）。</summary>
    public Action<ActionContext, AppliedEffect>? BeforeAction { get; init; }

    /// <summary>动作结束后（祝福回血）。</summary>
    public Action<ActionContext, AppliedEffect>? AfterAction { get; init; }

    /// <summary>计时动作开始前（迟钝/专注修改时长）。</summary>
    public Action<TimedActionContext, AppliedEffect>? BeforeTimedAction { get; init; }

    /// <summary>计时到期（烈毒发作/药物依赖发作/不朽结束）。</summary>
    public Action<AppliedEffect, float>? OnExpire { get; init; }
}
