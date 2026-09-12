using Alchemy.Core.Runs;
using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// 结局结算场景（scenes/result/victory.tscn / defeat.tscn 共用）：
/// 根据 Manager.Phase 显示 通关 / 倒下 文案 + 本局统计，并可从主界面返回主菜单。
/// 正式版没有 Debug 面板，胜负都要从这里回到主界面。
/// </summary>
public partial class EndSceneNode : Control
{
	[Export] private Label _titleLabel = null!; // 大标题（🎉 通关 / 💀 你倒下了…）
	[Export] private Label _statsLabel = null!; // 本局统计（职业/层/货币/口袋/时长）
	[Export] private Button _menuBtn = null!;   // 回到主菜单

	private GameState _game = null!;

	public override void _Ready()
	{
		_game = GetNode<GameState>("/root/GameState");
		if (_menuBtn != null)
		{
			_menuBtn.Pressed += BackToMenu;
		}

		Render();
	}

	private void Render()
	{
		var mgr = _game.Manager;
		bool win = mgr != null && mgr.Phase == RunPhase.Completed;

		if (_titleLabel != null)
		{
			_titleLabel.Text = win ? "🎉 通关！" : "💀 你倒下了…";
		}

		if (_statsLabel == null || mgr == null)
		{
			return;
		}

		var job = ContentCatalog.GetJob(mgr.JobId)?.DisplayName ?? mgr.JobId;
		_statsLabel.Text =
			$"职业：{job}\n" +
			$"到达：第 {mgr.ActIndex} 大层\n" +
			$"货币：{mgr.Run.Currency}\n" +
			$"口袋药材：{mgr.Run.Pocket.TotalCount}\n" +
			$"总时长：{FormatTime(_game.RunTotalSeconds)}";
	}

	private void BackToMenu()
	{
		_game.EndRun(); // 清掉当前局（Manager=null、计时归零）
		_game.ChangeScene("res://scenes/main_menu/main_menu.tscn");
	}

	private static string FormatTime(float seconds)
	{
		int total = (int)seconds;
		return $"{total / 60:00}:{total % 60:00}";
	}
}
