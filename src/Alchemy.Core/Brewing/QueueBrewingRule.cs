namespace Alchemy.Core.Brewing;

/// <summary>
/// 队列规则（FIFO）：效果从队尾加入；超过 5 格时最旧的（队首）被挤出。
/// </summary>
public sealed class QueueBrewingRule : IBrewingRule
{
    public static QueueBrewingRule Instance { get; } = new();

    public BrewingMode Mode => BrewingMode.Queue;

    public BrewingOperationResult Execute(AffixOp op, Potion potion)
    {
        switch (op.Type)
        {
            case AffixOpType.AddEffect:
                // 每层占一格：层数 = 加入的格子数（2 级恢复 = 2 格）
                for (int i = 0; i < op.Amount; i++)
                {
                    potion.Entries.Add(new EffectEntry(op.Effect, 1));
                }

                while (potion.Entries.Count > Potion.MaxSlots)
                {
                    potion.Entries.RemoveAt(0); // 挤出最旧的
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
