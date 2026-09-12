namespace Alchemy.Core.Brewing;

/// <summary>
/// 基底液体：决定药水初始颜色与炼药模式（职业）。
/// </summary>
public sealed record BaseLiquid(string Id, string DisplayName, PotionColor BaseColor, BrewingMode Mode);
