using System.Collections.Generic;

namespace Alchemy.Core.Encounters;

/// <summary>
/// 怪物模板：名字/生命/意图队列（按序循环执行，展示给玩家读敌）。
/// 数据来源将由 EnemyResource（.tres）驱动。
/// </summary>
public sealed record MonsterTemplate(
    string Id,
    string DisplayName,
    int MaxHp,
    IReadOnlyList<Intention> Intentions);
