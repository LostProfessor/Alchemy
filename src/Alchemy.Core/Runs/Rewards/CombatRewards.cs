using System.Collections.Generic;
using System.Linq;
using Alchemy.Core.Random;
using Alchemy.Core.Relics;

namespace Alchemy.Core.Runs.Rewards;

/// <summary>战斗胜利奖励计算：货币 + 额外遗物（精英/首领）。</summary>
public static class CombatRewards
{
    /// <summary>一次战斗奖励的结算结果。</summary>
    public sealed record Roll(int Currency, Relic? BonusRelic);

    /// <summary>
    /// 计算一场战斗胜利的奖励：
    /// 普通 25~45 货币；精英 50~80 + 随机遗物（非首领）；首领 100~150 货币（首领遗物改跨层三选一，不再战后直发）。
    /// </summary>
    public static Roll RollRewards(IRandomSource rng, IEnumerable<string> ownedRelicIds, bool isElite, bool isBoss, string? bossId = null)
    {
        _ = bossId; // 首领遗物改由跨层三选一提供（RunManager.PrepareBossRelicChoice 用 BossRelicPools）
        int currency = isBoss ? 100 + rng.Next(51) : isElite ? 50 + rng.Next(31) : 25 + rng.Next(21);
        Relic? bonus = isElite ? RelicCatalog.CreateRandom(rng, ownedRelicIds) : null; // 首领不再给直发遗物
        return new Roll(currency, bonus);
    }
}
