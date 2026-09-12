using System.Collections.Generic;
using System.Linq;
using Alchemy.Core.Brewing;
using Alchemy.Core.Combat;
using Alchemy.Core.Effects;
using Alchemy.Core.Entities;
using Alchemy.Core.GameData;
using Alchemy.Core.Runs;
using Alchemy.Core.Runs.Rewards;
using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// 战斗场景脚本（挂 scenes/combat/combat.tscn 根节点，类型 Control）。
/// 负责编排战斗：实例化敌人视图、炼药、投掷、奖励结算；
/// 玩家视图（PlayerView）/顶部栏（TopBar）/敌人视图（EnemyView）各自是预制体并自行 _Process 刷新。
/// </summary>
public partial class CombatNode : Control
{
	[Export] private HBoxContainer _enemyContainer = null!;  // 敌人容器：每个敌人实例化一个 EnemyView 放进来
	[Export] private PlayerView _playerView = null!;         // 玩家视图（scenes/ui/player_view.tscn：图像+血条+效果+投掷区）
	[Export] private Label _statusLabel = null!;             // 中心状态/胜负文字

	[Export] private PocketPanel _pocket = null!;            // 口袋（scenes/ui/pocket.tscn）
	[Export] private CauldronZone _cauldron = null!;         // 锅（scenes/ui/cauldron.tscn）
	[Export] private HBoxContainer _brewSlots = null!;       // 工作台 5 格
	[Export] private Button _completeBtn = null!;            // 完成制作
	[Export] private Label _brewStatus = null!;              // 炼药状态/倒计时/药水信息
	[Export] private Control _potionArea = null!;            // 出炉药水展示/拖拽区
	[Export] private RewardPanel _rewardPanel = null!;        // 奖励三选一面板（scenes/ui/reward_panel.tscn）
	[Export] private DebugPanel _debugPanel = null!;          // Debug 面板（场景静态实例，右下角）
	[Export] private Button _baseAquaBtn = null!;             // 基底：清水（队列）
	[Export] private Button _baseOilBtn = null!;              // 基底：浓油（栈）
	[Export] private Button _baseSlimeBtn = null!;           // 基底：黏液（反转）

	private GameState _game = null!;
	private CombatState _combat = null!;
	private BrewingSession _session = null!;
	private Potion? _finishedPotion;
	private int _lastSlotCount = -1;

	public override void _Ready()
	{
		_game = GetNode<GameState>("/root/GameState");

		if (_game.Manager?.ActiveCombat is not { } combat)
		{
			// 单独加载本场景（无进行中的局）或非战斗房：只提示，不崩溃
			GD.PushError("CombatNode: 当前没有进行中的战斗（请从地图进入战斗房）");
			return;
		}

		_combat = combat;

		if (_enemyContainer == null) GD.PushError("CombatNode: 漏拖 _enemyContainer（敌人容器）");
		if (_playerView == null) GD.PushError("CombatNode: 漏拖 _playerView（玩家视图）");
		if (_statusLabel == null) GD.PushError("CombatNode: 漏拖 _statusLabel（状态文字）");
		if (_pocket == null) GD.PushError("CombatNode: 漏拖 _pocket（口袋预制体）");
		if (_cauldron == null) GD.PushError("CombatNode: 漏拖 _cauldron（锅）");
		if (_brewSlots == null) GD.PushError("CombatNode: 漏拖 _brewSlots（工作台）");
		if (_completeBtn == null) GD.PushError("CombatNode: 漏拖 _completeBtn（完成制作）");
		if (_brewStatus == null) GD.PushError("CombatNode: 漏拖 _brewStatus（炼药状态）");
		if (_potionArea == null) GD.PushError("CombatNode: 漏拖 _potionArea（出炉药水区）");
		if (_rewardPanel == null) GD.PushError("CombatNode: 漏拖 _rewardPanel（奖励面板）");

		// 悬停信息面板（跟随鼠标的 tooltip）：单独 CanvasLayer 且层数最高，
		// 盖过场景里的 HUD(10)/Popup(20) 等层，避免被其他 UI 遮挡
		var tooltipLayer = new CanvasLayer { Layer = 100 };
		AddChild(tooltipLayer);
		var tooltipScene = GD.Load<PackedScene>("res://scenes/ui/info_tooltip.tscn");
		if (tooltipScene != null)
		{
			tooltipLayer.AddChild(tooltipScene.Instantiate<InfoTooltip>());
		}

		if (_debugPanel != null) _debugPanel.PocketChanged += RefreshPocket; // Debug 面板给药后刷新口袋栏

		if (_completeBtn != null) _completeBtn.Pressed += CompleteBrewing;
		if (_baseAquaBtn != null) _baseAquaBtn.Pressed += () => StartNewBrew(BaseLiquids.Aqua);
		if (_baseOilBtn != null) _baseOilBtn.Pressed += () => StartNewBrew(BaseLiquids.Oil);
		if (_baseSlimeBtn != null) _baseSlimeBtn.Pressed += () => StartNewBrew(BaseLiquids.Turbid);
		if (_cauldron != null) _cauldron.IngredientDropped += OnCauldronDropped; // 锅接收拖入
		if (_playerView != null) _playerView.PotionDropped += () => ThrowPotion(_game.Manager!.Player); // 玩家区接收投掷（对自己用药）
		if (_rewardPanel != null) _rewardPanel.RewardsClaimed += BackToMap; // 领完奖励回地图

		// 固定清水基底（占位；三种基底选择 UI 以后再补）
		_session = _game.Manager!.CreateBrewingSession(_combat);
		_session.StartBrew(BaseLiquids.Aqua);
		if (_cauldron != null) _cauldron.SetSession(_session); // 锅据此实时显示药水颜色

		BuildEnemyUi();
		RefreshPocket();
		UpdateBrewUi();

		// 自动存档：进入战斗房（检查点=上一完成房 → 读档从此重打本场战斗）
		_game.Save();
	}

