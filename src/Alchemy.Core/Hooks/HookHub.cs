using System;
using System.Collections.Generic;
using Alchemy.Core.Combat;
using Alchemy.Core.Entities;

namespace Alchemy.Core.Hooks;

/// <summary>
/// 钩子中枢：中央广播游戏事件给所有注册监听者（生物/遗物/其他）。
/// 触发顺序 = 注册顺序（对"触发先后影响结果"的规则至关重要）。
/// 与 Godot 信号的分工：这里管逻辑层事件；Godot 信号管表现层（UI/输入）。
/// </summary>
public sealed class HookHub
{
    private readonly List<IHookListener> _listeners = new();

    public void AddListener(IHookListener listener) => _listeners.Add(listener);

    public void RemoveListener(IHookListener listener) => _listeners.Remove(listener);

    public void RaiseBeforeDamage(DamageContext c) => ForEach(l => l.OnBeforeDamage(c));

    public void RaiseAfterDamage(DamageContext c) => ForEach(l => l.OnAfterDamage(c));

    public void RaiseBeforeHeal(HealContext c) => ForEach(l => l.OnBeforeHeal(c));

    public void RaiseAfterHeal(HealContext c) => ForEach(l => l.OnAfterHeal(c));

    public void RaiseBeforeBlockGained(BlockContext c) => ForEach(l => l.OnBeforeBlockGained(c));

    public void RaiseAfterBlockGained(BlockContext c) => ForEach(l => l.OnAfterBlockGained(c));

    public void RaiseBeforeAction(ActionContext c) => ForEach(l => l.OnBeforeAction(c));

    public void RaiseAfterAction(ActionContext c) => ForEach(l => l.OnAfterAction(c));

    public void RaiseBeforeTimedAction(TimedActionContext c) => ForEach(l => l.OnBeforeTimedAction(c));

    public void RaiseEffectApplied(AppliedEffect effect, int addedLayers) => ForEach(l => l.OnEffectApplied(effect, addedLayers));

    public void RaiseEffectRemoved(AppliedEffect effect) => ForEach(l => l.OnEffectRemoved(effect));

    public void RaiseCreatureDied(Creature creature) => ForEach(l => l.OnCreatureDied(creature));

    public void RaiseCombatStart(CombatState combat) => ForEach(l => l.OnCombatStart(combat));

    private void ForEach(Action<IHookListener> action)
    {
        foreach (var listener in _listeners)
        {
            action(listener);
        }
    }
}
