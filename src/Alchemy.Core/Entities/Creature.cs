using System;
using System.Collections.Generic;
using System.Linq;
using Alchemy.Core.Combat;
using Alchemy.Core.Effects;
using Alchemy.Core.Hooks;

namespace Alchemy.Core.Entities;

/// <summary>
/// 生物（玩家角色 / 敌人共用）。持有生命、格挡与效果列表；
/// 作为 IHookListener 把事件路由给自身全部效果的行为。
/// </summary>
public sealed class Creature : IHookListener
{
	public string Name { get; }

	public bool IsPlayer { get; }

	private int _maxHp;
	private int _currentHp;

	/// <summary>最大生命。用 <see cref="IncreaseMaxHp"/> / <see cref="DecreaseMaxHp"/> 修改（参考杀戮尖塔）。</summary>
	public int MaxHp
	{
		get => _maxHp;
		private set
		{
			if (_maxHp == value)
			{
				return;
			}

			int old = _maxHp;
			_maxHp = value;
			MaxHpChanged?.Invoke(old, value);
		}
	}

	/// <summary>当前生命。修改时触发 <see cref="CurrentHpChanged"/>。</summary>
	public int CurrentHp
	{
		get => _currentHp;
		set
		{
			if (_currentHp == value)
			{
				return;
			}

			int old = _currentHp;
			_currentHp = value;
			CurrentHpChanged?.Invoke(old, value);
		}
	}

	public int Block { get; set; }

	public List<AppliedEffect> Effects { get; } = new();

	/// <summary>所属战斗（未加入战斗时为 null）。</summary>
	public CombatState? Combat { get; set; }

	/// <summary>最大生命变化事件（旧值, 新值）。</summary>
	public event Action<int, int>? MaxHpChanged;

	/// <summary>当前生命变化事件（旧值, 新值）。</summary>
	public event Action<int, int>? CurrentHpChanged;

	public bool IsAlive => CurrentHp > 0;

	/// <summary>所属怪物模板 Id（敌人有值；供表现层按 Id 查美术/意图）。</summary>
	public string? TemplateId { get; }

	public Creature(string name, int maxHp, bool isPlayer = false, string? templateId = null)
	{
		Name = name;
		IsPlayer = isPlayer;
		TemplateId = templateId;
		_maxHp = maxHp;
		_currentHp = maxHp;
	}

	/// <summary>增加生命上限（参考杀戮尖塔：当前生命同步增加同样数值）。返回新上限。</summary>
	public int IncreaseMaxHp(int amount)
	{
		if (amount <= 0)
		{
			return MaxHp;
		}

		MaxHp = _maxHp + amount;
		CurrentHp = _currentHp + amount;
		return MaxHp;
	}

	/// <summary>减少生命上限（最低 1），当前生命被压到新上限。返回新上限。</summary>
	public int DecreaseMaxHp(int amount)
	{
		if (amount <= 0)
		{
			return MaxHp;
		}

		MaxHp = Math.Max(1, _maxHp - amount);
		if (CurrentHp > MaxHp)
		{
			CurrentHp = MaxHp;
		}

		return MaxHp;
	}

	/// <summary>存档恢复用：直接设置生命（不触发事件）。</summary>
	internal void RestoreHp(int maxHp, int currentHp)
	{
		_maxHp = Math.Max(1, maxHp);
		_currentHp = Math.Clamp(currentHp, 0, _maxHp);
	}

	public bool HasEffect(EffectId id) => GetEffect(id) != null;

	public AppliedEffect? GetEffect(EffectId id) => Effects.FirstOrDefault(e => e.Id == id);

	public AppliedEffect GetEffectOrThrow(EffectId id) =>
		GetEffect(id) ?? throw new InvalidOperationException($"生物 {Name} 缺少效果 {id}");

	/// <summary>
	/// 施加效果：已有则叠加层数（不超过上限）；计时类效果同时重置计时（药物依赖"续命"语义）。
	/// </summary>
	public AppliedEffect AddEffect(EffectId id, int layers)
	{
		if (layers <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(layers));
		}

