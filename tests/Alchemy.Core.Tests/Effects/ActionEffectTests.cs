using Alchemy.Core.Effects;
using Alchemy.Core.Hooks;
using Xunit;

namespace Alchemy.Core.Tests.Effects;

/// <summary>动作类效果：致幻/淤伤/祝福。</summary>
public class ActionEffectTests
{
    [Fact]
    public void Hallucinate_FailsAction_WhenChanceHits()
    {
        var (combat, player, _) = CombatFixture.Create();
        // 直接设 10 层 → 100% 失败（绕过 5 层上限，保证确定性）
        var effect = player.AddEffect(EffectId.Hallucinate, 1);
        effect.Layers = 10;

        var action = combat.BeginAction(player, "use_potion");

        Assert.True(action.Cancelled);
        // 10 层→100% 失败，但只消耗一层：10-1=9
        Assert.Equal(9, player.GetEffectOrThrow(EffectId.Hallucinate).Layers);
    }

    [Fact]
    public void Hallucinate_ConsumesLayer_RegardlessOfOutcome()
    {
        var (combat, player, _) = CombatFixture.Create(seed: 1);
        player.AddEffect(EffectId.Hallucinate, 1);

        combat.BeginAction(player, "use_potion");

        Assert.False(player.HasEffect(EffectId.Hallucinate)); // 无论成败都消耗一层
    }

    [Fact]
    public void Bruise_SelfDamageOnAction_ThenRemoved()
    {
        var (combat, player, _) = CombatFixture.Create();
        player.AddEffect(EffectId.Bruise, 2);

        combat.BeginAction(player, "brew");

        Assert.Equal(28, player.CurrentHp); // 自伤 2
        Assert.Equal(1, player.GetEffectOrThrow(EffectId.Bruise).Layers); // 层数 -1

        combat.BeginAction(player, "brew");
        Assert.Equal(27, player.CurrentHp); // 自伤 1
        Assert.False(player.HasEffect(EffectId.Bruise));
    }

    [Fact]
    public void Blessing_HealsOne_OnEveryAction()
    {
        var (combat, player, enemy) = CombatFixture.Create();
        player.AddEffect(EffectId.Blessing, 1);
        combat.DealDamage(new DamageContext(enemy, player, 10)); // 20/30

        combat.EndAction(combat.BeginAction(player, "brew"));
        combat.EndAction(combat.BeginAction(player, "use_potion"));

        Assert.Equal(22, player.CurrentHp); // 20 + 1 + 1
    }
}
