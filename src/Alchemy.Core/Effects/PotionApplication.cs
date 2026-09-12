using System;
using System.Collections.Generic;
using Alchemy.Core.Brewing;
using Alchemy.Core.Combat;
using Alchemy.Core.Entities;
using Alchemy.Core.Hooks;

namespace Alchemy.Core.Effects;

/// <summary>
/// 把一瓶药水施加到目标生物（"泼出去/喝下去"的一刻）：
/// 抗药性拦截 → 涤净即时清除负面 → 加深/消解调整层数 → 瞬时类即时结算 → 其余挂为效果。
/// </summary>
public static class PotionApplication
{
    public static void Apply(CombatState combat, Potion potion, Creature target, Creature? source = null)
    {
        var layersByEffect = potion.AggregateLayers();

        // ── 抗药性：无法受到所有药水效果影响；每种不同药水效果使抗药性 -1 ──
        var resistance = target.GetEffect(EffectId.Resistance);
        if (resistance != null)
        {
            int distinct = layersByEffect.Count;
            int removed = Math.Min(resistance.Layers, distinct);
            resistance.Layers -= removed;
            if (resistance.Layers <= 0)
            {
                target.RemoveEffect(resistance);
            }

            return;
        }

        foreach (var (effectId, layers) in layersByEffect)
        {
            var definition = EffectRegistry.Get(effectId);
            if (!definition.CanComeFromPotion)
            {
                continue; // 防御：非药水效果不来自药水
            }

            // ── 涤净：按从前到后移除负面效果 ──
            if (effectId == EffectId.Purify)
            {
                target.RemoveNegativeLayers(layers);
                continue;
            }

            // ── 加深/消解：调整施加层数 ──
            int adjusted = layers;
            if (target.HasEffect(EffectId.Deepen))
            {
                adjusted += 1;
            }

            if (target.HasEffect(EffectId.Dissolve))
            {
                adjusted -= 1;
            }

            if (adjusted <= 0)
            {
                continue;
            }

            // ── 瞬时类：立即结算 ──
            switch (effectId)
            {
                case EffectId.Heal:
                    combat.Heal(target, adjusted);
                    continue;
                case EffectId.IronSkin:
                    combat.GainBlock(target, adjusted);
                    continue;
                case EffectId.Corrode:
                    combat.DealDamage(new DamageContext(source, target, adjusted));
                    continue;
            }

            // ── 其余为持续效果：挂到生物身上（药物依赖在此"续命"）──
            target.AddEffect(effectId, adjusted);
        }
    }
}
