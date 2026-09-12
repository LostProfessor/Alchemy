using System.Collections.Generic;
using System.Linq;
using Alchemy.Core.Brewing;

namespace Alchemy.Core.Runs.Rewards;

/// <summary>
/// 奖励袋模板：只预设"大小"与"稀有度分布"，内容依靠随机算法生成（策划案确认）。
/// </summary>
public sealed record RewardBagTemplate(int SlotCount, IReadOnlyList<RarityWeight> RarityWeights)
{
    /// <summary>派生一个不含指定稀有度的模板（如"不出最高稀有度"的探索奖励）。</summary>
    public RewardBagTemplate Without(IngredientRarity rarity) =>
        new(SlotCount, RarityWeights.Where(w => w.Rarity != rarity).ToList());
}

/// <summary>稀有度权重（用于随机抽取药材稀有度）。</summary>
public readonly record struct RarityWeight(IngredientRarity Rarity, int Weight);

/// <summary>常用奖励模板。</summary>
public static class RewardTemplates
{
    /// <summary>战斗奖励：3 格药材袋，常见/罕见/稀有/传奇 权重 60/30/9/1。</summary>
    public static readonly RewardBagTemplate CombatIngredientBag = new(3, new[]
    {
        new RarityWeight(IngredientRarity.Common, 60),
        new RarityWeight(IngredientRarity.Uncommon, 30),
        new RarityWeight(IngredientRarity.Rare, 9),
        new RarityWeight(IngredientRarity.Legendary, 1),
    });
}
