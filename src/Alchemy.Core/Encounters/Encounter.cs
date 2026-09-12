using System.Collections.Generic;

namespace Alchemy.Core.Encounters;

/// <summary>一场遭遇 = 一组怪物组合。首领遭遇携带 BossId（用于首领遗物专属池）。</summary>
public sealed record Encounter(string Id, IReadOnlyList<MonsterTemplate> Monsters, string? BossId = null);
