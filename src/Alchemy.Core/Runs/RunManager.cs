using System;
using System.Collections.Generic;
using System.Linq;
using Alchemy.Core.Combat;
using Alchemy.Core.Entities;
using Alchemy.Core.GameData;
using Alchemy.Core.Encounters;
using Alchemy.Core.Map;
using Alchemy.Core.Relics;
using Alchemy.Core.Rooms;
using Alchemy.Core.Rooms.Events;
using Alchemy.Core.Runs.History;
using Alchemy.Core.Runs.Rewards;
using Alchemy.Core.Runs.Saves;

namespace Alchemy.Core.Runs;

/// <summary>
/// 整局主循环（一局游戏的流程驱动，基于地图节点）：
/// 开局(种子) → 地图选房 → 进房(战斗/事件/商店/宝藏/火堆) → 战斗胜利奖励三选一 → 下一房 → … → 通关/死亡/放弃。
///
/// ⚠️ 说明：这只是"一局"的循环，不是"整个游戏过程"的最外层主循环。
/// 最外层（主菜单 → 开局 → 结算 → 回主菜单）由 Godot 场景层 + 本类的 StartRun/EndRun 生命周期共同拼成。
/// </summary>
public sealed class RunManager
{
	/// <summary>总大层数（策划案：多大大层推进）。</summary>
	public const int TotalActs = 5;

	/// <summary>每大层的内容层数（不含首领），合计约 45~55 个房间。</summary>
	private static readonly int[] ActFloorCounts = { 9, 9, 10, 10, 10 };

	public RunState Run { get; }

	public RunRandom Random { get; }

	/// <summary>整局玩家生物：生命跨房间保留，战斗内效果/格挡每场结束清除。</summary>
	public Creature Player { get; }

	public RunPhase Phase { get; private set; } = RunPhase.NotStarted;

	public AbstractRoom? CurrentRoom { get; private set; }

	public CombatState? ActiveCombat { get; private set; }

	/// <summary>战斗胜利后待三选一的奖励袋（Reward 阶段非空）。</summary>
	public IReadOnlyList<RewardBag>? PendingRewards { get; private set; }

	/// <summary>战斗胜利待领取的货币（点“获取货币”才入账；不领=放弃销毁）。</summary>
	public int? PendingRewardCurrency { get; private set; }

	/// <summary>精英/首领战斗胜利后可选领取的额外遗物（不领=放弃销毁）。</summary>
	public Relic? PendingBonusRelic { get; private set; }

	/// <summary>开局额外遗物候选（普通池三选一，职业遗物已自动获得）。StartRun 后填充；无则 null。</summary>
	public IReadOnlyList<Relic>? PendingOpeningRelicChoices { get; private set; }

	/// <summary>首领战后、进入下一大层前待三选一的首领遗物候选（BossRelicChoice 阶段非空）。</summary>
	public IReadOnlyList<Relic>? PendingBossRelicChoices { get; private set; }

	/// <summary>首领战奖励是否待领取（领取后进入首领遗物选择）。</summary>
	public bool IsBossRewardPending { get; private set; }

	public int ActIndex { get; private set; } = 1;

	public IReadOnlyList<AbstractRoom> VisitedRooms => _visitedRooms;

	/// <summary>玩家走过的地图节点（UI 染色用）。</summary>
	public IReadOnlyList<MapPoint> VisitedMapPoints => _visitedMapPoints;

	/// <summary>当前节点可选的下一节点（= 子节点）；地图分支选择。不允许回退（只前进）。</summary>
	public IReadOnlyList<MapPoint> AvailableNextPoints =>
		CurrentMapPoint == null ? Array.Empty<MapPoint>() : CurrentMapPoint.Children;

	/// <summary>整局经历记录（房间/生命/药材/奖励）。</summary>
	public RunHistory History { get; } = new();

	/// <summary>当前大层的地图（StartRun 后非空）。</summary>
	public StandardActMap? Map { get; private set; }

	/// <summary>玩家当前所在的地图节点。</summary>
	public MapPoint? CurrentMapPoint { get; private set; }

	private readonly List<AbstractRoom> _visitedRooms = new();
	private readonly List<MapPoint> _visitedMapPoints = new();

	/// <summary>存档检查点节点：最近完成房间所在的节点（读档后在此等待重进下一房间）。</summary>
	private MapPoint? _checkpointNode;

	/// <summary>当前大层地图的生成种子（读档重建地图用）。</summary>
	private int _currentMapSeed;

	/// <summary>当前房间进入时的玩家生命（用于记录每房生命变化）。</summary>
	private int _roomStartHp;

