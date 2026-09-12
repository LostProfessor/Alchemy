# Alchemy · Godot 表现层对接接口文档（逻辑层 v1）

> 范围：`Alchemy.Core`（纯 C#，无 Godot 依赖）对 `Alchemy`（Godot 表现层，`src/Nodes/**`）暴露的全部对接接口。
> 分工：**逻辑层（本文档）已全部完成，177 个测试全绿；Godot 编辑器内场景/节点/Inspector 由你搭建。**
> 用法：先在场景里用脚本 `new RunManager(seed)` 拿一个实例（单局全局持有），然后按本表调用/读取。

---

## 0. 核心原则（先读这 3 条）

1. **单一时间源**：所有"实时"都由你驱动。战斗房间节点 `_Process(delta)` 里调用 `combat.AdvanceTime(delta)`；暂停 = 停喂 delta。时钟不存在表现层。
2. **你调用逻辑，逻辑不调用你**：逻辑层只暴露方法/属性/事件；场景切来切去是你的事（观察 `RunManager.Phase` + `CurrentRoom.Type` 决定切哪个场景）。
3. **检查点语义**：存档 = 最近完成房间的地图节点。战斗中退出 → 读档后从该房**重进重打**（逻辑层已支持）。

---

## 1. 整局入口 `RunManager`（最重要，一个实例管全局）

命名空间：`Alchemy.Core.Runs`。**单局只 new 一个**，生命周期 = 一局游戏。

| 成员 | 类型 | 说明 |
|---|---|---|
| `RunManager(int seed, int startingHp = 30)` | 构造 | 开局 seed（主菜单输入框）。`startingHp` 默认 30 |
| `StartRun()` | 方法 | 开局：进入第 1 大层、生成地图、进 `OnMap` |
| `Phase` | `RunPhase` | 状态机，见下。**主循环的“当前在哪”就靠它** |
| `Run` | `RunState` | 整局状态：`Pocket`(药材口袋) / `Relics`(遗物栏) / `Currency`(货币) |
| `Player` | `Creature` | 玩家生物：跨房间保留生命，战斗内效果/格挡每场结束清除 |
| `Random` | `RunRandom` | 整局随机源（同种子可重放；基本不用碰） |
| `ActIndex` | `int` | 当前大层 1~5 |
| `Map` | `StandardActMap?` | 当前大层地图（`StartRun` 后非空） |
| `CurrentMapPoint` | `MapPoint?` | 玩家当前地图节点 |
| `CurrentRoom` | `AbstractRoom?` | 当前房间（`InRoom` 阶段非空） |
| `ActiveCombat` | `CombatState?` | 进行中的战斗（战斗房非空） |
| `PendingRewards` | `IReadOnlyList<RewardBag>?` | 战斗胜利后待三选一（`Reward` 阶段非空） |
| `PendingBonusRelic` | `Relic?` | 精英/首领战胜利的额外遗物（领奖励时入账） |
| `IsBossRewardPending` | `bool` | 首领战奖励待领取（领取后通关/进下一大层） |
| `VisitedMapPoints` | `IReadOnlyList<MapPoint>` | 走过的节点（地图染色） |
| `AvailableNextPoints` | `IReadOnlyList<MapPoint>` | 当前节点可选下一节点（= 子节点）。**地图点按钮就遍历它** |
| `History` | `RunHistory` | 经历记录（复盘用，存档已含） |
| `EndRun(bool win)` / `AbandonRun()` | 方法 | 结算/放弃（切回主菜单由你做） |

### RunPhase 状态机

```
NotStarted ──StartRun()──▶ OnMap ──TryMoveTo/MoveToNextRoom──▶ InRoom
                                                                    │
      OnMap ◀──(房间完成：EndCombat→PickReward / PerformRest / PerformEventChoice
               / ClaimTreasure / LeaveShop / CompleteRoom)──────────┤
                                                                    ▼
                                                       战斗胜利 → Reward ──PickReward──▶ OnMap
                                                                    │ 首领领完 ──▶ 下一大层(ActIndex+1, BeginAct) 或 Completed
       玩家死亡(EndCombat(false)) ──▶ Defeated         通关 ──▶ Completed
```

### 地图选房（地图场景）

| 调用 | 说明 |
|---|---|
| `AvailableNextPoints` | 当前可点的下一批节点（不在里面的一律不可点/不可回退） |
| `TryMoveTo(MapPoint target)` | 移动到目标节点并进房；非法（不是子节点/不在 OnMap）返回 `false` |
| `PeekNextRoom()` | 预览当前节点第一个子节点的房间（可选，做“悬停预览”用） |
| `MoveToNextRoom()` | 自动进第一个子节点（测试/自动演示用；正式 UI 用 `TryMoveTo`） |
| `CurrentMapPoint.Coord` | `(Col, Row)` 用于把节点摆到屏幕上（见地图节） |

