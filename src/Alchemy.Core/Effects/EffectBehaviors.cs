using System;
using System.Collections.Generic;
using Alchemy.Core.Combat;
using Alchemy.Core.Entities;
using Alchemy.Core.Hooks;

namespace Alchemy.Core.Effects;

/// <summary>
/// 效果行为注册表：把 14 种药水效果 + 6 种非药水效果的行为逐一配置。
/// 行为按钩子触发（BeforeDamage/AfterDamage/BeforeAction/AfterAction/OnExpire）。
/// ⚠️ 迟钝/专注影响"计时操作时长"，属于战斗计时系统（阶段 3），此处仅占位。
/// </summary>
public static class EffectBehaviors
{
    private static readonly Dictionary<EffectId, EffectBehavior> _behaviors = Build();

    public static EffectBehavior Get(EffectId id) => _behaviors[id];

    private static Dictionary<EffectId, EffectBehavior> Build()
    {
        var map = new Dictionary<EffectId, EffectBehavior>();

        // ── 瞬时类（无持续行为；施加时在 PotionApplication 里即时结算）──
        Register(map, new EffectBehavior { Id = EffectId.Heal });
        Register(map, new EffectBehavior { Id = EffectId.IronSkin });
        Register(map, new EffectBehavior { Id = EffectId.Corrode });
        Register(map, new EffectBehavior { Id = EffectId.Purify });

        // ── 易感：下一次受到的伤害 +x，触发后消耗一层 ──
        Register(map, new EffectBehavior
        {
            Id = EffectId.Vulnerable,
            BeforeDamage = (ctx, e) =>
            {
                if (e.Owner != ctx.Target)
                {
                    return;
                }

                ctx.Amount += e.Layers;
                ConsumeLayer(e);
            },
        });

        // ── 坚韧：受到所有来源伤害 -1（不消耗）──
        Register(map, new EffectBehavior
        {
            Id = EffectId.Toughness,
            BeforeDamage = (ctx, e) =>
            {
                if (e.Owner != ctx.Target)
                {
                    return;
                }

                ctx.Amount = Math.Max(0, ctx.Amount - 1);
            },
        });

        // ── 精确：下次直接操作造成的伤害 +x，触发后消耗一层 ──
        Register(map, new EffectBehavior
        {
            Id = EffectId.Precise,
            BeforeDamage = (ctx, e) =>
            {
                if (!ctx.IsDirect || e.Owner != ctx.Source)
                {
                    return;
                }

                ctx.Amount += e.Layers;
                ConsumeLayer(e);
            },
        });

        // ── 击穿：下 x 次操作无视护甲（动作开始时消耗一层并标记）──
        Register(map, new EffectBehavior
        {
            Id = EffectId.Penetration,
            BeforeAction = (ctx, e) =>
            {
                if (e.Owner != ctx.Actor || e.Layers <= 0)
                {
                    return;
                }

                ctx.IgnoresArmor = true;
                ConsumeLayer(e);
            },
        });

        // ── 棘皮：受到伤害时反弹等量伤害（最高不超过 x 点），一次性 ──
        Register(map, new EffectBehavior
        {
            Id = EffectId.Thorny,
            AfterDamage = (ctx, e) =>
            {
                if (e.Owner != ctx.Target || ctx.Source == null)
                {
                    return;
                }

                int reflect = Math.Min(ctx.ActualDealt, e.Layers);
                if (reflect > 0)
                {
                    ctx.Source.Combat?.DealDamage(new DamageContext(ctx.Target, ctx.Source, reflect));
                }

                ctx.Target.RemoveEffect(e);
            },
        });

        // ── 致幻：下一次动作有 x*10% 概率失败（消耗一层）──
        Register(map, new EffectBehavior
        {
            Id = EffectId.Hallucinate,
            BeforeAction = (ctx, e) =>
            {
                if (e.Owner != ctx.Actor)
                {
                    return;
                }

                var combat = ctx.Actor.Combat;
                if (combat != null && combat.Random.Chance(e.Layers * 0.10))
                {
                    ctx.Cancelled = true;
                }

                ConsumeLayer(e);
            },
        });

        // ── 淤伤：下一次行动时受到 x 伤害，然后层数 -1 ──
        Register(map, new EffectBehavior
        {
            Id = EffectId.Bruise,
            BeforeAction = (ctx, e) =>
            {
                if (e.Owner != ctx.Actor)
                {
                    return;
                }

                ctx.Actor.Combat?.DealDamage(new DamageContext(ctx.Actor, ctx.Actor, e.Layers));
                ConsumeLayer(e);
            },
        });

        // ── 祝福：每次进行操作时，回复 1 点生命值 ──
        Register(map, new EffectBehavior
        {
            Id = EffectId.Blessing,
            AfterAction = (ctx, e) =>
            {
                if (e.Owner != ctx.Actor)
                {
                    return;
                }

                ctx.Actor.Combat?.Heal(ctx.Actor, 1);
            },
        });

        // ── 不朽：一段时间内生命值不会少于 x 点（AfterDamage 中夹持）──
        Register(map, new EffectBehavior
        {
            Id = EffectId.Immortal,
            IsTimed = true,
            DefaultDurationSeconds = EffectTimings.ImmortalSeconds,
            AfterDamage = (ctx, e) =>
            {
                if (e.Owner != ctx.Target)
                {
                    return;
                }

                int floor = e.Layers;
                if (ctx.Target.CurrentHp < floor)
                {
                    ctx.Target.CurrentHp = floor;
                }
            },
            OnExpire = (e, delta) => e.Owner.RemoveEffect(e),
        });

        // ── 烈毒：一段时间后造成 2x 伤害（无视护甲），随后移除 ──
        Register(map, new EffectBehavior
        {
            Id = EffectId.VirulentPoison,
            IsTimed = true,
            DefaultDurationSeconds = EffectTimings.VirulentPoisonSeconds,
            OnExpire = (e, delta) =>
            {
                e.DurationRemaining = null;
                e.Owner.Combat?.DealDamage(new DamageContext(null, e.Owner, 2 * e.Layers) { Unblockable = true });
                e.Owner.RemoveEffect(e);
            },
        });

        // ── 药物依赖：发作时受到 3x 伤害并移去一层；若仍有层数则重新计时 ──
        Register(map, new EffectBehavior
        {
            Id = EffectId.Dependency,
            IsTimed = true,
            DefaultDurationSeconds = EffectTimings.DependencySeconds,
            OnExpire = (e, delta) =>
            {
                e.DurationRemaining = null;
                e.Owner.Combat?.DealDamage(new DamageContext(null, e.Owner, 3 * e.Layers) { Unblockable = true });
                e.Layers -= 1;
                if (e.Layers <= 0)
                {
                    e.Owner.RemoveEffect(e);
                }
                else
                {
                    e.DurationRemaining = EffectTimings.DependencySeconds; // 重新计时
                }
            },
        });

        // ── 抗药性/加深/消解：无持续钩子，逻辑在 PotionApplication ──
        Register(map, new EffectBehavior { Id = EffectId.Resistance });
        Register(map, new EffectBehavior { Id = EffectId.Deepen });
        Register(map, new EffectBehavior { Id = EffectId.Dissolve });

        // ── 迟钝：下一次需要计时操作所需时间 +2x 秒（对玩家作用减半 → 玩家 +x）──
        Register(map, new EffectBehavior
        {
            Id = EffectId.Sluggish,
            BeforeTimedAction = (ctx, e) =>
            {
                if (e.Owner != ctx.Actor)
                {
                    return;
                }

                int bonus = ctx.Actor.IsPlayer ? e.Layers : e.Layers * 2;
                ctx.Duration += bonus;
                ConsumeLayer(e);
            },
        });

        // ── 专注：下一次需要计时操作所需时间 -2x 秒（对玩家减半 → 玩家 -x，最低 1 秒）──
        Register(map, new EffectBehavior
        {
            Id = EffectId.Focus,
            BeforeTimedAction = (ctx, e) =>
            {
                if (e.Owner != ctx.Actor)
                {
                    return;
                }

                int reduction = ctx.Actor.IsPlayer ? e.Layers : e.Layers * 2;
                ctx.Duration = Math.Max(ctx.MinDuration, ctx.Duration - reduction);
                ConsumeLayer(e);
            },
        });

        return map;
    }

    private static void Register(Dictionary<EffectId, EffectBehavior> map, EffectBehavior behavior) =>
        map.Add(behavior.Id, behavior);

    private static void ConsumeLayer(AppliedEffect effect)
    {
        effect.Layers -= 1;
        if (effect.Layers <= 0)
        {
            effect.Owner.RemoveEffect(effect);
        }
    }
}
