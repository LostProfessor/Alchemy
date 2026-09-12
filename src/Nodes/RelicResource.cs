using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// 遗物资源（content/relics/*.tres）：遗物的"表现数据"（显示名/描述/图标），
/// 供 UI 展示。逻辑遗物实例（Alchemy.Core.Relics.Relic，按 Id 唯一）与它**按 Id 绑定**：
/// 界面拿到逻辑遗物实例后，用 <see cref="ContentCatalog.GetRelicResource(string)"/> 按 Id 取对应 .tres。
/// 没有对应 .tres 时 UI 回退逻辑层遗物自带的 DisplayName（描述留空），因此"名称/描述资源化"是渐进式的。
/// </summary>
[GlobalClass]
public partial class RelicResource : Resource
{
	/// <summary>遗物唯一 id（与逻辑层遗物 Id 一致，如 aqua_craft / iron_bracer）。</summary>
	[Export] public string Id { get; set; } = string.Empty;

	/// <summary>显示名（炼金典籍 / 腐蚀手册 …）。</summary>
	[Export] public string DisplayName { get; set; } = string.Empty;

	/// <summary>描述（悬停/图鉴展示用）。</summary>
	[Export] public string Description { get; set; } = string.Empty;

	/// <summary>图标（Inspector 直接拖贴图；暂可留空，UI 会回退用文字首字占位）。</summary>
	[Export] public Texture2D? Icon { get; set; }
}
