using Alchemy.Core.Combat;
using Alchemy.Core.Entities;

namespace Alchemy.Core.Hooks;

/// <summary>
/// 钩子监听者。游戏逻辑事件（伤害/治疗/格挡/动作/效果生命周期）通过 HookHub 广播，
/// 生物、遗物等实现此接口并注册即可监听（顺序 = 注册顺序，确定性）。
/// 使用 C# 默认接口方法，未覆写的钩子自动忽略。
/// </summary>
public interface IHookListener
{
	void OnBeforeDamage(DamageContext c) { }

	void OnAfterDamage(DamageContext c) { }

	void OnBeforeHeal(HealContext c) { }

	void OnAfterHeal(HealContext c) { }

	void OnBeforeBlockGained(BlockContext c) { }

	void OnAfterBlockGained(BlockContext c) { }

	void OnBeforeAction(ActionContext c) { }

	void OnAfterAction(ActionContext c) { }

	/// <summary>计时动作开始前（迟钝/专注在此修改时长并消耗层数）。</summary>
	void OnBeforeTimedAction(TimedActionContext c) { }

	void OnEffectApplied(AppliedEffect effect, int addedLayers) { }

	void OnEffectRemoved(AppliedEffect effect) { }

	void OnCreatureDied(Creature creature) { }

	/// <summary>战斗开始（遗物常在此时给玩家发开场增益）。</summary>
	void OnCombatStart(CombatState combat) { }
}