	/// <summary>首领战待领（IsBossRewardPending）时记住 Boss 池 id：领完战斗奖励 → PrepareBossRelicChoice 用它重掷首领遗物候选（读档恢复奖励界面也依赖它）。</summary>
	private string? _pendingBossId;

	/// <summary>当前职业（JobCatalog 解析：决定 基底玩法/专属遗物/初始药材）。</summary>
	private readonly JobDefinition _job;

	public RunManager(int seed, int startingHp = 30, string jobId = "researcher")
	{
		Random = new RunRandom(seed);
		Run = new RunState();
		Player = new Creature("炼药师", startingHp, isPlayer: true);
		_job = JobCatalog.Resolve(jobId);
	}

	/// <summary>职业 id（researcher/elf/et…）。</summary>
	public string JobId => _job.Id;

	/// <summary>当前职业关联的基底 id（aqua/oil/slime；锅图等表现按它区分职业）。</summary>
	public string BaseLiquidId => _job.BaseLiquidId;

	/// <summary>开局：送职业初始药材与职业专属遗物，进入第 1 大层，生成地图，进入地图选房阶段。</summary>
	public void StartRun()
	{
		EnsurePhase(RunPhase.NotStarted);
		ActIndex = 1;
		GiveStarterPocket(); // 职业初始药材（解决"第一场战斗无材料可用"）
		Run.Relics.Add(RelicCatalog.CreateById(_job.RelicId) ?? new AquaCraft()); // 职业专属遗物
		BeginAct();
		PrepareOpeningRelicChoice(); // 开局额外遗物三选一候选（UI 层展示；可跳过）
	}

	/// <summary>开局额外遗物候选：从普通遗物池抽 ≤3 件尚未拥有（不含首领；与职业遗物天然不重复）。</summary>
	private void PrepareOpeningRelicChoice()
	{
		var excluded = Run.Relics.Relics.Select(r => r.Id).ToHashSet();
		var rng = Random.Next();
		var choices = new List<Relic>();
		for (int i = 0; i < 3 && choices.Count < 3; i++)
		{
			var relic = RelicCatalog.CreateRandom(rng, excluded);
			if (relic == null)
			{
				break;
			}

			excluded.Add(relic.Id);
			choices.Add(relic);
		}

		PendingOpeningRelicChoices = choices.Count > 0 ? choices : null;
	}

	/// <summary>开局遗物三选一：选第 index 件（Phase 仍 OnMap，可直接进房）。</summary>
	public void ChooseOpeningRelic(int index)
	{
		if (PendingOpeningRelicChoices == null || index < 0 || index >= PendingOpeningRelicChoices.Count)
		{
			return;
		}

		var relic = PendingOpeningRelicChoices[index];
		Run.Relics.Add(relic);
		History.RecordReward(ActIndex, CurrentRoom?.Id ?? string.Empty, $"开局遗物：{relic.DisplayName}", itemId: relic.Id);
		PendingOpeningRelicChoices = null;
	}

	/// <summary>开局遗物三选一：跳过（不拿额外遗物）。</summary>
	public void SkipOpeningRelic() => PendingOpeningRelicChoices = null;

	/// <summary>开局赠送药材：来自职业定义（JobCatalog，可被 .tres 替换）。</summary>
	private void GiveStarterPocket()
	{
		foreach (var (id, count) in _job.StarterIngredients)
		{
			Run.Pocket.Add(id, count);
		}
	}

	/// <summary>开始当前大层：生成该大层地图（按大层序号选遭遇与首领）。</summary>
	private void BeginAct()
	{
		var mapRng = Random.Next();
		_currentMapSeed = mapRng.Seed;
		Map = new StandardActMap(mapRng, ActIndex, ActFloorCounts[ActIndex - 1]);
		CurrentMapPoint = Map.Start;
		_checkpointNode = Map.Start;
		Phase = RunPhase.OnMap;
		_visitedRooms.Clear();
		_visitedMapPoints.Clear();
	}

	/// <summary>下一个可进入的房间（当前节点第一个子节点；无则 null）。</summary>
	public AbstractRoom? PeekNextRoom()
	{
		var next = FirstChildOf(CurrentMapPoint);
		return next == null ? null : CreateRoomFor(next);
	}

	/// <summary>
	/// 地图分支选择：移动到当前节点的某个子节点（不允许回退）。
	/// 目标不是子节点或不在 OnMap 阶段时返回 false。
	/// </summary>
	public bool TryMoveTo(MapPoint target)
	{
		if (Phase != RunPhase.OnMap || CurrentMapPoint == null || target == null)
		{
			return false;
		}

		if (!CurrentMapPoint.Children.Contains(target))
		{
			return false;
		}

		EnterMapPoint(target);
		return true;
	}

