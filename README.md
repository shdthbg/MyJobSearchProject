# MyWarChess — 战棋回合制战斗系统

## 项目概述

本项目是基于 Unity 的回合制战术 RPG 战斗系统原型。玩家自由探索场景，靠近敌人自动触发回合制战斗。战斗中通过 AP（行动点）系统控制移动和攻击，敌人具备完整的 AI 状态机（寻路→攻击→结束回合）。系统采用事件总线架构，模块间高度解耦。

## 核心功能

### 已实现
- **自由探索与角色选择**：点击己方角色切换主控，点击地面控制移动（NavMesh 寻路 + 距离限制）
- **入战检测与动态增援**：主控角色靠近敌人自动触发战斗，战斗中新敌人进入范围可动态加入队列
- **回合制战斗队列**：基于速度降序排列的双队列系统（当前队列 + 准备队列），回合自动流转
- **AP 系统**：每回合 AP 可配置，移动/攻击消耗不同点数，AP 预算影响 AI 决策
- **Animation Event 驱动攻击**：攻击动画通过 Animation Event 精确控制击打帧 → 伤害判定，近战远程统一
- **敌人 AI 状态机**：Idle → ChooseAction → Moving/Attacking → EndTurn，含移动中实时检测进入攻击范围
- **动态相机系统**：球面环绕相机，战斗时自动聚焦当前行动单位，遮挡检测
- **事件总线架构**：`EventBus` 解耦所有模块通信，TurnStart/Attacked/HealthChanged 等 10+ 事件类型
- **UI 骨架**：面板管理器 (`UIManager`) + 基类 (`BasePanel`)，回合指示 + AP 实时显示

### 待实现
- World Space 角色头顶血条 (`UnitHPBar`)
- 跳过回合按钮
- 队伍血条列表 (`UnitHPList`)
- 伤害飘字 (`DamagePopupMgr`)
- 胜利/战败面板与战败恢复
- 攻击范围光标（红绿显示）
- 远程攻击（射线检测）

## 系统架构

### 脚本清单（按职责分组）

#### 全局系统
| 脚本 | 类型 | 职责 |
|------|------|------|
| `EventBus` | 静态类 | 事件订阅/广播，支持泛型参数 |
| `E_EventType` | 枚举 | 10+ 事件类型定义 |
| `BattleManager` | 单例 MonoBehaviour | 战斗生命周期管理、AP 消耗、单位增援 |
| `BattleQueue` | 普通类 | 双队列回合排序与推进 |
| `SelectEvent` | 单例 MonoBehaviour | 角色选中/取消选中事件 |
| `InputManager` | MonoBehaviour | 鼠标输入分发（左键/Alt+左键） |
| `ClickSelector` | MonoBehaviour | 自由探索阶段点击选择与移动 |
| `AttackSystem` | MonoBehaviour | 监听 Attacked 事件 → 调用 HealthComponent.TakeDamage |

#### 角色组件（挂载在每个战斗单位上）
| 脚本 | 职责 |
|------|------|
| `UnitIdentity` | 唯一 ID、速度、队伍归属 (isPlayer) |
| `UnitAPManager` | AP 管理 + `APChanged` 事件广播 |
| `HealthComponent` | HP 管理 + `HealthChanged`/`UnitDied` 事件 |
| `NavMeshMoveCtrl` | NavMeshAgent 寻路 + 距离限制 + 自驱动到达检测 |
| `CharacterMoveControl` | 自由探索移动（入战后禁用） |
| `BaseAniCtrl` | 动画参数控制 + Animation Event 中继 |
| `EnemySensor` | NavMesh 路径距离判定（入战检测用） |
| `BattleProximityDetector` | 入战触发器 + 持续扫描动态增援 |

#### 战斗输入
| 脚本 | 职责 |
|------|------|
| `BattleInputHandler` | 战斗内鼠标点击移动/攻击 + 快捷键 |
| `PlayerAttackHandler` | 玩家攻击协调器，Animation Event → 伤害判定 |
| `EnemyAI` | 敌人状态机（5 状态：Idle→ChooseAction→Moving→Attacking→EndTurn） |

#### UI
| 脚本 | 职责 |
|------|------|
| `UIManager` | 单例，面板切换，BattleStart/End 自动联动 |
| `BasePanel` | 抽象基类，Show/Hide + OnShow/OnHide 生命周期 |
| `BattleUIPanel` | 回合指示 + AP 实时显示（事件驱动） |
| `BattleResultPanel` | 胜负面板（空壳） |
| `ExplorationUIPanel` | 探索面板（空壳） |

#### 数据结构 (`Struct/`)
| 文件 | 字段 |
|------|------|
| `AttackData` | `attackerID`, `targetID`, `damage` |
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
| `HealthChanged` | `(int id, int hp, int max)` | `HealthComponent` | (待实现: `UnitHPBar`) |
| `UnitDied` | `int unitID` | `HealthComponent` | `BattleManager` |

### 数据流（核心路径）

