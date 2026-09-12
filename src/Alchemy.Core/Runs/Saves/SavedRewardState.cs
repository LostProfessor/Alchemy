using System.Collections.Generic;

namespace Alchemy.Core.Runs.Saves;

/// <summary>
/// 奖励界面阶段（Reward / BossRelicChoice）的存档快照。
/// 有它=读档后应回到同样奖励界面继续选择（而非直接回地图把待领奖励销毁）。
/// 无它（null）= 非奖励阶段，读档回地图检查点。
/// </summary>
public sealed class SavedRewardState
{
	/// <summary>阶段：RunPhase.Reward 的 ToString() → "Reward"；BossRelicChoice → "BossRelicChoice"。</summary>
	public string Phase { get; set; } = "";

	/// <summary>当前是否为首领战的战斗奖励（领完 → 首领遗物三选一）。</summary>
	public bool IsBossRewardPending { get; set; }

	/// <summary>待领货币（未领才非空；已领=null）。</summary>
	public int? PendingRewardCurrency { get; set; }

	/// <summary>待领额外遗物 Id（精英/首领战；未领才非空）。</summary>
	public string? PendingBonusRelicId { get; set; }

	/// <summary>待三选一的药材袋快照（Reward 阶段未领时非空）。</summary>
	public List<SavedRewardBag>? PendingBags { get; set; }

	/// <summary>首领遗物三选一候选 Id 列表（BossRelicChoice 阶段非空）。</summary>
	public List<string>? PendingBossRelicIds { get; set; }

	/// <summary>首领战待领：跨层前重掷首领遗物候选需要的 Boss 池 id（= Encounter.BossId）。</summary>
	public string? PendingBossId { get; set; }
}

/// <summary>一个奖励袋的持久化快照（内容 = 若干条目）。</summary>
public sealed class SavedRewardBag
{
	public List<SavedRewardItem> Items { get; set; } = new();
}

/// <summary>一条奖励条目的持久化快照。</summary>
public sealed class SavedRewardItem
{
	/// <summary>条目类型：Ingredient / Currency。</summary>
	public string Kind { get; set; } = "";

	/// <summary>药材 Id（Kind=Ingredient 时）。</summary>
	public string IngredientId { get; set; } = "";

	/// <summary>药材数量（Kind=Ingredient 时）。</summary>
	public int Count { get; set; } = 1;

	/// <summary>货币数额（Kind=Currency 时）。</summary>
	public int Amount { get; set; }
}
