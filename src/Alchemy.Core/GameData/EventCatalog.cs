using System;
using System.Collections.Generic;
using System.Linq;
using Alchemy.Core.Brewing;
using Alchemy.Core.Random;
using Alchemy.Core.Relics;
using Alchemy.Core.Rooms.Events;

namespace Alchemy.Core.GameData;

/// <summary>
/// 事件目录：占位事件数据（后续可序列化为 .tres/JSON 资源文件）。
/// 每个事件 = 标题 + 2~3 个选项，选项动作全部走 EventExecutor 的现成方法。
/// </summary>
public static class EventCatalog
{
    private static readonly EventDefinition[] _defaultEvents =
    {
        new("wounded_hunter", "受伤的猎人", new[]
        {
            new EventChoice("包扎伤口（恢复 8 生命）", new[]
            {
                new EventAction(EventActionType.Heal, 8),
            }),
            new EventChoice("分给他 30 货币，换一件普通遗物", new[]
            {
                new EventAction(EventActionType.LoseCurrency, 30),
                new EventAction(EventActionType.GainRelic, RelicRarity: RelicRarity.Common),
            }),
            new EventChoice("离开", Array.Empty<EventAction>()),
        }),
        new("abandoned_shelf", "废弃药架", new[]
        {
            new EventChoice("翻找（获得 2 个罕见药材）", new[]
            {
                new EventAction(EventActionType.GainIngredient, IngredientRarity: IngredientRarity.Uncommon, Count: 2),
            }),
            new EventChoice("冒险深入（失去 12 生命，获得 1 个稀有药材）", new[]
            {
                new EventAction(EventActionType.Damage, 12),
                new EventAction(EventActionType.GainIngredient, IngredientRarity: IngredientRarity.Rare, Count: 1),
            }),
            new EventChoice("离开", Array.Empty<EventAction>()),
        }),
        new("greedy_merchant", "贪婪的商人", new[]
        {
            new EventChoice("花 80 货币买一件稀有遗物", new[]
            {
                new EventAction(EventActionType.LoseCurrency, 80),
                new EventAction(EventActionType.GainRelic, RelicRarity: RelicRarity.Rare),
            }),
            new EventChoice("离开", Array.Empty<EventAction>()),
        }),
        new("mystic_spring", "神秘泉水", new[]
        {
            new EventChoice("痛饮（生命上限 +5）", new[]
            {
                new EventAction(EventActionType.GainMaxHp, 5),
            }),
            new EventChoice("离开", Array.Empty<EventAction>()),
        }),
        new("forsaken_altar", "被遗弃的祭坛", new[]
        {
            new EventChoice("献祭（生命上限 -4，获得 1 件稀有遗物）", new[]
            {
                new EventAction(EventActionType.LoseMaxHp, 4),
                new EventAction(EventActionType.GainRelic, RelicRarity: RelicRarity.Rare),
            }),
            new EventChoice("离开", Array.Empty<EventAction>()),
        }),
    };

    private static IReadOnlyList<EventDefinition> _current = _defaultEvents;

    /// <summary>用内容资源（content/events/*.tres，由 Godot 侧注入）整体替换事件目录；空则忽略（保持默认硬编码）。</summary>
    public static void SetCatalog(IReadOnlyCollection<EventDefinition> events)
    {
        if (events == null || events.Count == 0)
        {
            return;
        }

        _current = events.ToList();
    }

    public static EventDefinition Random(IRandomSource rng) => _current[rng.Next(_current.Count)];
}
