using System;
using System.Collections.Generic;
using System.Linq;
using Alchemy.Core.Entities;
using Alchemy.Core.GameData;
using Alchemy.Core.Random;
using Alchemy.Core.Relics;
using Alchemy.Core.Runs;

namespace Alchemy.Core.Rooms.Events;

/// <summary>
/// 事件动作执行器：把事件选项的动作序列映射到现有的运行/玩家方法上。
/// 这样事件内容可以在资源文件（数据）里定义，执行逻辑只需一套。
/// </summary>
public static class EventExecutor
{
    public static void Apply(RunState run, Creature player, IRandomSource rng, IEnumerable<EventAction> actions)
    {
        foreach (var action in actions)
        {
            ApplyOne(run, player, rng, action);
        }
    }

    private static void ApplyOne(RunState run, Creature player, IRandomSource rng, EventAction action)
    {
        switch (action.Type)
        {
            case EventActionType.Damage:
                player.CurrentHp = Math.Max(0, player.CurrentHp - action.Amount);
                break;
            case EventActionType.Heal:
                player.CurrentHp = Math.Min(player.MaxHp, player.CurrentHp + action.Amount);
                break;
            case EventActionType.GainMaxHp:
                player.IncreaseMaxHp(action.Amount);
                break;
            case EventActionType.LoseMaxHp:
                player.DecreaseMaxHp(action.Amount);
                break;
            case EventActionType.GainBlock:
                player.Block += Math.Max(0, action.Amount);
                break;
            case EventActionType.GainCurrency:
                run.Currency += Math.Max(0, action.Amount);
                break;
            case EventActionType.LoseCurrency:
                run.Currency = Math.Max(0, run.Currency - Math.Max(0, action.Amount));
                break;
            case EventActionType.GainIngredient:
                for (int i = 0; i < action.Count; i++)
                {
                    run.Pocket.Add(Ingredients.Default.Pick(rng, action.IngredientRarity).Id);
                }

                break;
            case EventActionType.LoseIngredient:
                LoseRandomIngredients(run, rng, action.Count);
                break;
            case EventActionType.GainRelic:
            {
                // 首领遗物只能在首领战奖励中获得；事件即使数据写了 Boss 也降级为任意非首领
                var rarity = action.RelicRarity == RelicRarity.Boss ? (RelicRarity?)null : action.RelicRarity;
                var relic = RelicCatalog.CreateRandom(rng, run.Relics.Relics.Select(r => r.Id), rarity);
                if (relic != null)
                {
                    run.Relics.Add(relic);
                }

                break;
            }

            case EventActionType.LoseRelic:
            {
                var owned = run.Relics.Relics.ToList();
                if (owned.Count > 0)
                {
                    run.Relics.Remove(owned[rng.Next(owned.Count)]);
                }

                break;
            }
        }
    }

    private static void LoseRandomIngredients(RunState run, IRandomSource rng, int count)
    {
        var ids = run.Pocket.Counts.Keys.ToList();
        for (int i = 0; i < count && ids.Count > 0; i++)
        {
            var id = ids[rng.Next(ids.Count)];
            run.Pocket.TryConsume(id);
            if (!run.Pocket.Contains(id))
            {
                ids.Remove(id);
            }
        }
    }
}
