# 技术笔记：敌人 AI 动画混乱 Bug 排查全记录

> **日期**：2026-07-30  
> **分类**：Bug 排查 | Animator | 动画状态机 | Animation Event  
> **标签**：`EnemyAI` `BaseAniCtrl` `Has Exit Time` `Unity`

---

## 一、问题表现

入战后敌人出现以下症状：

1. **在玩家周围原地站立，满 AP 但跳过回合**（约每 3~4 轮循环一次）
2. `DoAttack` 日志正常输出，AP 被正确扣除为 1
3. 没有 `[EnemyAI] 击打点触发！` 日志 → 伤害从未产生
4. 攻击动画**间歇性**播放（有时成功有时失败）
5. **关键线索**：玩家攻击在同一 Animator Controller 上表现正常

---

## 二、排查过程

### 第一阶段：代码逻辑检查 ❌

```
检查项：
├── EnemyAI.DoAttack → 日志正常，Idle1ToAttack=true 被调用 ✅
├── BaseAniCtrl.OnAttackHit → Animation Event 触发节点存在 ✅
├── EnemyAI.Awake 事件订阅 → AnimationCtrl.AttackHitTriggered += HandleAttackHit ✅
├── Animator 层级 → GetComponentInChildren<Animator> 返回正确对象 ✅
└── 硬编码 attackTimer → 已改为 GetAttackClipLength() 自动读取 ✅
```

**结论：C# 代码层面完全正确。**

### 第二阶段：动画配置检查 ❌

```
检查项：
├── Animator Controller Parameters → idle1ToAttack (Bool) 存在 ✅
├── Idle → Attack Transition → Conditions: idle1ToAttack==true ✅
├── attack.anim → Animation Event: Function="OnAttackHit" ✅
└── BaseAniCtrl 和 Animator 在同一 GameObject ✅
```

**结论：配置层面也正确。**

### 第三阶段：关键突破 🔍

在 `BaseAniCtrl.Idle1ToAttack` setter 中加入延迟诊断协程后，追踪到关键时间线：

```
时间轴追踪（attackClipLength = 1.27s）：

t = 0.00s   DoAttack() → Idle1ToAttack = true
            Animator.SetBool("idle1ToAttack", true) 调用成功

t = 0.20s   协程检查：Animator.GetCurrentAnimatorStateInfo(0)
            → IsName("Attack") = FALSE  ← 还在 Idle！

t = 0.50s   再次检查：IsName("Attack") = FALSE  ← 仍在 Idle！！

t = 1.27s   代码倒计时到 0 → Idle1ToAttack = false → EndTurn

t = 1.50s   Animator 终于切换到 Attack 状态
            → 但 Idle1ToAttack 已经是 false 了！！
            → Animation Event 触发但 HandleAttackHit 中的 pendingAttackData
               已被 default 值覆盖 → 伤害未发送

t = 1.75s   攻击动画播完 → 过渡回 Idle → 回合早已结束
```

**Animator 的状态切换滞后了约 1.5 秒**，而代码窗口只有 1.27 秒——窗口完美错开。

---

## 三、根因

Animator Controller 中 **Idle → Attack 的 Transition 勾选了 "Has Exit Time"**。

```
┌──────────────────────────────────────┐
│ ☑ Has Exit Time         ← 罪魁祸首   │
│   Exit Time: 0.75                    │
│   Transition Duration: 0.25          │
│                                      │
│ Conditions:                          │
│   idle1ToAttack == true              │
└──────────────────────────────────────┘
```

### 延迟计算

```
Exit Time = 0.75  → Idle 动画必须播完 75% 才允许退出当前状态
Transition Duration = 0.25  → 两个动画的混合过渡需要 0.25 秒

总延迟 ≈ 0.75 + 0.25 + Animator 更新帧率开销 ≈ 1.2 ~ 1.5 秒

而 attack.anim 实际长度 = 1.27 秒（由 GetAttackClipLength 正确读取）
attackTimer 倒计时 = 1.27 秒

结论：倒计时先走完 → Idle1ToAttack 被设回 false → Transition 条件失效
      → Attack 状态被跳过 → Animation Event 不触发 → 无伤害
```