	/// <summary>战斗实时驱动：唯一时间入口，逻辑层一切倒计时都靠它推进。</summary>
	public override void _Process(double delta)
	{
		if (_combat == null)
		{
			return; // _Ready 因无战斗提前返回（防御）
		}

		_combat.AdvanceTime((float)delta);

		// 敌人/玩家/顶部栏等各自 _Process 实时刷新自身显示

		UpdateBrewUi();
		CheckEnd();
	}

	// ── 敌人区 ─────────────────────────────────────────

	/// <summary>为每个敌人实例化一个 EnemyView（预制体）放进敌人容器。</summary>
	private void BuildEnemyUi()
	{
		const string scenePath = "res://scenes/ui/enemy_view.tscn";
		var scene = GD.Load<PackedScene>(scenePath);
		if (scene == null)
		{
			GD.PushError($"敌人视图预制体不存在：{scenePath}");
			return;
		}

		foreach (var enemy in _combat.Enemies)
		{
			var view = scene.Instantiate<EnemyView>();
			view.CustomMinimumSize = new Vector2(170, 0);
			view.Bind(_combat, enemy);

			var target = enemy; // 闭包捕获
			view.PotionDropped += () => ThrowPotion(target); // 对敌人用药

			_enemyContainer!.AddChild(view);
		}
	}

	// ── 背包材料栏（按数量降序 + 横向滚动）──────────────

	private void RefreshPocket()
	{
		if (_pocket == null)
		{
			return;
		}

		_pocket.Clear();

		var counts = _game.Manager!.Run.Pocket.Counts
			.OrderByDescending(kv => kv.Value)
			.ToList();

		foreach (var (id, count) in counts)
		{
			var ingredient = Ingredients.Default.All.FirstOrDefault(i => i.Id == id);
			if (ingredient == null)
			{
				continue;
			}

			var card = IngredientCard.Create(ingredient, count);
			if (card != null)
			{
				_pocket.Bar.AddChild(card);
			}
		}
	}

	// ── 锅：拖放接收 ───────────────────────────────────

	/// <summary>材料被拖入锅：执行"加入炼药"（2s 倒计时），条件不满足则提示。</summary>
	private void OnCauldronDropped(Ingredient ingredient)
	{
		if (_session.ActivePotion != null
			&& !_session.HasPendingBrewAction
			&& _session.TryStartAddIngredient(ingredient))
		{
			RefreshPocket(); // 口袋已扣，刷新材料栏
		}
		else
		{
			_brewStatus!.Text = _session.ActivePotion == null
				? L.T("锅是空的：先点【完成制作】→【重新开锅】")
				: L.T("有动作在倒计时，稍候再投料");
		}
	}

	/// <summary>药水拖到目标（敌人/玩家区的 TargetZone 触发）：对该目标用药。</summary>
	private void ThrowPotion(Creature target)
	{
		if (_finishedPotion == null)
		{
			return;
		}

		_combat.ApplyPotion(_finishedPotion, target); // 敌人=攻击/削弱，自己=治疗/增益
		_finishedPotion = null;
		ClearPotionArea();
	}

	// ── 工作台与炼药状态 ───────────────────────────────

	private void CompleteBrewing()
	{
		if (_session.ActivePotion == null)
		{
			return; // 无锅：等玩家选基底开锅
		}

		_session.TryStartCompletePotion(potion =>
		{
			_finishedPotion = potion;
			ShowPotionView(potion); // 出炉 → 生成可拖拽的药水实体
		});
	}

	/// <summary>选基底开一锅（无锅时）。</summary>
	private void StartNewBrew(BaseLiquid baseLiquid)
	{
		_session.StartBrew(baseLiquid);
		_finishedPotion = null;
		_completeBtn!.Text = L.T("完成制作");
	}

