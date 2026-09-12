using System.Linq;
using Alchemy.Core.Runs;
using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// 职业资源（content/jobs/*.tres）：职业的"唯一主数据"，方便后续加新职业（= 新增一个 .tres）。
/// 与玩法解耦：<see cref="BaseLiquidId"/> 只是该职业关联的基底（炼药模式，战斗中可切换）；
/// 遗物/开局药材/立绘/口袋背景等与职业强绑定。立绘与口袋背景暂留空（等美术）。
/// </summary>
[GlobalClass]
public partial class JobResource : Resource
{
	/// <summary>职业唯一 id（如 researcher / elf / et）。</summary>
	[Export] public string Id { get; set; } = string.Empty;

	/// <summary>显示名（研究员 / 精灵 / E.T.）。</summary>
	[Export] public string DisplayName { get; set; } = string.Empty;

	/// <summary>关联基底 id（aqua/oil/slime）——决定默认炼药玩法，与职业解耦。</summary>
	[Export] public string BaseLiquidId { get; set; } = "aqua";

	/// <summary>开局专属遗物 id（如 aqua_craft）。</summary>
	[Export] public string RelicId { get; set; } = string.Empty;

	/// <summary>开局赠送的药材。</summary>
	[Export] public JobStarterIngredient[] StarterIngredients { get; set; } = [];

	// ── 表现资源（与职业强绑定，暂留空等美术）────────────
	/// <summary>角色立绘。</summary>
	[Export] public Texture2D? Portrait { get; set; }

	/// <summary>口袋背景（放置药材卡片的区域材质）。</summary>
	[Export] public Texture2D? PocketBackground { get; set; }

	/// <summary>映射成逻辑层职业定义（供 JobCatalog）。</summary>
	public JobDefinition ToJobDefinition() => new(
		Id, DisplayName, BaseLiquidId, RelicId,
		(StarterIngredients ?? []).Select(s => (Id: s.Id, Count: s.Count)).ToList());
}
