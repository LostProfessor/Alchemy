using System;
using System.Linq;
using Alchemy.Core.Combat;
using Alchemy.Core.Relics;
using Alchemy.Core.Rooms;
using Xunit;

namespace Alchemy.Core.Tests.Rooms;

public class TreasureRoomTests
{
    [Fact]
    public void Generate_CreatesRelicCurrencyAndIngredients()
    {
        var room = new TreasureRoom("treasure_1", 1);

        room.Generate(new BattleRandom(42), Array.Empty<string>());

        Assert.NotNull(room.Treasure);
        Assert.NotNull(room.Treasure!.Relic);
        Assert.NotEqual(RelicRarity.Boss, room.Treasure.Relic!.Rarity); // 宝藏不出首领遗物
        Assert.True(room.Treasure.Currency >= 40 && room.Treasure.Currency <= 80, $"货币应在 40~80，实际 {room.Treasure.Currency}");
        Assert.True(room.Treasure.Ingredients.Items.Count > 0);
    }

    [Fact]
    public void Generate_ExcludesOwnedRelics()
    {
        var room = new TreasureRoom("treasure_1", 1);

        room.Generate(new BattleRandom(1), new[] { "iron_bracer" });

        Assert.NotNull(room.Treasure!.Relic);
        Assert.NotEqual("iron_bracer", room.Treasure.Relic!.Id);
    }
}
