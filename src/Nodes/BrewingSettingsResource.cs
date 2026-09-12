using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// 炼药设置资源（content/settings/brewing_settings.tres）：炼药耗时等可调数值。
/// 启动时经 ContentCatalog.BuildBrewingSettings 注入逻辑层 BrewingTimings ——
/// 在 Inspector 里改 .tres 数值即可调平衡，零编译。
/// 以后要加其它全局平衡数值（火堆回血比例/奖励区间/起始 HP 等），在此加字段并让逻辑层消费。
/// </summary>
[GlobalClass]
public partial class BrewingSettingsResource : Resource
{
	/// <summary>加一味药材耗时（秒）。</summary>
	[Export(PropertyHint.Range, "0.1,10,0.1")]
	public float AddIngredientSeconds { get; set; } = 2f;

	/// <summary>完成制作耗时（秒）。</summary>
	[Export(PropertyHint.Range, "0.1,20,0.1")]
	public float CompletePotionSeconds { get; set; } = 3f;
}
