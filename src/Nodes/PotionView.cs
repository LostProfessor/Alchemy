using System.Linq;
using Alchemy.Core.Brewing;
using Alchemy.Core.Effects;
using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// 出炉的药水实体（scenes/ui/potion.tscn 预制体）：颜色块显示药水颜色，
/// 可拖拽到敌人（投掷攻击/削弱）或玩家区（对自己用药）。
/// 显示（色块/摘要/提示）都在预制体里可视化编辑——本脚本只负责 [Export] 绑定节点 + 填充内容。
/// </summary>
public partial class PotionView : PanelContainer
{
    public Potion Potion { get; private set; } = null!;

    /// <summary>药水颜色块（可换贴图/瓶身样式）。</summary>
    [Export] private ColorRect _colorRect = null!;

    /// <summary>药水摘要（基底名 + 效果×层数）。</summary>
    [Export] private Label _nameLabel = null!;

    /// <summary>拖拽提示。</summary>
    [Export] private Label _hintLabel = null!;

    /// <summary>从预制体实例化一张药水视图并填充内容；预制体缺失返回 null（调用方跳过）。</summary>
    public static PotionView? Create(Potion potion)
    {
        const string scenePath = "res://scenes/ui/potion.tscn";
        if (!ResourceLoader.Exists(scenePath))
        {
            GD.PushError($"药水预制体不存在：{scenePath}");
            return null;
        }

        var view = GD.Load<PackedScene>(scenePath).Instantiate<PotionView>();
        view.Setup(potion);
        return view;
    }

    private void Setup(Potion potion)
    {
        Potion = potion;

        var color = new Color(potion.Color.R / 255f, potion.Color.G / 255f, potion.Color.B / 255f);
        if (_colorRect != null)
        {
            _colorRect.Color = color;
        }

        if (_nameLabel != null)
        {
            _nameLabel.Text = Summary(potion);
        }

        if (_hintLabel != null)
        {
            _hintLabel.Visible = true;
        }
    }

    /// <summary>摘要：清水·恢复×2、铁皮×1（空锅药水则只显示基底名）。</summary>
    private static string Summary(Potion potion)
    {
        var effects = string.Join("、", potion.Entries.Select(e =>
        {
            string name = EffectRegistry.Definitions.TryGetValue(e.Effect, out var def)
                ? def.DisplayName
                : e.Effect.ToString();
            return $"{name}×{e.Layers}";
        }));

        return potion.IsEmpty ? potion.Base.DisplayName : $"{potion.Base.DisplayName}·{effects}";
    }

    /// <summary>开始拖拽：标记 type=potion，预览跟随鼠标显示药水颜色。</summary>
    public override Variant _GetDragData(Vector2 atPosition)
    {
        var data = new Godot.Collections.Dictionary { ["type"] = "potion" };

        var color = _colorRect != null ? _colorRect.Color : Colors.White;
        var preview = new ColorRect
        {
            Color = color,
            CustomMinimumSize = new Vector2(44, 44),
        };
        SetDragPreview(preview);
        return data;
    }
}
