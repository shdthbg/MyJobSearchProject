# MyWarChess — 回合制战棋战斗系统

> 🎥 **演示视频**（包含玩家寻路、入战触发、回合制攻防、敌人 AI 行动、血量/死亡/脱战完整闭环，Console 同步展示事件传递链路）：
>
> <video src="Docs/DevLog/初版演示.mp4" controls width="100%"></video>
> 或点击查看：[初版演示.mp4](Docs/DevLog/初版演示.mp4)

## 项目概述

本项目是一套基于 Unity 2022 LTS 的**回合制战术 RPG 战斗系统原型**，实现了"自由探索 ↔ 回合制战斗"的无缝切换。核心亮点：

- **事件驱动架构**：自研泛型事件总线（`EventBus`）解耦全部模块，13 种事件类型覆盖战斗全生命周期
- **双队列轮转算法**：按速度降序排列的回合队列，支持战斗中途动态插入/移除单位，同速稳定保序
- **完整战斗闭环**：攻击 → Animation Event 精确判定打击帧 → 伤害计算 → 血量更新 → World Space 头顶血条实时响应 → 死亡清除 → 胜负判定与脱战恢复
- **敌人 AI 状态机**：五状态状态机（Idle → ChooseAction → Moving → Attacking → EndTurn），含 AP 预算决策、移动中实时检测攻击范围、面向目标攻击
- **表现层与逻辑层分离**：`BattleManager` 作为中间层，将队列变更转为标准事件驱动 UI/动画，逻辑层不感知表现细节

## 核心功能

### 探索与战斗切换
- **自由探索**：鼠标点击角色选中 / 点击地面 NavMesh 寻路，支持移动距离限制与范围可视化，Alt+左键穿透角色强制地面移动
- **入战检测**：SphereCollider 触发器粗筛 → NavMesh 精确路径距离二级过滤，靠近敌人自动切换战斗模式
- **动态增援**：战斗中实时检测新敌人进入触发范围，自动初始化 AP 并加入下回合队列
- **脱战恢复**：战斗结束后恢复角色控制、重置移动目标、回收 UI，无缝返回探索状态

### 回合制战斗核心
- **双队列回合推进**：当前队列 + 准备队列，按速度降序轮转，支持死亡即时移出与同速保序
- **AP 行动点系统**：移动 1AP / 攻击 2AP（可配置），AP 耗尽自动结束回合，`APChanged` 事件驱动 UI 实时更新
- **攻击系统**：Animation Event 在动画精确帧触发 `OnAttackHit()` 回调 → 广播 `Attacked` 事件 → `AttackSystem` 调用 `HealthComponent.TakeDamage` → 死亡则广播 `UnitDied`
- **玩家操作**：战斗中点击地面移动 / 点击敌人攻击 / 数字键 `2` 攻击最近敌人 / 空格手动结束回合

### 敌人 AI
- **五状态 状态机**：Idle（等待回合）→ ChooseAction（AP 预算决策）→ Moving（NavMesh 寻路，预判进入攻击范围停止）→ Attacking（触发攻击动画，Animation Event 判定伤害）→ EndTurn
- **智能移动**：目标点设在 `攻击范围 × 0.8` 位置，确保到达后能攻击；移动中每帧重检距离，提前终止移动进入攻击
- **面向攻击**：攻击前自动 `LookRotation` 面向目标，避免背对攻击的视觉 Bug

### 表现层
- **World Space 头顶血条**：监听 `HealthChanged` 事件，自动根据阵营着色（玩家绿/敌人红），billboard 始终面朝摄像机，支持血量数值显示
- **回合指示 UI**：`BattleUIPanel` 事件驱动更新"你的回合"/"敌人回合"与当前 AP 消耗状态
- **动态摄像机**：球面环绕 + 战斗时自动聚焦当前行动单位，支持遮挡检测
- **动画系统**：`BaseAniCtrl` 统一管理动画参数，Animation Event 中继攻击命中回调，取消 Animator Transition 的 `Has Exit Time` 确保响应即时

### 架构亮点
- **事件总线 `EventBus`**：基于枚举 ID 的静态泛型实现，编译期类型安全检查，Action 引用缓存保障事件正确注销，无重复订阅
- **全模块解耦**：脚本间通过事件通信，无硬引用。例如 `EnemyAI` 不直接调用 `HealthComponent`，而是广播 `Attacked` 事件 → `AttackSystem` 独立处理伤害
- **单例 + 管理器模式**：`BattleManager`、`UIManager`、`SelectEvent` 全局唯一，`DontDestroyOnLoad` 跨场景保留

