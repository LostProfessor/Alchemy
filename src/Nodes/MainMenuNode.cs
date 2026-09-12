using System;
using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// 主菜单场景脚本（挂 scenes/main_menu/main_menu.tscn 根节点，类型 Control）。
/// 点"开始游戏"只解析并暂存种子 → 切到职业选择场景（job_select）选职业。
/// 控件在编辑器里摆好，然后在 Inspector 的脚本面板把 [Export] 字段拖上对应节点：
///   _seedInput / _startBtn / _continueBtn / _quitBtn / _randomSeedBtn（可留空）
/// </summary>
public partial class MainMenuNode : Control
{
	[Export] private LineEdit _seedInput = null!;    // 种子输入框
	[Export] private Button _startBtn = null!;       // 开始新游戏
	[Export] private Button _continueBtn = null!;    // 继续（读取存档）
	[Export] private Button _quitBtn = null!;        // 退出
	[Export] private Button _randomSeedBtn = null!;  // 🎲 随机种子（可留空）

	private GameState _game = null!;

	public override void _Ready()
	{
		_game = GetNode<GameState>("/root/GameState");

		if (_seedInput == null) GD.PushError("MainMenuNode: 漏拖 _seedInput（种子输入框）");
		if (_startBtn == null) GD.PushError("MainMenuNode: 漏拖 _startBtn（开始按钮）");
		if (_continueBtn == null) GD.PushError("MainMenuNode: 漏拖 _continueBtn（继续按钮）");
		if (_quitBtn == null) GD.PushError("MainMenuNode: 漏拖 _quitBtn（退出按钮）");

		_startBtn!.Pressed += StartNewRun;
		_continueBtn!.Pressed += ContinueRun;
		_quitBtn!.Pressed += () => GetTree().Quit();
		if (_randomSeedBtn != null)
		{
			_randomSeedBtn.Pressed += () => _seedInput!.Text = new Random().Next(1, int.MaxValue).ToString();
		}

		// 有存档才允许"继续"
		_continueBtn!.Disabled = !_game.HasSave;
	}

	private void StartNewRun()
	{
		// 只解析并暂存种子；职业在独立的职业选择场景里选
		int seed = int.TryParse(_seedInput!.Text, out var s) && s > 0
			? s
			: new Random().Next(1, int.MaxValue);
		_game.PendingSeed = seed;
		_game.ChangeScene("res://scenes/job_select/job_select.tscn");
	}

	private void ContinueRun()
	{
		if (_game.TryLoad())
		{
			_game.ChangeScene("res://scenes/map/map.tscn");
		}
		else
		{
			_continueBtn!.Text = "没有可用存档";
			_continueBtn!.Disabled = true;
		}
	}
}
