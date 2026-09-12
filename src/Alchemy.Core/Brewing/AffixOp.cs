using Alchemy.Core.Effects;

namespace Alchemy.Core.Brewing;

/// <summary>
/// 一次词条操作。带便捷工厂方法，方便构造药材定义。
/// </summary>
public readonly record struct AffixOp(AffixOpType Type, EffectId Effect = EffectId.None, int Amount = 0)
{
    public static AffixOp Add(EffectId effect, int layers = 1) => new(AffixOpType.AddEffect, effect, layers);

    public static AffixOp Remove(EffectId effect, int layers = 1) => new(AffixOpType.RemoveEffect, effect, layers);

    public static AffixOp RemoveLast(int count = 1) => new(AffixOpType.RemoveLast, Amount: count);

    public static AffixOp ReverseOrder() => new(AffixOpType.ReverseOrder);

    public static AffixOp ReversePolarity() => new(AffixOpType.ReversePolarity);

    public override string ToString() => Type switch
    {
        AffixOpType.AddEffect => $"+{Effect}x{Amount}",
        AffixOpType.RemoveEffect => $"-{Effect}x{Amount}",
        AffixOpType.RemoveLast => $"去尾x{Amount}",
        AffixOpType.ReverseOrder => "翻转顺序",
        AffixOpType.ReversePolarity => "反转极性",
        _ => Type.ToString(),
    };
}
