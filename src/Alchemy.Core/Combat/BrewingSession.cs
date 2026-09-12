using System;
using System.Linq;
using Alchemy.Core.Brewing;
using Alchemy.Core.Entities;
using Alchemy.Core.Runs;

namespace Alchemy.Core.Combat;

/// <summary>
/// 炼药会话：管理当前正在制作的药水与耗时动作（加药材 / 完成制作）。
/// 一次只允许一个炼药动作进行 —— 配合动作耗时形成"一段时间内可做药水的实际上限"。
/// </summary>
public sealed class BrewingSession
{
    private readonly CombatState _combat;
    private readonly Creature _player;
    private readonly IngredientPocket _pocket;

    public Potion? ActivePotion { get; private set; }

    public bool IsBrewing => ActivePotion != null;

    /// <summary>玩家是否正有一个炼药动作在倒计时。</summary>
    public bool HasPendingBrewAction =>
        _combat.PendingActions.Any(a => a.Actor == _player && a.Id.StartsWith("brew_"));

    /// <summary>药材被消耗时触发（用于历史记录）。</summary>
    public Action<Ingredient>? OnIngredientConsumed { get; set; }

    public BrewingSession(CombatState combat, Creature player, IngredientPocket pocket)
    {
        _combat = combat;
        _player = player;
        _pocket = pocket;
    }

    /// <summary>开始调制一瓶药水（选基底）。</summary>
    public void StartBrew(BaseLiquid baseLiquid)
    {
        if (IsBrewing)
        {
            throw new InvalidOperationException("已有正在制作的药水");
        }

        ActivePotion = new Potion(baseLiquid);
    }

    /// <summary>开始"加药材"耗时动作：立即从口袋扣药材，2 秒后真正加入药水。</summary>
    public bool TryStartAddIngredient(Ingredient ingredient)
    {
        if (ActivePotion == null || HasPendingBrewAction)
        {
            return false;
        }

        if (!_pocket.TryConsume(ingredient.Id))
        {
            return false;
        }

        OnIngredientConsumed?.Invoke(ingredient);

        var engine = new BrewingEngine(ActivePotion.Base.Mode);
        _combat.BeginTimedAction(_player, "brew_add", BrewingTimings.AddIngredientSeconds,
            _ => engine.Apply(ingredient, ActivePotion!));
        return true;
    }

    /// <summary>开始"完成制作"耗时动作：3 秒后产出药水并清空工作台。</summary>
    public bool TryStartCompletePotion(Action<Potion> onDone)
    {
        if (ActivePotion == null || HasPendingBrewAction)
        {
            return false;
        }

        _combat.BeginTimedAction(_player, "brew_complete", BrewingTimings.CompletePotionSeconds, _ =>
        {
            onDone(ActivePotion!);
            ActivePotion = null;
        });
        return true;
    }
}

/// <summary>
/// 炼药耗时（默认值 + 运行时被配置覆盖）。策划案"添加药剂和完成制作都需要一定时间"。
/// Godot 侧启动时从 content/settings/brewing_settings.tres 经 <see cref="Configure"/> 注入，调平衡零编译。
/// </summary>
public static class BrewingTimings
{
    /// <summary>加一味药材耗时（秒）默认值。</summary>
    public const float DefaultAddIngredientSeconds = 2f;

    /// <summary>完成制作耗时（秒）默认值。</summary>
    public const float DefaultCompletePotionSeconds = 3f;

    private static float _addIngredientSeconds = DefaultAddIngredientSeconds;
    private static float _completePotionSeconds = DefaultCompletePotionSeconds;

    /// <summary>加一味药材耗时（秒，运行时值）。</summary>
    public static float AddIngredientSeconds => _addIngredientSeconds;

    /// <summary>完成制作耗时（秒，运行时值）。</summary>
    public static float CompletePotionSeconds => _completePotionSeconds;

    /// <summary>用配置覆盖炼药耗时；≤0 的项忽略（保持当前/默认值）。</summary>
    public static void Configure(float addIngredientSeconds, float completePotionSeconds)
    {
        if (addIngredientSeconds > 0f)
        {
            _addIngredientSeconds = addIngredientSeconds;
        }

        if (completePotionSeconds > 0f)
        {
            _completePotionSeconds = completePotionSeconds;
        }
    }
}
