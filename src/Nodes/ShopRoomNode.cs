using System.Collections.Generic;
using System.Linq;
using Alchemy.Core.Brewing;
using Alchemy.Core.GameData;
using Alchemy.Core.Rooms;
using Alchemy.Core.Relics;
using Alchemy.Core.Runs.Rewards;
using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// 商店房场景（scenes/room/shop.tscn）：出售 遗物 + 药材袋，可逐件购买（TryBuyItem），
/// "离开商店" → 完成该房 → 回地图。
/// </summary>
public partial class ShopRoomNode : RoomNodeBase
{
	private Label? _moneyLabel;

	protected override void Render()
	{
		ClearContent();
		var mgr = Manager;
		if (mgr?.CurrentRoom is not ShopRoom room)
		{
			ShowMissing("当前不在商店房");
			return;
		}

		var title = new Label
		{
			Text = L.T("🏪 商店"),
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		title.AddThemeFontSizeOverride("font_size", 34);
		Content.AddChild(title);

		Content.AddChild(new Label
		{
			Text = L.T("老板神秘地笑了笑，示意你看看货架。"),
			HorizontalAlignment = HorizontalAlignment.Center,
			Modulate = new Color(0.75f, 0.75f, 0.75f),
		});
		Content.AddChild(new HSeparator());

		_moneyLabel = new Label { Text = L.F("货币：{0}", mgr.Run.Currency) };
		_moneyLabel.AddThemeFontSizeOverride("font_size", 22);
		Content.AddChild(_moneyLabel);

		Content.AddChild(new HSeparator());

		for (int i = 0; i < room.Stock.Count; i++)
		{
			var item = room.Stock[i];
			bool affordable = mgr.Run.Currency >= item.Price;

			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 12);

			var info = new Label
			{
				Text = L.F("{0}   ·   {1} 货币", ItemText(item), item.Price),
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				Modulate = ItemColor(item),
			};
			row.AddChild(info);

			var buy = new Button
			{
				Text = item.Sold ? L.T("已售") : L.T("购买"),
				Disabled = item.Sold || !affordable,
			};
			int index = i;
			buy.Pressed += () => Buy(index);
			row.AddChild(buy);

			Content.AddChild(row);
		}

		Content.AddChild(new HSeparator());

		var leave = new Button
		{
			Text = L.T("离开商店"),
			CustomMinimumSize = new Vector2(420, 0),
		};
		leave.Pressed += () =>
		{
			Manager?.LeaveShop(); // 完成该房（Phase → OnMap）
			BackToMap();
		};
		Content.AddChild(leave);
	}

	private void Buy(int index)
	{
		var mgr = Manager;
		if (mgr == null)
		{
			return;
		}

		if (mgr.TryBuyItem(index))
		{
			Render(); // 购买成功：刷新货币与货架状态
		}
		else if (_moneyLabel != null)
		{
			_moneyLabel.Text = L.F("货币：{0}（买不起这件）", mgr.Run.Currency);
		}
	}

	/// <summary>商品文案：遗物显示名字；药材袋显示内容（同种合并 ×N）。</summary>
	private static string ItemText(ShopItem item)
	{
		if (item.IsRelic && item.Relic != null)
		{
			return L.T(ContentCatalog.GetRelicResource(item.Relic.Id)?.DisplayName ?? item.Relic.DisplayName);
		}

		if (item.IsIngredientBag && item.IngredientBag != null)
		{
			return L.F("药材袋：{0}", BagSummary(item.IngredientBag));
		}

		return "?";
	}

	/// <summary>商品颜色：遗物按稀有度；药材袋按其最高稀有度。</summary>
	private static Color ItemColor(ShopItem item)
	{
		if (item.IsRelic && item.Relic != null)
		{
			return RarityColor(item.Relic.Rarity);
		}

		if (item.IsIngredientBag && item.IngredientBag != null)
		{
			var best = item.IngredientBag.Items
				.OfType<IngredientReward>()
				.Select(ir => RarityOf(ir.IngredientId))
				.DefaultIfEmpty(IngredientRarity.Common)
				.Max();
			return IngredientRarityColor(best);
		}

		return Colors.White;
	}

	private static IngredientRarity RarityOf(string id) =>
		Ingredients.Default.All.FirstOrDefault(i => i.Id == id)?.Rarity ?? IngredientRarity.Common;

	private static string BagSummary(RewardBag bag)
	{
		var grouped = bag.Items
			.OfType<IngredientReward>()
			.GroupBy(ir => ir.IngredientId)
			.Select(g => $"{IngredientName(g.Key)}×{g.Sum(x => x.Count)}");
		return string.Join("、", grouped);
	}

	private static string IngredientName(string id) =>
		ContentCatalog.GetIngredientResource(id)?.DisplayName
		?? Ingredients.Default.All.FirstOrDefault(i => i.Id == id)?.DisplayName
		?? id;

	private static Color RarityColor(RelicRarity rarity) => rarity switch
	{
		RelicRarity.Common => Colors.White,
		RelicRarity.Uncommon => new Color(0.4f, 0.9f, 0.4f),
		RelicRarity.Rare => new Color(0.4f, 0.6f, 1f),
		RelicRarity.Boss => new Color(1f, 0.7f, 0.2f),
		_ => Colors.White,
	};

	private static Color IngredientRarityColor(IngredientRarity rarity) => rarity switch
	{
		IngredientRarity.Common => Colors.White,
		IngredientRarity.Uncommon => new Color(0.4f, 0.9f, 0.4f),
		IngredientRarity.Rare => new Color(0.4f, 0.6f, 1f),
		IngredientRarity.Legendary => new Color(1f, 0.7f, 0.2f),
		_ => Colors.White,
	};
}
