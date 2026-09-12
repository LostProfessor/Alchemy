using System.Collections.Generic;

namespace Alchemy.Core.Brewing;

/// <summary>
/// 药材定义（模板）：稀有度 + 一串按顺序结算的词条操作。
/// 药材从"口袋"取用后即消耗（一次性）。
/// </summary>
public sealed record Ingredient(
	string Id,
	string DisplayName,
	IngredientRarity Rarity,
	IReadOnlyList<AffixOp> AffixOps);
