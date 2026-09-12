namespace Alchemy.Core.Brewing;

/// <summary>
/// 单次词条操作的结果。
/// </summary>
/// <param name="Succeeded">是否成功（栈满拒绝等为失败）。</param>
/// <param name="FailureReason">失败原因（本地化前的键/描述）。</param>
/// <param name="AffectedCount">受影响数量（移除/挤出的效果数）。</param>
public readonly record struct BrewingOperationResult(
    bool Succeeded,
    string? FailureReason = null,
    int AffectedCount = 0)
{
    public static BrewingOperationResult Success { get; } = new(true);

    public static BrewingOperationResult SuccessWith(int affectedCount) =>
        new(true, null, affectedCount);

    public static BrewingOperationResult Failed(string reason) => new(false, reason);
}
