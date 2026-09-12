using System.Linq;
using Alchemy.Core.GameData;
using Alchemy.Core.Map;
using Alchemy.Core.Rooms;
using Alchemy.Core.Runs;
using Alchemy.Core.Runs.Rewards;
using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// 地图场景脚本（挂 scenes/map/map.tscn 根节点，类型 Control）。
/// 静态控件在编辑器里摆好（顶部状态栏、房间结算面板、滚动容器），
/// 在 Inspector 的脚本面板把 [Export] 字段拖上对应节点。
/// 地图节点/连线内容动态，由代码渲染到 _mapCanvas（Control）里；
/// 地图横排（层数向左→右），超宽/超高时由 _mapScroll 滚动。
/// </summary>
public partial class MapNode : Control
{
	// ── 横向卷轴布局参数 ───────────────────────────────
	// 层数方向 = 横向（左→右），分叉(列)方向 = 纵向（上→下）。
	// 1280x720 下第 1 大层 11 层 ≈ 放得下；更长的层靠 MapScroll 横向滚动。
	private const float LayerSpacing = 175f;   // 相邻层（Row）的横向间距
	private const float BranchSpacing = 78f;   // 相邻分支（Col）的纵向间距
	private const float MarginX = 40f;         // 左侧留白
	private const float MarginY = 100f;         // 顶部给状态栏留白（改大=整个地图往下移）
	private const float NodeSize = 52f;        // 节点按钮尺寸（≈ 原 55 减 5%）

	[Export] private ScrollContainer _mapScroll = null!; // 地图滚动容器（全屏）：内容超宽/超高可滚
	[Export] private Control _mapCanvas = null!;         // 地图内容容器（_mapScroll 的子 Control）：节点/连线自动放这
	[Export] private Label _hudLabel = null!;            // 顶部状态文字（大层/生命/货币/口袋）
	[Export] private PanelContainer _roomPanel = null!;  // 房间结算面板（居中、默认隐藏）
	[Export] private Button _saveBtn = null!;            // 保存按钮（可留空）
	[Export] private Button _menuBtn = null!;            // 回主菜单按钮（可留空）
	[Export] private Button _pocketBtn = null!;          // "口袋"按钮：点开面板（暂停并降序显示材料）
	[Export] private PanelContainer _pocketPanel = null!;// 口袋面板（居中、默认隐藏；点开时暂停计时）

	private GameState _game = null!;
	private ConfirmationDialog? _abandonDialog;	// 「回主菜单」的放弃复核弹窗（懒建）

	public override void _Ready()
	{
		_game = GetNode<GameState>("/root/GameState");

		if (_mapScroll == null) GD.PushError("MapNode: 漏拖 _mapScroll（ScrollContainer）");
		if (_mapCanvas == null) GD.PushError("MapNode: 漏拖 _mapCanvas（Control 地图内容容器）");
		if (_roomPanel == null) GD.PushError("MapNode: 漏拖 _roomPanel（房间结算面板）");
		if (_pocketPanel == null) GD.PushError("MapNode: 漏拖 _pocketPanel（口袋面板）");
		// _hudLabel/_saveBtn/_menuBtn/_pocketBtn 均为可空：没拖就不启用
		//（如状态文字已由 TopBar 展示、按钮没在场景里建时，缺绑定也不崩）
		if (_saveBtn != null) _saveBtn.Pressed += () => _game.Save();
		if (_menuBtn != null) _menuBtn.Pressed += BackToMenu;
		if (_pocketBtn != null) _pocketBtn.Pressed += TogglePocket;
		_roomPanel!.Visible = false;
		_pocketPanel!.Visible = false;
		_pocketPanel.ProcessMode = ProcessModeEnum.Always; // 暂停时面板仍可交互（能点关闭）

		if (_game.Manager == null)
		{
			GD.PushError("MapNode: 当前没有进行中的局（先去职业选择开新局/读档）");
			return;
		}

		Refresh();
		ShowResumedRewardIfNeeded();
	}

	private void Refresh()
	{
		var mgr = _game.Manager!;
		if (_hudLabel != null)
		{
			_hudLabel.Text = L.F(
				"第 {0} 大层    生命 {1}/{2}    货币 {3}    口袋 {4}",
				mgr.ActIndex, mgr.Player.CurrentHp, mgr.Player.MaxHp, mgr.Run.Currency, mgr.Run.Pocket.TotalCount);
		}

		if (mgr.Phase == RunPhase.Completed)
		{
			ShowTextPanel(L.T("通关！"));
			return;
		}

		if (mgr.Phase == RunPhase.Defeated)
		{
			ShowTextPanel(L.T("你倒下了…"));
			return;
		}

		RebuildMap();
	}

