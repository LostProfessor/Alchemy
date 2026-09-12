using System.Collections.Generic;

namespace Alchemy.Core.Effects;

/// <summary>
/// 效果中央注册表：所有效果的"模板定义" + 反转配对表。
/// 反转配对是占位设计（增益↔减益），具体配对待策划确认后在此调整。
/// </summary>
public static class EffectRegistry
{
    private static readonly Dictionary<EffectId, EffectDefinition> _definitions = new();

    public static IReadOnlyDictionary<EffectId, EffectDefinition> Definitions => _definitions;

    static EffectRegistry()
    {
        // ── 占位反转配对（⚠️ 待你确认）────────────────────────────
        // 恢复↔腐蚀 / 铁皮↔易感 / 专注↔迟钝 / 精确↔淤伤 / 不朽↔烈毒 / 棘皮↔致幻 / 涤净↔药物依赖
        // 每种效果都配上了对立面，改表即可，引擎不感知具体配对。
        // ─────────────────────────────────────────────────────────

        Register(new EffectDefinition(EffectId.Heal, "恢复", EffectPolarity.Positive, 5, new ColorDelta(0, 15, 5))
        {
            OppositeId = EffectId.Corrode,
        });
        Register(new EffectDefinition(EffectId.Corrode, "腐蚀", EffectPolarity.Negative, 5, new ColorDelta(22, -8, -8))
        {
            OppositeId = EffectId.Heal,
        });

        Register(new EffectDefinition(EffectId.IronSkin, "铁皮", EffectPolarity.Positive, 5, new ColorDelta(5, 5, 12))
        {
            OppositeId = EffectId.Vulnerable,
        });
        Register(new EffectDefinition(EffectId.Vulnerable, "易感", EffectPolarity.Negative, 5, new ColorDelta(18, 6, -4))
        {
            OppositeId = EffectId.IronSkin,
        });

        Register(new EffectDefinition(EffectId.Focus, "专注", EffectPolarity.Positive, 5, new ColorDelta(14, 14, -10))
        {
            OppositeId = EffectId.Sluggish,
        });
        Register(new EffectDefinition(EffectId.Sluggish, "迟钝", EffectPolarity.Negative, 5, new ColorDelta(-8, -8, 6))
        {
            OppositeId = EffectId.Focus,
        });

        Register(new EffectDefinition(EffectId.Precise, "精确", EffectPolarity.Positive, 5, new ColorDelta(16, 12, -12))
        {
            OppositeId = EffectId.Bruise,
        });
        Register(new EffectDefinition(EffectId.Bruise, "淤伤", EffectPolarity.Negative, 5, new ColorDelta(-6, 0, 16))
        {
            OppositeId = EffectId.Precise,
        });

        Register(new EffectDefinition(EffectId.Immortal, "不朽", EffectPolarity.Positive, 5, new ColorDelta(10, 16, 12))
        {
            OppositeId = EffectId.VirulentPoison,
        });
        Register(new EffectDefinition(EffectId.VirulentPoison, "烈毒", EffectPolarity.Negative, 5, new ColorDelta(10, -4, 16))
        {
            OppositeId = EffectId.Immortal,
        });

        Register(new EffectDefinition(EffectId.Thorny, "棘皮", EffectPolarity.Positive, 5, new ColorDelta(16, 4, -12))
        {
            OppositeId = EffectId.Hallucinate,
        });
        Register(new EffectDefinition(EffectId.Hallucinate, "致幻", EffectPolarity.Negative, 5, new ColorDelta(16, 0, 16))
        {
            OppositeId = EffectId.Thorny,
        });

        Register(new EffectDefinition(EffectId.Purify, "涤净", EffectPolarity.Positive, 5, new ColorDelta(12, 12, 12))
        {
            OppositeId = EffectId.Dependency,
        });
        Register(new EffectDefinition(EffectId.Dependency, "药物依赖", EffectPolarity.Negative, 8, new ColorDelta(8, 0, 16))
        {
            OppositeId = EffectId.Purify,
        });

        // ── 非药水提供效果（CanComeFromPotion = false，颜色增量为 0、无反转配对）──
        Register(new EffectDefinition(EffectId.Resistance, "抗药性", EffectPolarity.Positive, 5, ColorDelta.Zero, CanComeFromPotion: false));
        Register(new EffectDefinition(EffectId.Toughness, "坚韧", EffectPolarity.Positive, 1, ColorDelta.Zero, CanComeFromPotion: false));
        Register(new EffectDefinition(EffectId.Blessing, "祝福", EffectPolarity.Positive, 1, ColorDelta.Zero, CanComeFromPotion: false));
        Register(new EffectDefinition(EffectId.Penetration, "击穿", EffectPolarity.Positive, 5, ColorDelta.Zero, CanComeFromPotion: false));
        Register(new EffectDefinition(EffectId.Deepen, "加深", EffectPolarity.Positive, 1, ColorDelta.Zero, CanComeFromPotion: false));
        Register(new EffectDefinition(EffectId.Dissolve, "消解", EffectPolarity.Negative, 1, ColorDelta.Zero, CanComeFromPotion: false));
    }

    private static void Register(EffectDefinition definition) => _definitions.Add(definition.Id, definition);

    public static EffectDefinition Get(EffectId id) => _definitions[id];

    public static bool TryGet(EffectId id, out EffectDefinition definition) =>
        _definitions.TryGetValue(id, out definition!);

    /// <summary>取反转配对。无配对时返回 false。</summary>
    public static bool TryGetOpposite(EffectId id, out EffectId opposite)
    {
        if (_definitions.TryGetValue(id, out var definition) && definition.OppositeId is { } opp)
        {
            opposite = opp;
            return true;
        }

        opposite = default;
        return false;
    }
}
