using System;

namespace Alchemy.Core.Brewing;

public static class BrewingRuleFactory
{
	public static IBrewingRule Create(BrewingMode mode) => mode switch
	{
		BrewingMode.Queue => QueueBrewingRule.Instance,
		BrewingMode.Stack => StackBrewingRule.Instance,
		BrewingMode.ReversedQueue => ReversedBrewingRule.Instance,
		_ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "未知的炼药模式"),
	};
}