	private void ShowPotionView(Potion potion)
	{
		ClearPotionArea();
		var view = PotionView.Create(potion);
		if (view != null)
		{
			_potionArea!.AddChild(view);
		}
		else
		{
			_brewStatus!.Text = L.T("药水出炉（预制体缺失，无法展示）");
		}
	}

	private void ClearPotionArea()
	{
		foreach (var child in _potionArea!.GetChildren())
		{
			child.QueueFree();
		}
	}

	private void UpdateBrewUi()
	{
		if (_session == null)
		{
			return; // 初始化未完成（防御）
		}

		// 工作台 5 格：内容变化才重建
		int count = _session.ActivePotion?.Entries.Count ?? -1;
		if (count != _lastSlotCount)
		{
			RebuildBrewSlots();
			_lastSlotCount = count;
		}

		var brewAction = _combat.PendingActions.FirstOrDefault(a => a.Id.StartsWith("brew_"));
		if (brewAction != null)
		{
			_brewStatus!.Text = L.F("调制中… {0:0.0}s", brewAction.Remaining);
			_completeBtn!.Disabled = true;
		}
		else if (_session.ActivePotion != null)
		{
			_brewStatus!.Text = L.T("拖入材料进锅，或点【完成制作】");
			_completeBtn!.Text = L.T("完成制作");
			_completeBtn.Disabled = false;
		}
		else
		{
			// 无锅：选基底开锅（出炉后也回到这里）
			_brewStatus!.Text = _finishedPotion != null
				? L.F("药水出炉！{0}    选基底开下一锅", PotionSummary(_finishedPotion))
				: L.T("选择基底开锅");
			_completeBtn!.Text = L.T("完成制作");
		}

		_completeBtn!.Visible = _session.ActivePotion != null; // 无锅时隐藏完成按钮
		SetBaseButtonsVisible(_session.ActivePotion == null);  // 无锅时显示基底按钮
	}

	private void SetBaseButtonsVisible(bool visible)
	{
		if (_baseAquaBtn != null) _baseAquaBtn.Visible = visible;
		if (_baseOilBtn != null) _baseOilBtn.Visible = visible;
		if (_baseSlimeBtn != null) _baseSlimeBtn.Visible = visible;
	}

	private void RebuildBrewSlots()
	{
		foreach (var child in _brewSlots!.GetChildren())
		{
			child.QueueFree();
		}

		var potion = _session.ActivePotion;
		if (potion == null)
		{
			_brewSlots.AddChild(new Label { Text = L.T("选择基底开锅") });
			return;
		}

		foreach (var entry in potion.Entries)
		{
			// 每层一格：直接显示效果名（层数恒为 1）
			_brewSlots.AddChild(new Label
			{
				Text = L.T(EffectRegistry.Get(entry.Effect).DisplayName),
			});
		}

		for (int i = potion.Entries.Count; i < Potion.MaxSlots; i++)
		{
			_brewSlots.AddChild(new Label { Text = "—" });
		}
	}

	private static string PotionSummary(Potion potion)
	{
		string content = L.Join(potion.AggregateLayers()
			.Select(kv => $"{L.T(EffectRegistry.Get(kv.Key).DisplayName)}×{kv.Value}"));
		return L.F("颜色 RGB({0},{1},{2})  内容：{3}",
			potion.Color.R, potion.Color.G, potion.Color.B, content);
	}

	// ── 胜负 ───────────────────────────────────────────

	private void CheckEnd()
	{
		var mgr = _game.Manager!;
		if (mgr.Phase != RunPhase.InRoom)
		{
			return;
		}

		if (!_combat.Enemies.Any(e => e.IsAlive))
		{
			// 胜利
			mgr.EndCombat(victory: true);
			if (mgr.Phase == RunPhase.Completed)
			{
				_game.ChangeScene(GameState.VictoryScenePath); // 最终 Boss → 直接进通关结算场景
			}
			else if (mgr.Phase == RunPhase.Reward)
			{
				// 自动存档：敌人全灭、房间已完成（检查点=本战斗房）
				_game.Save();
				_rewardPanel?.Open();
			}
			else
			{
				BackToMap(); // 兜底回地图
			}
		}
		else if (!mgr.Player.IsAlive)
		{
			mgr.EndCombat(victory: false); // 进入 Defeated
			_game.ChangeScene(GameState.DefeatScenePath); // 失败 → 失败结算场景（正式版没有 Debug 面板）
		}
	}

	// 奖励三选一面板逻辑已抽到 RewardPanel 预制体（Show()/点选 PickReward/RewardsClaimed 事件回地图）。
	// 跳转/弃局由 Debug 面板负责（战斗中回地图 = AbandonCombat，保留状态无奖励）。

	private void BackToMap()
	{
		_game.ChangeScene("res://scenes/map/map.tscn");
	}
}
