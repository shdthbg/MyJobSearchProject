# 项目 README 文档

## 项目概述

本项目是 Unity 环境下实现的一个回合制战斗系统，以游戏角色自由探索、靠近敌人自动进入战斗、战斗中消耗行动点（AP）进行移动/攻击为核心玩法。系统包含角色选择、入战检测、回合队列管理、AP 消耗、简单敌人 AI 以及动态增援机制。

## 核心功能

- **自由探索与角色选择**：玩家点击己方角色可切换主控角色，Alt+点击地面或左键点击地面可控制角色移动（非战斗状态）。
- **入战检测**：主控角色靠近敌人（距离 ≤ 阈值）时自动触发战斗，战斗期间触发器持续扫描，新增敌人可动态加入当前战斗。
- **回合制战斗队列**：基于速度排序的回合队列（`BattleQueue`），每个回合内所有单位按顺序行动，全部行动完毕后进入下一回合。
- **AP 系统**：每回合 3 点 AP，移动消耗 1 点，攻击消耗 2 点。AP 耗尽自动结束当前单位回合；AP 不足时可由玩家手动结束回合（空格键）。
- **动态增援**：战斗进行中，其他敌人进入触发器范围且满足距离条件时，自动插入准备队列，下回合开始行动。
- **相机跟随**：战斗时相机自动跟随当前行动单位（玩家或敌人），战斗结束后恢复跟随主控角色。
- **事件总线**：使用 `EventBus` 解耦模块间通信（如回合开始、单位行动、战斗结束等）。

## 系统架构

### 脚本一览

| 脚本 | 挂载对象 | 职责 |
| ------ | ---------- | ------ |
| `SelectEvent` | 全局单例 | 选中/取消选中角色的事件分发 |
| `ClickSelector` | 全局对象 | 自由探索阶段的鼠标点击选择与移动 |
| `BattleInputHandler` | 全局对象 | 战斗内输入处理（鼠标移动、快捷键攻击、空格结束回合） |
| `BattleManager` | 全局单例 | 战斗生命周期管理，AP 消耗，单位增援 |
| `BattleQueue` | 非 MonoBehaviour | 回合队列排序与推进 |
| `BattleProximityDetector` | 每个玩家角色 | 入战触发器与持续检测 |
| `EnemySensor` | 每个敌人 | NavMesh 路径距离判断 |
| `UnitAPManager` | 自动挂载至战斗单位 | AP 重置与消耗 |
| `UnitIdentity` | 每个单位 | 标识 ID、速度、是否是玩家 |
| `NavMeshMoveCtrl` | 玩家/敌人 | 基于 NavMesh 的限距移动 |
| `CamFollow` | 主相机 | 球面相机跟随与视线遮挡处理 |
| `EventBus` | 全局单例 | 事件系统 |
| `InputManager` | 全局对象 | 输入事件（左键、Alt+左键） |

### 数据流

```
ClickSelector (探索输入)
        ↓
SelectEvent.TriggerCharacterSelected → BattleProximityDetector.SetActive
        ↓
BattleProximityDetector.PerformDetection
    ├─ 战斗未激活 → BattleManager.StartBattle(participants)
    └─ 战斗已激活 → BattleManager.AddUnitToBattle(newEnemies)
        ↓
BattleManager.StartBattle → BattleQueue.InitQueue → 回合循环
        ↓
BattleManager.OnUnitTurnStartBridge → UnitAPManager.ResetAP
    ├─ 玩家 → BattleInputHandler 接收输入
    └─ 敌人 → EnemyAutoTurnEnd 协程（延迟后结束）
        ↓
BattleInputHandler 按1/2键 → BattleManager.TrySpendUnitAP → AP→0自动结束
BattleInputHandler 按空格 → EventBus 发布 TurnEnd
        ↓
BattleQueue.OnTurnEnd → 下一个单位或下一回合
```

## 使用说明

### 环境要求

- Unity 2020.3 或更高版本（建议 2022 LTS）
- 导入 `NavMesh Components`（用于烘焙 NavMesh） `Window > AI > Navigation`
- 导入 `Input System`（可选，当前使用旧版 Input Manager）
- 在场景中烘焙 NavMesh Surface，覆盖可行走区域

