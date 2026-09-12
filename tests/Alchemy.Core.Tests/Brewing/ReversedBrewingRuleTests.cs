using Alchemy.Core.Brewing;
using Alchemy.Core.Effects;
using Alchemy.Core.GameData;
using Xunit;

namespace Alchemy.Core.Tests.Brewing;

public class ReversedBrewingRuleTests
{
    private static Potion NewPotion() => new(BaseLiquids.Turbid);

    [Fact]
    public void Add_BecomesRemove()
    {
        var potion = NewPotion();
        // 先手工塞入 2 层"恢复"作为已被添加的效果
        potion.Entries.Add(new EffectEntry(EffectId.Heal, 2));

        var rule = ReversedBrewingRule.Instance;
        // 反转模式下 Add(Heal,1) → Remove(Heal,1)
        rule.Execute(AffixOp.Add(EffectId.Heal, 1), potion);

        Assert.Single(potion.Entries);
        Assert.Equal(1, potion.Entries[0].Layers);
    }

    [Fact]
    public void Remove_BecomesAdd()
    {
        var potion = NewPotion();
        var rule = ReversedBrewingRule.Instance;

        // 反转模式下 Remove(Heal,1) → Add(Heal,1)
        rule.Execute(AffixOp.Remove(EffectId.Heal, 1), potion);

        Assert.Single(potion.Entries);
        Assert.Equal(EffectId.Heal, potion.Entries[0].Effect);
        Assert.Equal(1, potion.Entries[0].Layers);
    }

    [Fact]
    public void RemoveLast_Unaffected()
    {
        var potion = NewPotion();
        potion.Entries.Add(new EffectEntry(EffectId.Heal, 1));
        potion.Entries.Add(new EffectEntry(EffectId.Corrode, 1));

        var rule = ReversedBrewingRule.Instance;
        var result = rule.Execute(AffixOp.RemoveLast(1), potion);

        Assert.True(result.Succeeded);
        Assert.Single(potion.Entries);
        Assert.Equal(EffectId.Heal, potion.Entries[0].Effect);
    }

    [Fact]
    public void ReversePolarity_UnaffectedByMode()
    {
        var potion = NewPotion();
        potion.Entries.Add(new EffectEntry(EffectId.Heal, 2));

        var rule = ReversedBrewingRule.Instance;
        rule.Execute(AffixOp.ReversePolarity(), potion);

        Assert.Equal(EffectId.Corrode, potion.Entries[0].Effect);
        Assert.Equal(2, potion.Entries[0].Layers);
    }
}