	/// <summary>在地图上自动进入下一房间（取当前节点第一个子节点；供测试/自动演示用）。</summary>
	public void MoveToNextRoom()
	{
		EnsurePhase(RunPhase.OnMap);

		var next = FirstChildOf(CurrentMapPoint);
		if (next == null)
		{
			AdvanceToNextActOrWin();
			return;
		}

		EnterMapPoint(next);
	}

	private void EnterMapPoint(MapPoint point)
	{
		CurrentMapPoint = point;
		_visitedMapPoints.Add(point);
		CurrentRoom = CreateRoomFor(point);
		_visitedRooms.Add(CurrentRoom);
		_roomStartHp = Player.CurrentHp;
		History.RecordRoomEntered(ActIndex, CurrentRoom.Id, $"进入 {CurrentRoom.Type} {CurrentRoom.Id}");
		Phase = RunPhase.InRoom;
		OnRoomEntered(CurrentRoom);
	}

	private void OnRoomEntered(AbstractRoom room)
	{
		switch (room)
		{
			case CombatRoom combatRoom:
				StartCombat(combatRoom);
				break;
			case RestSiteRoom:
				// 等待玩家 PerformRest 选择（睡觉/探索）
				break;
			case EventRoom eventRoom:
				eventRoom.Event = EventCatalog.Random(Random.Next());
				break;
			case TreasureRoom treasureRoom:
				treasureRoom.Generate(Random.Next(), Run.Relics.Relics.Select(r => r.Id));
				break;
			case ShopRoom shopRoom:
				shopRoom.GenerateStock(Random.Next(), Run.Relics.Relics.Select(r => r.Id));
				break;
			default:
				// 兜底：直接完成
				CompleteRoom();
				break;
		}
	}

	private void StartCombat(CombatRoom room)
	{
		var combat = new CombatState(Random.Next()); // 战斗随机从整局种子派生
		combat.AddAlly(Player);
		foreach (var monster in room.Encounter.Monsters)
		{
			var enemy = new Creature(monster.DisplayName, monster.MaxHp, templateId: monster.Id);
			combat.AddEnemy(enemy);
			combat.ScheduleEnemyAction(enemy, monster.Intentions); // 按意图队列行动
		}

		combat.AttachRelics(Run.Relics.Relics);
		combat.Start();
		ActiveCombat = combat;
	}

	/// <summary>战斗结束（由 UI/测试在敌人全灭或玩家死亡时调用）。胜利则进入奖励（含货币；精英/首领另有遗物）。</summary>
	public void EndCombat(bool victory)
	{
		EnsurePhase(RunPhase.InRoom);
		if (ActiveCombat == null)
		{
			throw new InvalidOperationException("当前没有进行中的战斗");
		}

		// 战斗后清理：清除战斗内状态（效果/格挡），保留生命跨房间
		Player.Block = 0;
		Player.Effects.Clear();
		Player.Combat = null;
		ActiveCombat = null;

		if (!victory)
		{
			EndRun(win: false);
			return;
		}

		MarkRoomCompleted();

		bool isBoss = CurrentMapPoint?.Type == MapPointType.Boss;
		bool isElite = CurrentMapPoint?.Type == MapPointType.Elite;
		string? bossId = (CurrentRoom as CombatRoom)?.Encounter.BossId; // 用于首领专属遗物池

		// 首领战待领：记住 Boss 池 id（跨层前重掷首领遗物候选用；非首领战清空）
		_pendingBossId = isBoss ? bossId : null;

		// 最后一个大层的首领：胜利即通关（无战斗奖励、无首领遗物选择，直接进通关结算）
		if (isBoss && ActIndex >= TotalActs)
		{
			EndRun(win: true);
			return;
		}

		// 货币奖励 + 额外遗物（精英/首领）—— 见 CombatRewards。
		// 注意：现在不直接入账，全部变成“可选块”在奖励界面领取（可跳过销毁）。
		var rewardRng = Random.Next();
		var reward = CombatRewards.RollRewards(rewardRng, Run.Relics.Relics.Select(r => r.Id), isElite, isBoss, bossId);
		PendingRewardCurrency = reward.Currency;
		PendingBonusRelic = reward.BonusRelic;

		IsBossRewardPending = isBoss;
		Phase = RunPhase.Reward;
		PendingRewards = new RewardRoller(rewardRng, Ingredients.Default)
			.RollThree(RewardTemplates.CombatIngredientBag);
	}

