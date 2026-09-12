namespace Alchemy.Core.Brewing;

/// <summary>
/// 栈规则（LIFO）：效果压栈顶（尾部）；栈满（5 格）后拒绝添加，除非先用移除类词条腾出空间。
/// </summary>
public sealed class StackBrewingRule : IBrewingRule
{
    public static StackBrewingRule Instance { get; } = new();

    public BrewingMode Mode => BrewingMode.Stack;

    public BrewingOperationResult Execute(AffixOp op, Potion potion)
    {
        switch (op.Type)
        {
            case AffixOpType.AddEffect:
                // 每层占一格：剩余格子不够放不下时整体拒绝
                if (potion.Entries.Count + op.Amount > Potion.MaxSlots)
                {
                    return BrewingOperationResult.Failed("栈已满，无法添加效果，需先移除");
                }

                for (int i = 0; i < op.Amount; i++)
                {
                    potion.Entries.Add(new EffectEntry(op.Effect, 1));
                }

                return BrewingOperationResult.Success;

            case AffixOpType.RemoveEffect:
                return BrewingOperationResult.SuccessWith(
                    BrewingOps.RemoveLayers(potion.Entries, op.Effect, op.Amount));

            case AffixOpType.RemoveLast:
                return BrewingOperationResult.SuccessWith(
                    BrewingOps.RemoveLast(potion.Entries, op.Amount > 0 ? op.Amount : 1));

            case AffixOpType.ReverseOrder:
                potion.Entries.Reverse();
                return BrewingOperationResult.Success;

            case AffixOpType.ReversePolarity:
                BrewingOps.ReversePolarity(potion.Entries);
                return BrewingOperationResult.Success;

            default:
                return BrewingOperationResult.Failed($"未知词条操作: {op.Type}");
        }
    }
}
