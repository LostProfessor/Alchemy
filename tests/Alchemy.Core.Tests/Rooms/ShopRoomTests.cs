using System;
using System.Linq;
using Alchemy.Core.Brewing;
using Alchemy.Core.Combat;
using Alchemy.Core.Relics;
using Alchemy.Core.Rooms;
using Xunit;

namespace Alchemy.Core.Tests.Rooms;

public class ShopRoomTests
{
    [Fact]
    public void GenerateStock_HasRelicsAndIngredientBags()
    {
        var room = new ShopRoom("shop_1", 1);

        room.GenerateStock(new BattleRandom(42), Array.Empty<string>());

        var relics = room.Stock.Where(i => i.IsRelic).ToList();
        var bags = room.Stock.Where(i => i.IsIngredientBag).ToList();

        Assert.Equal(ShopRoom.RelicItemCount, relics.Count);
        Assert.Equal(ShopRoom.IngredientBagCount, bags.Count);
        Assert.True(relics.Count(i => i.Relic!.Rarity == RelicRarity.Common) >= 1, "至少 1 个普通遗物（保底）");
        Assert.All(relics, i => Assert.NotEqual(RelicRarity.Boss, i.Relic!.Rarity)); // 商店不出首领遗物
        Assert.All(room.Stock, i => Assert.True(i.Price > 0));
    }

    [Fact]
    public void GenerateStock_NoDuplicateRelics()
    {
        for (int seed = 1; seed <= 10; seed++)
        {
            var room = new ShopRoom("shop_1", 1);
            room.GenerateStock(new BattleRandom(seed), Array.Empty<string>());

            var ids = room.Stock.Where(i => i.IsRelic).Select(i => i.Relic!.Id).ToList();
            Assert.Equal(ids.Count, ids.Distinct().Count());
        }
    }

    [Fact]
    public void GenerateStock_IngredientBags_5To10Herbs_PricedByQuantity()
    {
        for (int seed = 1; seed <= 20; seed++)
        {
            var room = new ShopRoom("shop_1", 1);
            room.GenerateStock(new BattleRandom(seed), Array.Empty<string>());

            var bags = room.Stock.Where(i => i.IsIngredientBag).ToList();
            Assert.Equal(ShopRoom.IngredientBagCount, bags.Count);

            foreach (var item in bags)
            {
                int total = item.IngredientBag!.Items.Count;
                Assert.InRange(total, 5, 10);
                // 每株最便宜 3、最贵 17 → 总价应在 [3N, 17N]
                Assert.InRange(item.Price, 3 * total, 17 * total);
            }
        }
    }

    [Fact]
    public void ShopPricing_IngredientUnitPrice_WithinBands()
    {
        var rng = new BattleRandom(3);
        var bands = new[]
        {
            (IngredientRarity.Common, 3, 5),
            (IngredientRarity.Uncommon, 5, 7),
            (IngredientRarity.Rare, 8, 12),
            (IngredientRarity.Legendary, 13, 17),
        };

        foreach (var (rarity, low, high) in bands)
        {
            for (int i = 0; i < 200; i++)
            {
                int price = ShopPricing.RollIngredientUnitPrice(rng, rarity);
                Assert.InRange(price, low, high);
            }
        }
    }
}
