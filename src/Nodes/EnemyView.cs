using System.Linq;
using Alchemy.Core.Combat;
using Alchemy.Core.Entities;
using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// 单个敌人视图（scenes/ui/enemy_view.tscn）：战斗场景只保留一个“放敌人实体的容器”，
/// 每个敌人实例化一个 EnemyView 并 Bind(combat, enemy)。
/// 内容：图像 + 名字 + 血条 + 下次操作倒计时条(橙) + 当前意图 + 效果栏，
/// 并作为投掷目标（对敌人用药，继承 TargetZone）。
/// 每帧从绑定的 CombatState 实时刷新（倒计时/意图/血条/效果）。
/// </summary>
public partial class EnemyView : TargetZone
{
	[Export] private TextureRect _image = null!;        // 敌人插图
	[Export] private Label _nameLabel = null!;          // 敌人名
	[Export] private ProgressBar _hpBar = null!;        // 血条
	[Export] private Label _hpValue = null!;            // 血量数字（当前 / 最大）
	[Export] private ProgressBar _actionBar = null!;    // 下次操作倒计时（橙）
	[Export] private Label _intentionLabel = null!;     // 当前意图
	[Export] private HBoxContainer _effectsBar = null!; // 身上效果

	private CombatState? _combat;
	private Creature? _enemy;
	private Tween? _poseTween; // 抬手/瘫痪等姿势补间（新动作会打断旧的）

	/// <summary>绑定一场战斗里的一个敌人，并填充静态内容（插图/名字）。</summary>
	public void Bind(CombatState combat, Creature enemy)
	{
		_combat = combat;
		_enemy = enemy;
		combat.ActionTelegraphed += OnActionTelegraphed; // 订阅"出手前预兆"
		combat.ActionInterrupted += OnActionInterrupted;  // 订阅"被打断（破招）"

		var icon = ContentCatalog.GetEnemyIcon(enemy.TemplateId ?? string.Empty);
		if (icon != null)
		{
			_image.Texture = icon;
			_image.Visible = true;
		}
		else
		{
			_image.Visible = false;
		}

		_nameLabel.Text = L.T(enemy.Name);
	}

	public override void _ExitTree()
	{
		if (_combat != null)
		{
			_combat.ActionTelegraphed -= OnActionTelegraphed; // 防悬空订阅
			_combat.ActionInterrupted -= OnActionInterrupted;
		}
	}

	/// <summary>动作预兆：若是本敌人的出手，播预备动作。</summary>
	private void OnActionTelegraphed(ActionTelegraph telegraph)
	{
		if (_enemy == null || !_enemy.IsAlive || telegraph.Actor != _enemy)
		{
			return;
		}

		PlayWindUp();
	}

	/// <summary>
	/// 敌人出手前的预备动作（抬手/蓄力）。目前是占位表现（缩放脉冲，让预兆"看得见"）；
	/// 将来接入帧动画时，把这里换成 _anim.Play("windup") 之类即可 —— 预兆信号本身不用改。
	/// 若要按意图类型区分动作，可用 _combat.GetCurrentIntention(_enemy) 判断攻击/防御/施法。
	/// </summary>
	private void PlayWindUp()
	{
		_poseTween?.Kill();

		PivotOffset = Size / 2f; // 以中心为轴缩放
		Scale = Vector2.One;

		_poseTween = CreateTween();
		_poseTween.TweenProperty(this, "scale", new Vector2(1.12f, 1.12f), 0.15);
		_poseTween.TweenProperty(this, "scale", Vector2.One, 0.35);
	}

	/// <summary>打断（破招）：本敌人被打断 → 取消抬手，播瘫痪表现。</summary>
	private void OnActionInterrupted(ActionInterrupted interrupted)
	{
		if (_enemy == null || interrupted.Actor != _enemy)
		{
			return;
		}

		PlayStagger();
	}

	/// <summary>
	/// 瘫痪表现。当前是占位（取消抬手 + 下蹲回弹）；将来接动画时换成 _anim.Play("stagger")，
	/// 音效可在这里播（AudioStreamPlayer 的 Bus 用 "Sfx"）。
	/// </summary>
	private void PlayStagger()
	{
		_poseTween?.Kill(); // 取消抬手
		Scale = Vector2.One;

		PivotOffset = Size / 2f;
		_poseTween = CreateTween();
		_poseTween.TweenProperty(this, "scale", new Vector2(0.94f, 0.9f), 0.12);
		_poseTween.TweenProperty(this, "scale", Vector2.One, 0.3);
	}

	public override void _Process(double delta)
	{
		if (_combat == null || _enemy == null)
		{
			return;
		}

		// 死亡：整块置灰 + 收起来行动/意图/效果栏，意图处标“倒下”（血条已被逻辑层清 0）
		if (!_enemy.IsAlive)
		{
			Modulate = new Color(0.55f, 0.55f, 0.55f, 1f);
			if (_hpBar != null)
			{
				_hpBar.MaxValue = Mathf.Max(1, _enemy.MaxHp);
				_hpBar.Value = 0;
			}

			if (_hpValue != null) _hpValue.Text = $"0 / {_enemy.MaxHp}";

			if (_actionBar != null) _actionBar.Visible = false;
			if (_effectsBar != null) _effectsBar.Visible = false;
			if (_intentionLabel != null)
			{
				_intentionLabel.Text = L.T("💀 倒下");
				_intentionLabel.Modulate = new Color(0.55f, 0.55f, 0.55f); // 清掉警示/瘫痪色
			}

			return;
		}

		// 存活：恢复正常显示，实时刷新
		Modulate = Colors.White;
		if (_actionBar != null) _actionBar.Visible = true;
		if (_effectsBar != null) _effectsBar.Visible = true;

		if (_hpBar != null)
		{
			_hpBar.MaxValue = Mathf.Max(1, _enemy.MaxHp);
			_hpBar.Value = _enemy.CurrentHp;
		}

		if (_hpValue != null) _hpValue.Text = $"{_enemy.CurrentHp} / {_enemy.MaxHp}";

		if (_actionBar != null)
		{
			// 出手/瘫痪都走同一条倒计时条（瘫痪时条重新走满）
			var action = _combat.PendingActions.FirstOrDefault(
				a => a.Actor == _enemy && a.Id.StartsWith("enemy_"));
			_actionBar.Value = action == null ? 0f : 1f - action.Remaining / action.Duration;
		}

		if (_intentionLabel != null)
		{
			if (_combat.IsEnemyStaggered(_enemy))
			{
				// 被打断后的瘫痪（蓝色）
				_intentionLabel.Text = L.T("💫 瘫痪");
				_intentionLabel.Modulate = new Color(0.6f, 0.75f, 1f);
			}
			else
			{
				var current = _combat.GetCurrentIntention(_enemy);
				_intentionLabel.Text = current == null
					? string.Empty
					: current.Interruptible
						? L.F("⚠ {0}", CombatUi.IntentionText(current))
						: CombatUi.IntentionText(current);

				// 可打断的意图用警示色，提示"这招可以破"（将来可在此加特殊光效/音效）
				_intentionLabel.Modulate = current is { Interruptible: true }
					? new Color(1f, 0.7f, 0.25f)
					: Colors.White;
			}
		}

		CombatUi.RefreshEffects(_effectsBar, _enemy);
	}
}
