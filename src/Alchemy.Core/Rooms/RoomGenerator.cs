using System;
using System.Collections.Generic;
using Alchemy.Core.Encounters;
using Alchemy.Core.Random;

namespace Alchemy.Core.Rooms;

/// <summary>
/// 房间生成器（占位：线性楼层序列，替代完整地图节点图；后续接地图系统）。
/// 权重：战斗 60 / 事件 20 / 宝藏 10 / 商店 10，层末为战斗房（首领遭遇）。
/// </summary>
public sealed class RoomGenerator
{
    private readonly IRandomSource _rng;
    private readonly int _floorsPerAct;

    public RoomGenerator(IRandomSource rng, int floorsPerAct = 8)
    {
        _rng = rng;
        _floorsPerAct = floorsPerAct;
    }

    public IReadOnlyList<AbstractRoom> GenerateAct()
    {
        var rooms = new List<AbstractRoom>(_floorsPerAct + 1);
        for (int floor = 1; floor <= _floorsPerAct; floor++)
        {
            rooms.Add(CreateRoom(RollType(), floor));
        }

        // 层末首领（占位为战斗房）
        rooms.Add(new CombatRoom($"boss_{_floorsPerAct + 1}", _floorsPerAct + 1, EncounterFactory.BossForAct(1)));
        return rooms;
    }

    private RoomType RollType()
    {
        int roll = _rng.Next(100);
        if (roll < 60)
        {
            return RoomType.Combat;
        }

        if (roll < 80)
        {
            return RoomType.Event;
        }

        if (roll < 90)
        {
            return RoomType.Treasure;
        }

        return RoomType.Shop;
    }

    private AbstractRoom CreateRoom(RoomType type, int floor) => type switch
    {
        RoomType.Combat => new CombatRoom($"combat_{floor}", floor, EncounterFactory.Random(_rng, 1)),
        RoomType.Event => new EventRoom($"event_{floor}", floor),
        RoomType.Treasure => new TreasureRoom($"treasure_{floor}", floor),
        RoomType.Shop => new ShopRoom($"shop_{floor}", floor),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };
}
