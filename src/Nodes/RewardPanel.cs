using System;
using System.Linq;
using Alchemy.Core.GameData;
using Alchemy.Core.Relics;
using Alchemy.Core.Runs;
using Alchemy.Core.Runs.Rewards;
using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// 奖励三选一面板（scenes/ui/reward_panel.tscn）：战斗胜利后弹出，
/// 展示"三选一奖励袋"，点选后领取（PickReward）并触发 <see cref="RewardsClaimed"/>（战斗场景据此回地图）。
/// </summary>
public partial class RewardPanel : PanelContainer
{
	/// <summary>奖励领取完成（PickReward 已执行、面板已收起）→ CombatNode 订阅并回地图。</summary>
	public event Action? RewardsClaimed;

	[Export] private VBoxContainer _list = null!; // 标题 + 奖励袋按钮（每次 Show 重建）

	private GameState _game = null!;

	public override void _Ready()
	{
		_game = GetNode<GameState>("/root/GameState");
		Visible = false; // 默认隐藏，由战斗场景在胜利后调用 Open()
	}

	/// <summary>奖励阶段展示：货币 / 额外遗物 / 三选一袋 逐块领取，可“跳过全部”离开。</summary>
	public void Open()
	{
		var mgr = _game.Manager;
		if (mgr == null || mgr.Phase != RunPhase.Reward)
		{
			return;
		}

		Clear();

		_list.AddChild(new Label
		{
			Text = L.T("战斗胜利！点击领取想要的奖励（可跳过全部）"),
			HorizontalAlignment = HorizontalAlignment.Center,
		});

		// 货币块（点“获取货币”才入账）
		if (mgr.PendingRewardCurrency is { } currency && currency > 0)
		{
			_list.AddChild(Block(L.F("获取货币  +{0}", currency), () => { mgr.ClaimRewardCurrency(); }));
		}

		// 额外遗物块（精英/首领战）
		if (mgr.PendingBonusRelic != null)
		{
			var relic = mgr.PendingBonusRelic;
			string name = ContentCatalog.GetRelicResource(relic.Id)?.DisplayName ?? relic.DisplayName;
			_list.AddChild(Block(L.F("获得遗物：{0}", L.T(name)), () => { mgr.ClaimBonusRelic(); }));
		}

		// 药材袋三选一
		if (mgr.PendingRewards != null)
		{
			_list.AddChild(new HSeparator());
			foreach (var bag in mgr.PendingRewards)
			{
				var captured = bag;
				_list.AddChild(Block(L.F("选择药材袋：{0}", BagSummary(captured)), () => mgr.PickReward(captured)));
			}
		}

		// 跳过剩余全部（未领奖励销毁）
		_list.AddChild(new HSeparator());
		_list.AddChild(Block(L.T("跳过剩余奖励（离开）"), mgr.SkipRewards));

		Visible = true;
	}

	/// <summary>读档恢复奖励界面用：按当前阶段展示对应内容（Reward 战斗奖励 / BossRelicChoice 首领遗物）。</summary>
	public void ShowPending()
	{
		var mgr = _game.Manager;
		if (mgr == null)
		{
			return;
		}

		if (mgr.Phase == RunPhase.Reward)
		{
			Open();
		}
		else if (mgr.Phase == RunPhase.BossRelicChoice)
		{
			ShowBossRelicChoice();
		}
	}

	/// <summary>一块奖励的领取按钮：点它执行领取，然后刷新/收尾。</summary>
	private Button Block(string text, Action action)
	{
		var btn = new Button { Text = text, CustomMinimumSize = new Vector2(240, 0) };
		btn.Pressed += () =>
		{
			action();
			AfterClaim();
		};
		return btn;
	}

	/// <summary>任一领取/跳过动作后：仍在 Reward 阶段（还有块未处理）→ 重绘；否则收尾触发回地图。</summary>
	private void AfterClaim()
	{
		var mgr = _game.Manager;
		if (mgr == null)
		{
			return;
		}

		if (mgr.Phase == RunPhase.Reward)
		{
			Open(); // 还有未处理块 → 重建界面（已领的消失）
		}
		else if (mgr.Phase == RunPhase.BossRelicChoice)
		{
			ShowBossRelicChoice(); // 战斗奖励领完 → 同面板继续首领遗物三选一
		}
		else
		{
			Visible = false;
			Clear();
			RewardsClaimed?.Invoke(); // 全部完成 → CombatNode 回地图（首领已跨层/通关）
		}
	}

	/// <summary>首领遗物三选一（进入下一层前）：候选列表 + 跳过。</summary>
	private void ShowBossRelicChoice()
	{
		var mgr = _game.Manager;
		if (mgr == null || mgr.Phase != RunPhase.BossRelicChoice || mgr.PendingBossRelicChoices == null)
		{
			return;
		}

		Clear();
		_list.AddChild(new Label
		{
			Text = L.T("首领遗物 · 选择一件（准备进入下一层）"),
			HorizontalAlignment = HorizontalAlignment.Center,
		});
		_list.AddChild(new HSeparator());

		for (int i = 0; i < mgr.PendingBossRelicChoices.Count; i++)
		{
			var relic = mgr.PendingBossRelicChoices[i];
			string name = ContentCatalog.GetRelicResource(relic.Id)?.DisplayName ?? relic.DisplayName;
			int index = i;
			var btn = new Button
			{
				Text = L.F("选择：{0}", L.T(name)),
				CustomMinimumSize = new Vector2(240, 0),
			};
			btn.AddThemeColorOverride("font_color", RarityColor(relic.Rarity));
			btn.Pressed += () =>
			{
				mgr.ChooseBossRelic(index);
				AfterClaim();
			};
			_list.AddChild(btn);
		}

		_list.AddChild(new HSeparator());
		_list.AddChild(Block(L.T("跳过（不选首领遗物）"), mgr.SkipBossRelic));

		Visible = true;
	}

	private static Color RarityColor(RelicRarity rarity) => rarity switch
	{
		RelicRarity.Common => Colors.White,
		RelicRarity.Uncommon => new Color(0.4f, 0.9f, 0.4f),
		RelicRarity.Rare => new Color(0.4f, 0.6f, 1f),
		RelicRarity.Boss => new Color(1f, 0.7f, 0.2f),
		_ => Colors.White,
	};

	private void Clear()
	{
		foreach (var child in _list.GetChildren())
		{
			child.QueueFree();
		}
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
}