	/// <summary>领取货币奖励（点“获取货币”才入账）；已领/不在奖励阶段返回 false。</summary>
	public bool ClaimRewardCurrency()
	{
		if (Phase != RunPhase.Reward || PendingRewardCurrency == null)
		{
			return false;
		}

		int amount = PendingRewardCurrency.Value;
		Run.Currency += amount;
		History.RecordReward(ActIndex, CurrentRoom?.Id ?? string.Empty, $"战斗胜利货币 +{amount}", amount: amount);
		PendingRewardCurrency = null;
		FinishIfSettled();
		return true;
	}

	/// <summary>领取额外遗物（精英/首领的可选遗物块）；已领/不在奖励阶段返回 false。</summary>
	public bool ClaimBonusRelic()
	{
		if (Phase != RunPhase.Reward || PendingBonusRelic == null)
		{
			return false;
		}

		Run.Relics.Add(PendingBonusRelic);
		History.RecordReward(ActIndex, CurrentRoom?.Id ?? string.Empty, $"获得遗物 {PendingBonusRelic.DisplayName}", itemId: PendingBonusRelic.Id);
		PendingBonusRelic = null;
		FinishIfSettled();
		return true;
	}

	/// <summary>玩家从三选一奖励袋中选一个写入口袋；其余未领块（货币/遗物）仍待玩家决定。</summary>
	public void PickReward(RewardBag bag)
	{
		EnsurePhase(RunPhase.Reward);
		RewardRoller.ApplyTo(bag, Run);
		History.RecordReward(ActIndex, CurrentRoom?.Id ?? string.Empty, $"领取奖励袋（{bag.Items.Count} 件）");
		PendingRewards = null;
		FinishIfSettled();
	}

	/// <summary>跳过剩余所有未领奖励（货币/遗物/袋全部销毁）并收尾。</summary>
	public void SkipRewards()
	{
		if (Phase != RunPhase.Reward)
		{
			return;
		}

		PendingRewards = null;
		PendingBonusRelic = null;
		PendingRewardCurrency = null;
		FinishIfSettled();
	}

	/// <summary>奖励全部处理完（领完或跳过）后的收尾：首领→推进/通关；否则回地图。</summary>
	private void FinishIfSettled()
	{
		if (PendingRewards != null || PendingBonusRelic != null || PendingRewardCurrency != null)
		{
			return; // 还有未决定/未领的块，保持 Reward 阶段
		}

		if (IsBossRewardPending)
		{
			IsBossRewardPending = false;
			var bossId = _pendingBossId;
			_pendingBossId = null;
			PrepareBossRelicChoice(bossId); // 跨层前首领遗物三选一
			return;
		}

		Phase = RunPhase.OnMap;
	}

	/// <summary>首领战后：从该首领专属池抽 ≤3 件尚未拥有的候选，等玩家三选一（选完才进下一大层）。</summary>
	private void PrepareBossRelicChoice(string? bossId)
	{
		// 最后一个大层的 Boss 打完直接通关，不再给首领遗物三选一
		if (ActIndex >= TotalActs)
		{
			AdvanceToNextActOrWin();
			return;
		}

		var pool = BossRelicPools.PoolFor(bossId ?? string.Empty);
		var owned = Run.Relics.Relics.Select(r => r.Id).ToHashSet();
		var candidates = pool?.Where(p => !owned.Contains(p.Id)).Select(p => p.Factory()).ToList();
		if (candidates == null || candidates.Count == 0)
		{
			AdvanceToNextActOrWin(); // 无可选（池空/已全拥有）→ 直接进入下一大层/通关
			return;
		}

		var rng = Random.Next();
		var optionPool = new List<Relic>(candidates);
		var options = new List<Relic>();
		while (options.Count < 3 && optionPool.Count > 0)
		{
			int i = rng.Next(optionPool.Count);
			options.Add(optionPool[i]);
			optionPool.RemoveAt(i);
		}

		PendingBossRelicChoices = options;
		Phase = RunPhase.BossRelicChoice;
	}

	/// <summary>首领遗物三选一：选第 index 件（0-based）并进入下一大层/通关。</summary>
	public void ChooseBossRelic(int index)
	{
		EnsurePhase(RunPhase.BossRelicChoice);
		if (PendingBossRelicChoices == null || index < 0 || index >= PendingBossRelicChoices.Count)
		{
			throw new ArgumentOutOfRangeException(nameof(index));
		}

		var relic = PendingBossRelicChoices[index];
		Run.Relics.Add(relic);
		History.RecordReward(ActIndex, CurrentRoom?.Id ?? string.Empty, $"获得首领遗物 {relic.DisplayName}", itemId: relic.Id);
		PendingBossRelicChoices = null;
		AdvanceToNextActOrWin();
	}

