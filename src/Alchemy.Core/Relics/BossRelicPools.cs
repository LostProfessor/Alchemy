using System;
using System.Collections.Generic;

namespace Alchemy.Core.Relics;

/// <summary>
/// 首领遗物池：每个首领敌人有专属于自己的首领遗物池，
/// 玩家战胜该首领后获取的首领遗物奖励只能从这个池子中抽取。
/// 首领遗物只能在首领战奖励中获得（商店/精英/宝藏/事件一律不给）。
/// </summary>
public static class BossRelicPools
{
    /// <summary>哥布林王（大层1）→ 专属池。</summary>
    public static readonly IReadOnlyList<(string Id, Func<Relic> Factory)> GoblinKing = new (string, Func<Relic>)[]
    {
        ("goblin_crown", () => new GoblinCrown()),
    };

    /// <summary>女巫首领（大层2）→ 专属池。</summary>
    public static readonly IReadOnlyList<(string Id, Func<Relic> Factory)> WitchBoss = new (string, Func<Relic>)[]
    {
        ("runecrown", () => new Runecrown()),
        ("phoenix_heart", () => new PhoenixHeart()),
    };

    /// <summary>石像魔像（大层3）→ 专属池。</summary>
    public static readonly IReadOnlyList<(string Id, Func<Relic> Factory)> GolemBoss = new (string, Func<Relic>)[]
    {
        ("stoneheart", () => new Stoneheart()),
    };

    /// <summary>亡灵领主（大层4）→ 专属池。</summary>
    public static readonly IReadOnlyList<(string Id, Func<Relic> Factory)> LichKing = new (string, Func<Relic>)[]
    {
        ("death_bell", () => new DeathBell()),
    };

    /// <summary>深渊之主（大层5）→ 专属池。</summary>
    public static readonly IReadOnlyList<(string Id, Func<Relic> Factory)> AbyssLord = new (string, Func<Relic>)[]
    {
        ("abyss_heart", () => new AbyssHeart()),
    };

    /// <summary>所有首领遗物池（用于存档恢复等）。</summary>
    public static IEnumerable<IReadOnlyList<(string Id, Func<Relic> Factory)>> AllPools
    {
        get
        {
            yield return GoblinKing;
            yield return WitchBoss;
            yield return GolemBoss;
            yield return LichKing;
            yield return AbyssLord;
        }
    }

    /// <summary>按首领 ID 取专属池；未知首领返回 null。</summary>
    public static IReadOnlyList<(string Id, Func<Relic> Factory)>? PoolFor(string bossId) => bossId switch
    {
        "goblin_king" => GoblinKing,
        "witch_boss" => WitchBoss,
        "golem_boss" => GolemBoss,
        "lich_king" => LichKing,
        "abyss_lord" => AbyssLord,
        _ => null,
    };
}
