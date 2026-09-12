using System.Collections.Generic;
using System.Linq;

namespace Alchemy.Core.Relics;

/// <summary>遗物栏：整局持有（跨战斗、跨房间），随存档持久化。</summary>
public sealed class RelicInventory
{
    private readonly List<Relic> _relics = new();

    public IReadOnlyList<Relic> Relics => _relics;

    public void Add(Relic relic) => _relics.Add(relic);

    public bool Remove(Relic relic) => _relics.Remove(relic);

    public bool Has(string id) => _relics.Any(r => r.Id == id);

    public int Count => _relics.Count;
}
