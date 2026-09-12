using System.Collections.Generic;

namespace Alchemy.Core.Brewing;

/// <summary>
/// 炼药引擎：按顺序把药材的词条操作依次施加到药水上（先放入的先结算）。
/// </summary>
public sealed class BrewingEngine
{
    public BrewingMode Mode { get; }

    private readonly IBrewingRule _rule;

    public BrewingEngine(BrewingMode mode)
    {
        Mode = mode;
        _rule = BrewingRuleFactory.Create(mode);
    }

    public BrewingResult Apply(Ingredient ingredient, Potion potion)
    {
        var results = new List<BrewingOperationResult>();
        foreach (var op in ingredient.AffixOps)
        {
            results.Add(_rule.Execute(op, potion));
        }

        return new BrewingResult(results);
    }
}
