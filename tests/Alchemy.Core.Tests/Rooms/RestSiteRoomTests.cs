using System.Linq;
using Alchemy.Core.Brewing;
using Alchemy.Core.Combat;
using Alchemy.Core.Entities;
using Alchemy.Core.GameData;
using Alchemy.Core.Relics;
using Alchemy.Core.Rooms;
using Alchemy.Core.Runs;
using Alchemy.Core.Runs.Rewards;
using Xunit;

namespace Alchemy.Core.Tests.Rooms;

public class RestSiteRoomTests
{
    [Fact]
    public void Sleep_Heals30PercentOfLostHp()
    {
        var run = new RunState();
        var player = new Creature("玩家", 30, isPlayer: true);
        player.CurrentHp = 20; // 损失 10 → 回复 3

        RestSiteActions.Sleep(run, player);

        Assert.Equal(23, player.CurrentHp);
    }

    [Fact]
    public void Sleep_ClampsAtMaxHp()
    {
        var run = new RunState();
        var player = new Creature("玩家", 30, isPlayer: true);
        player.CurrentHp = 29; // 损失 1 → 回复 0（不超上限）

        RestSiteActions.Sleep(run, player);

        Assert.Equal(29, player.CurrentHp);
    }

    [Fact]
    public void Sleep_WithRestHealModifierRelic_IncreasesHeal()
    {
        var run = new RunState();
        run.Relics.Add(new WarmBedroll()); // +50%
        var player = new Creature("玩家", 30, isPlayer: true);
        player.CurrentHp = 20; // 基础回复 3 → 3+1=4

        RestSiteActions.Sleep(run, player, healModifier: heal =>
            run.Relics.Relics.OfType<IRestHealModifier>().Aggregate(heal, (acc, r) => r.ModifyRestHeal(acc)));

        Assert.Equal(24, player.CurrentHp);
    }

    [Fact]
    public void Explore_GrantsCommonRelic_AndIngredients()
    {
        var run = new RunState();
        var rng = new BattleRandom(42);

        RestSiteActions.Explore(run, rng, Ingredients.Default);

        Assert.Equal(1, run.Relics.Count);
        Assert.Equal(RelicRarity.Common, run.Relics.Relics[0].Rarity);
        Assert.True(run.Pocket.TotalCount > 0);
    }

    [Fact]
    public void NoLegendaryTemplate_NeverRollsLegendary()
    {
        var roller = new RewardRoller(new BattleRandom(1), Ingredients.Default);
        var template = RewardTemplates.CombatIngredientBag.Without(IngredientRarity.Legendary);

        for (int i = 0; i < 300; i++)
        {
            var bag = roller.RollBag(template);
            foreach (var item in bag.Items.OfType<IngredientReward>())
            {
                var ingredient = Ingredients.Default.All.First(x => x.Id == item.IngredientId);
                Assert.NotEqual(IngredientRarity.Legendary, ingredient.Rarity);
            }
        }
    }
}
