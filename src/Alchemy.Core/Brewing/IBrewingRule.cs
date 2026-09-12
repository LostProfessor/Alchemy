namespace Alchemy.Core.Brewing;

/// <summary>
/// 炼药规则（策略模式）：同一瓶药水在不同基底（队列/栈/反转）下对词条操作的不同响应。
/// </summary>
public interface IBrewingRule
{
    BrewingMode Mode { get; }

    BrewingOperationResult Execute(AffixOp op, Potion potion);
}
