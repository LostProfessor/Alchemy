using System;
using Alchemy.Core.Effects;

namespace Alchemy.Core.Brewing;

/// <summary>
/// 药水颜色（0~255 三通道）。基底颜色 + 各效果 RGB 增量，越界自动 clamp。
/// </summary>
public readonly record struct PotionColor(byte R, byte G, byte B)
{
    public static PotionColor Clamp(int r, int g, int b) =>
        new((byte)Math.Clamp(r, 0, 255), (byte)Math.Clamp(g, 0, 255), (byte)Math.Clamp(b, 0, 255));

    public static PotionColor operator +(PotionColor color, ColorDelta delta) =>
        Clamp(color.R + delta.R, color.G + delta.G, color.B + delta.B);

    public override string ToString() => $"RGB({R},{G},{B})";
}
