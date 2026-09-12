using System;
using System.Collections.Generic;
using System.Linq;
using Alchemy.Core.Random;

namespace Alchemy.Core.Relics;

/// <summary>
/// 遗物目录：按稀有度分组的遗物工厂（后续数据化后改为从 .tres 资源构建）。
/// ⚠️ 不含首领遗物：首领遗物只能在首领战奖励中获取（见 BossRelicPools），
/// 商店/精英/宝藏/事件一律不得给首领遗物。
/// </summary>
public static class RelicCatalog
{
    private static readonly IReadOnlyDictionary<RelicRarity, IReadOnlyList<(string Id, Func<Relic> Factory)>> _byRarity =
        new Dictionary<RelicRarity, IReadOnlyList<(string, Func<Relic>)>>
        {
            [RelicRarity.Common] = new (string, Func<Relic>)[] { ("iron_bracer", () => new IronBracer()), ("wooden_amulet", () => new WoodenAmulet()), ("lucky_clover", () => new LuckyClover()), ("warm_bedroll", () => new WarmBedroll()) },
            [RelicRarity.Uncommon] = new (string, Func<Relic>)[] { ("steel_will", () => new SteelWill()), ("thorn_cloak", () => new ThornCloak()) },
            [RelicRarity.Rare] = new (string, Func<Relic>)[] { ("mana_crystal", () => new ManaCrystal()) },
            // 首领遗物不在通用池中，见 BossRelicPools
        };

    /// <summary>
    /// 随机抽一个尚未拥有的遗物；rarity 指定则限定该稀有度，null 则任意非首领稀有度。
    /// 首领稀有度在此不可用（首领遗物只能经首领战奖励获取）。
    /// </summary>
    public static Relic? CreateRandom(IRandomSource rng, IEnumerable<string> excludeIds, RelicRarity? rarity = null)
    {
        if (rarity == RelicRarity.Boss)
        {
            throw new InvalidOperationException("首领遗物只能通过首领战奖励获取（BossRelicPools）");
        }

        var excluded = excludeIds.ToHashSet();
        var pools = rarity.HasValue
            ? new[] { _byRarity[rarity.Value] }
            : _byRarity.Values; // 所有非首领稀有度
        var pool = pools.SelectMany(list => list).Where(e => !excluded.Contains(e.Id)).ToList();
        if (pool.Count == 0)
        {
            return null;
        }

        return pool[rng.Next(pool.Count)].Factory();
    }

    public static Relic? CreateRandomCommon(IRandomSource rng, IEnumerable<string> excludeIds) =>
        CreateRandom(rng, excludeIds, RelicRarity.Common);

    /// <summary>职业专属遗物池（开局按职业赠送，不入随机池）。</summary>
    private static readonly IReadOnlyDictionary<string, (string Id, Func<Relic> Factory)> _jobRelics =
        new Dictionary<string, (string, Func<Relic>)>
        {
            ["aqua"] = ("aqua_craft", () => new AquaCraft()),
            ["oil"] = ("oil_craft", () => new OilCraft()),
            ["slime"] = ("slime_craft", () => new SlimeCraft()),
        };

    /// <summary>按职业 Id 取职业专属遗物：改由 JobCatalog 的 RelicId + CreateById 提供（此方法已弃用删除）。</summary>

    /// <summary>按遗物 ID 重建实例（含首领遗物），用于存档恢复；未知返回 null。</summary>
    public static Relic? CreateById(string id)
    {
        foreach (var list in _byRarity.Values)
        {
            foreach (var (relicId, factory) in list)
            {
                if (relicId == id)
                {
                    return factory();
                }
            }
        }

        foreach (var pool in BossRelicPools.AllPools)
        {
            foreach (var (relicId, factory) in pool)
            {
                if (relicId == id)
                {
                    return factory();
                }
            }
        }

        foreach (var entry in _jobRelics.Values)
        {
            if (entry.Id == id)
            {
                return entry.Factory();
            }
        }

        return null;
    }
}
