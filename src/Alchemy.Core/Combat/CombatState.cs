using System;
using System.Collections.Generic;
using System.Linq;
using Alchemy.Core.Brewing;
using Alchemy.Core.Effects;
using Alchemy.Core.Encounters;
using Alchemy.Core.Entities;
using Alchemy.Core.Hooks;

namespace Alchemy.Core.Combat;

/// <summary>
/// 战斗状态：持有双方生物、钩子中枢、战斗随机数与战斗时钟。
/// 负责伤害/治疗/格挡的核心结算流程、动作包装、时间推进与药水施加。
/// </summary>
public sealed class CombatState
{
    private readonly List<Creature> _allies = new();
    private readonly List<Creature> _enemies = new();
    private readonly List<TimedAction> _pendingActions = new();
    private readonly Dictionary<Creature, Intention> _enemyCurrentIntention = new();

    public HookHub Hooks { get; } = new();

    public BattleRandom Random { get; }

    /// <summary>战斗累计时间（秒）。</summary>
    public float Time { get; private set; }

    public IReadOnlyList<Creature> Allies => _allies;

    public IReadOnlyList<Creature> Enemies => _enemies;

    public IEnumerable<Creature> AllCreatures => _allies.Concat(_enemies);

    /// <summary>所有玩家生物（本作通常 1 名）。</summary>
    public IEnumerable<Creature> PlayerCreatures => AllCreatures.Where(c => c.IsPlayer);

    /// <summary>首个玩家生物（无则 null）。</summary>
    public Creature? Player => PlayerCreatures.FirstOrDefault();

    public CombatState(int? randomSeed = null) => Random = new BattleRandom(randomSeed);

    /// <summary>用外部随机源构造（整局种子派生的战斗随机流）。</summary>
    public CombatState(BattleRandom random) => Random = random ?? new BattleRandom();

    public void AddAlly(Creature creature) => Attach(creature, _allies);

    public void AddEnemy(Creature creature) => Attach(creature, _enemies);

    private void Attach(Creature creature, List<Creature> list)
    {
        creature.Combat = this;
        list.Add(creature);
        Hooks.AddListener(creature);
    }

    public void RemoveCreature(Creature creature)
    {
        _allies.Remove(creature);
        _enemies.Remove(creature);
        Hooks.RemoveListener(creature);
        creature.Combat = null;
    }

    /// <summary>挂接整局遗物（作为钩子监听者），遗物据此被动响应战斗事件。</summary>
    public void AttachRelics(IEnumerable<IHookListener> relics)
    {
        foreach (var relic in relics)
        {
            Hooks.AddListener(relic);
        }
    }

    /// <summary>宣告战斗开始（触发 OnCombatStart，遗物在此发开场增益）。应在组装完双方与遗物后调用。</summary>
    public void Start() => Hooks.RaiseCombatStart(this);

    public IEnumerable<Creature> GetOpponentsOf(Creature creature) =>
        _allies.Contains(creature) ? _enemies : _allies;

    // ── 动作包装 ────────────────────────────────────────────────

    /// <summary>开始一个动作（抛出 BeforeAction，致幻失败会标记 Cancelled）。</summary>
    public ActionContext BeginAction(Creature actor, string actionType)
    {
        var ctx = new ActionContext(actor, actionType);
        Hooks.RaiseBeforeAction(ctx);
        return ctx;
    }

    /// <summary>结束一个动作（抛出 AfterAction，祝福在此回血）。</summary>
    public void EndAction(ActionContext ctx) => Hooks.RaiseAfterAction(ctx);

    // ── 伤害结算核心流程 ────────────────────────────────────────

    public DamageResult DealDamage(DamageContext ctx)
    {
        Hooks.RaiseBeforeDamage(ctx); // 易感/坚韧/精确/不朽等在此修改 Amount

        int amount = Math.Max(0, ctx.Amount);

        // 格挡吸收
        if (amount > 0 && !ctx.IgnoreBlock && !ctx.Unblockable && ctx.Target.Block > 0)
        {
            int absorbed = Math.Min(ctx.Target.Block, amount);
            ctx.BlockAbsorbed = absorbed;
            ctx.Target.Block -= absorbed;
            amount -= absorbed;
        }

        // 生命损失
        if (amount > 0)
        {
            ctx.Target.CurrentHp -= amount;
            if (ctx.Target.CurrentHp < 0)
            {
                ctx.Target.CurrentHp = 0; // 生命不为负
            }

            ctx.ActualDealt = amount;
        }

        Hooks.RaiseAfterDamage(ctx); // 不朽夹持生命、棘皮反弹等

        if (!ctx.Target.IsAlive)
        {
            Hooks.RaiseCreatureDied(ctx.Target);
        }

        return new DamageResult(ctx.ActualDealt, ctx.BlockAbsorbed);
    }

    // ── 治疗 / 格挡 ─────────────────────────────────────────────

    public void Heal(Creature target, int amount)
    {
        var ctx = new HealContext(target, amount);
        Hooks.RaiseBeforeHeal(ctx);
        target.CurrentHp = Math.Min(target.MaxHp, target.CurrentHp + Math.Max(0, ctx.Amount));
        Hooks.RaiseAfterHeal(ctx);
    }

    public void GainBlock(Creature target, int amount)
    {
        var ctx = new BlockContext(target, amount);
        Hooks.RaiseBeforeBlockGained(ctx);
        target.Block += Math.Max(0, ctx.Amount);
        Hooks.RaiseAfterBlockGained(ctx);
    }

    // ── 时间推进（计时类效果 + 计时动作统一由 Godot 喂入的 delta 驱动）────────