	/// <summary>首领遗物三选一：放弃选择，直接进入下一大层/通关。</summary>
	public void SkipBossRelic()
	{
		EnsurePhase(RunPhase.BossRelicChoice);
		PendingBossRelicChoices = null;
		AdvanceToNextActOrWin();
	}

	/// <summary>还有大层则进入下一大层（跨层保留生命/口袋/遗物/货币，不回血）；否则通关。</summary>
	private void AdvanceToNextActOrWin()
	{
		if (ActIndex < TotalActs)
		{
			ActIndex++;
			BeginAct();
			return;
		}

		EndRun(win: true);
	}

	/// <summary>火堆休息：选择睡觉或探索，随后完成该房回到地图。</summary>
	public void PerformRest(RestChoice choice)
	{
		EnsurePhase(RunPhase.InRoom);
		if (CurrentRoom is not RestSiteRoom)
		{
			throw new InvalidOperationException("当前不在火堆休息房");
		}

		switch (choice)
		{
			case RestChoice.Sleep:
				RestSiteActions.Sleep(Run, Player, ApplyRestHealModifiers);
				History.RecordReward(ActIndex, CurrentRoom?.Id ?? string.Empty, "火堆休息（睡觉）");
				break;
			case RestChoice.Explore:
				RestSiteActions.Explore(Run, Random.Next(), Ingredients.Default);
				History.RecordReward(ActIndex, CurrentRoom?.Id ?? string.Empty, "火堆休息（探索）");
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(choice), choice, null);
		}

