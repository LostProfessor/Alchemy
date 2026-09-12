using Alchemy.Core.Brewing;
using Alchemy.Core.Effects;
using Alchemy.Core.GameData;
using Xunit;

namespace Alchemy.Core.Tests.Brewing;

public class StackBrewingRuleTests
{
    private static Potion NewPotion() => new(BaseLiquids.Oil);

    [Fact]
    public void Add_FillsUntilMax()
    {
        var potion = NewPotion();
        var rule = StackBrewingRule.Instance;

        // 每层占一格：5 次各加 1 层填满
        for (int i = 0; i < Potion.MaxSlots; i++)
        {
            var result = rule.Execute(AffixOp.Add(EffectId.Heal), potion);
            Assert.True(result.Succeeded);
        }

        Assert.Equal(Potion.MaxSlots, potion.Count);
        Assert.All(potion.Entries, e => Assert.Equal(1, e.Layers));
    }

    [Fact]
    public void Add_MultiLayer_RejectsWhenNotEnoughSlots()
    {
        var potion = NewPotion();
        var rule = StackBrewingRule.Instance;
        rule.Execute(AffixOp.Add(EffectId.Heal), potion); // 1 格
        rule.Execute(AffixOp.Add(EffectId.Heal), potion); // 2 格

        // 还剩 3 格，加 4 层应整体拒绝、不产生部分添加
        var result = rule.Execute(AffixOp.Add(EffectId.Heal, 4), potion);
        Assert.False(result.Succeeded);
        Assert.Equal(2, potion.Count);
    }

    [Fact]
    public void Add_WhenFull_Rejects()
    {
        var potion = NewPotion();
        var rule = StackBrewingRule.Instance;

        for (int i = 0; i < Potion.MaxSlots; i++)
        {
            rule.Execute(AffixOp.Add(EffectId.Heal), potion);
        }

        var result = rule.Execute(AffixOp.Add(EffectId.Corrode), potion);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.FailureReason);
        Assert.Equal(Potion.MaxSlots, potion.Count);
    }

    [Fact]
    public void Remove_FreesSpace_ThenAddSucceeds()
    {
        var potion = NewPotion();
        var rule = StackBrewingRule.Instance;

        for (int i = 0; i < Potion.MaxSlots; i++)
        {
            rule.Execute(AffixOp.Add(EffectId.Heal), potion);
        }

        rule.Execute(AffixOp.Remove(EffectId.Heal, 1), potion);
        var result = rule.Execute(AffixOp.Add(EffectId.Corrode), potion);

        Assert.True(result.Succeeded);
        Assert.Equal(Potion.MaxSlots, potion.Count);
        Assert.Contains(potion.Entries, e => e.Effect == EffectId.Corrode);
    }

    [Fact]
    public void RemoveLast_PopsTop()
    {
        var potion = NewPotion();
        var rule = StackBrewingRule.Instance;
        rule.Execute(AffixOp.Add(EffectId.Heal), potion);
        rule.Execute(AffixOp.Add(EffectId.Corrode), potion);
        rule.Execute(AffixOp.Add(EffectId.Focus), potion); // 栈顶

        var result = rule.Execute(AffixOp.RemoveLast(1), potion);

        Assert.Equal(1, result.AffectedCount);
        Assert.Equal(2, potion.Count);
        Assert.Equal(EffectId.Corrode, potion.Entries[^1].Effect);
    }

    [Fact]
    public void Engine_RecordsStackFullFailure()
    {
        var potion = NewPotion();
        var engine = new BrewingEngine(BrewingMode.Stack);
        var ingredient = new Ingredient("overflow", "溢流", IngredientRarity.Common, new[]
        {
            AffixOp.Add(EffectId.Heal), AffixOp.Add(EffectId.IronSkin),
            AffixOp.Add(EffectId.Corrode), AffixOp.Add(EffectId.VirulentPoison),
            AffixOp.Add(EffectId.Sluggish), AffixOp.Add(EffectId.Focus), // 第 6 个被拒
        });

        var result = engine.Apply(ingredient, potion);

        Assert.False(result.AllSucceeded);
        Assert.Single(result.FailureReasons);
        Assert.Equal(Potion.MaxSlots, potion.Count);
    }
}
