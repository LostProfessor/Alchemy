namespace Alchemy.Core.Effects;

/// <summary>
/// 计时类效果的默认时长（秒）。后续可移动到数据资源文件（.tres/JSON）。
/// </summary>
public static class EffectTimings
{
    /// <summary>烈毒：一段时间后造成 2x 伤害。</summary>
    public const float VirulentPoisonSeconds = 3f;

    /// <summary>药物依赖：未续则发作。</summary>
    public const float DependencySeconds = 5f;

    /// <summary>不朽：持续时间内生命不低于 x 点。</summary>
    public const float ImmortalSeconds = 10f;
}
