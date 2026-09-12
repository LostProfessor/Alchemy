using System.Collections.Generic;
using System.Linq;
using Alchemy.Core.GameData;
using Alchemy.Core.Random;
using Alchemy.Core.Relics;
using Alchemy.Core.Runs.Rewards;

namespace Alchemy.Core.Rooms;

/// <summary>宝藏房：一次特殊奖励 = 1 遗物 + 货币 + 一次药材组合（策划案"随机刷"）。</summary>
public sealed class TreasureRoom : AbstractRoom
{
    public TreasureReward? Treasure { get; private set; }

    public TreasureRoom(string id, int floor) : base(id, RoomType.Treasure, floor)
    {
    }

    /// <summary>进入时生成宝藏内容。</summary>
    public void Generate(IRandomSource rng, IEnumerable<string> excludedRelicIds)
    {
        Treasure = new TreasureReward
        {
            Relic = RelicCatalog.CreateRandom(rng, excludedRelicIds, rarity: null), // 任意稀有度
            Currency = 40 + rng.Next(41), // 40~80
            Ingredients = new RewardRoller(rng, Ingredients.Default).RollBag(RewardTemplates.CombatIngredientBag),
        };
    }
}

/// <summary>宝藏房的一次奖励内容（三部分各自可"逐块领取"，未领即放弃）。</summary>
public sealed class TreasureReward
{
    /// <summary>货币部分是否已领取。</summary>
    public bool CurrencyClaimed { get; set; }

    /// <summary>遗物部分是否已领取（Relic 为空时无此块）。</summary>
    public bool RelicClaimed { get; set; }

    /// <summary>药材袋部分是否已领取。</summary>
    public bool BagClaimed { get; set; }

    public Relic? Relic { get; set; }

    public int Currency { get; set; }

    public RewardBag Ingredients { get; set; } = null!;

    /// <summary>所有可领取部分都已领取（没有的部分视为已领）。</summary>
    public bool AllClaimed => CurrencyClaimed && (Relic == null || RelicClaimed) && BagClaimed;
}