		MarkRoomCompleted();
		Phase = RunPhase.OnMap;
	}

	/// <summary>事件房：玩家选择第 index 个选项（0-based），应用动作并完成该房。</summary>
	public void PerformEventChoice(int index)
	{
		EnsurePhase(RunPhase.InRoom);
		if (CurrentRoom is not EventRoom eventRoom || eventRoom.Event == null)
		{
			throw new InvalidOperationException("当前不在事件房");
		}

		if (index < 0 || index >= eventRoom.Event.Choices.Count)
		{
			throw new ArgumentOutOfRangeException(nameof(index));
		}

		EventExecutor.Apply(Run, Player, Random.Next(), eventRoom.Event.Choices[index].Actions);
		History.RecordReward(ActIndex, CurrentRoom?.Id ?? string.Empty, $"事件选择：{eventRoom.Event.Choices[index].Label}");

		// 事件扣血致死：直接判负（不完成房间/不回地图；读档从上一房重进本事件房重选，同战斗死亡语义）
		if (!Player.IsAlive)
		{
			EndRun(win: false);
			return;
		}

		MarkRoomCompleted();
		Phase = RunPhase.OnMap;
	}

	/// <summary>取当前宝藏房奖励（不在宝藏房/无奖励返回 false）。</summary>
	private bool TryTreasureReward(out TreasureReward reward)
	{
		reward = null!;
		if (Phase != RunPhase.InRoom || CurrentRoom is not TreasureRoom treasureRoom || treasureRoom.Treasure == null)
		{
			return false;
		}

		reward = treasureRoom.Treasure;
		return true;
	}

	/// <summary>宝箱：领取货币部分（可逐块领，仍留在房内）。成功返回 true。</summary>
	public bool ClaimTreasureCurrency()
	{
		if (!TryTreasureReward(out var reward) || reward.CurrencyClaimed)
		{
			return false;
		}

		reward.CurrencyClaimed = true;
		Run.Currency += reward.Currency;
		History.RecordReward(ActIndex, CurrentRoom?.Id ?? string.Empty, $"宝箱货币 +{reward.Currency}", amount: reward.Currency);
		return true;
	}

	/// <summary>宝箱：领取遗物部分（可逐块领，仍留在房内）。成功返回 true（无遗物/已领 false）。</summary>
	public bool ClaimTreasureRelic()
	{
		if (!TryTreasureReward(out var reward) || reward.Relic == null || reward.RelicClaimed)
		{
			return false;
		}

		reward.RelicClaimed = true;
		var relic = reward.Relic;
		Run.Relics.Add(relic);
		History.RecordReward(ActIndex, CurrentRoom?.Id ?? string.Empty, $"获得遗物 {relic.DisplayName}", itemId: relic.Id);
		return true;
	}

	/// <summary>宝箱：领取药材袋部分（可逐块领，仍留在房内）。成功返回 true。</summary>
	public bool ClaimTreasureBag()
	{
		if (!TryTreasureReward(out var reward) || reward.BagClaimed)
		{
			return false;
		}

		reward.BagClaimed = true;
		var bag = reward.Ingredients;
		RewardRoller.ApplyTo(bag, Run);
		History.RecordReward(ActIndex, CurrentRoom?.Id ?? string.Empty, $"宝箱药材袋（{bag.Items.Count} 件）");
		return true;
	}

	/// <summary>宝箱：全部领完或"放弃剩余"离开 → 完成该房回地图（未领部分作废）。</summary>
	public void FinishTreasure()
	{
		EnsurePhase(RunPhase.InRoom);
		if (CurrentRoom is not TreasureRoom)
		{
			throw new InvalidOperationException("当前不在宝藏房");
		}

		MarkRoomCompleted();
		Phase = RunPhase.OnMap;
	}

	/// <summary>宝藏房：领取全部（货币+遗物+药材袋）并完成该房（兼容旧调用/测试）。</summary>
	public void ClaimTreasure()
	{
		ClaimTreasureCurrency();
		ClaimTreasureRelic();
		ClaimTreasureBag();
		FinishTreasure();
	}

	/// <summary>商店房：购买第 index 件商品（遗物→入遗物栏 / 药材袋→入口袋）；成功返回 true。</summary>
	public bool TryBuyItem(int index)
	{
		EnsurePhase(RunPhase.InRoom);
		if (CurrentRoom is not ShopRoom shopRoom)
		{
			throw new InvalidOperationException("当前不在商店房");
		}

		var item = shopRoom.Stock.ElementAtOrDefault(index);
		if (item == null || item.Sold || Run.Currency < item.Price)
		{
			return false;
		}

		Run.Currency -= item.Price;
		if (item.IsRelic && item.Relic != null)
		{
			Run.Relics.Add(item.Relic);
			History.RecordReward(ActIndex, CurrentRoom?.Id ?? string.Empty,
				$"购买遗物 {item.Relic.DisplayName}（{item.Price}）", amount: -item.Price, itemId: item.Relic.Id);
		}
		else if (item.IsIngredientBag && item.IngredientBag != null)
		{
			var bag = item.IngredientBag;
			RewardRoller.ApplyTo(bag, Run);
			History.RecordReward(ActIndex, CurrentRoom?.Id ?? string.Empty,
				$"购买药材袋（{bag.Items.Count} 株 · {item.Price}）", amount: -item.Price);
		}

		item.Sold = true;
		return true;
	}

	/// <summary>商店房：离开商店并完成该房。</summary>
	public void LeaveShop()
	{
		EnsurePhase(RunPhase.InRoom);
		if (CurrentRoom is not ShopRoom)
		{
			throw new InvalidOperationException("当前不在商店房");
		}

		MarkRoomCompleted();
		Phase = RunPhase.OnMap;
	}

	/// <summary>创建一个接好历史记录回调的炼药会话（战斗内用，构造时传入战斗/玩家/口袋）。</summary>
	public BrewingSession CreateBrewingSession(CombatState combat)
	{
		return new BrewingSession(combat, Player, Run.Pocket)
		{
			OnIngredientConsumed = ingredient =>
				History.RecordIngredientConsumed(ActIndex, CurrentRoom?.Id ?? string.Empty, ingredient.Id),
		};
	}

	/// <summary>应用所有遗物对睡觉回复量的修饰（IRestHealModifier）。</summary>
	private int ApplyRestHealModifiers(int baseHeal)
	{
		int result = baseHeal;
		foreach (var relic in Run.Relics.Relics.OfType<IRestHealModifier>())
		{
			result = relic.ModifyRestHeal(result);
		}

		return result;
	}

	/// <summary>结束当前房间（事件/宝藏/商店占位：直接完成回到地图）。</summary>
	/// <summary>
	/// 放弃当前战斗（撤退/调试用）：清理战斗状态并回到地图。
	/// 该房间不标记完成、无奖励；Phase 恢复 OnMap 以便继续走图。
	/// </summary>
	public void AbandonCombat()
	{
		if (Phase != RunPhase.InRoom || ActiveCombat == null)
		{
			return;
		}

		Player.Block = 0;
		Player.Effects.Clear();
		Player.Combat = null;
		ActiveCombat = null;
		Phase = RunPhase.OnMap;
	}

	public void CompleteRoom()
	{
		if (Phase != RunPhase.InRoom)
		{
			return;
		}

		MarkRoomCompleted();
		Phase = RunPhase.OnMap;
	}

	/// <summary>统一完成房间：标记完成 + 更新存档检查点 + 记录生命变化/房间完成。</summary>
	private void MarkRoomCompleted()
	{
		if (CurrentRoom == null)
		{
			return;
		}

		CurrentRoom.MarkCompleted();
		if (CurrentMapPoint != null)
		{
			_checkpointNode = CurrentMapPoint;
		}

		History.RecordHpChange(ActIndex, CurrentRoom.Id, _roomStartHp, Player.CurrentHp);
		History.RecordRoomCompleted(ActIndex, CurrentRoom.Id, $"完成 {CurrentRoom.Type} {CurrentRoom.Id}");
	}

	/// <summary>结束本局（通关/死亡）。"回到游戏入口"由 Godot 场景层负责。</summary>
	public void EndRun(bool win)
	{
		Phase = win ? RunPhase.Completed : RunPhase.Defeated;
		CurrentRoom = null;
		ActiveCombat = null;
	}

	public void AbandonRun()
	{
		if (Phase is RunPhase.Completed or RunPhase.Defeated or RunPhase.Abandoned)
		{
			return;
		}

		Phase = RunPhase.Abandoned;
		CurrentRoom = null;
		ActiveCombat = null;
	}

	private static MapPoint? FirstChildOf(MapPoint? point) =>
		point is { Children.Count: > 0 } ? point.Children[0] : null;

	private static MapPoint? FindPoint(StandardActMap map, int col, int row) =>
		col < 0 || row < 0 ? null : map.AllPoints.FirstOrDefault(p => p.Coord.Col == col && p.Coord.Row == row);

	/// <summary>把当前整局状态序列化为存档数据（检查点 = 最近完成房间的节点）。</summary>
	public RunSaveData CreateSaveData()
	{
		return new RunSaveData
		{
			Version = 1,
			JobId = JobId,
			MasterSeed = Random.MasterSeed,
			StreamCounter = Random.StreamCounter,
			ActIndex = ActIndex,
			CurrentMapSeed = _currentMapSeed,
			CurrentMapCol = _checkpointNode?.Coord.Col ?? -1,
			CurrentMapRow = _checkpointNode?.Coord.Row ?? -1,
			PlayerMaxHp = Player.MaxHp,
			PlayerCurrentHp = Player.CurrentHp,
			Currency = Run.Currency,
			Pocket = new Dictionary<string, int>(Run.Pocket.Counts),
			RelicIds = Run.Relics.Relics.Select(r => r.Id).ToList(),
			History = History.Entries.ToList(),
			RewardState = BuildSavedRewardState(),
		};
	}

	/// <summary>奖励界面阶段 → 存档快照；非 Reward/BossRelicChoice 阶段返回 null（读档回地图检查点）。</summary>
	private SavedRewardState? BuildSavedRewardState()
	{
		if (Phase is not (RunPhase.Reward or RunPhase.BossRelicChoice))
		{
			return null;
		}

		return new SavedRewardState
		{
			Phase = Phase.ToString(),
			IsBossRewardPending = IsBossRewardPending,
			PendingRewardCurrency = PendingRewardCurrency,
			PendingBonusRelicId = PendingBonusRelic?.Id,
			PendingBags = PendingRewards?.Select(ToSavedBag).ToList(),
			PendingBossRelicIds = PendingBossRelicChoices?.Select(r => r.Id).ToList(),
			PendingBossId = _pendingBossId,
		};
	}

	private static SavedRewardBag ToSavedBag(RewardBag bag) => new()
	{
		Items = bag.Items.Select(item => item switch
		{
			IngredientReward ir => new SavedRewardItem { Kind = "Ingredient", IngredientId = ir.IngredientId, Count = ir.Count },
			CurrencyReward cr => new SavedRewardItem { Kind = "Currency", Amount = cr.Amount },
			_ => new SavedRewardItem { Kind = "Unknown" },
		}).ToList(),
	};

	/// <summary>从存档数据重建整局（恢复地图/随机游标/状态/历史），读档后从检查点等下一房间重进。</summary>
	public static RunManager LoadFromSaveData(RunSaveData data)
	{
		var manager = new RunManager(data.MasterSeed, jobId: data.JobId ?? "researcher");
		manager.Random.SetState(data.MasterSeed, data.StreamCounter);
		manager.ActIndex = Math.Clamp(data.ActIndex, 1, TotalActs);
		manager._currentMapSeed = data.CurrentMapSeed;

		var mapRng = new BattleRandom(data.CurrentMapSeed);
		manager.Map = new StandardActMap(mapRng, manager.ActIndex, ActFloorCounts[manager.ActIndex - 1]);
		manager._checkpointNode = FindPoint(manager.Map, data.CurrentMapCol, data.CurrentMapRow) ?? manager.Map.Start;
		manager.CurrentMapPoint = manager._checkpointNode;
		manager.Phase = RunPhase.OnMap;

		manager.Player.RestoreHp(data.PlayerMaxHp, data.PlayerCurrentHp);
		manager.Run.Currency = Math.Max(0, data.Currency);
		foreach (var (id, count) in data.Pocket)
		{
			manager.Run.Pocket.Add(id, count);
		}

		foreach (var relicId in data.RelicIds)
		{
			var relic = RelicCatalog.CreateById(relicId);
			if (relic != null)
			{
				manager.Run.Relics.Add(relic);
			}
		}

		manager.History.Restore(data.History);

		// 奖励界面存档：恢复到 Reward / BossRelicChoice，待领块原样放回（读档后回到同样界面继续选）
		if (data.RewardState != null && data.RewardState.Phase is "Reward" or "BossRelicChoice")
		{
			manager.IsBossRewardPending = data.RewardState.IsBossRewardPending;
			manager.PendingRewardCurrency = data.RewardState.PendingRewardCurrency;
			manager.PendingBonusRelic = data.RewardState.PendingBonusRelicId != null
				? RelicCatalog.CreateById(data.RewardState.PendingBonusRelicId)
				: null;
			manager.PendingRewards = data.RewardState.PendingBags?
				.Select(FromSavedBag)
				.Where(bag => bag != null)
				.Cast<RewardBag>()
				.ToList();
			manager.PendingBossRelicChoices = data.RewardState.PendingBossRelicIds?
				.Select(RelicCatalog.CreateById)
				.Where(r => r != null)
				.Cast<Relic>()
				.ToList();
			manager._pendingBossId = data.RewardState.PendingBossId;
			manager.Phase = data.RewardState.Phase == "BossRelicChoice"
				? RunPhase.BossRelicChoice
				: RunPhase.Reward;
		}

		return manager;
	}

	private static RewardBag? FromSavedBag(SavedRewardBag saved)
	{
		if (saved.Items.Count == 0)
		{
			return null;
		}

		var items = new List<RewardItem>();
		foreach (var item in saved.Items)
		{
			switch (item.Kind)
			{
				case "Ingredient":
					items.Add(new IngredientReward(item.IngredientId, item.Count));
					break;
				case "Currency":
					items.Add(new CurrencyReward(item.Amount));
					break;
			}
		}

		return items.Count == 0 ? null : new RewardBag(items);
	}

	private AbstractRoom CreateRoomFor(MapPoint point)
	{
		var floor = point.Coord.Row;
		var id = $"{point.Type.ToString().ToLowerInvariant()}_{point.Coord.Col}_{point.Coord.Row}";

		// 事件节点进入时揭晓：7% 常规战斗 / 7% 宝箱 / 7% 商店 / 79% 真事件
		if (point.Type == MapPointType.Event)
		{
			return RollEventDestination(id, floor);
		}

		return point.Type switch
		{
			MapPointType.Combat or MapPointType.Elite or MapPointType.Boss =>
				new CombatRoom(id, floor, point.Encounter ?? throw new InvalidOperationException($"节点 {id} 缺少遭遇")),
			MapPointType.Treasure => new TreasureRoom(id, floor),
			MapPointType.Shop => new ShopRoom(id, floor),
			MapPointType.RestSite => new RestSiteRoom(id, floor),
			_ => throw new ArgumentOutOfRangeException(nameof(point.Type), point.Type, null),
		};
	}

	/// <summary>事件节点掷骰决定实际房间（确定性：同一派生随机流，同种子可重放）。</summary>
	private AbstractRoom RollEventDestination(string id, int floor)
	{
		int roll = Random.Next().Next(100); // 0~99
		return roll switch
		{
			< 7 => new CombatRoom(id, floor, EncounterFactory.Random(Random.Next(), ActIndex)),
			< 14 => new TreasureRoom(id, floor),
			< 21 => new ShopRoom(id, floor),
			_ => new EventRoom(id, floor),
		};
	}

	private void EnsurePhase(RunPhase expected)
	{
		if (Phase != expected)
		{
			throw new InvalidOperationException($"当前阶段 {Phase}，期望 {expected}");
		}
	}
}
