using Alchemy.Core.Combat;
using Alchemy.Core.Effects;
using Alchemy.Core.Entities;
using Alchemy.Core.Hooks;
using Xunit;

namespace Alchemy.Core.Tests.Entities;

public class CreatureTests
{
    [Fact]
    public void TakeDamage_ReducesHp()
    {
        var (combat, player, enemy) = CombatFixture.Create();

        var result = combat.DealDamage(new DamageContext(player, enemy, 10));

        Assert.Equal(10, result.ActualDamage);
        Assert.Equal(0, result.BlockAbsorbed);
        Assert.Equal(20, enemy.CurrentHp);
        Assert.True(enemy.IsAlive);
    }

    [Fact]
    public void TakeDamage_BlockAbsorbsFirst()
    {
        var (combat, player, enemy) = CombatFixture.Create();
        combat.GainBlock(enemy, 7);

        var result = combat.DealDamage(new DamageContext(player, enemy, 10));

        Assert.Equal(3, result.ActualDamage);
        Assert.Equal(7, result.BlockAbsorbed);
        Assert.Equal(0, enemy.Block);
        Assert.Equal(27, enemy.CurrentHp);
    }

    [Fact]
    public void Damage_OverflowBlock_DamagesHp()
    {
        var (combat, player, enemy) = CombatFixture.Create();
        combat.GainBlock(enemy, 7);

        combat.DealDamage(new DamageContext(player, enemy, 4));

        Assert.Equal(3, enemy.Block);
        Assert.Equal(30, enemy.CurrentHp);
    }

    [Fact]
    public void Heal_ClampsToMaxHp()
    {
        var (combat, player, enemy) = CombatFixture.Create();
        combat.DealDamage(new DamageContext(player, enemy, 10));

        combat.Heal(enemy, 100);

        Assert.Equal(enemy.MaxHp, enemy.CurrentHp);
    }

    [Fact]
    public void Death_WhenHpReachesZero()
    {
        var (combat, player, enemy) = CombatFixture.Create();

        combat.DealDamage(new DamageContext(player, enemy, 30));

        Assert.Equal(0, enemy.CurrentHp);
        Assert.False(enemy.IsAlive);
    }

    [Fact]
    public void AddEffect_StacksUpToMaxLayers()
    {
        var (_, _, enemy) = CombatFixture.Create();

        enemy.AddEffect(EffectId.Vulnerable, 3);
        enemy.AddEffect(EffectId.Vulnerable, 4);

        var vulnerable = enemy.GetEffectOrThrow(EffectId.Vulnerable);
        Assert.Equal(5, vulnerable.Layers); // 上限 5
    }

    [Fact]
    public void RemoveNegativeLayers_FrontToBack_RemovesOnlyNegative()
    {
        var (_, _, enemy) = CombatFixture.Create();
        enemy.AddEffect(EffectId.Vulnerable, 2);  // 负面
        enemy.AddEffect(EffectId.Immortal, 1);    // 正面
        enemy.AddEffect(EffectId.Hallucinate, 3); // 负面

        int removed = enemy.RemoveNegativeLayers(4);

        Assert.Equal(4, removed);
        Assert.False(enemy.HasEffect(EffectId.Vulnerable));              // 负面被移除 2
        Assert.True(enemy.HasEffect(EffectId.Immortal));                 // 正面不受影响
        Assert.Equal(1, enemy.GetEffectOrThrow(EffectId.Hallucinate).Layers); // 剩 2 层额度扣到致幻上
    }

    // ── 生命上限修改（参考杀戮尖塔）──

    [Fact]
    public void IncreaseMaxHp_SyncsCurrentHp()
    {
        var creature = new Creature("玩家", 30, isPlayer: true);
        creature.CurrentHp = 10;

        int newMax = creature.IncreaseMaxHp(5);

        Assert.Equal(35, newMax);
        Assert.Equal(35, creature.MaxHp);
        Assert.Equal(15, creature.CurrentHp); // 当前生命同步 +5
    }

    [Fact]
    public void DecreaseMaxHp_ClampsCurrentHp()
    {
        var creature = new Creature("玩家", 30, isPlayer: true);
        creature.CurrentHp = 25;

        creature.DecreaseMaxHp(10); // 上限 30→20

        Assert.Equal(20, creature.MaxHp);
        Assert.Equal(20, creature.CurrentHp); // 被压到新上限
    }

    [Fact]
    public void DecreaseMaxHp_NotBelowOne()
    {
        var creature = new Creature("玩家", 10, isPlayer: true);

        creature.DecreaseMaxHp(100);

        Assert.Equal(1, creature.MaxHp);
        Assert.Equal(1, creature.CurrentHp);
    }

    [Fact]
    public void HpChanges_FireEvents()
    {
        var creature = new Creature("玩家", 30, isPlayer: true);
        int maxChanged = 0;
        int curChanged = 0;
        creature.MaxHpChanged += (_, _) => maxChanged++;
        creature.CurrentHpChanged += (_, _) => curChanged++;

        creature.IncreaseMaxHp(5);
        creature.CurrentHp = 20;

        Assert.Equal(1, maxChanged);
        Assert.Equal(2, curChanged); // 增加上限同步 + 手动设置
    }
}
