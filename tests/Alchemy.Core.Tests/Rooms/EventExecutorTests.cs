using System.Linq;
using Alchemy.Core.Brewing;
using Alchemy.Core.Combat;
using Alchemy.Core.Entities;
using Alchemy.Core.Relics;
using Alchemy.Core.Rooms.Events;
using Alchemy.Core.Runs;
using Xunit;

namespace Alchemy.Core.Tests.Rooms;

public class EventExecutorTests
{
    private static (RunState Run, Creature Player, BattleRandom Rng) NewContext()
    {
        var run = new RunState();
        var player = new Creature("玩家", 30, isPlayer: true);
        return (run, player, new BattleRandom(7));
    }

    [Fact]
    public void Damage_ReducesHp()
    {
        var (run, player, rng) = NewContext();

        EventExecutor.Apply(run, player, rng, new[] { new EventAction(EventActionType.Damage, 10) });

        Assert.Equal(20, player.CurrentHp);
    }

    [Fact]
    public void Heal_IncreasesHp_ClampsAtMax()
    {
        var (run, player, rng) = NewContext();
        player.CurrentHp = 10;

        EventExecutor.Apply(run, player, rng, new[] { new EventAction(EventActionType.Heal, 50) });

        Assert.Equal(30, player.CurrentHp);
    }

    [Fact]
    public void GainCurrency_Adds()
    {
        var (run, player, rng) = NewContext();

        EventExecutor.Apply(run, player, rng, new[] { new EventAction(EventActionType.GainCurrency, 25) });

        Assert.Equal(25, run.Currency);
    }

    [Fact]
    public void LoseCurrency_ClampsAtZero()
    {
        var (run, player, rng) = NewContext();
        run.Currency = 10;

        EventExecutor.Apply(run, player, rng, new[] { new EventAction(EventActionType.LoseCurrency, 30) });

        Assert.Equal(0, run.Currency);
    }

    [Fact]
    public void GainIngredient_AddsToPocket()
    {
        var (run, player, rng) = NewContext();

        EventExecutor.Apply(run, player, rng,
            new[] { new EventAction(EventActionType.GainIngredient, IngredientRarity: IngredientRarity.Rare, Count: 2) });

        Assert.Equal(2, run.Pocket.TotalCount);
    }

    [Fact]
    public void GainRelic_AddsRelic_NotDuplicate()
    {
        var (run, player, rng) = NewContext();
        run.Relics.Add(new IronBracer()); // 已有铁皮手环

        EventExecutor.Apply(run, player, rng, new[] { new EventAction(EventActionType.GainRelic, RelicRarity: RelicRarity.Common) });

        Assert.Equal(2, run.Relics.Count);
        Assert.Single(run.Relics.Relics, r => r.Id == "iron_bracer"); // 不重复获得
    }

    [Fact]
    public void LoseIngredient_RemovesFromPocket()
    {
        var (run, player, rng) = NewContext();
        run.Pocket.Add("glowcap", 3);
        run.Pocket.Add("bitterroot", 1);

        EventExecutor.Apply(run, player, rng, new[] { new EventAction(EventActionType.LoseIngredient, Count: 2) });

        Assert.Equal(2, run.Pocket.TotalCount);
    }

    [Fact]
    public void LoseRelic_RemovesOneOwnedRelic()
    {
        var (run, player, rng) = NewContext();
        run.Relics.Add(new IronBracer());
        run.Relics.Add(new WoodenAmulet());

        EventExecutor.Apply(run, player, rng, new[] { new EventAction(EventActionType.LoseRelic) });

        Assert.Equal(1, run.Relics.Count);
    }

    [Fact]
    public void GainRelic_BossRequest_NeverGrantsBossRelic()
    {
        var (run, player, rng) = NewContext();

        // 即使数据请求首领稀有度，也降级为任意非首领遗物（首领遗物只能来自首领战奖励）
        EventExecutor.Apply(run, player, rng,
            new[] { new EventAction(EventActionType.GainRelic, RelicRarity: RelicRarity.Boss) });

        Assert.Equal(1, run.Relics.Count);
        Assert.NotEqual(RelicRarity.Boss, run.Relics.Relics[0].Rarity);
    }

    [Fact]
    public void GainMaxHp_IncreasesMaxAndCurrent()
    {
        var (run, player, rng) = NewContext();
        player.CurrentHp = 10;

        EventExecutor.Apply(run, player, rng, new[] { new EventAction(EventActionType.GainMaxHp, 5) });

        Assert.Equal(35, player.MaxHp);
        Assert.Equal(15, player.CurrentHp);
    }

    [Fact]
    public void LoseMaxHp_DecreasesMaxAndClampsCurrent()
    {
        var (run, player, rng) = NewContext();
        player.CurrentHp = 25;

        EventExecutor.Apply(run, player, rng, new[] { new EventAction(EventActionType.LoseMaxHp, 10) });

        Assert.Equal(20, player.MaxHp);
        Assert.Equal(20, player.CurrentHp);
    }
}
