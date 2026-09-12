using System.Linq;
using Alchemy.Core.Combat;
using Alchemy.Core.Entities;
using Alchemy.Core.Hooks;

namespace Alchemy.Core.Relics;

/// <summary>
/// 遗物基类：整局持久的增益来源。
/// - 被动遗物：覆写 <see cref="IHookListener"/> 钩子，监听相应事件（策划案"被动遗物监听相应的事件"）。
/// - 主动遗物：实现 <see cref="CanActivate"/>/<see cref="Activate"/>，由玩家主动发动。
/// 遗物随整局存活（跨战斗），战斗开始时经 CombatState.AttachRelics 挂接到钩子中枢。
/// </summary>
public abstract class Relic : IHookListener
{
	public string Id { get; }

	public string DisplayName { get; }

	public RelicRarity Rarity { get; }

	/// <summary>是否具有主动技能。</summary>
	public bool HasActive { get; }

	protected Relic(string id, string displayName, RelicRarity rarity, bool hasActive = false)
	{
		Id = id;
		DisplayName = displayName;
		Rarity = rarity;
		HasActive = hasActive;
	}

	/// <summary>主动技能当前是否可用（充能/次数/冷却等）。</summary>
	public virtual bool CanActivate(CombatState combat, Creature user) => false;

	/// <summary>发动主动技能；返回是否成功发动。</summary>
	public virtual bool Activate(CombatState combat, Creature user) => false;

	/// <summary>取战斗中的首个玩家生物（无则 null）。</summary>
	protected static Creature? PlayerOf(CombatState combat) =>
		combat.AllCreatures.FirstOrDefault(c => c.IsPlayer);

	// ── 接口钩子：显式实现并转发到虚方法（子类用 override 覆写）────────
	void IHookListener.OnBeforeDamage(DamageContext c) => OnBeforeDamage(c);
	void IHookListener.OnAfterDamage(DamageContext c) => OnAfterDamage(c);
	void IHookListener.OnBeforeHeal(HealContext c) => OnBeforeHeal(c);
	void IHookListener.OnAfterHeal(HealContext c) => OnAfterHeal(c);
	void IHookListener.OnBeforeBlockGained(BlockContext c) => OnBeforeBlockGained(c);
	void IHookListener.OnAfterBlockGained(BlockContext c) => OnAfterBlockGained(c);
	void IHookListener.OnBeforeAction(ActionContext c) => OnBeforeAction(c);
	void IHookListener.OnAfterAction(ActionContext c) => OnAfterAction(c);
	void IHookListener.OnBeforeTimedAction(TimedActionContext c) => OnBeforeTimedAction(c);
	void IHookListener.OnEffectApplied(AppliedEffect e, int addedLayers) => OnEffectApplied(e, addedLayers);
	void IHookListener.OnEffectRemoved(AppliedEffect e) => OnEffectRemoved(e);
	void IHookListener.OnCreatureDied(Creature c) => OnCreatureDied(c);
	void IHookListener.OnCombatStart(CombatState c) => OnCombatStart(c);

	// ── 可覆写钩子（默认空实现）──
	protected virtual void OnBeforeDamage(DamageContext c) { }
	protected virtual void OnAfterDamage(DamageContext c) { }
	protected virtual void OnBeforeHeal(HealContext c) { }
	protected virtual void OnAfterHeal(HealContext c) { }
	protected virtual void OnBeforeBlockGained(BlockContext c) { }
	protected virtual void OnAfterBlockGained(BlockContext c) { }
	protected virtual void OnBeforeAction(ActionContext c) { }
	protected virtual void OnAfterAction(ActionContext c) { }
	protected virtual void OnBeforeTimedAction(TimedActionContext c) { }
	protected virtual void OnEffectApplied(AppliedEffect e, int addedLayers) { }
	protected virtual void OnEffectRemoved(AppliedEffect e) { }
	protected virtual void OnCreatureDied(Creature c) { }
	protected virtual void OnCombatStart(CombatState combat) { }
}
