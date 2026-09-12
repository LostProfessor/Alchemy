using System.Linq;
using Alchemy.Core.Effects;
using Alchemy.Core.Entities;
using Alchemy.Core.Encounters;
using Godot;

namespace Alchemy.Nodes;

/// <summary>战斗 UI 共享的静态显示辅助（效果栏文字 / 敌人意图文字）。</summary>
public static class CombatUi
{
	/// <summary>每帧重建一个生物的效果层数显示。</summary>
	public static void RefreshEffects(HBoxContainer? bar, Creature creature)
	{
		if (bar == null)
		{
			return;
		}

		foreach (var child in bar.GetChildren())
		{
			child.QueueFree();
		}

		foreach (var effect in creature.Effects)
		{
			var def = EffectRegistry.Get(effect.Id);
			string text = effect.DurationRemaining.HasValue
				? L.F("{0}×{1}·{2:0}s", L.T(def.DisplayName), effect.Layers, effect.DurationRemaining)
				: L.F("{0}×{1}", L.T(def.DisplayName), effect.Layers);
			bar.AddChild(new Label { Text = text });
		}
	}

	/// <summary>把敌人当前意图显示成文字（如 "攻击 8" / "防御 6" / "施法 易感×1"）。</summary>
	public static string IntentionText(Intention intention) => intention.ActionType switch
	{
		IntentionActionType.Attack => L.F("{0} {1}", L.T(intention.DisplayName), intention.Damage),
		IntentionActionType.Defend => L.F("{0} {1}", L.T(intention.DisplayName), intention.Block),
		IntentionActionType.ApplyEffect =>
			L.F("{0} {1}×{2}", L.T(intention.DisplayName), L.T(EffectRegistry.Get(intention.Effect).DisplayName), intention.EffectLayers),
		_ => L.T(intention.DisplayName),
	};
}
