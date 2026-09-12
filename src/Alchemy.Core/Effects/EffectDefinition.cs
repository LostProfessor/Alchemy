namespace Alchemy.Core.Effects;

/// <summary>
/// 效果定义（纯数据模板）：ID、名称、极性、层数上限、颜色增量、反转配对。
/// 模板不入存档——存档只存"这局有什么"（见策划案存档设计）。
/// </summary>
public sealed record EffectDefinition(
    EffectId Id,
    string DisplayName,
    EffectPolarity Polarity,
    int MaxLayers,
    ColorDelta ColorDelta,
    bool CanComeFromPotion = true)
{
    /// <summary>
    /// 反转极性后的对应效果 ID（增益 ↔ 减益互换）。
    /// </summary>
    public EffectId? OppositeId { get; init; }
}