	/// <summary>读档恢复的是奖励界面（Reward/BossRelicChoice）→ 弹回奖励面板继续选择（选完/跳过才放行进图）。</summary>
	private void ShowResumedRewardIfNeeded()
	{
		var mgr = _game.Manager;
		if (mgr == null)
		{
			return;
		}

		if (mgr.Phase != RunPhase.Reward && mgr.Phase != RunPhase.BossRelicChoice)
		{
			return;
		}

		var scene = GD.Load<PackedScene>("res://scenes/ui/reward_panel.tscn");
		if (scene == null)
		{
			return;
		}

		var panel = scene.Instantiate<RewardPanel>();

		// ⚠️ 本场景根节点没有真实尺寸（控件都用 1280x720 绝对坐标摆），
		// 所以遮罩不能靠“锚点铺满父节点”，要挂到全屏 CanvasLayer 再按视口尺寸铺。
		var layer = new CanvasLayer { Name = "RewardOverlayLayer", Layer = 30 };
		AddChild(layer);

		var viewportSize = GetViewportRect().Size;
		var overlay = new Control
		{
			Name = "RewardOverlay",
			Position = Vector2.Zero,
			Size = viewportSize,
			MouseFilter = Control.MouseFilterEnum.Stop, // 盖住地图、挡鼠标
		};
		layer.AddChild(overlay);

		var backdrop = new ColorRect { Color = new Color(0, 0, 0, 0.6f) };
		backdrop.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		overlay.AddChild(backdrop);

		var center = new CenterContainer();
		center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		center.AddChild(panel);
		overlay.AddChild(center);

		panel.RewardsClaimed += () =>
		{
			layer.QueueFree();
			Refresh(); // 首领遗物选完可能已跨大层 → 重绘新大层地图
		};
		panel.ShowPending();
	}

	/// <summary>点开/收起口袋面板：点开暂停计时，收起恢复。</summary>
	private void TogglePocket()
	{
		if (_pocketPanel!.Visible)
		{
			ClosePocket();
		}
		else
		{
			OpenPocket();
		}
	}

	private void OpenPocket()
	{
		foreach (var child in _pocketPanel!.GetChildren())
		{
			child.QueueFree();
		}

		var list = new VBoxContainer();
		list.AddThemeConstantOverride("separation", 4);
		list.AddChild(new Label { Text = L.T("药材口袋") });

		var counts = _game.Manager!.Run.Pocket.Counts
			.OrderByDescending(kv => kv.Value) // 按数量从高到低
			.ToList();
		if (counts.Count == 0)
		{
			list.AddChild(new Label { Text = L.T("（口袋是空的）") });
		}

		foreach (var (id, count) in counts)
		{
			var name = Ingredients.Default.All.FirstOrDefault(i => i.Id == id)?.DisplayName ?? id;
			list.AddChild(new Label { Text = $"{L.T(name)} ×{count}" });
		}

		var close = new Button { Text = L.T("关闭") };
		close.Pressed += ClosePocket;
		list.AddChild(close);

		_pocketPanel.AddChild(list);
		_pocketPanel.Visible = true;
		GetTree().Paused = true; // 打开面板 = 暂停（冻结普通节点的计时/输入）
	}

	private void ClosePocket()
	{
		_pocketPanel!.Visible = false;
		GetTree().Paused = false; // 恢复
	}

	private void RebuildMap()
	{
		foreach (var child in _mapCanvas!.GetChildren())
		{
			child.QueueFree();
		}

		var mgr = _game.Manager!;
		var map = mgr.Map!;

		// 连线：每个点 → 其子节点
		foreach (var p in map.AllPoints)
		{
			foreach (var child in p.Children)
			{
				AddLine(PosOf(p.Coord), PosOf(child.Coord));
			}
		}

		// 起点（不可点，用文字；首领用贴图）
		AddPoint(map.Start, null, L.T("起"), enabled: false, new Color(0.6f, 0.6f, 0.6f));
		AddPoint(map.Boss, IconTextureOf(MapPointType.Boss), string.Empty,
			mgr.AvailableNextPoints.Contains(map.Boss),
			mgr.AvailableNextPoints.Contains(map.Boss) ? Colors.White : Colors.Gray);

		// 内容节点
		foreach (var p in map.AllPoints)
		{
			bool available = mgr.AvailableNextPoints.Contains(p);
			bool visited = mgr.VisitedMapPoints.Contains(p);
			var color = available ? Colors.White : visited ? Colors.DimGray : Colors.Gray;
			AddPoint(p, IconTextureOf(p.Type), IconOf(p.Type), available, color);
		}

		// 按行列范围撑开内容容器，ScrollContainer 据此出滚动条
		FitCanvasSize(map);
	}

