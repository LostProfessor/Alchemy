namespace Alchemy.Core.Relics;

// ── 职业专属遗物（开局按职业赠送，不入随机池；占位无效果，效果后续补）──

/// <summary>清水系·炼金典籍（占位）。</summary>
public sealed class AquaCraft : Relic
{
    public AquaCraft() : base("aqua_craft", "炼金典籍", RelicRarity.Common)
    {
    }
}

/// <summary>浓油系·腐蚀手册（占位）。</summary>
public sealed class OilCraft : Relic
{
    public OilCraft() : base("oil_craft", "腐蚀手册", RelicRarity.Common)
    {
    }
}

/// <summary>黏液系·反转卷轴（占位）。</summary>
public sealed class SlimeCraft : Relic
{
    public SlimeCraft() : base("slime_craft", "反转卷轴", RelicRarity.Common)
    {
    }
}
