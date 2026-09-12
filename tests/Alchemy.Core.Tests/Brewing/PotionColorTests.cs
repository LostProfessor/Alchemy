using Alchemy.Core.Brewing;
using Alchemy.Core.Effects;
using Alchemy.Core.GameData;
using Xunit;

namespace Alchemy.Core.Tests.Brewing;

public class PotionColorTests
{
    [Fact]
    public void EmptyPotion_UsesBaseColor()
    {
        var potion = new Potion(BaseLiquids.Aqua);

        Assert.Equal(new PotionColor(180, 220, 255), potion.Color);
    }

    [Fact]
    public void Effects_ShiftColorFromBase()
    {
        var potion = new Potion(BaseLiquids.Aqua);
        var engine = new BrewingEngine(BrewingMode.Queue);
        var ingredient = new Ingredient("glowcap", "萤光菇", IngredientRarity.Common,
            new[] { AffixOp.Add(EffectId.Heal, 1) });

        engine.Apply(ingredient, potion);

        // 清水(180,220,255) + 恢复增量(0,15,5) → (180,235,255)
        Assert.Equal(new PotionColor(180, 235, 255), potion.Color);
    }

    [Fact]
    public void Color_ClampsHighAt255()
    {
        var potion = new Potion(BaseLiquids.Aqua);
        var engine = new BrewingEngine(BrewingMode.Queue);
        // 5 层腐蚀（每层一格，填满 5 格）→ 增量 5×(22,-8,-8)，R 溢出 290 → clamp 255
        var ops = new AffixOp[Potion.MaxSlots];
        for (int i = 0; i < ops.Length; i++)
        {
            ops[i] = AffixOp.Add(EffectId.Corrode, 1);
        }

        engine.Apply(new Ingredient("acid", "强酸", IngredientRarity.Rare, ops), potion);

        Assert.Equal(255, potion.Color.R);
        Assert.Equal(180, potion.Color.G);
        Assert.Equal(215, potion.Color.B);
    }

    [Fact]
    public void Color_AccumulatesLayers()
    {
        var potion = new Potion(BaseLiquids.Aqua);
        var engine = new BrewingEngine(BrewingMode.Queue);
        // 5 层迟钝（每层一格）→ 增量 5×(-8,-8,6)，B 溢出 285 → clamp 255
        var ops = new AffixOp[Potion.MaxSlots];
        for (int i = 0; i < ops.Length; i++)
        {
            ops[i] = AffixOp.Add(EffectId.Sluggish, 1);
        }

        engine.Apply(new Ingredient("sludge", "泥垢", IngredientRarity.Common, ops), potion);

        Assert.Equal(140, potion.Color.R);
        Assert.Equal(180, potion.Color.G);
        Assert.Equal(255, potion.Color.B);
    }

    [Fact]
    public void AggregateLayers_SumsPerEffect()
    {
        var potion = new Potion(BaseLiquids.Aqua);
        var rule = QueueBrewingRule.Instance;
        rule.Execute(AffixOp.Add(EffectId.Heal, 2), potion);
        rule.Execute(AffixOp.Add(EffectId.Corrode, 1), potion);
        rule.Execute(AffixOp.Add(EffectId.Heal, 1), potion);

        var layers = potion.AggregateLayers();

        Assert.Equal(3, layers[EffectId.Heal]);
        Assert.Equal(1, layers[EffectId.Corrode]);
    }
}
