using System.Linq;
using Alchemy.Core.Brewing;
using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// 药材资源（Godot Resource）：稀有度 + 词条数组 + 插图 + 文字描述。
/// 在 Inspector 里编辑属性（含拖插图），存成 .tres 内容资源。
/// 由 <see cref="ContentCatalog"/> 扫描并映射成逻辑层 <see cref="Ingredient"/>。
/// </summary>
[GlobalClass]
public partial class IngredientResource : Resource
{
	[Export] public string Id { get; set; } = string.Empty;

	[Export] public string DisplayName { get; set; } = string.Empty;

	/// <summary>文字描述（图鉴/悬停提示用）。</summary>
	[Export] public string Description { get; set; } = string.Empty;

	[Export] public IngredientRarity Rarity { get; set; } = IngredientRarity.Common;

	/// <summary>插图（Inspector 直接拖贴图）。</summary>
	[Export] public Texture2D? Icon { get; set; }

	/// <summary>词条组合：多个词条按顺序结算，加/减可混排。</summary>
	[Export] public AffixOpResource[] AffixOps { get; set; } = [];

	/// <summary>映射成逻辑层药材。</summary>
	public Ingredient ToIngredient() => new(
		Id,
		DisplayName,
		Rarity,
		AffixOps.Select(op => op.ToAffixOp()).ToList());
}
