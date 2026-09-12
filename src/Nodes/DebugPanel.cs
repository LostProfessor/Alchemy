using System;
using System.Linq;
using Alchemy.Core.Brewing;
using Alchemy.Core.Combat;
using Alchemy.Core.GameData;
using Alchemy.Core.Hooks;
using Alchemy.Core.Runs;
using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// Debug 面板（scenes/ui/debug_panel.tscn）：快捷跳转按钮 + 直接拿任意药材入包。
/// 跳转：回主菜单（弃局）、回地图（战斗中=先 AbandonCombat 结束本场、保留血量/货币、无奖励）、
/// 直达职业选择（弃局）、直接胜利/失败本场。
/// 给药后触发 <see cref="PocketChanged"/>（战斗场景订阅后刷新口袋栏）。
/// </summary>
public partial class DebugPanel : PanelContainer
{
	/// <summary>给药后触发（CombatNode 订阅并刷新口袋）。</summary>
	public event Action? PocketChanged;

	[Export] private Button _menuBtn = null!;      // 回主菜单
	[Export] private Button _mapBtn = null!;       // 回地图（战斗中撤退）
	[Export] private Button _jobBtn = null!;       // 直达职业选择
	[Export] private Button _winBtn = null!;       // 直接胜利本场
	[Export] private Button _loseBtn = null!;      // 直接失败本场
	[Export] private Button _saveBtn = null!;      // 手动存档（房内保存=读档重打本房）
	[Export] private GridContainer _ingredientList = null!; // 给药网格（4 列平铺，不滚动）

	private GameState _game = null!;

	public override void _Ready()
	{
		_game = GetNode<GameState>("/root/GameState");

		if (_menuBtn != null) _menuBtn.Pressed += GoMainMenu;
		if (_mapBtn != null) _mapBtn.Pressed += GoMap;
		if (_jobBtn != null) _jobBtn.Pressed += GoJobSelect;
		if (_winBtn != null) _winBtn.Pressed += ForceWin;
		if (_loseBtn != null) _loseBtn.Pressed += ForceLose;
		if (_saveBtn != null) _saveBtn.Pressed += () => _game.Save(); // 手动存档

		BuildIngredientButtons();
	}

	// ── 快捷跳转 ───────────────────────────────────────

	private void GoMap()
	{
		var mgr = _game.Manager;
		if (mgr != null && mgr.Phase == RunPhase.InRoom)
		{
			mgr.AbandonCombat(); // 战斗中：结束本场，保留血量/货币，无奖励
		}

		_game.ChangeScene("res://scenes/map/map.tscn");
	}

	private void GoMainMenu()
	{
		_game.EndRun(); // 弃掉当前局
		_game.ChangeScene("res://scenes/main_menu/main_menu.tscn");
	}

	private void GoJobSelect()
	{
		_game.EndRun(); // 弃掉当前局（调试直达职业选择开新局）
		_game.ChangeScene("res://scenes/job_select/job_select.tscn");
	}

	/// <summary>本场直接胜利：把每个敌人打 99999（让 CombatNode 检测到全灭 → 走正常胜利/奖励/存档流程）。</summary>
	private void ForceWin()
	{
		var combat = _game.Manager?.ActiveCombat;
		var player = _game.Manager?.Player;
		if (combat == null || player == null)
		{
			return;
		}

		foreach (var enemy in combat.Enemies.ToList())
		{
			combat.DealDamage(new DamageContext(player, enemy, 99999));
		}
	}

	/// <summary>本场直接失败：把玩家打 99999 直到倒下（若被不朽/复活类效果拦下则多补几次）。</summary>
	private void ForceLose()
	{
		var combat = _game.Manager?.ActiveCombat;
		var player = _game.Manager?.Player;
		if (combat == null || player == null)
		{
			return;
		}

		for (int i = 0; i < 5 && player.IsAlive; i++)
		{
			combat.DealDamage(new DamageContext(player, player, 99999));
		}
	}

	// ── 给药列表 ───────────────────────────────────────

	private void BuildIngredientButtons()
	{
		if (_ingredientList == null)
		{
			return;
		}

		foreach (var ing in Ingredients.Default.All.OrderByDescending(i => i.Rarity))
		{
			var btn = new Button
			{
				Text = ing.DisplayName,
				CustomMinimumSize = new Vector2(0, 30),
				TooltipText = RarityName(ing.Rarity), // 悬停显示稀有度
			};
			btn.AddThemeColorOverride("font_color", RarityColor(ing.Rarity));
			btn.Pressed += () =>
			{
				_game.Manager?.Run.Pocket.Add(ing.Id);
				PocketChanged?.Invoke();
			};
			_ingredientList.AddChild(btn);
		}
	}

	private static Color RarityColor(IngredientRarity rarity) => rarity switch
	{
		IngredientRarity.Common => Colors.White,
		IngredientRarity.Uncommon => new Color(0.4f, 0.9f, 0.4f),
		IngredientRarity.Rare => new Color(0.4f, 0.6f, 1f),
		IngredientRarity.Legendary => new Color(1f, 0.7f, 0.2f),
		_ => Colors.White,
	};

	private static string RarityName(IngredientRarity rarity) => rarity switch
	{
		IngredientRarity.Common => "普通",
		IngredientRarity.Uncommon => "罕见",
		IngredientRarity.Rare => "稀有",
		IngredientRarity.Legendary => "传奇",
		_ => "?",
	};
}
