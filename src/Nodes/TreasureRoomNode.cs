using System;
using System.Linq;
using Alchemy.Core.GameData;
using Alchemy.Core.Rooms;
using Alchemy.Core.Runs.Rewards;
using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// 宝藏房场景（scenes/room/treasure.tscn）：进房时宝藏已生成（遗物 + 货币 + 药材袋）。
/// 奖励拆成**逐块**：货币/遗物/药材袋各一个"领取"按钮，可只领其中几块，也可"离开"放弃剩余——
/// 交互与战斗奖励面板一致（点一块领一块，全领或离开才完成房间回地图）。
/// </summary>
public partial class TreasureRoomNode : RoomNodeBase
{
	protected override void Render()
	{
		ClearContent();
		var mgr = Manager;
		if (mgr?.CurrentRoom is not TreasureRoom room || room.Treasure == null)
		{
			ShowMissing("当前不在宝藏房");
			return;
		}

		var reward = room.Treasure;

		var title = new Label
		{
			Text = L.T("💎 宝箱"),
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		title.AddThemeFontSizeOverride("font_size", 34);
		Content.AddChild(title);

		Content.AddChild(new Label
		{
			Text = L.T("一段旅程的馈赠。点想要的奖励领取（可只拿其中几样）"),
			HorizontalAlignment = HorizontalAlignment.Center,
			Modulate = new Color(0.75f, 0.75f, 0.75f),
		});
		Content.AddChild(new HSeparator());

		// 逐块：货币 / 遗物 / 药材袋（已领的块不再显示）
		if (!reward.CurrencyClaimed)
		{
			Content.AddChild(ClaimButton(L.F("获取货币  +{0}", reward.Currency), mgr.ClaimTreasureCurrency));
		}

		if (reward.Relic != null && !reward.RelicClaimed)
		{
			var relic = reward.Relic;
			var btn = ClaimButton(
				L.F("获得遗物：{0}", L.T(ContentCatalog.GetRelicResource(relic.Id)?.DisplayName ?? relic.DisplayName)),
				mgr.ClaimTreasureRelic);
			btn.AddThemeColorOverride("font_color", RarityColor(relic.Rarity));
			Content.AddChild(btn);
		}

		if (!reward.BagClaimed)
		{
			Content.AddChild(ClaimButton(L.F("获取药材袋：{0}", BagSummary(reward.Ingredients)), mgr.ClaimTreasureBag));
		}

		Content.AddChild(new HSeparator());
		var leave = new Button
		{
			Text = L.T("离开（放弃剩余奖励）"),
			CustomMinimumSize = new Vector2(420, 0),
		};
		leave.Pressed += () =>
		{
			mgr.FinishTreasure(); // 未领部分作废，完成房间
			BackToMap();
		};
		Content.AddChild(leave);
	}

	/// <summary>一块奖励的领取按钮：点它领取对应块，然后刷新/收尾。</summary>
	private Button ClaimButton(string text, Func<bool> claim)
	{
		var btn = new Button { Text = text, CustomMinimumSize = new Vector2(420, 0) };
		btn.Pressed += () =>
		{
			if (claim())
			{
				AfterClaim();
			}
		};
		return btn;
	}

	/// <summary>领完一块后：全领完 → 完成房间回地图；否则重绘剩余块。</summary>
	private void AfterClaim()
	{
		var mgr = Manager;
		if (mgr?.CurrentRoom is TreasureRoom { Treasure: { } reward })
		{
			if (reward.AllClaimed)
			{
				mgr.FinishTreasure();
				BackToMap();
				return;
			}

			Render(); // 还有未领块 → 重建界面（已领的消失）
			return;
		}

		BackToMap();
	}

	private static string BagSummary(RewardBag bag) =>
		string.Join("\n", bag.Items.Select(item => item switch
		{
			IngredientReward ir => $"{L.T(IngredientName(ir.IngredientId))} ×{ir.Count}",
			CurrencyReward cr => L.F("货币 {0}", cr.Amount),
			_ => "?",
		}));

	private static string IngredientName(string id) =>
		Ingredients.Default.All.FirstOrDefault(i => i.Id == id)?.DisplayName ?? id;

	private static Color RarityColor(Alchemy.Core.Relics.RelicRarity rarity) => rarity switch
	{
		Alchemy.Core.Relics.RelicRarity.Common => Colors.White,
		Alchemy.Core.Relics.RelicRarity.Uncommon => new Color(0.4f, 0.9f, 0.4f),
		Alchemy.Core.Relics.RelicRarity.Rare => new Color(0.4f, 0.6f, 1f),
		Alchemy.Core.Relics.RelicRarity.Boss => new Color(1f, 0.7f, 0.2f),
		_ => Colors.White,
	};
}
