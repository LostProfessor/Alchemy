using Alchemy.Core.Brewing;
using Alchemy.Core.Relics;

namespace Alchemy.Core.Rooms.Events;

/// <summary>
/// 一次事件动作：类型 + 参数。全部字段都有默认值，构造时按需命名传参。
/// 这是纯数据，可被序列化为 .tres/JSON 资源文件。
/// </summary>
public sealed record EventAction(
    EventActionType Type,
    int Amount = 0,
    int Count = 1,
    IngredientRarity IngredientRarity = IngredientRarity.Common,
    RelicRarity RelicRarity = RelicRarity.Common);
