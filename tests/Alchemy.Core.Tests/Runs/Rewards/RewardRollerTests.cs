using System.Collections.Generic;
using System.Linq;
using Alchemy.Core.Brewing;
using Alchemy.Core.Combat;
using Alchemy.Core.Effects;
using Alchemy.Core.GameData;
using Alchemy.Core.Runs;
using Alchemy.Core.Runs.Rewards;
using Xunit;

namespace Alchemy.Core.Tests.Runs.Rewards;

public class RewardRollerTests
{
    private static RewardRoller NewRoller(int seed) =>
        new(new BattleRandom(seed), Ingredients.Default);

    [Fact]
    public void RollBag_RespectsSlotCount()
    {
        var bag = NewRoller(1).RollBag(RewardTemplates.CombatIngredientBag);

        Assert.Equal(3, bag.Items.Count);
        Assert.All(bag.Items, i => Assert.IsType<IngredientReward>(i));
    }

    [Fact]
    public void RollBag_SingleRarity_ProducesOnlyThatRarity()
    {
        var template = new RewardBagTemplate(5, new[]
        {
            new RarityWeight(IngredientRarity.Rare, 1),
        });
        var roller = NewRoller(3);

        var bag = roller.RollBag(template);

        foreach (var item in bag.Items.Cast<IngredientReward>())
        {
            var ing = Ingredients.Default.All.First(i => i.Id == item.IngredientId);
            Assert.Equal(IngredientRarity.Rare, ing.Rarity);
        }
    }

    [Fact]
    public void RollThree_ProducesThreeBags()
    {
        var bags = NewRoller(42).RollThree(RewardTemplates.CombatIngredientBag);

        Assert.Equal(3, bags.Count);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(7)]
    [InlineData(42)]
    [InlineData(12345)]
    [InlineData(999)]
    public void RollThree_BagsAreMutuallyDistinct_ForManySeeds(int seed)
    {
        var bags = NewRoller(seed).RollThree(RewardTemplates.CombatIngredientBag);

        Assert.False(RewardBag.ContentEquals(bags[0], bags[1]), "袋1与袋2不应完全相同");
        Assert.False(RewardBag.ContentEquals(bags[0], bags[2]), "袋1与袋3不应完全相同");
        Assert.False(RewardBag.ContentEquals(bags[1], bags[2]), "袋2与袋3不应完全相同");
    }

    [Fact]
    public void RollThree_TinyPool_StillReturnsThreeBags_BestEffort()
    {
        // 只有 2 种药材、每袋 1 格 → 最多 2 种不同组合，无法凑 3 袋不同；应兜底返回 3 袋
        var tinyCatalog = new IngredientCatalog(new List<Ingredient>
        {
            new("a", "甲", IngredientRarity.Common, new[] { AffixOp.Add(EffectId.Heal, 1) }),
            new("b", "乙", IngredientRarity.Common, new[] { AffixOp.Add(EffectId.Corrode, 1) }),
        });
        var roller = new RewardRoller(new BattleRandom(7), tinyCatalog);
        var template = new RewardBagTemplate(1, new[] { new RarityWeight(IngredientRarity.Common, 1) });

        var bags = roller.RollThree(template);

        Assert.Equal(3, bags.Count);
    }

    [Fact]
    public void ApplyTo_AddsIngredientsToPocket()
    {
        var run = new RunState();
        var bag = new RewardBag(new RewardItem[]
        {
            new IngredientReward("glowcap", 2),
            new IngredientReward("bitterroot", 1),
        });

        RewardRoller.ApplyTo(bag, run);

        Assert.Equal(2, run.Pocket.CountOf("glowcap"));
        Assert.Equal(1, run.Pocket.CountOf("bitterroot"));
        Assert.Equal(3, run.Pocket.TotalCount);
    }

    [Fact]
    public void ApplyTo_AddsCurrency()
    {
        var run = new RunState();
        var bag = new RewardBag(new RewardItem[] { new CurrencyReward(25) });

        RewardRoller.ApplyTo(bag, run);

        Assert.Equal(25, run.Currency);
    }

    [Fact]
    public void RewardBag_ContentEquals_IgnoresOrder()
    {
        var a = new RewardBag(new RewardItem[] { new IngredientReward("x"), new IngredientReward("y") });
        var b = new RewardBag(new RewardItem[] { new IngredientReward("y"), new IngredientReward("x") });

        Assert.True(RewardBag.ContentEquals(a, b));
    }

    [Fact]
    public void RewardBag_ContentEquals_DifferentCounts_AreDifferent()
    {
        var a = new RewardBag(new RewardItem[] { new IngredientReward("x") });
        var b = new RewardBag(new RewardItem[] { new IngredientReward("x"), new IngredientReward("x") });

        Assert.False(RewardBag.ContentEquals(a, b));
    }
}
