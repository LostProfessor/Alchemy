using System;
using System.Collections.Generic;
using System.Linq;
using Alchemy.Core.Brewing;
using Alchemy.Core.Encounters;
using Alchemy.Core.GameData;
using Alchemy.Core.Runs;
using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// 内容资源目录：扫描 res://content 下的 .tres 内容资源，构建逻辑层（Alchemy.Core）目录。
/// "数据资源化"的入口——GameState 启动时调用，把 .tres 内容喂给逻辑层。
/// </summary>
public static class ContentCatalog
{
    public const string IngredientsDir = "res://content/ingredients";

    public const string EnemiesDir = "res://content/enemies";

    private static readonly Dictionary<string, IngredientResource> _ingredients = new();
    private static readonly Dictionary<string, EnemyResource> _enemies = new();

    /// <summary>
    /// 从 content/ingredients/*.tres 构建药材目录；
    /// 目录不存在或没有 .tres 时返回 null（调用方保持默认硬编码）。
    /// </summary>
    public static IngredientCatalog? BuildIngredientCatalog()
    {
        var resources = LoadResources<IngredientResource>(IngredientsDir);
        _ingredients.Clear();
        foreach (var r in resources)
        {
            _ingredients[r.Id] = r;
        }

        GD.Print($"[ContentCatalog] 药材资源加载：{resources.Count} 个");
        if (resources.Count == 0)
        {
            return null;
        }

        return new IngredientCatalog(resources.Select(r => r.ToIngredient()));
    }

    /// <summary>从 content/enemies/*.tres 构建敌人模板目录；没有 .tres 时返回 null。</summary>
    public static IReadOnlyDictionary<string, MonsterTemplate>? BuildMonsterTemplates()
    {
        var resources = LoadResources<EnemyResource>(EnemiesDir);
        _enemies.Clear();
        foreach (var r in resources)
        {
            _enemies[r.Id] = r;
        }

        GD.Print($"[ContentCatalog] 敌人资源加载：{resources.Count} 个");
        if (resources.Count == 0)
        {
            return null;
        }

        return resources.ToDictionary(r => r.Id, r => r.ToMonsterTemplate());
    }

    // ── 表现层查询（插图/描述，供卡片/敌人视图用）────────────────

    public static IngredientResource? GetIngredientResource(string id) =>
        _ingredients.TryGetValue(id, out var r) ? r : null;

    public static Texture2D? GetIngredientIcon(string id) => GetIngredientResource(id)?.Icon;

    public static string GetIngredientDescription(string id) =>
        GetIngredientResource(id)?.Description ?? string.Empty;

    public static EnemyResource? GetEnemyResource(string id) =>
        _enemies.TryGetValue(id, out var r) ? r : null;

    public static Texture2D? GetEnemyIcon(string id) => GetEnemyResource(id)?.Icon;

    // ── 职业资源（表现层主数据；后续扩展新职业 = 在 content/jobs 加一个 .tres）──────────

    public const string JobsDir = "res://content/jobs";

    private static readonly Dictionary<string, JobResource> _jobs = new();

    /// <summary>加载职业资源：填表现层缓存，并注入逻辑层 JobCatalog（无 .tres 时保持逻辑层默认）。</summary>
    public static void BuildJobs()
    {
        var resources = LoadResources<JobResource>(JobsDir);
        _jobs.Clear();
        var definitions = new Dictionary<string, JobDefinition>();
        foreach (var r in resources)
        {
            _jobs[r.Id] = r;
            definitions[r.Id] = r.ToJobDefinition();
        }

        JobCatalog.SetCatalog(definitions); // 空则保持逻辑层默认
        GD.Print($"[ContentCatalog] 职业资源加载：{resources.Count} 个");
    }

    /// <summary>按职业 id 查职业资源（researcher/elf/et…）。</summary>
    public static JobResource? GetJob(string id) =>
        _jobs.TryGetValue(id, out var r) ? r : null;

    /// <summary>按关联基底 id 反查职业（当前每基底一职业，供表现层按基底找职业资源）。</summary>
    public static JobResource? GetJobByBaseLiquid(string baseLiquidId) =>
        _jobs.Values.FirstOrDefault(j => j.BaseLiquidId == baseLiquidId);

    // ── 遗物资源（表现数据：名称/描述/图标；逻辑遗物实例按 Id 与之绑定）────────

    public const string RelicsDir = "res://content/relics";

    private static readonly Dictionary<string, RelicResource> _relics = new();

