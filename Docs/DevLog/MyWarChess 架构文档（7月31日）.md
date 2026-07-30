# MyWarChess 架构文档

## 脚本清单（按职责分组）

### 全局系统
- EventBus: 静态事件总线，订阅/广播解耦
- BattleManager: 单例，管理战斗生命周期
- BattleQueue: 回合队列（nowRoundQueue + readyQueue）
- UIManager: 单例，管理面板切换
- SelectEvent: 选中角色事件
- InputManager: 鼠标输入分发

### 角色组件（每个战斗单位上）
- UnitIdentity: ID + 速度 + 队伍归属
- UnitAPManager: AP 管理 + APChanged 事件
- HealthComponent: HP 管理 + HealthChanged/UnitDied 事件
- NavMeshMoveCtrl: NavMeshAgent 寻路 + 距离限制
- CharacterMoveControl: 自由探索移动（入战后禁用）
- EnemyAI: 敌人状态机（Idle→ChooseAction→Moving→Attacking→EndTurn）
- BaseAniCtrl: 动画参数控制 + Animation Event 中继
- PlayerAttackHandler: 玩家攻击协调（Animation Event 驱动）

### UI
- BasePanel: 面板基类（Show/Hide + 事件生命周期）
- BattleUIPanel: TurnIndicator + APDisplay
- BattleResultPanel: 胜负弹窗（待实现）
- UnitHPBar: World Space 血条（待实现）

### 事件一览
| 事件 | 参数 | 触发者 | 消费者 |
|------|------|------|------|
| BattleStart | List<(id,speed,obj)> | BattleProximityDetector | UIManager, EnemyAI |
| TurnStart | int unitID | BattleQueue | EnemyAI, BattleUIPanel |
| TurnEnd | int unitID | EnemyAI, BattleInputHandler | BattleQueue |
| APChanged | UnitAPData | UnitAPManager | BattleUIPanel |
| Attacked | AttackData | EnemyAI, PlayerAttackHandler | AttackSystem |
| HealthChanged | (id,hp,max) | HealthComponent | (HPBar之后) |
| UnitDied | int unitID | HealthComponent | BattleManager |
| BattleEnd | 无 | BattleManager | UIManager |