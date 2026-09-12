using Alchemy.Core.Combat;

namespace Alchemy.Core.Runs;

/// <summary>
/// 整局随机源：持有主种子，按请求次序派生确定性的独立随机流（战斗/奖励/地图各自分流）。
/// 相同主种子 + 相同请求顺序 → 完全可重放（存档一致性、种子分享都靠它）。
/// </summary>
public sealed class RunRandom
{
    private int _masterSeed;
    private int _streamCounter;

    public int MasterSeed => _masterSeed;

    /// <summary>已派生的流数量（用于存档恢复随机游标）。</summary>
    public int StreamCounter => _streamCounter;

    public RunRandom(int masterSeed) => _masterSeed = masterSeed;

    /// <summary>用存档中的游标恢复（保证后续随机确定）。</summary>
    public void SetState(int masterSeed, int streamCounter)
    {
        _masterSeed = masterSeed;
        _streamCounter = streamCounter;
    }

    /// <summary>派生下一个独立随机流（确定性）。</summary>
    public BattleRandom Next()
    {
        int seed = Combine(_masterSeed, _streamCounter++);
        return new BattleRandom(seed);
    }

    private static int Combine(int a, int b)
    {
        unchecked
        {
            int h = 17 * 31 + a;
            h = h * 31 + b;
            return h;
        }
    }
}
