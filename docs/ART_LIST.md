# 美术贴图需求清单（Alchemy）

> 图片放 `Theme/textures/` 下四个子文件夹（`backgrounds/` / `enemies/` / `ingredients/` / `ui/`）——你已建好。
> 格式建议：PNG 透明底；**命名 = 小写下划线，与对应 `.tres` 的 Id 完全一致**（如 `snakeberry.png`、`big_slime.png`），后面拖引用时对号入座。
> 做完就在框里打勾 ☑️。

---

## ① 药材插图 —— 16 张（最优先）→ `Theme/textures/ingredients/`

> 用在**材料卡**上（战斗口袋里的药材卡直接显示）。建议 128~256px 方形图标、透明底。
> 对应文件：`content/ingredients/*.tres`（16 个都已建好，只差拖图）。

- [√] `ambergris` 龙涎
- [√] `ash` 灰烬
- [√] `bitterroot` 苦根
- [√] `cindercrystal` 炽晶
- [√] `gildmoss` 镀金苔
- [√] `glowcap` 萤光菇
- [√] `moss` 苔藓
- [√] `muddleweed` 迷惘草
- [√] `phoenixfeather` 凤凰羽
- [√] `phosphor` 磷粉
- [√] `reverseflower` 逆鳞花
- [√] `shadowlotus` 影莲
- [√] `snakeberry` 蛇莓
- [√] `timesand` 时砂
- [√] `vine` 绞藤
- [√] `voidpetal` 虚空瓣

---

## ② 敌人插图 —— 19 张 → `Theme/textures/enemies/`

> 用在**战斗大头像**（敌人区插图）。建议 256px+，方形或竖版、透明底。
> ⚠️ 目前只有 4 个敌人 `.tres`（史莱姆/野狼/哥布林/哥布林王）；其余 15 个 `.tres` 数据文件还没建——图可以都先做，`.tres` 等图做好后补（或让我先生成，你只拖图）。

### 大层 1
- [√] `slime` 史莱姆　（已有 .tres）
- [√] `wolf` 野狼　（已有 .tres）
- [√] `goblin` 哥布林　（已有 .tres）

### 大层 2
- [√] `big_slime` 大史莱姆
- [√] `goblin_shaman` 哥布林萨满

### 大层 3
- [√] `troll` 巨魔
- [临时] `ogre` 食人魔
- [临时] `cultist` 教徒

### 大层 4
- [临时] `wight` 尸鬼
- [ ] `dark_knight` 黑骑士
- [ ] `banshee` 女妖

### 大层 5
- [ ] `demon` 恶魔
- [ ] `dragon_whelp` 幼龙
- [ ] `archmage` 大法师

### 首领（每大层 1 个）
- [ ] `goblin_king` 哥布林王　（已有 .tres）
- [ ] `witch_boss` 女巫首领
- [ ] `golem_boss` 石像魔像
- [ ] `lich_king` 亡灵领主
- [ ] `abyss_lord` 深渊之主

---

## ③ 背景图 —— 4 张（可先 1 张通用）→ `Theme/textures/backgrounds/`

> 当前所有场景都是纯色。建议 1920×1080（16:9）。

- [ ] 主菜单背景
- [ ] 职业选择背景
- [ ] 地图背景
- [ ] 战斗背景

---

## ④ UI 图 —— 可选 → `Theme/textures/ui/`

> 优先级最低；没图时先用文字/色块顶着。

- [ ] 地图点图标 ×7：战斗 / 精英 / 事件 / 宝藏 / 火堆 / 商店 / 首领（现在显示"战精事宝火商首"文字）
- [ ] 锅（工作台）贴图 ×1
- [ ] 药水瓶 ×1~4（可做不同颜色变体对应药水效果）
- [ ] 按钮 / 面板九宫格（可选，工作量较大）

---

## ⑤ 效果图标 —— 20 个 → `Theme/textures/ui/effects/`

> 用在**战斗效果栏**（现在显示文字"效果名×层数·秒"，之后换成图标+层数）。
> 建议 16~32px 方形小图标、透明底；命名 = EffectId 小写（如 `heal.png`、`virulentpoison.png`）。
> 标注：🟢 = 增益（Positive）／🔴 = 减益（Negative）。

**药水效果（14）**
- [ ] `heal` 恢复 🟢
- [ ] `ironskin` 铁皮 🟢
- [ ] `corrode` 腐蚀 🔴
- [ ] `virulentpoison` 烈毒 🔴
- [ ] `sluggish` 迟钝 🔴
- [ ] `focus` 专注 🟢
- [ ] `thorny` 棘皮 🟢
- [ ] `vulnerable` 易感 🔴
- [ ] `dependency` 药物依赖 🔴
- [ ] `purify` 涤净 🟢
- [ ] `hallucinate` 致幻 🔴
- [ ] `precise` 精确 🟢
- [ ] `immortal` 不朽 🟢
- [ ] `bruise` 淤伤 🔴

**非药水效果（6，来自遗物/怪物特性）**
- [ ] `resistance` 抗药性 🟢
- [ ] `toughness` 坚韧 🟢
- [ ] `blessing` 祝福 🟢
- [ ] `penetration` 击穿 🟢
- [ ] `deepen` 加深 🟢
- [ ] `dissolve` 消解 🔴

---

## 制作顺序 & 总数

| 优先级 | 类别 | 数量 |
|---|---|---|
| ⭐ 必做 | 药材 | 16 |
| ⭐ 必做 | 敌人 | 19 |
| 次要 | 背景 | 4 |
| 可选 | UI | 7~30 |

**核心 = 35 张；全做完 ≈ 45~50 张。**

做回来之后：药材/敌人的图直接在 `content/**/*.tres` 的 `Icon` 字段拖进去即可（脚本已支持，判空安全）。