进房后 `Phase == InRoom`，此时**读 `CurrentRoom.Type` 决定切哪个场景**（见第 4 节房间分发）。

---

## 2. 地图系统

### `StandardActMap`（`Alchemy.Core.Map`）

| 成员 | 说明 |
|---|---|
| `const int Width = 7` | 7 列网格 |
| `ContentFloors` | 内容行数（不含起始行/首领行），第 1 大层为 9 |
| `TreasureRow` | 固定宝箱行（中部） |
| `Start` / `Boss` | 起始节点 / 首领节点（都在中间列，行=0 / 行=ContentFloors+1） |
| `AllPoints` | 全部内容节点（不含 Start/Boss） |
| `PointsInRow(int row)` | 某行的节点列表 |

### `MapPoint`

| 成员 | 说明 |
|---|---|
| `Coord` → `MapCoord(Col, Row)` | 列 0~6、行 1~ContentFloors（起点行=0、首领行=ContentFloors+1） |
| `Type` | `MapPointType`：`Combat / Elite / Event / Treasure / RestSite / Shop / Boss`（`Unassigned` 理论上不会出现） |
| `Children` / `Parents` | 连线关系（`Children` = 下一步可去） |
| `Encounter` | 战斗类节点预掷的遭遇（`Type==Combat/Elite/Boss` 时非空） |

**地图渲染建议**：用 `Map.Coord` 的 `(Col, Row)` 直接映射到屏幕坐标（`x = Col * stepX`，`y = Row * stepY`），`Type` 决定图标，`VisitedMapPoints` 染色，`AvailableNextPoints` 高亮为可点。连线用 `Children/Parents` 画。

---

## 3. 战斗系统 `CombatState`（`Alchemy.Core.Combat`）

由 `RunManager.ActiveCombat` 提供；**不要在表现层 new**。战斗房场景的完整驱动方式：

```csharp
// 每帧（战斗房场景 _Process）
manager.ActiveCombat?.AdvanceTime(delta);   // ⬅️ 唯一的时间喂入口

// 战斗结束判定（由你监控，敌人全灭或玩家死亡时调用一次）
manager.EndCombat(victory: /* bool */);
```

### 重要成员

| 成员 | 说明 |
|---|---|
| `Time` | 累计战斗时间（秒） |
| `Allies` / `Enemies` / `AllCreatures` | 双方生物列表 |
| `Player` | 首个玩家生物 |
| `PendingActions` | 正在倒计时的动作（敌人行动/炼药操作），进度条 UI 用它 |
| `Hooks` | 钩子中枢（遗物已自动挂好，表现层不用管） |
| `Heal(target, amount)` / `GainBlock(target, amount)` | 治疗/格挡（经钩子结算） |
| `ApplyPotion(potion, target, source = null)` | **把一瓶药水泼/喝到目标**（投掷药水就调这个） |
| `BeginAction(actor, actionType)` / `EndAction(ctx)` | 动作包装（致幻打断用；一般不用碰） |
| `BeginTimedAction(...)` / `ScheduleEnemyAction(...)` | 敌人 AI 已在 `StartCombat` 里调度好，不用管 |

### `Creature`（玩家/敌人共用，`Alchemy.Core.Entities`）

| 成员 | 说明 |
|---|---|
| `Name` / `IsPlayer` / `IsAlive` | 基础信息 |
| `MaxHp` / `CurrentHp` / `Block` | 生命/格挡（**直接读**用于 HUD） |
| `CurrentHpChanged(int old, int new)` | C# 事件，订阅后刷新血条 |
| `MaxHpChanged(int old, int new)` | C# 事件（生命上限变化） |
| `Effects` | `List<AppliedEffect>`，显示状态图标/剩余时长 |
| `IncreaseMaxHp / DecreaseMaxHp` | 改生命上限（事件加/祭坛扣） |

`AppliedEffect`：`Id`(效果类型) / `Layers`(层数) / `DurationRemaining`(`float?`，计时类效果才有剩余秒)。

### 敌人移动条
敌人是“到点自动打你”的实时战斗（`ScheduleEnemyAction` 已调度）。每个敌人的下一次行动进度 = 在 `PendingActions` 里找 `Id.StartsWith("enemy_move_")` 且 `Actor == 该敌人` 的那条，`Remaining / Duration` 就是倒计时比例。

---

## 4. 房间分发（表现层核心骨架）

`Phase == InRoom` 时按 `CurrentRoom.Type` 切场景，各房间的**数据来源 + 完成的调用**：

