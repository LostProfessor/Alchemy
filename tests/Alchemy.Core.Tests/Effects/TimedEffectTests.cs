using Alchemy.Core.Combat;
using Alchemy.Core.Effects;
using Alchemy.Core.Hooks;
using Xunit;

namespace Alchemy.Core.Tests.Effects;

/// <summary>计时类效果：烈毒/药物依赖/不朽。</summary>
public class TimedEffectTests
{
    [Fact]
    public void VirulentPoison_AfterDuration_Deals2x_Unblockable_Removed()
    {
        var (combat, player, enemy) = CombatFixture.Create();
        enemy.AddEffect(EffectId.VirulentPoison, 3);
        combat.GainBlock(enemy, 10);

        combat.AdvanceTime(EffectTimings.VirulentPoisonSeconds - 0.1f);
        Assert.Equal(10, enemy.Block); // 未到期
        Assert.True(enemy.HasEffect(EffectId.VirulentPoison));

        combat.AdvanceTime(0.2f);

        Assert.Equal(24, enemy.CurrentHp); // 30 - 2*3=6，无视护甲
        Assert.Equal(10, enemy.Block);     // 护甲未被消耗
        Assert.False(enemy.HasEffect(EffectId.VirulentPoison));
    }

    [Fact]
    public void Dependency_OnExpiry_Takes3x_DecrementsLayer_ResetsTimer()
    {
        var (combat, player, enemy) = CombatFixture.Create();
        enemy.AddEffect(EffectId.Dependency, 2);

        combat.AdvanceTime(EffectTimings.DependencySeconds);

        Assert.Equal(24, enemy.CurrentHp); // 30 - 3*2=6
        Assert.Equal(1, enemy.GetEffectOrThrow(EffectId.Dependency).Layers); // 移去一层
        Assert.NotNull(enemy.GetEffectOrThrow(EffectId.Dependency).DurationRemaining); // 重新计时

        combat.AdvanceTime(EffectTimings.DependencySeconds);
        Assert.Equal(21, enemy.CurrentHp); // 24 - 3*1=3
        Assert.False(enemy.HasEffect(EffectId.Dependency)); // 层数归零移除
    }

    [Fact]
    public void Dependency_Reapply_RefreshesTimer_AndAddsLayers()
    {
        var (combat, player, enemy) = CombatFixture.Create();
        enemy.AddEffect(EffectId.Dependency, 2);

        // 过了 3 秒（未到 5 秒发作线），再次服用 y=3 层 → 重置计时 + 层数 2+3=5
        combat.AdvanceTime(3f);
        enemy.AddEffect(EffectId.Dependency, 3);

        var dependency = enemy.GetEffectOrThrow(EffectId.Dependency);
        Assert.Equal(5, dependency.Layers);
        Assert.Equal(EffectTimings.DependencySeconds, dependency.DurationRemaining!.Value, precision: 3);

        // 重新计满 5 秒才发作
        combat.AdvanceTime(EffectTimings.DependencySeconds - 0.1f);
        Assert.Equal(30, enemy.CurrentHp);
        combat.AdvanceTime(0.2f);
        Assert.Equal(15, enemy.CurrentHp); // 30 - 3*5=15
    }

    [Fact]
    public void Immortal_Expires_NoLongerClamps()
    {
        var (combat, player, enemy) = CombatFixture.Create();
        enemy.AddEffect(EffectId.Immortal, 5);

        combat.AdvanceTime(EffectTimings.ImmortalSeconds);
        Assert.False(enemy.HasEffect(EffectId.Immortal));

        combat.DealDamage(new DamageContext(player, enemy, 8));
        Assert.Equal(22, enemy.CurrentHp); // 不再夹持
    }
}
