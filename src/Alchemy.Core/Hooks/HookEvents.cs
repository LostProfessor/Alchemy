using System;
using System.Collections.Generic;
using Alchemy.Core.Entities;

namespace Alchemy.Core.Hooks;

/// <summary>伤害事件上下文（BeforeDamage 中可修改 Amount 形成修饰链，AfterDamage 中读 ActualDealt）。</summary>
public sealed class DamageContext
{
	/// <summary>伤害来源（烈毒等无来源时为 null）。</summary>
	public Creature? Source { get; }

	public Creature Target { get; }

	public int BaseAmount { get; }

	/// <summary>结算伤害值（钩子可修改）。</summary>
	public int Amount { get; set; }

	/// <summary>是否为"直接操作"造成的伤害（精确适用）。</summary>
	public bool IsDirect { get; set; }

	/// <summary>无视护甲（击穿）。</summary>
	public bool IgnoreBlock { get; set; }

	/// <summary>无视护甲（烈毒等），且不经过格挡。</summary>
	public bool Unblockable { get; set; }

	/// <summary>被格挡吸收的量（AfterDamage 可读）。</summary>
	public int BlockAbsorbed { get; set; }

	/// <summary>实际造成的生命损失（AfterDamage 可读）。</summary>
	public int ActualDealt { get; set; }

	public DamageContext(Creature? source, Creature target, int baseAmount)
	{
		Source = source;
		Target = target;
		BaseAmount = baseAmount;
		Amount = baseAmount;
	}
}

/// <summary>治疗事件上下文。</summary>
public sealed class HealContext
{
	public Creature Target { get; }

	public int BaseAmount { get; }

	public int Amount { get; set; }

	public HealContext(Creature target, int baseAmount)
	{
		Target = target;
		BaseAmount = baseAmount;
		Amount = baseAmount;
	}
}

/// <summary>获得格挡事件上下文。</summary>
public sealed class BlockContext
{
	public Creature Target { get; }

	public int BaseAmount { get; }

	public int Amount { get; set; }

	public BlockContext(Creature target, int baseAmount)
	{
		Target = target;
		BaseAmount = baseAmount;
		Amount = baseAmount;
	}
}

/// <summary>
/// 动作事件上下文（炼药、投掷药水、敌人行动等）。
/// BeforeAction 中可标记 Cancelled（致幻失败）或 IgnoresArmor（击穿）。
/// </summary>
public sealed class ActionContext
{
	public Creature Actor { get; }

	public string ActionType { get; }

	/// <summary>动作是否被取消（致幻失败 → 药剂未正常使用、敌人动作未进行）。</summary>
	public bool Cancelled { get; set; }

	/// <summary>本次动作是否无视护甲（击穿）。</summary>
	public bool IgnoresArmor { get; set; }

	public ActionContext(Creature actor, string actionType)
	{
		Actor = actor;
		ActionType = actionType;
	}
}

/// <summary>
/// 计时动作上下文（敌人行动/炼药操作等需要耗时的动作）。
/// BeforeTimedAction 中可修改 Duration（迟钝/专注），并设置最小时长下限。
/// </summary>
public sealed class TimedActionContext
{
	public Creature Actor { get; }

	public string Id { get; }

	public float BaseDuration { get; }

	/// <summary>可被修饰的时长（最终取 Max(MinDuration, Duration)）。</summary>
	public float Duration { get; set; }

	/// <summary>时长下限（默认 1 秒，避免被减到 0 以下）。</summary>
	public float MinDuration { get; set; } = 1f;

	public TimedActionContext(Creature actor, string id, float baseDuration)
	{
		Actor = actor;
		Id = id;
		BaseDuration = baseDuration;
		Duration = baseDuration;
	}
}
