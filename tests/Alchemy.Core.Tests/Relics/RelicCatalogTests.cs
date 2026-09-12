using System;
using Alchemy.Core.Combat;
using Alchemy.Core.Relics;
using Xunit;

namespace Alchemy.Core.Tests.Relics;

public class RelicCatalogTests
{
    [Fact]
    public void CreateRandom_SpecificRarity_ReturnsThatRarity()
    {
        var rng = new BattleRandom(3);

        for (int i = 0; i < 20; i++)
        {
            var relic = RelicCatalog.CreateRandom(rng, Array.Empty<string>(), RelicRarity.Rare);
            Assert.NotNull(relic);
            Assert.Equal(RelicRarity.Rare, relic!.Rarity);
        }
    }

    [Fact]
    public void CreateRandom_ExcludesOwned()
    {
        var rng = new BattleRandom(1);
        var first = RelicCatalog.CreateRandom(rng, Array.Empty<string>(), RelicRarity.Common);

        for (int i = 0; i < 50; i++)
        {
            var next = RelicCatalog.CreateRandom(rng, new[] { first!.Id }, RelicRarity.Common);
            Assert.NotNull(next);
            Assert.NotEqual(first.Id, next!.Id);
        }
    }

    [Fact]
    public void CreateRandom_Any_NeverReturnsBoss()
    {
        var rng = new BattleRandom(5);

        for (int i = 0; i < 100; i++)
        {
            var relic = RelicCatalog.CreateRandom(rng, Array.Empty<string>(), rarity: null);
            Assert.NotNull(relic);
            Assert.NotEqual(RelicRarity.Boss, relic!.Rarity); // 任意稀有度也不含首领
        }
    }

    [Fact]
    public void CreateRandom_BossRarity_Throws()
    {
        var rng = new BattleRandom(1);

        Assert.Throws<InvalidOperationException>(() =>
            RelicCatalog.CreateRandom(rng, Array.Empty<string>(), RelicRarity.Boss));
    }

    [Fact]
    public void CreateRandom_EmptyPool_ReturnsNull()
    {
        var rng = new BattleRandom(1);
        // Rare 池只有一个 mana_crystal，排除后为空
        var relic = RelicCatalog.CreateRandom(rng, new[] { "mana_crystal" }, RelicRarity.Rare);
        Assert.Null(relic);
    }
}
