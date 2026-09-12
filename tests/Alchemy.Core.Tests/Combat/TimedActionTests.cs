using Alchemy.Core.Combat;
using Alchemy.Core.Effects;
using Alchemy.Core.Encounters;
using Alchemy.Core.Hooks;
using Xunit;

namespace Alchemy.Core.Tests.Combat;

public class TimedActionTests
{
    [Fact]
    public void AdvanceTime_CountsDownAndFires()
    {
        var (combat, player, _) = CombatFixture.Create();
        int fired = 0;
        combat.BeginTimedAction(player, "test", 5f, _ => fired++);

        combat.AdvanceTime(5f);

        Assert.Equal(1, fired);
        Assert.Empty(combat.PendingActions);
    }

    [Fact]
    public void AdvanceTime_Partial_DoesNotFireEarly()
    {
        var (combat, player, _) = CombatFixture.Create();
        int fired = 0;
        combat.BeginTimedAction(player, "test", 5f, _ => fired++);

        combat.AdvanceTime(3f);
        Assert.Equal(0, fired);

        combat.AdvanceTime(2f);
        Assert.Equal(1, fired);
    }

    [Fact]
    public void Enemy_ActsAfterInterval_ThenRearms()
    {
        var (combat, player, enemy) = CombatFixture.Create();

        combat.ScheduleEnemyAction(enemy, new[] { new Intention("atk", "攻击", 10f, IntentionActionType.Attack, Damage: 5) });
        combat.AdvanceTime(10);
        Assert.Equal(25, player.CurrentHp); // 第一次行动

        combat.AdvanceTime(10);
        Assert.Equal(20, player.CurrentHp); // 重新计时后第二次行动
    }

    [Fact]
    public void Enemy_Dead_DoesNotAct()
    {
        var (combat, player, enemy) = CombatFixture.Create();
        combat.ScheduleEnemyAction(enemy, new[] { new Intention("atk", "攻击", 10f, IntentionActionType.Attack, Damage: 5) });
        combat.DealDamage(new DamageContext(player, enemy, 999)); // 杀死敌人

        combat.AdvanceTime(10);

        Assert.Equal(30, player.CurrentHp); // 敌人已死不再行动
    }

    // ── 迟钝 / 专注 对计时动作时长的修饰 ──────────────────────────

    [Fact]
    public void Sluggish_IncreasesDuration_PlayerHalved()
    {
        var (combat, player, _) = CombatFixture.Create();
        player.AddEffect(EffectId.Sluggish, 2); // 玩家 +x = +2

        var action = combat.BeginTimedAction(player, "test", 5f, _ => { });

        Assert.Equal(7f, action.Duration, precision: 3);
        Assert.Equal(1, player.GetEffectOrThrow(EffectId.Sluggish).Layers); // 消耗一层
    }

    [Fact]
    public void Focus_DecreasesDuration_PlayerHalved_Min1()
    {
        var (combat, player, _) = CombatFixture.Create();
        player.AddEffect(EffectId.Focus, 5); // 玩家 -x = -5

        var action = combat.BeginTimedAction(player, "test", 1.5f, _ => { });

        Assert.Equal(1f, action.Duration, precision: 3); // 最低 1 秒
        Assert.Equal(4, player.GetEffectOrThrow(EffectId.Focus).Layers);
    }

    [Fact]
    public void Sluggish_Enemy_FullEffect()
    {
        var (combat, _, enemy) = CombatFixture.Create();
        enemy.AddEffect(EffectId.Sluggish, 2); // 敌人 +2x = +4

        var action = combat.BeginTimedAction(enemy, "test", 5f, _ => { });

        Assert.Equal(9f, action.Duration, precision: 3);
    }
}
