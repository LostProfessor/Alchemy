using Alchemy.Core.Combat;
using Alchemy.Core.Effects;
using Alchemy.Core.Hooks;
using Xunit;

namespace Alchemy.Core.Tests.Effects;

/// <summary>伤害相关效果：易感/坚韧/精确/击穿/不朽/棘皮。</summary>
public class DamageModifierEffectTests
{
    [Fact]
    public void Vulnerable_IncreasesDamage_ThenConsumesLayer()
    {
        var (combat, player, enemy) = CombatFixture.Create();
        enemy.AddEffect(EffectId.Vulnerable, 2);

        combat.DealDamage(new DamageContext(player, enemy, 5));

        Assert.Equal(23, enemy.CurrentHp); // 30 - (5+2)
        Assert.Equal(1, enemy.GetEffectOrThrow(EffectId.Vulnerable).Layers);

        combat.DealDamage(new DamageContext(player, enemy, 5));

        Assert.Equal(17, enemy.CurrentHp); // 23 - (5+1) = 17
        Assert.False(enemy.HasEffect(EffectId.Vulnerable));
    }

    [Fact]
    public void Toughness_ReducesDamageByOne_AlwaysActive()
    {
        var (combat, player, enemy) = CombatFixture.Create();
        enemy.AddEffect(EffectId.Toughness, 1);

        combat.DealDamage(new DamageContext(player, enemy, 5));

        Assert.Equal(26, enemy.CurrentHp);
        Assert.True(enemy.HasEffect(EffectId.Toughness)); // 不消耗
    }

    [Fact]
    public void Toughness_ReducesToOne_WhenDamageIsOne()
    {
        var (combat, player, enemy) = CombatFixture.Create();
        enemy.AddEffect(EffectId.Toughness, 1);

        combat.DealDamage(new DamageContext(player, enemy, 1));

        Assert.Equal(30, enemy.CurrentHp); // 1-1=0，无伤害
    }

    [Fact]
    public void Precise_AddsDamage_OnlyOnDirectAttack()
    {
        var (combat, player, enemy) = CombatFixture.Create();
        player.AddEffect(EffectId.Precise, 2);

        // 非直接操作（如药水持续伤害）不触发
        combat.DealDamage(new DamageContext(player, enemy, 5) { IsDirect = false });
        Assert.Equal(25, enemy.CurrentHp);
        Assert.Equal(2, player.GetEffectOrThrow(EffectId.Precise).Layers);

        // 直接操作触发 +2，消耗一层
        combat.DealDamage(new DamageContext(player, enemy, 5) { IsDirect = true });
        Assert.Equal(18, enemy.CurrentHp); // 25 - (5+2)
        Assert.Equal(1, player.GetEffectOrThrow(EffectId.Precise).Layers);
    }

    [Fact]
    public void Penetration_IgnoresBlock_ConsumesPerAction()
    {
        var (combat, player, enemy) = CombatFixture.Create();
        player.AddEffect(EffectId.Penetration, 2);
        combat.GainBlock(enemy, 10);

        var action = combat.BeginAction(player, "use_potion");
        Assert.True(action.IgnoresArmor);

        combat.DealDamage(new DamageContext(player, enemy, 5) { IgnoreBlock = action.IgnoresArmor });
        Assert.Equal(25, enemy.CurrentHp);
        Assert.Equal(10, enemy.Block); // 护甲未被消耗
        Assert.Equal(1, player.GetEffectOrThrow(EffectId.Penetration).Layers);
    }

    [Fact]
    public void Immortal_ClampsHpFloor_WhileTimed()
    {
        var (combat, player, enemy) = CombatFixture.Create();
        enemy.AddEffect(EffectId.Immortal, 5);

        combat.DealDamage(new DamageContext(player, enemy, 28));

        Assert.Equal(5, enemy.CurrentHp); // 30-28=2 → 夹持到 5
        Assert.True(enemy.IsAlive);

        // 到期后移除，不再夹持
        combat.AdvanceTime(EffectTimings.ImmortalSeconds);
        Assert.False(enemy.HasEffect(EffectId.Immortal));

        combat.DealDamage(new DamageContext(player, enemy, 6));
        Assert.Equal(0, enemy.CurrentHp);
        Assert.False(enemy.IsAlive);
    }

    [Fact]
    public void Thorny_ReflectsToSource_CappedByLayers()
    {
        var (combat, player, enemy) = CombatFixture.Create();
        enemy.AddEffect(EffectId.Thorny, 3); // 反弹最高 3

        combat.DealDamage(new DamageContext(player, enemy, 10));

        Assert.Equal(20, enemy.CurrentHp);         // 敌人承受 10
        Assert.Equal(27, player.CurrentHp);        // 玩家反弹 3（封顶）
        Assert.False(enemy.HasEffect(EffectId.Thorny)); // 一次性
    }

    [Fact]
    public void Thorny_ReflectsFullAmount_WhenWithinCap()
    {
        var (combat, player, enemy) = CombatFixture.Create();
        enemy.AddEffect(EffectId.Thorny, 5);

        combat.DealDamage(new DamageContext(player, enemy, 4));

        Assert.Equal(26, player.CurrentHp); // 反弹 4（未到 5 封顶）
    }
}