    public void AdvanceTime(float seconds)
    {
        if (seconds <= 0)
        {
            return;
        }

        Time += seconds;

        // 1) 计时类效果（烈毒/药物依赖/不朽）
        var timed = AllCreatures.SelectMany(c => c.Effects)
                                .Where(e => e.DurationRemaining.HasValue)
                                .ToList();

        foreach (var effect in timed)
        {
            if (!effect.Owner.Effects.Contains(effect))
            {
                continue; // 已被移除
            }

            effect.DurationRemaining -= seconds;
            if (effect.DurationRemaining <= 0)
            {
                EffectBehaviors.Get(effect.Id).OnExpire?.Invoke(effect, seconds);
            }
        }

        // 2) 计时动作（敌人行动/炼药操作）；倒序迭代，允许回调中增删
        for (int i = _pendingActions.Count - 1; i >= 0; i--)
        {
            var action = _pendingActions[i];
            action.Remaining -= seconds;

            // 预兆：进入"执行前 telegraph 秒"时触发一次（先于完成判定，保证信号先到）
            if (!action.Telegraphed && action.TelegraphSeconds > 0f && action.Remaining <= action.TelegraphSeconds)
            {
                action.Telegraphed = true;
                ActionTelegraphed?.Invoke(new ActionTelegraph(action.Actor, action.Id, action, Math.Max(0f, action.Remaining)));
            }

            if (action.Remaining <= 0)
            {
                _pendingActions.RemoveAt(i);
                action.OnComplete(this);
            }
        }
    }

    // ── 计时动作（敌人行动/炼药操作）─────────────────────────────

    public IReadOnlyList<TimedAction> PendingActions => _pendingActions;

    /// <summary>动作预兆事件：计时动作进入"执行前 telegraph 秒"时触发一次（表现层据此播抬手/蓄力动画）。</summary>
    public event Action<ActionTelegraph>? ActionTelegraphed;

    /// <summary>敌人行动的默认预兆提前量（秒）：意图未单独指定（TelegraphSeconds&lt;0）时用它（默认 1.5s）。</summary>
    public float EnemyTelegraphSeconds { get; set; } = 1.5f;

    /// <summary>
    /// 开始一个计时动作（敌人行动/炼药操作等）。
    /// 先抛 OnBeforeTimedAction（迟钝/专注修改时长），再进入倒计时队列。
    /// telegraphSeconds &gt; 0 时，动作执行前该秒数会触发一次 ActionTelegraphed（预兆）。
    /// </summary>
    public TimedAction BeginTimedAction(Creature actor, string id, float baseDuration, Action<CombatState> onComplete, float telegraphSeconds = 0f)
    {
        var ctx = new TimedActionContext(actor, id, baseDuration);
        Hooks.RaiseBeforeTimedAction(ctx);
        float duration = Math.Max(ctx.MinDuration, ctx.Duration);
        var action = new TimedAction(id, actor, duration, onComplete, telegraphSeconds);
        _pendingActions.Add(action);
        return action;
    }

    /// <summary>
    /// 调度敌人行动：按意图队列循环执行（攻击/防御/施加效果），
    /// 执行后间隔该意图的 IntervalSeconds 进入下一个意图。敌人/玩家死亡后自动停止。
    /// </summary>
    public void ScheduleEnemyAction(Creature enemy, IReadOnlyList<Intention> intentions)
    {
        ScheduleNextIntention(enemy, intentions, 0);
    }

    private void ScheduleNextIntention(Creature enemy, IReadOnlyList<Intention> intentions, int index)
    {
        var intention = intentions[index % intentions.Count];
        _enemyCurrentIntention[enemy] = intention; // 记录当前意图（表现层展示）

        // 预兆提前量：意图可单独指定（>=0），否则用战斗默认（EnemyTelegraphSeconds）
        float telegraph = intention.TelegraphSeconds >= 0f ? intention.TelegraphSeconds : EnemyTelegraphSeconds;

        _pendingActions.Add(new TimedAction($"enemy_move_{enemy.Name}", enemy, intention.IntervalSeconds, combat =>
        {
            var player = combat.Player;
            if (player == null || !player.IsAlive || !enemy.IsAlive)
            {
                return; // 行动目标或自身已死，不再行动
            }

            ExecuteIntention(enemy, intention);
            ScheduleNextIntention(enemy, intentions, index + 1); // 进入队列下一个意图
        }, telegraph));
    }

    /// <summary>敌人当前正在倒计时/将要执行的意图（供表现层展示，无则 null）。</summary>
    public Intention? GetCurrentIntention(Creature enemy) =>
        _enemyCurrentIntention.TryGetValue(enemy, out var intention) ? intention : null;

    private void ExecuteIntention(Creature enemy, Intention intention)
    {
        switch (intention.ActionType)
        {
            case IntentionActionType.Attack:
                DealDamage(new DamageContext(enemy, Player!, intention.Damage));
                break;

            case IntentionActionType.Defend:
                GainBlock(enemy, intention.Block);
                break;

            case IntentionActionType.ApplyEffect:
                var target = intention.TargetSelf ? enemy : Player!;
                if (intention.EffectLayers > 0)
                {
                    target.AddEffect(intention.Effect, intention.EffectLayers);
                }

                break;
        }
    }

    // ── 药水施加 ────────────────────────────────────────────────

    /// <summary>把一瓶药水施加到目标（"泼出去/喝下去"的一刻）。</summary>
    public void ApplyPotion(Potion potion, Creature target, Creature? source = null) =>
        PotionApplication.Apply(this, potion, target, source);
}
