namespace Alchemy.Core.Rooms.Events;

/// <summary>
/// 事件动作类型：事件选项对"现有方法"的调用（增/减生命、货币、药材、遗物等）。
/// 事件内容 = 数据（选项 + 动作序列），不是代码类。
/// </summary>
public enum EventActionType
{
    /// <summary>玩家受到伤害（Amount）。</summary>
    Damage,

    /// <summary>玩家恢复生命（Amount）。</summary>
    Heal,

    /// <summary>玩家生命上限 +Amount（当前生命同步增加，参考杀戮尖塔）。</summary>
    GainMaxHp,

    /// <summary>玩家生命上限 -Amount（当前生命被压到新上限）。</summary>
    LoseMaxHp,

    /// <summary>玩家获得格挡（Amount）。</summary>
    GainBlock,

    /// <summary>获得货币（Amount）。</summary>
    GainCurrency,

    /// <summary>失去货币（Amount）。</summary>
    LoseCurrency,

    /// <summary>获得 Count 个指定稀有度的随机药材。</summary>
    GainIngredient,

    /// <summary>随机失去 Count 个药材。</summary>
    LoseIngredient,

    /// <summary>获得一个指定稀有度的随机遗物（不重复已有）。</summary>
    GainRelic,

    /// <summary>随机失去一个已有遗物。</summary>
    LoseRelic,
}