## 系统架构

### 脚本清单（按职责分组）

#### 全局系统
| 脚本 | 类型 | 职责 |
|------|------|------|
| `EventBus` | 静态类 | 事件订阅/广播，支持泛型参数 |
| `E_EventType` | 枚举 | 13 种事件类型定义(其中2种预留) |
| `BattleManager` | 单例 MonoBehaviour | 战斗生命周期管理、AP 消耗、单位增援与脱战恢复 |
| `BattleQueue` | 普通类 | 双队列回合排序与推进 |
| `SelectEvent` | 单例 MonoBehaviour | 角色选中/取消选中事件 |
| `InputManager` | MonoBehaviour | 鼠标输入分发（左键/Alt+左键） |
| `ClickSelector` | MonoBehaviour | 自由探索阶段点击选择与移动，过滤非玩家角色 |
| `AttackSystem` | MonoBehaviour | 监听 Attacked 事件 → 调用 HealthComponent.TakeDamage |

#### 角色组件（挂载在每个战斗单位上）
| 脚本 | 职责 |
|------|------|
| `UnitIdentity` | 唯一 ID、速度、队伍归属 (isPlayer)，自动收集玩家单位列表 |
| `UnitAPManager` | AP 管理 + `APChanged` 事件广播 |
| `HealthComponent` | HP 管理 + `HealthChanged`/`UnitDied` 事件广播 |
| `NavMeshMoveCtrl` | NavMeshAgent 寻路 + 距离限制 + 自驱动到达/卡死检测 |
| `CharacterMoveControl` | 自由探索移动（入战后自动禁用） |
| `BaseAniCtrl` | 动画参数控制 + Animation Event 中继（`AttackHitTriggered`） |
| `EnemySensor` | NavMesh 精确路径距离判定（入战检测用） |
| `BattleProximityDetector` | SphereCollider 触发器 + 持续扫描动态增援 |

#### 战斗输入与 AI
| 脚本 | 职责 |
|------|------|
| `BattleInputHandler` | 战斗内鼠标点击移动/攻击 + 数字键快捷攻击 + 空格结束回合 |
| `PlayerAttackHandler` | 玩家攻击协调器，触发动画 → Animation Event → 广播 Attacked |
| `EnemyAI` | 敌人五状态 FSM（Idle→ChooseAction→Moving→Attacking→EndTurn） |

#### UI
| 脚本 | 职责 |
|------|------|
| `UIManager` | 单例，面板切换，BattleStart/End 自动联动 |
| `BasePanel` | 抽象基类，Show/Hide + OnShow/OnHide 生命周期（事件订阅/注销） |
| `BattleUIPanel` | 回合指示 + AP 实时显示（事件驱动） |
| `UnitHPBar` | World Space 头顶血条，跟随角色 + billboard 面朝摄像机 |
| `BattleResultPanel` | 胜负面板（占位） |
| `ExplorationUIPanel` | 探索面板（占位） |

#### 数据结构 (`Struct/`)
| 文件 | 字段 |
|------|------|
| `AttackData` | `attackerID`, `targetID`, `damage`, `target` |
| `UnitAPData` | `unitID`, `currentAP`, `maxAP` |

### 事件一览

| 事件 | 参数类型 | 触发者 | 消费者 |
|------|------|------|------|
| `BattleStart` | `List<(int,float,GameObject)>` | `BattleProximityDetector` | `UIManager`, `EnemyAI` |
| `BattleEnd` | 无 | `BattleManager` | `UIManager` |
| `RoundStart` | `List<int>` | `BattleQueue` | 日志/未来 UI |
| `TurnStart` | `int unitID` | `BattleQueue` | `EnemyAI`, `BattleUIPanel` |
| `TurnEnd` | `int unitID` | `EnemyAI`, `PlayerAttackHandler` | `BattleQueue` |
| `AllUnitsActed` | 无 | `BattleQueue` | 日志 |
| `APChanged` | `UnitAPData` | `UnitAPManager` | `BattleUIPanel` |
| `Attacked` | `AttackData` | `EnemyAI`, `PlayerAttackHandler` | `AttackSystem` |
| `HealthChanged` | `(int id, int hp, int max)` | `HealthComponent` | `UnitHPBar` |
| `UnitDied` | `int unitID` | `HealthComponent` | `BattleManager` |
| `BattleQueueUpdated` | `List<GameObject>` | `BattleQueue` | 日志/未来 UI |
| `AnimNotify` | `AnimNotifyData`(预留) | `BaseAniCtrl` | 日志/未来通用动画回调 |
| `UnitMoved` | `UnitMoveData`(预留) | `NavMeshMoveCtrl` | 日志/未来 UI |

