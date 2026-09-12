using Alchemy.Core.Brewing;
using Alchemy.Core.Effects;
using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// 词条资源：一个药材的"增加/减少效果"操作，可在 Inspector 编辑，存进药材 .tres。
/// 一个药材可以有多个词条（数组），加/减可混排、按顺序结算。
/// </summary>
[GlobalClass]
public partial class AffixOpResource : Resource
{
    [Export] public AffixOpType Type { get; set; } = AffixOpType.AddEffect;

    [Export] public EffectId Effect { get; set; } = EffectId.None;

    [Export] public int Amount { get; set; } = 1;

    public AffixOp ToAffixOp() => new(Type, Effect, Amount);
}
