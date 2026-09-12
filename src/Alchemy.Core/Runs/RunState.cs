using Alchemy.Core.Relics;

namespace Alchemy.Core.Runs;

/// <summary>
/// 整局状态（后续扩展：地图、当前房间、奖励历史等），随存档持久化。
/// </summary>
public sealed class RunState
{
    /// <summary>药材口袋：局内累积、一次性消耗、无 CD。</summary>
    public IngredientPocket Pocket { get; } = new();

    /// <summary>遗物栏：整局增益，跨战斗生效。</summary>
    public RelicInventory Relics { get; } = new();

    /// <summary>货币：做成玩家专属属性，不单独建类（策划案确认）。</summary>
    public int Currency { get; set; }
}
