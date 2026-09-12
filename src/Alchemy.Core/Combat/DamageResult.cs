namespace Alchemy.Core.Combat;

/// <summary>一次伤害结算的结果。</summary>
public readonly record struct DamageResult(int ActualDamage, int BlockAbsorbed);