	private void AddLine(Vector2 from, Vector2 to)
	{
		var line = new Line2D
		{
			Points = new[] { from, to },
			Width = 3f,
			DefaultColor = new Color(0.4f, 0.4f, 0.45f),
		};
		_mapCanvas!.AddChild(line);
	}

	private void AddPoint(MapPoint p, Texture2D? icon, string fallbackText, bool enabled, Color color)
	{
		var btn = new Button
		{
			Disabled = !enabled,
			Position = PosOf(p.Coord) - new Vector2(NodeSize / 2f, NodeSize / 2f),
			CustomMinimumSize = new Vector2(NodeSize, NodeSize),
			Modulate = color,
		};

		if (icon != null)
		{
			// 用贴图标记：铺满按钮、保持比例、不挡点击
			var tex = new TextureRect
			{
				Texture = icon,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				MouseFilter = Control.MouseFilterEnum.Ignore,
			};
			tex.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			btn.AddChild(tex);
		}
		else
		{
			btn.Text = fallbackText;
		}

		btn.Pressed += () => OnPointPressed(p);
		_mapCanvas!.AddChild(btn);
	}

	private void OnPointPressed(MapPoint point)
	{
		var mgr = _game.Manager!;
		if (!mgr.TryMoveTo(point))
		{
			return;
		}

		// 进房后按房间类型切到对应独立场景（战斗 / 事件 / 火堆 / 宝箱 / 商店）
		var scene = RoomSceneOf(mgr.CurrentRoom?.Type ?? RoomType.Combat);
		if (scene != null && ResourceLoader.Exists(scene))
		{
			_game.ChangeScene(scene);
			return;
		}

		GD.PushError($"房间场景不存在：{scene}（请建 scenes/room 下对应场景）");
		ShowRoomPanel(); // 兜底：老的地图内面板
	}

	/// <summary>房间类型 → 独立场景路径（战斗/事件/火堆/宝藏/商店）。</summary>
	private static string? RoomSceneOf(RoomType type) => type switch
	{
		RoomType.Combat => "res://scenes/combat/combat.tscn",
		RoomType.Event => "res://scenes/room/event.tscn",
		RoomType.RestSite => "res://scenes/room/rest.tscn",
		RoomType.Treasure => "res://scenes/room/treasure.tscn",
		RoomType.Shop => "res://scenes/room/shop.tscn",
		_ => null,
	};

	private static string IconOf(MapPointType type) => L.T(type switch
	{
		MapPointType.Combat => "战",
		MapPointType.Elite => "精",
		MapPointType.Event => "事",
		MapPointType.Treasure => "宝",
		MapPointType.RestSite => "火",
		MapPointType.Shop => "商",
		MapPointType.Boss => "首",
		_ => "?",
	});

	/// <summary>地图节点标记贴图（Theme/textures/ui/，与 IconOf 对应；无图时回退文字）。</summary>
	private static Texture2D IconTextureOf(MapPointType type) => type switch
	{
		MapPointType.Combat => GD.Load<Texture2D>("res://Theme/textures/ui/common_fight.png"),
		MapPointType.Elite => GD.Load<Texture2D>("res://Theme/textures/ui/elite_fight.png"),
		MapPointType.Event => GD.Load<Texture2D>("res://Theme/textures/ui/event.png"),
		MapPointType.Treasure => GD.Load<Texture2D>("res://Theme/textures/ui/chest.png"),
		MapPointType.RestSite => GD.Load<Texture2D>("res://Theme/textures/ui/campfire.png"),
		MapPointType.Shop => GD.Load<Texture2D>("res://Theme/textures/ui/shop.png"),
		MapPointType.Boss => GD.Load<Texture2D>("res://Theme/textures/ui/boss_fight.png"),
		_ => null!,
	};

	// 横向卷轴投影：层数(Row) → 右，分叉(Col) → 下
	private static Vector2 PosOf(MapCoord c) => new(MarginX + c.Row * LayerSpacing, MarginY + c.Col * BranchSpacing);

	/// <summary>按当前大层的行列范围算出内容总尺寸，撑开 MapCanvas 让 ScrollContainer 能滚。</summary>
	private void FitCanvasSize(StandardActMap map)
	{
		int maxRow = map.ContentFloors + 1; // 首领行
		int maxCol = StandardActMap.Width - 1;
		var size = new Vector2(
			MarginX + maxRow * LayerSpacing + NodeSize,
			MarginY + maxCol * BranchSpacing + NodeSize);
		_mapCanvas!.CustomMinimumSize = size;
		_mapCanvas.Size = size;
	}

