using System.Linq;
using Alchemy.Core.Encounters;
using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// 敌人资源（Godot Resource）：名字 / 生命 / 意图队列 / 插图。
/// 在 Inspector 编辑（含拖插图、配意图），存成 .tres 内容资源，
/// 由 <see cref="ContentCatalog"/> 扫描并映射成逻辑层 <see cref="MonsterTemplate"/>。
/// </summary>
[GlobalClass]
public partial class EnemyResource : Resource
{
    [Export] public string Id { get; set; } = string.Empty;

    [Export] public string DisplayName { get; set; } = string.Empty;

    [Export] public int MaxHp { get; set; } = 10;

    /// <summary>插图（Inspector 直接拖贴图；后面可换动画）。</summary>
    [Export] public Texture2D? Icon { get; set; }

    /// <summary>意图队列：按顺序循环执行，展示给玩家读敌。</summary>
    [Export] public IntentionResource[] Intentions { get; set; } = [];

    public MonsterTemplate ToMonsterTemplate() => new(
        Id,
        DisplayName,
        MaxHp,
        Intentions.Select(i => i.ToIntention()).ToList());
}
