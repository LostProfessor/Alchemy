using System.Linq;
using Alchemy.Core.Effects;
using Alchemy.Core.Entities;
using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// 玩家视图（scenes/ui/player_view.tscn）：玩家图像槽 + HP 血条 + “HP/护盾”文字 + 玩家身上效果栏，
/// 同时作为投掷目标（对自己用药，继承 TargetZone 的拖放接收）。
/// 数据实时取自当前整局的玩家实体（GameState.Manager.Player）。
/// </summary>
public partial class PlayerView : TargetZone
{
	[Export] private TextureRect _image = null!;       // 玩家图像槽（占位，以后拖玩家立绘）
	[Export] private ProgressBar _hpBar = null!;       // 生命条
	[Export] private Label _hpText = null!;            // “名字 HP/最大 · 护盾 X”
	[Export] private HBoxContainer _effectsBar = null!; // 玩家身上效果（每帧刷新）

	private GameState _game = null!;
	private Creature? _player;

	public override void _Ready()
	{
		_game = GetNode<GameState>("/root/GameState");
		_player = _game.Manager?.Player;
	}

	public override void _Process(double delta)
	{
		// 惰性取玩家（进入战斗后才有）
		if (_player == null)
		{
			_player = _game?.Manager?.Player;
			return;
		}

		if (_hpBar != null)
		{
			_hpBar.MaxValue = Mathf.Max(1, _player.MaxHp);
			_hpBar.Value = _player.CurrentHp;
		}

		if (_hpText != null)
		{
			_hpText.Text = L.F("{0}  {1}/{2}   护盾 {3}", L.T(_player.Name), _player.CurrentHp, _player.MaxHp, _player.Block);
		}

		CombatUi.RefreshEffects(_effectsBar, _player);
	}
}
