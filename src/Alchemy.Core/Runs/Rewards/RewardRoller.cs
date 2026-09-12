using System;
using System.Collections.Generic;
using System.Linq;
using Alchemy.Core.Brewing;
using Alchemy.Core.GameData;
using Alchemy.Core.Random;

namespace Alchemy.Core.Runs.Rewards;

/// <summary>
/// 奖励生成器：按模板随机生成奖励袋；"三选一"时保证三袋内容不完全一致（设计硬性要求）。
/// </summary>
public sealed class RewardRoller
{
    private const int MaxDistinctAttempts = 30;

    private readonly IRandomSource _rng;
    private readonly IngredientCatalog _catalog;

    public RewardRoller(IRandomSource rng, IngredientCatalog catalog)
    {
        _rng = rng;
        _catalog = catalog;
    }

    /// <summary>按模板随机生成一个奖励袋。</summary>
    public RewardBag RollBag(RewardBagTemplate template)
    {
        var items = new List<RewardItem>(template.SlotCount);
        for (int i = 0; i < template.SlotCount; i++)
        {
            var rarity = RollRarity(template.RarityWeights);
            var ingredient = _catalog.Pick(_rng, rarity);
            items.Add(new IngredientReward(ingredient.Id));
        }

        return new RewardBag(items);
    }

    /// <summary>
    /// 生成三个奖励袋，两两内容不完全一致。
    /// 若药材池过小实在凑不出 3 种不同组合（极端情况），做尽力而为兜底。
    /// </summary>
    public IReadOnlyList<RewardBag> RollThree(RewardBagTemplate template)
    {
        var bags = new List<RewardBag>();
        int attempts = 0;
        while (bags.Count < 3 && attempts < MaxDistinctAttempts)
        {
            attempts++;
            var bag = RollBag(template);
            if (bags.Any(b => RewardBag.ContentEquals(b, bag)))
            {
                continue; // 与已有袋完全相同 → 重 roll
            }

            bags.Add(bag);
        }

        // 兜底：仍不足 3 袋时补齐
        while (bags.Count < 3)
        {
            bags.Add(RollBag(template));
        }

        return bags;
    }

    /// <summary>把玩家选中的奖励袋内容写入整局状态（药材入口袋、货币累加）。</summary>
    public static void ApplyTo(RewardBag bag, RunState run)
    {
        foreach (var item in bag.Items)
        {
            switch (item)
            {
                case IngredientReward ir:
                    run.Pocket.Add(ir.IngredientId, ir.Count);
                    break;
                case CurrencyReward cr:
                    run.Currency += cr.Amount;
                    break;
            }
        }
    }

    private IngredientRarity RollRarity(IReadOnlyList<RarityWeight> weights)
    {
        int total = weights.Sum(w => w.Weight);
        if (total <= 0)
        {
            throw new InvalidOperationException("稀有度权重总和必须大于 0");
        }

        int roll = _rng.Next(total);
        int acc = 0;
        foreach (var w in weights)
        {
            acc += w.Weight;
            if (roll < acc)
            {
                return w.Rarity;
            }
        }

        return weights[^1].Rarity;
    }
}
