namespace Alchemy.Core.Effects;

/// <summary>
/// 效果极性。用于"反转所有效果"词条：增益 ↔ 减益互换。
/// </summary>
public enum EffectPolarity
{
    /// <summary>增益（对持有者有利）。</summary>
    Positive,

    /// <summary>减益（对持有者不利）。</summary>
    Negative,

    /// <summary>中性（暂未用到，预留）。</summary>
    Neutral,
}
