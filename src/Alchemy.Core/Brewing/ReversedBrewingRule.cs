namespace Alchemy.Core.Brewing;

/// <summary>
/// 反转队列规则：采用正常队列（FIFO，可溢出挤出），但所有词条操作作用相反——添加↔移除。
/// 顺序/极性类操作不受影响（自身的逆）。
/// </summary>
public sealed class ReversedBrewingRule : IBrewingRule
{
	public static ReversedBrewingRule Instance { get; } = new();

	public BrewingMode Mode => BrewingMode.ReversedQueue;

	public BrewingOperationResult Execute(AffixOp op, Potion potion)
	{
		var transformed = op.Type switch
		{
			AffixOpType.AddEffect => op with { Type = AffixOpType.RemoveEffect },
			AffixOpType.RemoveEffect => op with { Type = AffixOpType.AddEffect },
			_ => op,
		};

		return QueueBrewingRule.Instance.Execute(transformed, potion);
	}
}
