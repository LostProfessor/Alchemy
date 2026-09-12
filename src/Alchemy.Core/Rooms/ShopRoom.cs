using System.Collections.Generic;
using System.Linq;
using Alchemy.Core.Brewing;
using Alchemy.Core.GameData;
using Alchemy.Core.Random;
using Alchemy.Core.Relics;
using Alchemy.Core.Runs.Rewards;

namespace Alchemy.Core.Rooms;

/// <summary>
/// 商店房：出售 3 件随机遗物（1 保底普通，其余非首领遗物随机）+ 5 袋药材（每袋 5~10 株、混合稀有度）。
/// </summary>
public sealed class ShopRoom : AbstractRoom
{
    /// <summary>遗物商品数量。</summary>
    public const int RelicItemCount = 3;

    /// <summary>药材袋商品数量。</summary>
    public const int IngredientBagCount = 5;

    public List<ShopItem> Stock { get; } = new();

    public ShopRoom(string id, int floor) : base(id, RoomType.Shop, floor)
    {
    }

    /// <summary>进入时生成货架：遗物（保底 1 普通，其余非首领随机）+ 药材袋，不与已有遗物重复。</summary>
    public void GenerateStock(IRandomSource rng, IEnumerable<string> excludedRelicIds)
    {
        Stock.Clear();
        var excluded = excludedRelicIds.ToHashSet();

        // 遗物：第 1 件保底普通，其余随机稀有度（首领遗物商店一律不出）
        for (int i = 0; i < RelicItemCount; i++)
        {
            bool guaranteedCommon = i == 0;
            var relic = RelicCatalog.CreateRandom(
                rng, excluded, guaranteedCommon ? RelicRarity.Common : (RelicRarity?)null);
            if (relic == null)
            {
                continue;
            }

            excluded.Add(relic.Id);
            Stock.Add(new ShopItem { Relic = relic, Price = ShopPricing.PriceOf(relic.Rarity) });
        }

        // 药材袋：每袋 5~10 株、逐株掷稀有度（偏常见），价格 = Σ 每株单价（按稀有度区间随机）
        var catalog = Ingredients.Default;
        for (int i = 0; i < IngredientBagCount; i++)
        {
            var bag = RollShopBag(rng, catalog, out int price);
            Stock.Add(new ShopItem { IngredientBag = bag, Price = price });
        }
    }

    /// <summary>掷一袋药材：5~10 株混合稀有度；价格按每株稀有度计。</summary>
    private static RewardBag RollShopBag(IRandomSource rng, IngredientCatalog catalog, out int price)
    {
        int total = 5 + rng.Next(6); // 5~10
        var items = new List<RewardItem>();
        price = 0;
        for (int i = 0; i < total; i++)
        {
            var rarity = RollRarity(rng);
            var ingredient = catalog.Pick(rng, rarity);
            items.Add(new IngredientReward(ingredient.Id, 1));
            price += ShopPricing.RollIngredientUnitPrice(rng, rarity);
        }

        return new RewardBag(items);
    }

    /// <summary>药材稀有度掷点：偏常见（55 常见 / 30 罕见 / 12 稀有 / 3 传奇）。</summary>
    private static IngredientRarity RollRarity(IRandomSource rng)
    {
        int roll = rng.Next(100);
        return roll switch
        {
            < 55 => IngredientRarity.Common,
            < 85 => IngredientRarity.Uncommon,
            < 97 => IngredientRarity.Rare,
            _ => IngredientRarity.Legendary,
        };
    }
}

/// <summary>货架上的一个商品：要么是遗物，要么是药材袋（二选一）。</summary>
public sealed class ShopItem
{
    /// <summary>遗物商品（IsRelic 时非空）。</summary>
    public Relic? Relic { get; set; }

    /// <summary>药材袋商品（IsIngredientBag 时非空）。</summary>
    public RewardBag? IngredientBag { get; set; }

    public int Price { get; set; }

    public bool Sold { get; set; }

    public bool IsRelic => Relic != null;

    public bool IsIngredientBag => IngredientBag != null;
}

/// <summary>商店定价：遗物按稀有度定；药材按每株稀有度区间计（普通3~5/罕见5~7/稀有8~12/传奇13~17）。</summary>
public static class ShopPricing
{
    public static int PriceOf(RelicRarity rarity) => rarity switch
    {
        RelicRarity.Common => 50,
        RelicRarity.Uncommon => 75,
        RelicRarity.Rare => 120,
        RelicRarity.Boss => 200,
        _ => 50,
    };

    /// <summary>单个药材单价：按稀有度在区间内随机取整。</summary>
    public static int RollIngredientUnitPrice(IRandomSource rng, IngredientRarity rarity)
    {
        var (low, high) = rarity switch
        {
            IngredientRarity.Common => (3, 5),
            IngredientRarity.Uncommon => (5, 7),
            IngredientRarity.Rare => (8, 12),
            IngredientRarity.Legendary => (13, 17),
            _ => (3, 5),
        };

        return low + rng.Next(high - low + 1);
    }
}

