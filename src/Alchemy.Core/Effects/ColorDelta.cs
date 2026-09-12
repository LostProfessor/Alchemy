namespace Alchemy.Core.Effects;

/// <summary>
/// RGB 通道的整数增量。每种效果对药水颜色进行一次加减运算（可乘层数）。
/// 最终颜色在 <see cref="Brewing.PotionColor"/> 中 clamp 到 0~255。
/// </summary>
public readonly record struct ColorDelta(int R, int G, int B)
{
    public static ColorDelta Zero => new(0, 0, 0);

    public static ColorDelta operator *(ColorDelta d, int layers) =>
        new(d.R * layers, d.G * layers, d.B * layers);

    public static ColorDelta operator +(ColorDelta a, ColorDelta b) =>
        new(a.R + b.R, a.G + b.G, a.B + b.B);

    public override string ToString() => $"ΔRGB({R},{G},{B})";
}
