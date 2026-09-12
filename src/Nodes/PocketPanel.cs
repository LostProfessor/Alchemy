using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// 口袋面板预制体脚本（scenes/ui/pocket.tscn）：
/// 外层 PanelContainer 是可换材质的背景框（以后给不同职业画口袋材质 = 改根节点 panel 样式/贴图），
/// 内部 ScrollContainer 负责横向滚动，药材卡（IngredientCard）加到 <see cref="Bar"/> 里。
/// </summary>
public partial class PocketPanel : PanelContainer
{
	[Export] private ScrollContainer _scroll = null!; // 滚动容器
	[Export] private HBoxContainer _bar = null!;      // 卡片栏（放材料卡）

	/// <summary>卡片栏（HBoxContainer）——CombatNode 把材料卡加到这里。</summary>
	public HBoxContainer Bar => _bar;

	/// <summary>清空卡片栏（口袋刷新重建时调用）。</summary>
	public void Clear()
	{
		foreach (var child in _bar.GetChildren())
		{
			child.QueueFree();
		}
	}
}
