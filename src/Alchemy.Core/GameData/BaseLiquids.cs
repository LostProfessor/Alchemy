using Alchemy.Core.Brewing;

namespace Alchemy.Core.GameData;

/// <summary>
/// 三种基底液体 = 三个"职业"（不同的炼药模式与配色）。
/// </summary>
public static class BaseLiquids
{
    /// <summary>队列基底：配方以"按进入顺序挤出最旧"为核心。</summary>
    public static readonly BaseLiquid Aqua = new("aqua", "清水", new PotionColor(180, 220, 255), BrewingMode.Queue);

    /// <summary>栈基底：配方以"栈满拒绝、先清后添"为核心。</summary>
    public static readonly BaseLiquid Oil = new("oil", "浓油", new PotionColor(200, 170, 90), BrewingMode.Stack);

    /// <summary>黏液基底：配方以"添加↔移除互换"为核心。</summary>
    public static readonly BaseLiquid Turbid = new("slime", "黏液", new PotionColor(160, 90, 190), BrewingMode.ReversedQueue);

    public static BaseLiquid ForMode(BrewingMode mode) => mode switch
    {
        BrewingMode.Queue => Aqua,
        BrewingMode.Stack => Oil,
        BrewingMode.ReversedQueue => Turbid,
        _ => Aqua,
    };
}