		var definition = EffectRegistry.Get(id);
		var behavior = EffectBehaviors.Get(id);

		var existing = GetEffect(id);
		if (existing != null)
		{
			existing.Layers = Math.Min(definition.MaxLayers, existing.Layers + layers);
			if (behavior.IsTimed)
			{
				existing.DurationRemaining = behavior.DefaultDurationSeconds; // 重置计时
			}

			Combat?.Hooks.RaiseEffectApplied(existing, layers);
			return existing;
		}

		var effect = new AppliedEffect
		{
			Id = id,
			Owner = this,
			Layers = Math.Min(definition.MaxLayers, layers),
		};
		if (behavior.IsTimed)
		{
			effect.DurationRemaining = behavior.DefaultDurationSeconds;
		}

		Effects.Add(effect);
		Combat?.Hooks.RaiseEffectApplied(effect, layers);
		return effect;
	}

	public bool RemoveEffect(AppliedEffect effect)
	{
		if (!Effects.Remove(effect))
		{
			return false;
		}

		Combat?.Hooks.RaiseEffectRemoved(effect);
		return true;
	}

	/// <summary>涤净：按"从前到后"的顺序移除最多 maxLayers 层负面效果，返回实际移除层数。</summary>
	public int RemoveNegativeLayers(int maxLayers)
	{
		int remaining = maxLayers;
		for (int i = 0; i < Effects.Count && remaining > 0; i++)
		{
			var effect = Effects[i];
			if (EffectRegistry.Get(effect.Id).Polarity != EffectPolarity.Negative)
			{
				continue;
			}

			int take = Math.Min(effect.Layers, remaining);
			effect.Layers -= take;
			remaining -= take;
			if (effect.Layers <= 0)
			{
				Effects.RemoveAt(i);
				i--;
				Combat?.Hooks.RaiseEffectRemoved(effect);
			}
		}

		return maxLayers - remaining;
	}

	// ── 钩子路由：把事件转发给自身全部效果的行为（快照遍历，允许行为中移除效果）──

	void IHookListener.OnBeforeDamage(DamageContext c) => Route(e => EffectBehaviors.Get(e.Id).BeforeDamage?.Invoke(c, e));

	void IHookListener.OnAfterDamage(DamageContext c) => Route(e => EffectBehaviors.Get(e.Id).AfterDamage?.Invoke(c, e));

	void IHookListener.OnBeforeHeal(HealContext c) => Route(e => EffectBehaviors.Get(e.Id).BeforeHeal?.Invoke(c, e));

	void IHookListener.OnAfterHeal(HealContext c) => Route(e => EffectBehaviors.Get(e.Id).AfterHeal?.Invoke(c, e));

	void IHookListener.OnBeforeBlockGained(BlockContext c) => Route(e => EffectBehaviors.Get(e.Id).BeforeBlockGained?.Invoke(c, e));

	void IHookListener.OnAfterBlockGained(BlockContext c) => Route(e => EffectBehaviors.Get(e.Id).AfterBlockGained?.Invoke(c, e));

	void IHookListener.OnBeforeAction(ActionContext c) => Route(e => EffectBehaviors.Get(e.Id).BeforeAction?.Invoke(c, e));

	void IHookListener.OnAfterAction(ActionContext c) => Route(e => EffectBehaviors.Get(e.Id).AfterAction?.Invoke(c, e));

	void IHookListener.OnBeforeTimedAction(TimedActionContext c) => Route(e => EffectBehaviors.Get(e.Id).BeforeTimedAction?.Invoke(c, e));

	void IHookListener.OnEffectApplied(AppliedEffect effect, int addedLayers) { }

	void IHookListener.OnEffectRemoved(AppliedEffect effect) { }

	void IHookListener.OnCreatureDied(Creature creature) { }

	private void Route(Action<AppliedEffect> action)
	{
		foreach (var effect in Effects.ToList())
		{
			action(effect);
		}
	}
}
