using System.Collections.Generic;
using System.Linq;

namespace Alchemy.Core.Brewing;

/// <summary>
/// 一次炼药（应用一份药材）的汇总结果。
/// </summary>
public sealed class BrewingResult
{
    public IReadOnlyList<BrewingOperationResult> Operations { get; }

    public BrewingResult(IReadOnlyList<BrewingOperationResult> operations) => Operations = operations;

    public bool AllSucceeded => Operations.All(o => o.Succeeded);

    public IReadOnlyList<string> FailureReasons =>
        Operations.Where(o => !o.Succeeded)
                  .Select(o => o.FailureReason ?? "未知原因")
                  .ToList();
}