| 房间 | 读什么（渲染） | 完成后调什么 |
|---|---|---|
| `CombatRoom` | `manager.ActiveCombat`（见第 3、5 节） | 敌人全灭/玩家死 → `manager.EndCombat(victory)` → 切奖励场景 |
| `Reward`（战斗胜利后） | `manager.PendingRewards`（3 个袋） | 选一个 → `manager.PickReward(bag)` → 回地图 |
| `RestSiteRoom` | 两个按钮（睡觉/探索） | `manager.PerformRest(RestChoice.Sleep / Explore)` |
| `EventRoom` | `room.Event`（`EventDefinition`：`Title` + `Choices`，每个 `Choice` 有 `Label`） | 点选项 → `manager.PerformEventChoice(index)` |
| `TreasureRoom` | `room.Treasure`（`TreasureReward`：`Relic`/`Currency`/`Ingredients`） | `manager.ClaimTreasure()` |
| `ShopRoom` | `room.Stock`（`List<ShopItem>`：`Relic` + `Price` + `Sold`） | 买 → `manager.TryBuyRelic(index)`；离开 → `manager.LeaveShop()` |

> `RoomType` 枚举：`Combat / Event / Treasure / Shop / RestSite`。
> 首领战也是 `CombatRoom`（`Encounter.BossId != null`），胜利后 `IsBossRewardPending == true`，领完奖励自动推进下一大层/通关。

---

## 5. 炼药 `BrewingSession`（战斗房最核心的交互）

**永远通过 `manager.CreateBrewingSession(manager.ActiveCombat!)` 创建**（已自动接好历史记录回调）。

```csharp
var session = manager.CreateBrewingSession(combat);

// 1) 选基底（三个“职业”）
session.StartBrew(BaseLiquids.Aqua);      // 队列（挤出最旧）—— BaseLiquids.Aqua
// session.StartBrew(BaseLiquids.Oil);    // 栈（满拒）
// session.StartBrew(BaseLiquids.Turbid); // 反转队列（加↔删互换）

// 2) 加药材（点击口袋里的药材 → 立即扣口袋 + 2s 倒计时后入药）
bool ok = session.TryStartAddIngredient(ingredient);   // 口袋不足/正在动作 → false

// 3) 完成制作（3s 后产出，把药水交给你）
bool ok2 = session.TryStartCompletePotion(potion => {
    // potion 做好了 —— 之后可 combat.ApplyPotion(potion, target)
});
```

### 关键成员

| 成员 | 说明 |
|---|---|
| `ActivePotion` / `IsBrewing` | 当前工作台药水 / 是否有药水在做 |
| `HasPendingBrewAction` | 是否正有一个炼药动作在倒计时（**一次只允许一个动作**） |
| `OnIngredientConsumed` | 药材消耗回调（`RunManager` 已接历史，表现层不用管） |

### 药水/药材数据（渲染用）

- `Potion`：`Base`(基底) / `Entries`(`List<EffectEntry>`，最多 `MaxSlots=5`) / `Color`(`PotionColor RGB`) —— **药水颜色自动算好**，直接用 `Color.R/G/B` 给瓶身材质/自发光上色。
- `EffectEntry`：`Effect`(效果类型) / `Layers`(层数)，渲染成 5 格图标。
- `Ingredient`：`record`(`Id, DisplayName, Rarity, AffixOps`)。稀有度 `Common/Uncommon/Rare/Legendary`。口袋：`manager.Run.Pocket` → `Counts`(Id→数量) / `CountOf(id)` / `TryConsume`（由炼药自动扣，UI 只管显示）。
- 效果显示名/颜色：`EffectRegistry.Get(EffectId)` → `DisplayName` + `ColorDelta`（在 `Alchemy.Core.Effects`，可查）。

---

## 6. 奖励（`Alchemy.Core.Runs.Rewards`）

- `RewardBag`：`Items`(`IReadOnlyList<RewardItem>`)；渲染每袋 = 逐条显示。
- `RewardItem`：`IngredientReward(IngredientId, Count)` / `CurrencyReward(Amount)`（遗物类奖励走 `PendingBonusRelic`）。
- `manager.PickReward(bag)`：领取 → 药材入口袋、货币入账、额外遗物入账 → 回地图（或首领后推进/通关）。
- 展示提示：三袋内容**两两不完全一致**，可正常摆三张卡。

---

## 7. 遗物（`Alchemy.Core.Relics`）

| 成员 | 说明 |
|---|---|
| `manager.Run.Relics.Relics` | 已拥有的遗物列表（跨房间/跨战斗） |
| `Relic.Id / DisplayName / Rarity` | 显示用；`HasActive` = 是否有主动技能 |
| `Relic.CanActivate(combat, user)` / `Activate(combat, user)` | 主动遗物（如法力水晶）：玩家按钮点击就调这俩 |
| `RelicCatalog.CreateRandom / CreateRandomCommon` | 一般不用（奖励已代劳） |