```
BattleProximityDetector.PerformDetection
  └─ BattleManager.StartBattle(participants)
       ├─ 为每个单位挂 UnitAPManager + 禁用 CharacterMoveControl
       ├─ BattleQueue.InitQueue → TurnStart(unitID)
       │    ├─ EnemyAI.OnTurnStart → 状态机激活
       │    └─ BattleUIPanel.OnTurnStart → 更新 HUD
       └─ UIManager.ShowPanel(battlePanel)

回合内（敌人）：
  EnemyAI.ChooseAction → DoMove/DoAttack
    ├─ TrySpendAP → APChanged → BattleUIPanel 更新
    ├─ Move() → NavMeshAgent 寻路 → CheckMoving 检测到达
    └─ DoAttack → Idle1ToAttack = true → Animation Event
         └─ BaseAniCtrl.AttackHitTriggered → EnemyAI.HandleAttackHit
              └─ EventTrigger(Attacked) → AttackSystem → HealthComponent.TakeDamage

回合内（玩家）：
  BattleInputHandler.HandleLeftClick
    ├─ 点击敌人 → PlayerAttackHandler.DoAttack → Animation Event → 伤害
    ├─ 点击地面 → TrySpendAP(1) → moveCtrl.Move()
    └─ 空格 → EventTrigger(TurnEnd)

死亡链：
  HealthComponent.TakeDamage → currentHP ≤ 0 → EventTrigger(UnitDied)
    → BattleManager.OnExternalUnitDied → BattleQueue.RemoveUnit → EndBattle(一方全灭)
```

## 使用说明

### 环境要求
- Unity 2022 LTS 或更高版本
- 场景需烘焙 NavMesh Surface（`Window > AI > Navigation`）
- 无需额外插件

### 场景配置步骤

1. **全局对象**（任意 GameObject，建议放一个名为 `Managers` 的根节点下）：
   - `BattleManager` + `UIManager` + `SelectEvent` + `EventBus`（空脚本即可） + `InputManager` + `ClickSelector`
   - `BattleManager.Inspector`：拖 `ClickSelector` 引用

2. **玩家角色 Prefab**：
   - `UnitIdentity`（`isPlayer=true`, 唯一 `unitID`, 设置 `speed`）
   - `NavMeshMoveCtrl`（配置 `moveDistance`, `moveSpeed`）
   - `CharacterMoveControl`
   - `HealthComponent`（配置 `maxHP`）
   - `BattleProximityDetector`（配置 `triggerRadius`/`engageDistance`）
   - `PlayerAttackHandler`
   - 子物体：`BaseAniCtrl` + `Animator`（带 `attack.anim` + Animation Event `OnAttackHit`）

3. **敌人 Prefab**：
   - `UnitIdentity`（`isPlayer=false`）
   - `NavMeshMoveCtrl`
   - `HealthComponent`
   - `EnemyAI`（配置 `attackRange`, `attackDamage`）
   - `EnemySensor`
   - 子物体：`BaseAniCtrl` + `Animator`（同上，Animation Event 配好）

4. **UI（UIRoot Canvas）**：
   - `UIManager`：拖三个 Panel 引用
   - `BattlePanel` → `BattleUIPanel`：拖 `TurnIndicator` 和 `APDisplay` 的 `Text` 组件
   - Animator Controller 的 Attack Transition：取消 `Has Exit Time`

### 快捷键

| 按键 | 上下文 | 功能 |
|------|------|------|
| 鼠标左键 | 探索中 | 选择角色 / 点击地面移动 |
| Alt+左键 | 探索中 | 强制地面移动（穿透角色） |
| 鼠标左键 | 战斗中 | 点击地面移动(1AP) / 点击敌人攻击(2AP) |
| 数字键 `2` | 战斗中 | 攻击最近的敌人（消耗 2AP） |
| 空格 | 战斗中 | 手动结束当前回合 |
| 鼠标右键拖拽 | 全局 | 旋转摄像机 |
| 滚轮 | 全局 | 缩放摄像机 |
| `F1` | 全局 | 测试：启动战斗（`BattleTestStarter`） |

## 配置参数速查

| 脚本 | 参数 | 默认值 | 说明 |
|------|------|:---:|------|
| `UnitAPManager` | `maxAP` | 3 | 每回合行动点数 |
| `EnemyAI` | `attackRange` | 2 | 近战攻击范围(米) |
| `EnemyAI` | `attackDamage` | 15 | 攻击伤害 |
| `NavMeshMoveCtrl` | `moveDistance` | 10 | 单次移动最大距离 |
| `BattleProximityDetector` | `triggerRadius` | 20 | 触发器半径 |
| `BattleProximityDetector` | `engageDistance` | 20 | 入战路径距离阈值 |

## 已知问题

- 暂时使用 Legacy Text 组件（中文字体；后续迁移 TMP）
- 暂无 World Space 血条，伤害只能在 Console 确认
- 暂无战斗结果面板，一方全灭后仅 Console 日志
- 远程攻击逻辑尚未实现

## 贡献指南

扩展功能时：
1. 新事件类型在 `E_EventType` 枚举中添加
2. 复杂数据使用 `Struct/` 下的结构体作为事件参数
3. UI 面板继承 `BasePanel`，在 `OnShow/OnHide` 中管理事件订阅/注销
4. `UIManager` 中拖入新 Panel 引用
