using Alchemy.Core.Brewing;
using Alchemy.Core.Effects;
using Alchemy.Core.GameData;
using Alchemy.Core.Hooks;
using Xunit;

namespace Alchemy.Core.Tests.Effects;

/// <summary>药水施加：瞬时结算 / 持续效果 / 抗药性 / 加深·消解 / 涤净。</summary>
public class PotionApplicationTests
{
    private static Potion NewPotion(BrewingMode mode = BrewingMode.Queue) => new(BaseLiquids.ForMode(mode));

    [Fact]
    public void ApplyPotion_InstantEffects_HealBlockDamage()
    {
        var (combat, player, enemy) = CombatFixture.Create();
        var potion = NewPotion();
        var engine = new BrewingEngine(BrewingMode.Queue);
        engine.Apply(new Ingredient("mix", "混合", IngredientRarity.Common,
            new[] { AffixOp.Add(EffectId.Corrode, 3) }), potion);

        combat.ApplyPotion(potion, enemy, source: player);

        Assert.Equal(27, enemy.CurrentHp); // 腐蚀 3
        Assert.Equal(30, player.CurrentHp); // 玩家不受影响
    }

    [Fact]
    public void ApplyPotion_SelfDrink_HealApplies()
    {
        var (combat, player, _) = CombatFixture.Create();
        combat.DealDamage(new DamageContext(null, player, 10));
        var potion = NewPotion();
        new BrewingEngine(BrewingMode.Queue).Apply(
            new Ingredient("mix", "混合", IngredientRarity.Common, new[] { AffixOp.Add(EffectId.Heal, 4) }), potion);

        combat.ApplyPotion(potion, player);

        Assert.Equal(24, player.CurrentHp);
    }

    [Fact]
    public void ApplyPotion_AppliesStatusEffects()
    {
        var (combat, player, enemy) = CombatFixture.Create();
        var potion = NewPotion();
        new BrewingEngine(BrewingMode.Queue).Apply(
            new Ingredient("mix", "混合", IngredientRarity.Common,
                new[] { AffixOp.Add(EffectId.IronSkin, 2), AffixOp.Add(EffectId.Vulnerable, 3) }), potion);

        combat.ApplyPotion(potion, enemy, source: player);

        Assert.Equal(2, enemy.Block);
        Assert.Equal(3, enemy.GetEffectOrThrow(EffectId.Vulnerable).Layers);
    }

    [Fact]
    public void Resistance_BlocksAllPotionEffects_AndDecrements()
    {
        var (combat, player, enemy) = CombatFixture.Create();
        enemy.AddEffect(EffectId.Resistance, 3);
        var potion = NewPotion();
        new BrewingEngine(BrewingMode.Queue).Apply(
            new Ingredient("mix", "混合", IngredientRarity.Common,
                new[] { AffixOp.Add(EffectId.Corrode, 2), AffixOp.Add(EffectId.Vulnerable, 1) }), potion);

        combat.ApplyPotion(potion, enemy, source: player);

        // 两种不同效果 → 抗药性 3-2=1；无任何效果被施加
        Assert.Equal(1, enemy.GetEffectOrThrow(EffectId.Resistance).Layers);
        Assert.Equal(30, enemy.CurrentHp);
        Assert.Equal(0, enemy.Block);
        Assert.False(enemy.HasEffect(EffectId.Vulnerable));
    }

    [Fact]
    public void Deepen_AddsOneLayer_Dissolve_SubtractsOne()
    {
        var (combat, _, enemy) = CombatFixture.Create();
        enemy.AddEffect(EffectId.Deepen, 1);
        var potion = NewPotion();
        new BrewingEngine(BrewingMode.Queue).Apply(
            new Ingredient("mix", "混合", IngredientRarity.Common, new[] { AffixOp.Add(EffectId.Vulnerable, 2) }), potion);

        combat.ApplyPotion(potion, enemy);

        Assert.Equal(3, enemy.GetEffectOrThrow(EffectId.Vulnerable).Layers); // 2+1

        // 换一个有消解的目标
        var (combat2, _, enemy2) = CombatFixture.Create();
        enemy2.AddEffect(EffectId.Dissolve, 1);
        combat2.ApplyPotion(potion, enemy2);
        Assert.Equal(1, enemy2.GetEffectOrThrow(EffectId.Vulnerable).Layers); // 2-1
    }

    [Fact]
    public void Purify_RemovesNegativeEffects_FrontToBack()
    {
        var (combat, player, enemy) = CombatFixture.Create();
        enemy.AddEffect(EffectId.Vulnerable, 2);
        enemy.AddEffect(EffectId.Hallucinate, 1);
        var potion = NewPotion();
        new BrewingEngine(BrewingMode.Queue).Apply(
            new Ingredient("cleanse", "净水", IngredientRarity.Common, new[] { AffixOp.Add(EffectId.Purify, 2) }), potion);

        combat.ApplyPotion(potion, enemy, source: player);

        // 从前到后：2 层额度先清掉易感(2)，致幻(1)保留
        Assert.False(enemy.HasEffect(EffectId.Vulnerable));
        Assert.True(enemy.HasEffect(EffectId.Hallucinate));
        Assert.Equal(1, enemy.GetEffectOrThrow(EffectId.Hallucinate).Layers);
    }

    [Fact]
    public void Dependency_RefreshViaPotion_ResetsTimer()
    {
        var (combat, player, enemy) = CombatFixture.Create();
        enemy.AddEffect(EffectId.Dependency, 2);
        combat.AdvanceTime(3f); // 还剩 2 秒

        var potion = NewPotion();
        new BrewingEngine(BrewingMode.Queue).Apply(
            new Ingredient("addict", "瘾剂", IngredientRarity.Rare, new[] { AffixOp.Add(EffectId.Dependency, 3) }), potion);
        combat.ApplyPotion(potion, enemy, source: player);

        var dependency = enemy.GetEffectOrThrow(EffectId.Dependency);
        Assert.Equal(5, dependency.Layers); // 2+3
        Assert.Equal(EffectTimings.DependencySeconds, dependency.DurationRemaining!.Value, precision: 3);
    }
}