> **设计原则**：任何新功能只需在 `E_EventType` 中新增枚举值 → 生产者广播 → 消费者订阅，无需修改现有模块。

### 核心数据流

```
探索阶段：
ClickSelector 点击角色 → SelectEvent → BattleProximityDetector 激活触发器
└─ SphereCollider 检测敌人进入 → NavMesh 路径距离判定
└─ BattleManager.StartBattle(participants)
├─ 禁用 CharacterMoveControl + 初始化 UnitAPManager
├─ BattleQueue.InitQueue → TurnStart
└─ UIManager 切换到 BattleUIPanel

战斗回合：
TurnStart → ResetAP → APChanged(3/3)
├─ [玩家] BattleInputHandler 点击 → 移动(1AP)/攻击(2AP) → AP 耗尽自动 EndTurn
│ └─ PlayerAttackHandler → Animator → Animation Event → Attacked → 伤害
└─ [敌人] EnemyAI 状态机 → Move/Attack → Animation Event → Attacked → 伤害

伤害链：
AttackSystem.OnAttack → HealthComponent.TakeDamage
├─ HealthChanged(id, hp, max) → UnitHPBar 实时更新
└─ hp ≤ 0 → UnitDied(id) → BattleManager 移除单位 → 胜负判定 → EndBattle
```

## 使用说明

### 环境要求
- Unity 2022 LTS 或更高版本
- 场景需烘焙 NavMesh Surface（`Window > AI > Navigation`）
- 无需额外插件

### 场景配置步骤

1. **全局对象**（建议挂在 `Managers` 空节点下）：
   - `BattleManager` + `UIManager` + `SelectEvent` + `EventBus` + `InputManager` + `ClickSelector`

2. **玩家角色 Prefab**：
   - `UnitIdentity`（`isPlayer=true`, 设置 `speed`）
   - `NavMeshMoveCtrl` + `CharacterMoveControl`
   - `HealthComponent` + `UnitAPManager`
   - `BattleProximityDetector`（`triggerRadius=20`, `engageDistance=10`）
   - `PlayerAttackHandler`
   - 子对象：`BaseAniCtrl` + `Animator`（攻击动画需配置 Animation Event `OnAttackHit`）
   - 子对象：`HPBarCanvas` → `UnitHPBar`（HPFill + HPBackground + 可选 HPText）

3. **敌人 Prefab**：
   - `UnitIdentity`（`isPlayer=false`）
   - `NavMeshMoveCtrl` + `HealthComponent` + `UnitAPManager`
   - `EnemyAI`（`attackRange`, `attackDamage`）
   - `EnemySensor`
   - 子对象：`BaseAniCtrl` + `Animator`（同上，Animation Event 配好）
   - 子对象：`HPBarCanvas` → `UnitHPBar`

4. **UI Canvas**：
   - `UIManager`：拖入三个 Panel 引用
   - Animator Controller 的 Attack Transition：取消 `Has Exit Time`

### 快捷键

| 按键 | 上下文 | 功能 |
|------|------|------|
| 鼠标左键 | 探索中 | 选择角色 / 点击地面移动 |
| Alt+左键 | 探索中 | 强制地面移动（穿透角色） |
| 鼠标左键 | 战斗中 | 点击地面移动(1AP) / 点击敌人攻击(2AP) |
| 数字键 `2` | 战斗中 | 攻击最近的敌人（消耗 2AP） |
| 空格 | 战斗中 | 手动结束当前回合 |
| `F1` | 全局 | 测试：立即启动一场战斗（`BattleTestStarter`） |
| 鼠标右键拖拽 | 全局 | 旋转摄像机 |
| 滚轮 | 全局 | 缩放摄像机 |

## 已知问题

- 暂时使用 Legacy Text 组件（中文字体兼容；后续迁移 TMP）
- 战斗结果面板 / 战败恢复尚未实现
- 远程攻击逻辑尚未实现

## 贡献指南

扩展功能时：
1. 新事件类型在 `E_EventType` 枚举中添加
2. 复杂数据使用 `Struct/` 下的结构体作为事件参数
3. UI 面板继承 `BasePanel`，在 `OnShow/OnHide` 中管理事件订阅/注销
4. `UIManager` 中拖入新 Panel 引用