### 间歇性"成功"的解释

当敌人恰好处于纯 Idle 状态，且 Idle 动画已经播到接近末尾时，Exit Time 条件几乎立即满足，Transition 有机会在 `attackTimer` 倒计时结束前完成。这就形成了：

```
大多数情况：Idle 动画从 0% 开始 → Exit Time 要等 0.75s → 超时 → 失败
少数情况：Idle 动画已播到 80% → Exit Time 立即满足 → 成功
```

### 为什么玩家攻击正常？

玩家使用**同一个 Animator Controller**，理论上也有同样的延迟。但有两个关键区别：

1. **玩家的 `EndAttackRoutine` 协程**中 `WaitForSeconds(clipLength)` 后才设 `Idle1ToAttack=false`——如果 clipLength 大于 ExitTime 延迟，Transition 有机会完成
2. **玩家敌人之间**：敌人由 `EnemyAI.Attacking` 状态倒计时结束触发 `EndTurn`，而玩家的 Animation Event 和协程是两条独立的轨道——Animation Event 触发伤害不受倒计时影响

玩家的"正常"是一种侥幸——在某些极端时序下也会出现同样问题，只是当前测试场景中恰巧没触发。

---

## 四、修复

在 Unity Editor 的 Animator Controller 中：

1. 选中 Idle → Attack 的 Transition 箭头
2. Inspector 面板 → **取消勾选 `Has Exit Time`**
3. `Transition Duration` 设为 `0`

```
修复后：
┌──────────────────────────────────────┐
│ ☐ Has Exit Time                      │
│   Transition Duration: 0             │
│                                      │
│ Conditions:                          │
│   idle1ToAttack == true              │
└──────────────────────────────────────┘
```

**零代码改动，只改 Animator Controller 的一个复选框。**

---

## 五、教训总结

| # | 教训 | 说明 |
| --- | ------ | ------ |
| 1 | **Bug 不只在代码里** | C# 逻辑、事件流、Animator 参数全部正确，但 Unity 的动画过渡机制独立于代码运行 |
| 2 | **Has Exit Time 是容易被忽视的坑** | 创建 Transition 时默认勾选；任何"立即响应"的动画切换都应取消 |
| 3 | **间歇性 ≠ 随机** | "有时成功有时失败"是状态残留和时序窗口问题，不是真正的随机数 |
| 4 | **Animation Event 和代码时钟是两条轨道** | 永远不要假设 `animator.SetBool` 后动画"立刻"切换到目标状态 |
| 5 | **玩家正常 ≠ 系统正常** | 同样的 Controller 在不同代码路径下表现不同，不能因为一端正常就排除配置问题 |
| 6 | **诊断先行，改代码在后** | 协程延迟诊断比改十处代码更快定位问题 |

---

## 六、连带架构收益

此 Bug 的排查过程推动了以下架构改进，均已在本日落地：

1. `BaseAniCtrl.AttackHitTriggered` 事件 → 解耦动画层和逻辑层
2. `PlayerAttackHandler` 新建组件 → 玩家/敌人统一走 Animation Event 伤害
3. `GetAttackClipLength()` → 消除动画长度硬编码
4. `EnemyAI.CheckMoving` 职责收窄 → 状态机每帧只做一件事
5. `NavMeshMoveCtrl.Update()` → 组件自管理生命周期，不依赖外部调用

---

## 七、关键日志示例

```
// 成功路径（修复后）：
[EnemyAI] DoAttack 进入 | Idle1ToAttack当前值=False | 目标=PlayerUnit | AP=2
[EnemyAI] 攻击动画 [attack] 长度=1.27s
[BaseAniCtrl] 击打帧触发 at 50.04s
[EnemyAI] 击打点触发！目标=1，伤害=15
[EnemyAI] EndTurn | Idle1ToAttack=False | Idle1ToWalk=False

// 失败路径（修复前）：
[EnemyAI] DoAttack 进入 | Idle1ToAttack当前值=False | 目标=PlayerUnit | AP=2
(无任何后续日志 → 动画被跳过 → 倒计时静默超时)
[EnemyAI] EndTurn | Idle1ToAttack=False | Idle1ToWalk=False
```
