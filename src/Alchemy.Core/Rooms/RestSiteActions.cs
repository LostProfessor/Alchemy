using System;
using System.Linq;
using Alchemy.Core.Brewing;
using Alchemy.Core.Entities;
using Alchemy.Core.GameData;
using Alchemy.Core.Random;
using Alchemy.Core.Relics;
using Alchemy.Core.Runs;
using Alchemy.Core.Runs.Rewards;

namespace Alchemy.Core.Rooms;

/// <summary>火堆休息房的两个选项逻辑。</summary>
public static class RestSiteActions
{
    /// <summary>睡觉回复比例：已损失生命（最大-当前）的 30%。</summary>
    public const float SleepHealFraction = 0.3f;

    /// <summary>睡觉基础回复量 = 已损失生命的 30%（向下取整）。</summary>
    public static int CalculateSleepHeal(Creature player)
    {
        int lost = player.MaxHp - player.CurrentHp;
        return (int)(lost * SleepHealFraction);
    }

    /// <summary>睡觉：回复已损失生命的 30%，可被遗物（IRestHealModifier）修饰，不超上限。</summary>
    public static void Sleep(RunState run, Creature player, Func<int, int>? healModifier = null)
    {
        int heal = CalculateSleepHeal(player);
        if (healModifier != null)
        {
            heal = healModifier(heal);
        }

        heal = Math.Max(0, heal);
        player.CurrentHp = Math.Min(player.MaxHp, player.CurrentHp + heal);
    }

    /// <summary>探索：随机获得一个普通（Common）遗物（不重复已有）+ 一次不含传奇的药材奖励。</summary>
    public static void Explore(RunState run, IRandomSource rng, IngredientCatalog catalog)
    {
        var relic = RelicCatalog.CreateRandomCommon(rng, run.Relics.Relics.Select(r => r.Id));
        if (relic != null)
        {
            run.Relics.Add(relic);
        }

        var template = RewardTemplates.CombatIngredientBag.Without(IngredientRarity.Legendary);
        var bag = new RewardRoller(rng, catalog).RollBag(template);
        RewardRoller.ApplyTo(bag, run);
    }
}
