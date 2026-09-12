using Alchemy.Core.Brewing;
using Alchemy.Core.Relics;
using Alchemy.Core.Rooms.Events;
using Godot;

namespace Alchemy.Nodes;

/// <summary>一次事件动作：类型 + 参数（纯数据，可在 .tres 里编辑）。</summary>
[GlobalClass]
public partial class EventActionResource : Resource
{
    /// <summary>动作类型（Damage/Heal/GainIngredient…）。</summary>
    [Export] public EventActionType Type { get; set; } = EventActionType.GainCurrency;

    /// <summary>数值（伤害/治疗/货币/格挡等）。</summary>
    [Export] public int Amount { get; set; }

    /// <summary>次数（药材数量等）。</summary>
    [Export] public int Count { get; set; } = 1;

    /// <summary>药材稀有度（Gain/LoseIngredient 用）。</summary>
    [Export] public IngredientRarity IngredientRarity { get; set; } = IngredientRarity.Common;

    /// <summary>遗物稀有度（GainRelic 用；写 Boss 会自动降级为非首领）。</summary>
    [Export] public RelicRarity RelicRarity { get; set; } = RelicRarity.Common;

    public EventAction ToAction() => new(Type, Amount, Count, IngredientRarity, RelicRarity);
}
