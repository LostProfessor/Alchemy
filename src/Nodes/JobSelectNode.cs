using System;
using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// 职业选择场景脚本（挂 scenes/job_select/job_select.tscn 根节点，类型 Control）。
/// 主菜单点"开始游戏"（解析好种子存入 GameState.PendingSeed）→ 本场景；
/// 三张职业卡选完 → StartNewRun(seed, 基底id) → 进地图。
/// 控件在编辑器里摆好，然后在 Inspector 的脚本面板把 [Export] 字段拖上对应节点：
///   _researcherBtn / _elfBtn / _alienBtn / _backBtn / _infoLabel（可留空）
/// </summary>
public partial class JobSelectNode : Control
{
    [Export] private Button _researcherBtn = null!; // 职业：研究员（清水·队列炼药·炼金典籍）
    [Export] private Button _elfBtn = null!;        // 职业：精灵（浓油·栈炼药·腐蚀手册）
    [Export] private Button _alienBtn = null!;      // 职业：E.T.（黏液·反转炼药·反转卷轴）
    [Export] private Button _backBtn = null!;       // 返回主菜单
    [Export] private Label _infoLabel = null!;      // 底部说明（可留空）

    private GameState _game = null!;

    public override void _Ready()
    {
        _game = GetNode<GameState>("/root/GameState");

        if (_researcherBtn == null) GD.PushError("JobSelectNode: 漏拖 _researcherBtn（研究员）");
        if (_elfBtn == null) GD.PushError("JobSelectNode: 漏拖 _elfBtn（精灵）");
        if (_alienBtn == null) GD.PushError("JobSelectNode: 漏拖 _alienBtn（E.T.）");
        if (_backBtn == null) GD.PushError("JobSelectNode: 漏拖 _backBtn（返回）");

        // 职业 → job id（JobResource/JobCatalog 决定遗物 + 初始药材 + 关联基底）
        _researcherBtn!.Pressed += () => StartWithJob("researcher");
        _elfBtn!.Pressed += () => StartWithJob("elf");
        _alienBtn!.Pressed += () => StartWithJob("et");
        _backBtn!.Pressed += () => _game.ChangeScene("res://scenes/main_menu/main_menu.tscn");
    }

    private void StartWithJob(string jobId)
    {
        int seed = _game.PendingSeed > 0 ? _game.PendingSeed : new Random().Next(1, int.MaxValue);
        _game.StartNewRun(seed, jobId);
        // 开局额外遗物三选一场景（无候选时它会直接进地图）
        _game.ChangeScene("res://scenes/opening/opening.tscn");
    }
}
