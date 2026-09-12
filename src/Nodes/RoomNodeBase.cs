using Alchemy.Core.Runs;
using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// 特殊房间场景基类（事件/火堆/宝藏/商店四张独立 .tscn 共用）：
/// 持有 GameState、公共内容容器，提供"回地图"。
/// 进入时机：地图点选节点后已 TryMoveTo（Phase=InRoom、CurrentRoom 就位），
/// 本场景 _Ready 时渲染房间内容，玩家操作后调用逻辑层接口，再 BackToMap()。
/// </summary>
public abstract partial class RoomNodeBase : Control
{
	public const string MapScenePath = "res://scenes/map/map.tscn";

	/// <summary>内容容器：标题/选项/列表等都渲染到这里（场景里摆好即可）。</summary>
	[Export] private VBoxContainer _content = null!;

	protected VBoxContainer Content => _content;

	protected GameState _game = null!;

	/// <summary>当前整局（未开局/局已结束则 null）。</summary>
	protected RunManager? Manager => _game.Manager;

	public override void _Ready()
	{
		_game = GetNode<GameState>("/root/GameState");
		// 整局已结束（理论不会进房，防呆）→ 直接进结算场景，不渲染/不存档
		if (_game.GoToResultIfRunOver())
		{
			return;
		}

		// 自动存档：进入特殊房（事件/宝箱/商店/火堆；检查点=上一完成房 → 读档重进本房）
		_game.Save();
		Render();
	}

	/// <summary>子类渲染本房间内容（每次进房调用一次；内容固定，无需每帧刷新）。</summary>
	protected abstract void Render();

	/// <summary>清空内容容器（重新渲染前调用）。</summary>
	protected void ClearContent()
	{
		foreach (var child in Content.GetChildren())
		{
			child.QueueFree();
		}
	}

	/// <summary>切回地图场景（整局已结束如事件致死 → 直接进失败/通关结算场景）。</summary>
	protected void BackToMap()
	{
		// 整局已结束（如事件扣血致死）→ 不存档、直接进结算场景
		if (_game.GoToResultIfRunOver())
		{
			return;
		}

		// 自动存档：离开特殊房（房通常已完成 → 检查点=本房，读档从它前进）
		_game.Save();

		if (ResourceLoader.Exists(MapScenePath))
		{
			_game.ChangeScene(MapScenePath);
		}
		else
		{
			GD.PushError($"地图场景不存在：{MapScenePath}");
		}
	}

	/// <summary>不在对应房间/没有进行中的局时：显示提示 + 回地图按钮。</summary>
	protected void ShowMissing(string message)
	{
		ClearContent();
		Content.AddChild(new Label { Text = L.T(message) });
		var back = new Button { Text = L.T("返回地图") };
		back.Pressed += BackToMap;
		Content.AddChild(back);
	}
}