> 遗物的被动钩子（每战斗开始回血、格挡加成等）已自动挂到战斗钩子中枢，**表现层零接入**。

---

## 8. 存档（`Alchemy.Core.Runs.Saves`）＋ 经历记录（`Alchemy.Core.Runs.History`）

### 存 / 读

```csharp
// 存：主菜单“保存”或每完成一房后自动存
var data = manager.CreateSaveData();                 // 快照当前整局
SaveService.SaveToFile(data, "user://save.json");    // 路径你自己定（user:// 等）
// 或 var json = SaveService.Serialize(data);

// 读：主菜单“继续”
var loadedData = SaveService.LoadFromFile("user://save.json");
var manager = RunManager.LoadFromSaveData(loadedData);   // 重建整局
// 读档后 Phase == OnMap，CurrentMapPoint == 检查点（最近完成房间的节点），
// 让玩家点 AvailableNextPoints 进下一房即可（战斗中退出 → 重进该房重打）。
```

| 成员 | 说明 |
|---|---|
| `RunSaveData` | 版本号/随机游标/大层/地图种子/检查点坐标/玩家生命/货币/口袋/遗物 Id/历史 |
| `SaveService.Serialize / Deserialize / SaveToFile / LoadFromFile` | System.Text.Json，缩进输出，无需额外依赖 |
| 文件不存在 | `LoadFromFile` 返回 `null` → 主菜单“继续”置灰 |

### 经历记录（复盘面板）

| 成员 | 说明 |
|---|---|
| `manager.History.Entries` | `IReadOnlyList<HistoryEntry>`，按 `Order` 升序 |
| `HistoryEntry` | `Type`(RoomEntered/RoomCompleted/HpChanged/IngredientConsumed/RewardObtained) + `Act/RoomId/Message/Amount/HpBefore/HpAfter/ItemId` |
| 渲染 | 就是一个时间线列表，逐条显示 `Message`（已含中文文案，如“生命 30→22”“消耗药材 glowcap”），药材 Id 可查 `Ingredients.Default` 显示名 |

---

## 9. 推荐搭建顺序（从哪开始）

**建议从“地图 + 房间分发骨架”开始**，因为它是整个游戏最外层、风险最低、能最快看到整局闭环：

1. **应用外壳**：主菜单场景（“开始新游戏”→ 输入/随机 seed → `new RunManager(seed)` + `StartRun()`；有存档时显示“继续”）→ 切地图场景。
2. **地图场景**：渲染 `Map.AllPoints`（7 列网格，`Type` 图标，`Children` 连线），高亮 `AvailableNextPoints`，点 `TryMoveTo`。**到此你就有了“开局→选房”的闭环。**
3. **房间分发器**：一个场景根节点脚本，`_Process` 里看 `Phase` 和 `CurrentRoom.Type`，按表切到 战斗/奖励/火堆/事件/宝藏/商店 子场景。**先用占位面板（打印信息 + 一个“完成”按钮）把每个房间都跑通。**
4. **战斗场景**（工程量最大，拆成小步）：
   a. 敌人/玩家血条 + `AdvanceTime(delta)` 驱动敌人行动条；
   b. 口袋栏 + 基底选择 + `BrewingSession` 加药材/完成制作 + 5 格药水工作台；
   c. 药水投掷（`combat.ApplyPotion(potion, target)`）+ 敌人行动可视化 + 效果图标；
   d. 胜负判定 → `EndCombat`。
5. **奖励三选一**：渲染 `PendingRewards` → `PickReward`。
6. **火堆 / 事件 / 宝藏 / 商店**：数据都有了，纯 UI。
7. **存档**：完成一房自动 `CreateSaveData`，主菜单“继续”读档。
8. **复盘面板**：`History.Entries` 时间线；进阶难度、敌人 AI 强化留给后续逻辑层。

> 每步都可在完成后跑 `dotnet test`（现有 177 个测试是回归保障），逻辑层行为不会因你的 UI 而变。

---

## 10. 常用命名空间速查

| 要做什么 | 命名空间 |
|---|---|
| 整局/存档/历史 | `Alchemy.Core.Runs` · `.Runs.Saves` · `.Runs.History` · `.Runs.Rewards` |
| 战斗/炼药 | `Alchemy.Core.Combat` |
| 地图 | `Alchemy.Core.Map` |
| 房间 | `Alchemy.Core.Rooms` · `.Rooms.Events` |
| 遗物 | `Alchemy.Core.Relics` |
| 效果/药水/药材 | `Alchemy.Core.Effects` · `.Brewing` · `.GameData` |
| 遭遇/敌人 | `Alchemy.Core.Encounters` |
