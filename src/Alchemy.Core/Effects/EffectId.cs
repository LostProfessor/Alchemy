namespace Alchemy.Core.Effects;

/// <summary>
/// 药水效果 ID（策划案"设想中的药水效果" 1~14）。
/// 层数上限默认 5（一瓶药水最多 5 个效果槽），药物依赖除外（上限 8）。
/// </summary>
public enum EffectId
{
    None = 0,

    /// <summary>恢复：回复 x 点生命值。</summary>
    Heal,

    /// <summary>铁皮：获得 x 点格挡值。</summary>
    IronSkin,

    /// <summary>腐蚀：造成 x 点伤害。</summary>
    Corrode,

    /// <summary>烈毒：在一段时间之后，造成 2x 点伤害。</summary>
    VirulentPoison,

    /// <summary>迟钝：下一次需要计时操作所需时间增加 2x 秒（对玩家作用减半）。</summary>
    Sluggish,

    /// <summary>专注：下一次需要计时操作所需时间减少 2x 秒（对玩家作用减半，最低 1 秒）。</summary>
    Focus,

    /// <summary>棘皮：下一次受到伤害时对另一方造成相等伤害（最高不超过 x 点）。</summary>
    Thorny,

    /// <summary>易感：下一次受到的伤害值增加 x。</summary>
    Vulnerable,

    /// <summary>药物依赖：未续则发作，造成 3x 伤害并移去一层；发作前使用 y 层则重置计时并增加 y 层（上限 8）。</summary>
    Dependency,

    /// <summary>涤净：按照从前到后的顺序，移除 x 层负面效果。</summary>
    Purify,

    /// <summary>致幻：下一次动作有 x*10% 的概率失败。</summary>
    Hallucinate,

    /// <summary>精确：下次直接操作造成的伤害增加 x。</summary>
    Precise,

    /// <summary>不朽：在一定时间内，你的生命值不会少于 x 点。</summary>
    Immortal,

    /// <summary>淤伤：下一次行动时受到 x 点伤害，然后层数减 1。</summary>
    Bruise,

    // ── 非药水提供效果（CanComeFromPotion = false，来自遗物/怪物特性）──

    /// <summary>抗药性：具有 y 层时无法受到所有药水效果影响；每接受一种不同的药水效果后层数 -1。</summary>
    Resistance,

    /// <summary>坚韧：受到所有来源伤害 -1（仅能拥有一层）。</summary>
    Toughness,

    /// <summary>祝福：每次进行操作时，回复 1 点生命值（仅能拥有一层）。</summary>
    Blessing,

    /// <summary>击穿：具有 x 层时，下 x 次操作无视护甲。</summary>
    Penetration,

    /// <summary>加深：角色所有附带的药水效果增加 1 层（最高一层）。</summary>
    Deepen,

    /// <summary>消解：角色所有附带的药水效果减少 1 层（最高一层）。</summary>
    Dissolve,
}