    /// <summary>加载遗物资源（content/relics/*.tres），供 UI 展示名称/描述/图标。</summary>
    public static void BuildRelics()
    {
        var resources = LoadResources<RelicResource>(RelicsDir);
        _relics.Clear();
        foreach (var r in resources)
        {
            _relics[r.Id] = r;
        }

        GD.Print($"[ContentCatalog] 遗物资源加载：{resources.Count} 个");
    }

    /// <summary>按遗物 id 查资源（没有对应 .tres 时返回 null，UI 回退逻辑层遗物自带的名称）。</summary>
    public static RelicResource? GetRelicResource(string id) =>
        _relics.TryGetValue(id, out var r) ? r : null;

    public static Texture2D? GetRelicIcon(string id) => GetRelicResource(id)?.Icon;

    public static string GetRelicName(string id) => GetRelicResource(id)?.DisplayName ?? string.Empty;

    public static string GetRelicDescription(string id) => GetRelicResource(id)?.Description ?? string.Empty;

    // ── 遭遇资源（每大层一张表：非首领遭遇池 + 首领；注入逻辑层 EncounterFactory）──

    public const string EncountersDir = "res://content/encounters";

    /// <summary>加载遭遇表资源并注入 EncounterFactory；目录为空时保持逻辑层默认。</summary>
    public static void BuildEncounters()
    {
        var resources = LoadResources<EncounterTableResource>(EncountersDir);
        var acts = new Dictionary<int, Alchemy.Core.Encounters.EncounterFactory.ActContent>();
        foreach (var table in resources)
        {
            acts[table.ActIndex] = table.ToActContent();
        }

        Alchemy.Core.Encounters.EncounterFactory.SetActs(acts); // 空则忽略
        GD.Print($"[ContentCatalog] 遭遇资源加载：{resources.Count} 个");
    }

    // ── 事件资源（每个事件一个 .tres：标题/描述/选项/动作；注入逻辑层 EventCatalog）──

    public const string EventsDir = "res://content/events";

    private static readonly Dictionary<string, EventResource> _events = new();

    /// <summary>加载事件资源并注入 EventCatalog；目录为空时保持逻辑层默认。</summary>
    public static void BuildEvents()
    {
        var resources = LoadResources<EventResource>(EventsDir);
        _events.Clear();
        var definitions = new List<Alchemy.Core.Rooms.Events.EventDefinition>();
        foreach (var r in resources)
        {
            _events[r.Id] = r;
            definitions.Add(r.ToDefinition());
        }

        Alchemy.Core.GameData.EventCatalog.SetCatalog(definitions); // 空则忽略
        GD.Print($"[ContentCatalog] 事件资源加载：{resources.Count} 个");
    }

    /// <summary>按事件 id 查资源（UI 拿标题/描述用）。</summary>
    public static EventResource? GetEventResource(string id) =>
        _events.TryGetValue(id, out var r) ? r : null;

    public static string GetEventDescription(string id) => GetEventResource(id)?.Description ?? string.Empty;

    // ── 炼药设置（content/settings/brewing_settings.tres：耗时等可调数值，注入逻辑层 BrewingTimings）──

    public const string SettingsDir = "res://content/settings";

    /// <summary>加载炼药设置并注入逻辑层 BrewingTimings（没有 .tres / 目录为空时保持默认 2s / 3s）。</summary>
    public static void BuildBrewingSettings()
    {
        var resources = LoadResources<BrewingSettingsResource>(SettingsDir);
        var settings = resources.FirstOrDefault();
        if (settings == null)
        {
            GD.Print("[ContentCatalog] 炼药设置：未找到 .tres（保持默认 2s / 3s）");
            return;
        }

        Alchemy.Core.Combat.BrewingTimings.Configure(settings.AddIngredientSeconds, settings.CompletePotionSeconds);
        GD.Print($"[ContentCatalog] 炼药设置加载：加料 {settings.AddIngredientSeconds}s / 完成 {settings.CompletePotionSeconds}s");
    }

    private static List<T> LoadResources<T>(string dir)
        where T : Resource
    {
        var result = new List<T>();
        if (!DirAccess.DirExistsAbsolute(dir))
        {
            return result;
        }

        var dirAccess = DirAccess.Open(dir);
        if (dirAccess == null)
        {
            return result;
        }

        foreach (var fileName in dirAccess.GetFiles())
        {
            if (!fileName.EndsWith(".tres", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var path = dir + "/" + fileName;
            var res = ResourceLoader.Load<T>(path);
            if (res != null)
            {
                result.Add(res);
            }
        }

        return result;
    }
}
