using System;
using Alchemy.Core.Combat;
using Alchemy.Core.Relics;
using Alchemy.Core.Runs.Rewards;
using Xunit;

namespace Alchemy.Core.Tests.Relics;

public class BossRelicPoolsTests
{
    [Theory]
    [InlineData("goblin_king", "goblin_crown")]
    [InlineData("witch_boss", "runecrown")]
    [InlineData("witch_boss", "phoenix_heart")]
    [InlineData("golem_boss", "stoneheart")]
    [InlineData("lich_king", "death_bell")]
    [InlineData("abyss_lord", "abyss_heart")]
    public void PoolFor_ContainsBossRelics(string bossId, string relicId)
    {
        var pool = BossRelicPools.PoolFor(bossId);

        Assert.NotNull(pool);
        Assert.Contains(pool!, p => p.Id == relicId);
    }

    [Fact]
    public void PoolFor_UnknownBoss_ReturnsNull()
    {
        Assert.Null(BossRelicPools.PoolFor("unknown_boss"));
    }

    [Fact]
    public void BossReward_NoDirectRelic()
    {
        var rng = new BattleRandom(3);

        // 首领战后不再直发遗物（改跨层三选一，候选见 RunManager.PrepareBossRelicChoice）
        var roll = CombatRewards.RollRewards(rng, Array.Empty<string>(), isElite: false, isBoss: true, bossId: "golem_boss");

        Assert.True(roll.Currency >= 100 && roll.Currency <= 150, "首领货币应在 100~150");
        Assert.Null(roll.BonusRelic);
    }

    [Fact]
    public void EliteReward_NeverBossRelic()
    {
        var rng = new BattleRandom(5);

        for (int i = 0; i < 50; i++)
        {
            var roll = CombatRewards.RollRewards(rng, Array.Empty<string>(), isElite: true, isBoss: false);
            Assert.NotNull(roll.BonusRelic);
            Assert.NotEqual(RelicRarity.Boss, roll.BonusRelic!.Rarity); // 精英不给首领遗物
        }
    }
}
