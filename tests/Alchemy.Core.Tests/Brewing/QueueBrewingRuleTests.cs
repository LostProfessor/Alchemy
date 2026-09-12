using System.Linq;
using Alchemy.Core.Brewing;
using Alchemy.Core.Effects;
using Alchemy.Core.GameData;
using Xunit;

namespace Alchemy.Core.Tests.Brewing;

public class QueueBrewingRuleTests
{
    private static Potion NewPotion() => new(BaseLiquids.Aqua);

    [Fact]
    public void Add_AppendsEntry()
    {
        var potion = NewPotion();
        var rule = QueueBrewingRule.Instance;

        rule.Execute(AffixOp.Add(EffectId.Heal, 2), potion);
        rule.Execute(AffixOp.Add(EffectId.Corrode, 1), potion);

        // 每层一格：2 级恢复 = 2 格 + 1 腐蚀 = 3 格
        Assert.Equal(3, potion.Count);
        Assert.Equal(EffectId.Heal, potion.Entries[0].Effect);
        Assert.Equal(1, potion.Entries[0].Layers);
        Assert.Equal(EffectId.Heal, potion.Entries[1].Effect);
        Assert.Equal(EffectId.Corrode, potion.Entries[2].Effect);
    }

    [Fact]
    public void Overflow_EvictsOldest()
    {
        var potion = NewPotion();
        var rule = QueueBrewingRule.Instance;
        var ids = new[]
        {
            EffectId.Heal, EffectId.IronSkin, EffectId.Corrode,
            EffectId.VirulentPoison, EffectId.Sluggish, EffectId.Focus,
        };

        foreach (var id in ids)
        {
            rule.Execute(AffixOp.Add(id), potion);
        }

        // 6 个加入，最旧的 Heal 被挤出，剩 5 个，队首变为 IronSkin
        Assert.Equal(Potion.MaxSlots, potion.Count);
        Assert.DoesNotContain(potion.Entries, e => e.Effect == EffectId.Heal);
        Assert.Equal(EffectId.IronSkin, potion.Entries[0].Effect);
    }

    [Fact]
    public void RemoveEffect_RemovesLayers_Partial()
    {
        var potion = NewPotion();
        var rule = QueueBrewingRule.Instance;
        rule.Execute(AffixOp.Add(EffectId.Heal, 2), potion);
        rule.Execute(AffixOp.Add(EffectId.Corrode, 1), potion);

        var result = rule.Execute(AffixOp.Remove(EffectId.Heal, 1), potion);

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.AffectedCount);
        Assert.Equal(1, potion.Entries[0].Layers);
        Assert.Equal(2, potion.Count);
    }

    [Fact]
    public void RemoveEffect_RemovesEntryWhenZero()
    {
        var potion = NewPotion();
        var rule = QueueBrewingRule.Instance;
        rule.Execute(AffixOp.Add(EffectId.Heal, 2), potion);

        rule.Execute(AffixOp.Remove(EffectId.Heal, 2), potion);

        Assert.Empty(potion.Entries);
    }

    [Fact]
    public void RemoveLast_RemovesFromBack()
    {
        var potion = NewPotion();
        var rule = QueueBrewingRule.Instance;
        rule.Execute(AffixOp.Add(EffectId.Heal), potion);
        rule.Execute(AffixOp.Add(EffectId.Corrode), potion);
        rule.Execute(AffixOp.Add(EffectId.Focus), potion);

        var result = rule.Execute(AffixOp.RemoveLast(2), potion);

        Assert.Equal(2, result.AffectedCount);
        Assert.Single(potion.Entries);
        Assert.Equal(EffectId.Heal, potion.Entries[0].Effect);
    }

    [Fact]
    public void ReverseOrder_FlipsEntries()
    {
        var potion = NewPotion();
        var rule = QueueBrewingRule.Instance;
        rule.Execute(AffixOp.Add(EffectId.Heal), potion);
        rule.Execute(AffixOp.Add(EffectId.Corrode), potion);
        rule.Execute(AffixOp.Add(EffectId.Focus), potion);

        rule.Execute(AffixOp.ReverseOrder(), potion);

        Assert.Equal(EffectId.Focus, potion.Entries[0].Effect);
        Assert.Equal(EffectId.Corrode, potion.Entries[1].Effect);
        Assert.Equal(EffectId.Heal, potion.Entries[2].Effect);
    }

    [Fact]
    public void ReversePolarity_SwapsToOpposite_NoCancellation()
    {
        var potion = NewPotion();
        var rule = QueueBrewingRule.Instance;
        rule.Execute(AffixOp.Add(EffectId.Heal, 2), potion);
        rule.Execute(AffixOp.Add(EffectId.Corrode, 1), potion);

        rule.Execute(AffixOp.ReversePolarity(), potion);

        // 2 个恢复 + 1 个腐蚀，反极性 → 2 个腐蚀 + 1 个恢复。每层一格、不抵消。
        Assert.Equal(3, potion.Count);
        Assert.Equal(1, potion.Entries.Count(e => e.Effect == EffectId.Heal));
        Assert.Equal(2, potion.Entries.Count(e => e.Effect == EffectId.Corrode));
        Assert.All(potion.Entries, e => Assert.Equal(1, e.Layers)); // 每层一格，层数恒为 1
    }
}
