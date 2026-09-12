using Alchemy.Core.Brewing;
using Alchemy.Core.Effects;
using Alchemy.Core.GameData;
using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// 游戏入口节点。阶段 1 里只做一件小事：把词条炼药引擎跑一遍，
/// 证明"逻辑层（Alchemy.Core）↔ 表现层（Godot）"已打通。
/// </summary>
public partial class NGame : Node
{
    public override void _Ready()
    {
        GD.Print("[Alchemy] 骨架就绪 —— 词条炼药引擎已加载");

        Demo(BrewingMode.Queue, "萤光菇",
            AffixOp.Add(EffectId.Heal, 2),
            AffixOp.Add(EffectId.Corrode, 1));

        Demo(BrewingMode.Stack, "焦油块",
            AffixOp.Add(EffectId.IronSkin, 3),
            AffixOp.Add(EffectId.Vulnerable, 1),
            AffixOp.Add(EffectId.Corrode, 2),
            AffixOp.Add(EffectId.Heal, 1),
            AffixOp.Add(EffectId.Focus, 1),
            AffixOp.Add(EffectId.Precise, 1)); // 栈满，第 6 个会被拒绝

        Demo(BrewingMode.ReversedQueue, "反转花",
            AffixOp.Add(EffectId.Heal, 1),   // 反转模式下 Add 变成 Remove
            AffixOp.Remove(EffectId.IronSkin, 1)); // 反转模式下 Remove 变成 Add
    }

    private static void Demo(BrewingMode mode, string ingredientName, params AffixOp[] ops)
    {
        var liquid = BaseLiquids.ForMode(mode);
        var potion = new Potion(liquid);
        var engine = new BrewingEngine(mode);
        var ingredient = new Ingredient("demo." + ingredientName, ingredientName, IngredientRarity.Common, ops);
        var result = engine.Apply(ingredient, potion);

        GD.Print($"[Alchemy][{mode}] {ingredientName} → 颜色={potion.Color} 条目=[{string.Join(" | ", potion.Entries)}] 全成功={result.AllSucceeded}");
    }
}
