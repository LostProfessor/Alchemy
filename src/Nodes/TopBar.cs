using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// 顶部信息栏（scenes/ui/top_bar.tscn）：绑定当前整局的玩家实体实时显示，
/// 内容包括 大层/房间、玩家血量+护盾、货币、口袋药材数、战斗计时、整局总计时。
/// 没有进行中的局时保持空（主菜单等场景不挂即可）。
/// </summary>
public partial class TopBar : PanelContainer
{
	[Export] private Label _floorLabel = null!;        // 大层 · 房间
	[Export] private Label _hpLabel = null!;           // 玩家 HP / 护盾
	[Export] private Label _currencyLabel = null!;     // 货币
	[Export] private Label _pocketLabel = null!;       // 口袋药材数
	[Export] private Label _combatTimeLabel = null!;   // 当前战斗计时
	[Export] private Label _totalTimeLabel = null!;    // 整局总计时
	[Export] private Button _settingsBtn = null!;      // 设置按钮（→ 设置场景：保存/后续设置）

	private GameState _game = null!;

	public override void _Ready()
	{
		_game = GetNode<GameState>("/root/GameState");
		if (_settingsBtn != null)
		{
			_settingsBtn.Pressed += OpenSettings;
		}
	}

	/// <summary>进设置场景（记录来源场景供“返回”）；战斗/地图等各场景的 TopBar 都能打开。</summary>
	private void OpenSettings()
	{
		const string settingsScene = "res://scenes/settings/settings.tscn";
		var current = GetTree().CurrentScene?.SceneFilePath;
		if (!string.IsNullOrEmpty(current) && current != settingsScene)
		{
			_game.ReturnScenePath = current;
		}

		_game.ChangeScene(settingsScene);
	}

	public override void _Process(double delta)
	{
		var mgr = _game.Manager;
		if (mgr == null)
		{
			return;
		}

		var p = mgr.Player;

		if (_floorLabel != null)
		{
			_floorLabel.Text = mgr.CurrentRoom == null
				? $"第 {mgr.ActIndex} 层"
				: $"第 {mgr.ActIndex} 层 · {mgr.CurrentRoom.Type}";
		}

		if (_hpLabel != null)
		{
			_hpLabel.Text = $"{p.Name}  {p.CurrentHp}/{p.MaxHp} · 护盾 {p.Block}";
		}

		if (_currencyLabel != null)
		{
			_currencyLabel.Text = $"货币 {mgr.Run.Currency}";
		}

		if (_pocketLabel != null)
		{
			_pocketLabel.Text = $"口袋 {mgr.Run.Pocket.TotalCount}";
		}

		if (_combatTimeLabel != null)
		{
			_combatTimeLabel.Text = mgr.ActiveCombat == null
				? "战斗 -"
				: $"战斗 {mgr.ActiveCombat.Time:0.0}s";
		}

		if (_totalTimeLabel != null)
		{
			_totalTimeLabel.Text = $"总时长 {FormatTime(_game.RunTotalSeconds)}";
		}
	}

	private static string FormatTime(float seconds)
	{
		int total = (int)seconds;
		return $"{total / 60:00}:{total % 60:00}";
	}
}
