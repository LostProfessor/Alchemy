namespace Alchemy.Core.Runs.Rewards;

/// <summary>奖励条目基类（药材/货币/遗物，遗物后续扩展）。</summary>
public abstract record RewardItem
{
    /// <summary>稳定比较键（用于判断两袋内容是否完全相同）。</summary>
    public abstract string SortKey { get; }
}

/// <summary>药材奖励：指定药材及其数量。</summary>
public sealed record IngredientReward(string IngredientId, int Count = 1) : RewardItem
{
    public override string SortKey => $"ing:{IngredientId}:{Count}";
}

/// <summary>货币奖励。</summary>
public sealed record CurrencyReward(int Amount) : RewardItem
{
    public override string SortKey => $"gold:{Amount}";
}
