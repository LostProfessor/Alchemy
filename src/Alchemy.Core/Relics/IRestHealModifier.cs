namespace Alchemy.Core.Relics;

/// <summary>
/// 遗物对火堆"睡觉"回复量的修饰接口（策划案"可以被相关遗物影响"）。
/// 遗物实现此接口即自动被 RunManager.PerformRest 应用。
/// </summary>
public interface IRestHealModifier
{
    int ModifyRestHeal(int amount);
}