	// ── 房间最小闭环结算 ────────────────────────────────

	private void ShowRoomPanel()
	{
		var mgr = _game.Manager!;
		ClearPanel();

		var title = new Label
		{
			Text = mgr.CurrentRoom != null ? L.RoomTypeName(mgr.CurrentRoom.Type) : L.T("房间"),
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		title.AddThemeFontSizeOverride("font_size", 28);
		_roomPanel!.AddChild(title);

		_roomPanel!.AddChild(new Label { Text = L.F("进入 {0}", mgr.CurrentRoom?.Id ?? "?") });

		var done = new Button { Text = L.T("完成房间（战斗默认胜利）") };
		done.Pressed += CompleteCurrentRoom;
		_roomPanel!.AddChild(done);

		_roomPanel!.Visible = true;
	}

	private void ShowRewardPanel()
	{
		var mgr = _game.Manager!;
		ClearPanel();

		var title = new Label
		{
			Text = L.T("战斗胜利！选择一个奖励袋"),
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		title.AddThemeFontSizeOverride("font_size", 24);
		_roomPanel!.AddChild(title);

		foreach (var bag in mgr.PendingRewards!)
		{
			var btn = new Button { Text = BagSummary(bag) };
			btn.Pressed += () => PickReward(bag);
			_roomPanel!.AddChild(btn);
		}

		_roomPanel!.Visible = true;
	}

	private void ShowTextPanel(string text)
	{
		ClearPanel();
		var label = new Label
		{
			Text = text,
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		label.AddThemeFontSizeOverride("font_size", 32);
		_roomPanel!.AddChild(label);

		var back = new Button { Text = L.T("回主菜单") };
		back.Pressed += BackToMenu;
		_roomPanel!.AddChild(back);

		_roomPanel!.Visible = true;
	}

	private void CompleteCurrentRoom()
	{
		var mgr = _game.Manager!;
		if (mgr.Phase != RunPhase.InRoom)
		{
			return;
		}

		if (mgr.ActiveCombat != null)
		{
			mgr.EndCombat(victory: true);
		}
		else if (mgr.CurrentRoom is RestSiteRoom)
		{
			mgr.PerformRest(RestChoice.Sleep);
		}
		else if (mgr.CurrentRoom is EventRoom)
		{
			mgr.PerformEventChoice(0);
		}
		else if (mgr.CurrentRoom is TreasureRoom)
		{
			mgr.ClaimTreasure();
		}
		else if (mgr.CurrentRoom is ShopRoom)
		{
			mgr.LeaveShop();
		}
		else
		{
			mgr.CompleteRoom();
		}

		// 战斗胜利后进入奖励阶段 → 三选一
		if (mgr.Phase == RunPhase.Reward)
		{
			ShowRewardPanel();
			return;
		}

		ClosePanelAndRefresh();
	}

	private void PickReward(RewardBag bag)
	{
		_game.Manager!.PickReward(bag);
		ClosePanelAndRefresh();
	}

	private void ClosePanelAndRefresh()
	{
		_roomPanel!.Visible = false;
		ClearPanel();
		Refresh();
	}

	private void ClearPanel()
	{
		foreach (var child in _roomPanel!.GetChildren())
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

	/// <summary>
	/// 「回主菜单」= <b>放弃本局</b>（归档到历史 + 删掉进行中的存档，不能“继续”）。
	/// 语义与设置页的「保存并退出」（保留存档）不同，所以先弹窗复核，避免误触丢档。
	/// </summary>
	private void BackToMenu()
	{
		if (_game.Manager == null)
		{
			_game.ChangeScene(GameState.MainMenuScenePath); // 没有进行中的局 → 无需确认
			return;
		}

		_abandonDialog ??= BuildAbandonDialog();
		_abandonDialog.DialogText =
			L.T("放弃本局并返回主菜单？") + "\n" +
			L.T("本局会归档进「历史记录」，进行中的存档将被删除（无法再继续）。");
		_abandonDialog.PopupCentered();
	}

	/// <summary>放弃复核弹窗（首次使用时创建并常驻本场景）。</summary>
	private ConfirmationDialog BuildAbandonDialog()
	{
		var dialog = new ConfirmationDialog
		{
			Title = L.T("确认放弃"),
			OkButtonText = L.T("放弃并返回"),
			CancelButtonText = L.T("取消"),
		};
		dialog.Confirmed += () =>
		{
			_game.EndRun();
			_game.ChangeScene(GameState.MainMenuScenePath);
		};
		AddChild(dialog);
		return dialog;
	}
}