### 场景配置步骤

1. **创建地面**：添加 Plane，设置 Layer 为 `Ground`（或自定义）。
2. **创建玩家角色**：
   - 任意 3D 模型，添加 `NavMeshAgent` 组件。
   - 添加 `NavMeshMoveCtrl` 脚本，设置 `moveDistance = 10`，`moveSpeed` 等。
   - 添加 `UnitIdentity`，填写 `unitID`（唯一），勾选 `isPlayer`，设置 `speed`。
   - 添加 `CharacterMoveControl`（如果已有）。
   - 将角色放入 `characterLayer`（用于点击选择）。
3. **创建敌人**：
   - 模型 + `NavMeshAgent` + `NavMeshMoveCtrl`（可选，AI 尚仅结束回合）。
   - 添加 `UnitIdentity`，`isPlayer` 不勾选，填写 `unitID`。
   - 添加 `EnemySensor` 脚本。
   - 放入 `enemyLayer`。
4. **配置管理器**：
   - 场景中必须有 `SelectEvent` 单例（挂任意对象）。
   - `BattleManager` 单例（挂任意对象），并在 Inspector 中拖拽 `ClickSelector` 引用。
   - `BattleInputHandler`：挂任意对象，拖拽 `InputManager`、选择 `characterLayer`（可选）和 `walkableLayer`。
   - `CamFollow`：挂主相机，拖拽初始目标。
   - `EventBus`：挂任意对象。
   - `InputManager`：挂任意对象，配置左键/Alt+左键事件。
5. **烘焙 NavMesh**：打开 Navigation 窗口，选中地面，勾选 Navigation Static，Bake。

### 快捷键（测试用）

| 按键 | 功能 |
| ------ | ------ |
| 鼠标左键（战斗外） | 选择角色 / 点击地面移动 |
| Alt + 左键（战斗外） | 强制点击地面移动（穿透角色） |
| 鼠标左键（战斗内） | 移动当前玩家单位（消耗1AP） |
| 数字键 `2` | 攻击（消耗2AP，尚未实现伤害） |
| 空格 | 手动结束当前玩家回合 |
| 鼠标右键拖拽 | 旋转摄像机 |
| 滚轮 | 缩放摄像机 |

### 入战与战斗流程

1. 选中一个玩家角色（主控）。
2. 控制角色靠近敌人，当两者 NavMesh 路径距离 ≤ `engageDistance` 时，自动触发战斗。
3. 战斗内：鼠标点击可行走地面 → 角色移动并消耗 1 AP；按 2 消耗 2 AP（攻击，日志显示但不造成伤害）；AP 归零自动结束回合。空格可强制结束。
4. 敌人回合：等待 5 秒后自动结束（测试用，未来将改为 AI 决策）。
5. 所有单位行动完毕后进入下一回合，直到战斗结束（目前通过 `EndBattle` 或 `BattleQueue.BattleQueueClear` 触发）。

## 配置参数说明

### `BattleProximityDetector`

- `engageDistance`：战斗触发距离（路径长度），默认 20。
- `triggerRadius`：触发器半径，默认 20（应 ≥ engageDistance）。

### `NavMeshMoveCtrl`

- `moveDistance`：每次移动最大距离（AP 限制），默认 10。
- `moveSpeed`：移动速度。

### `UnitIdentity`

- `unitID`：唯一编号（不可重复）。
- `speed`：速度值（影响回合顺序，越高越先行动）。
- `isPlayer`：是否为玩家单位。

### `BattleManager`

- `clickSelector`：拖拽 `ClickSelector` 引用，用于战斗时禁用自由探索输入。

## 已知问题与后续计划

- 敌人 AI 仅自动结束回合，未实现移动/攻击决策。
- 攻击未绑定伤害逻辑，也未联动 `UnitDied` 事件。
- 战斗结束后未自动切换到胜利/失败状态，需手动调用 `EndBattle`。
- `UnitAPManager` 的 `currentAP` 在 Inspector 上可读，但当前未在 UI 显示。

## 贡献与修改

如需扩展功能（如技能系统、装备、多队伍），建议保持 `EventBus` 事件模式，在 `BattleManager` 中添加新的事件桥接，并遵循现有的 AP 消耗接口。
