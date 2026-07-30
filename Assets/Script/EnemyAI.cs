using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


public enum E_EnemyState
{
    Idle,           // 等待回合
    ChooseAction,   // 决策下一步
    Moving,         // 移动中
    Attacking,      // 攻击执行
    EndTurn         // 结束回合
}
public class EnemyAI : MonoBehaviour
{
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private int attackDamage = 15;

    private int myUnitID;
    private E_EnemyState currentState = E_EnemyState.Idle;
    private bool isMyTurn;
    private float attackTimer = 0f;

    private UnitIdentity identity;
    private UnitAPManager apManager;
    private NavMeshMoveCtrl moveCtrl;
    private BaseAniCtrl animationCtrl;

    private Transform targetPlayer;
    
    private float attackClipLength;                  // 攻击动画总秒数
    private AttackData pendingAttackData;            // 缓存待触发的攻击数据

    void Awake()
    {
        identity = GetComponent<UnitIdentity>();
        myUnitID = identity.unitID;
        apManager = GetComponent<UnitAPManager>();
        moveCtrl = GetComponent<NavMeshMoveCtrl>();
        animationCtrl = GetComponentInChildren<BaseAniCtrl>();

        var animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            var aniCtrl = animator.GetComponent<BaseAniCtrl>();
            if (aniCtrl != null)
            {
                aniCtrl.AttackHitTriggered += HandleAttackHit;
            }
            else
            {
                Debug.LogWarning("[EnemyAI] 未找到 BaseAniCtrl 组件，攻击命中事件无法触发");
            }
        }
    }

    void OnEnable()
    {
        EventBus.AddEventListener<int>(E_EventType.TurnStart,OnTurnStart);
    }
    void OnDisable()
    {
        EventBus.RemoveEventListener<int>(E_EventType.TurnStart,OnTurnStart);
    }
    void OnDestroy()
    {
        if(animationCtrl != null)
        {
            animationCtrl.AttackHitTriggered -= HandleAttackHit;
        }
    }
    void Update()
    {
       if(!isMyTurn) return;
        switch (currentState)
        {
            case E_EnemyState.ChooseAction:
                ChooseAction();
                break;
            case E_EnemyState.Moving:
                CheckMoving();
                break;
            case E_EnemyState.Attacking:
                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0f)
                {
                    animationCtrl.Idle1ToAttack = false;
                    animationCtrl.WalkToAttack = false;
                    currentState = E_EnemyState.EndTurn;
                }
                break;
            case E_EnemyState.EndTurn:
                EndMyTurn();
                break;
        }
    }

    private void ChooseAction()
    {

        targetPlayer = FindClosestPlayer();
        if(targetPlayer == null)
        {
            currentState = E_EnemyState.EndTurn;
            return;
        }
        float distance = Vector3.Distance(transform.position,targetPlayer.position);

        if(distance<=attackRange && apManager.currentAP>=2)
        {
            DoAttack();
        }
        else if(distance >attackRange && apManager.currentAP>=1)
        {
            DoMove();
        }
        else
        {
            currentState = E_EnemyState.EndTurn;
        }
    }

    private void OnTurnStart(int unitID)
    {
        if(unitID == identity.unitID)
        {
            isMyTurn = true;
            currentState = E_EnemyState.ChooseAction;
        }
    }
    private void DoMove()
    {
        BattleManager.Instance.TrySpendCurrentUnitAP(myUnitID,1);

        Vector3 directionToPlayer = (targetPlayer.position - transform.position).normalized;
        float stopDistance = attackRange * 0.8f;  // 走到攻击范围的80%处，留20%余量
        
        moveCtrl.moveEndPos = targetPlayer.position - directionToPlayer * stopDistance;

        moveCtrl.Move();

        currentState = E_EnemyState.Moving;
        animationCtrl.Idle1ToWalk = true;
    }

    /// <summary>
    /// 由 animationCtrl.AttackHitTriggered 事件回调，
    /// 事件源头是 attack.anim 的 Animation Event → BaseAniCtrl.OnAttackHit()
    /// </summary>
    private void HandleAttackHit()
    {
        EventBus.EventTrigger(E_EventType.Attacked, pendingAttackData);
        Debug.Log($"[EnemyAI] 击打点触发！目标={pendingAttackData.targetID}，伤害={pendingAttackData.damage}");
    }

    private void DoAttack()
    {
        Debug.Log($"[EnemyAI] DoAttack 进入 | Idle1ToAttack当前值={animationCtrl.Idle1ToAttack} | 目标={targetPlayer?.name} | AP={apManager.currentAP}");
        // 1. 读取动画长度（在触发动画之前）
        attackClipLength = GetAttackClipLength();
        attackTimer = attackClipLength;
    
        // 2. 扣 AP
        BattleManager.Instance.TrySpendCurrentUnitAP(myUnitID, 2);
    
        // 3. 缓存攻击数据（等击打帧回调时再用）
        pendingAttackData = new AttackData
        {
            attackerID = myUnitID,
            targetID = targetPlayer.GetComponent<UnitIdentity>().unitID,
            damage = attackDamage
        };
    
        currentState = E_EnemyState.Attacking;
    
        if (moveCtrl.isMoving)
        {
            moveCtrl.StopMove();
            animationCtrl.Idle1ToWalk = false;
        }
    
        // 4. 触发攻击动画 → 动画播到击打帧 → Animation Event → HandleAttackHit → 伤害判定
        // 诊断：确认我们操作的 BaseAniCtrl 和 Animator 的关系
        var aniComp = animationCtrl?.GetComponent<Animator>();
        var aniInChildren = GetComponentInChildren<Animator>();
        Debug.Log($"[EnemyAI 诊断] animationCtrl 在={animationCtrl?.gameObject.name} | 它上面的Animator={aniComp?.name ?? "NULL"} | 子物体中的Animator={aniInChildren?.name ?? "NULL"} | 两者是同一个?={aniComp == aniInChildren}");
        
        animationCtrl.Idle1ToAttack = true;
    }

    private void CheckMoving()
    {
        // 移动过程中实时检测是否已经进入攻击范围且AP足够，如果是则立即打断移动并攻击
        if (moveCtrl.isMoving)
        {
            float distance = Vector3.Distance(transform.position, targetPlayer.position);
            if (distance <= attackRange && apManager.currentAP >= 2)
            {
                moveCtrl.StopMove();                // 需要 NavMeshMoveCtrl 提供 Stop() 方法
                animationCtrl.Idle1ToWalk = false;
                currentState = E_EnemyState.ChooseAction; // 回到决策状态，下一帧会执行攻击
            }
        }
        else   // 移动结束（到达目标或被打断）
        {
            animationCtrl.Idle1ToWalk = false;
            currentState = apManager.currentAP > 0 ? E_EnemyState.ChooseAction : E_EnemyState.EndTurn;
        }
    }

    private void EndMyTurn()
    {
        Debug.Log($"[EnemyAI] EndTurn | Idle1ToAttack={animationCtrl.Idle1ToAttack} | Idle1ToWalk={animationCtrl.Idle1ToWalk}");
        isMyTurn = false;
        animationCtrl.Idle1ToWalk = false;
        EventBus.EventTrigger(E_EventType.TurnEnd,myUnitID);
    }

    private Transform FindClosestPlayer()
    {
        Transform closest = null;
        float minDist = Mathf.Infinity;
        foreach (var obj in UnitIdentity.playerUnits)
        {
            if (obj != null && obj.activeSelf) // 确保玩家存活
            {
                float dist = Vector3.Distance(transform.position, obj.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = obj.transform;
                }
            }
        }
        return closest;
    }

    private float GetAttackClipLength()
    {
        if (animationCtrl == null)
        {
            Debug.LogWarning("[EnemyAI] animationCtrl 为空，使用默认攻击时长 1s");
            return 1f;
        }
    
        var animator = animationCtrl.GetComponent<Animator>();
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning("[EnemyAI] Animator 不可用，使用默认攻击时长 1s");
            return 1f;
        }
    
        foreach (var clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name.IndexOf("attack", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Debug.Log($"[EnemyAI] 攻击动画 [{clip.name}] 长度={clip.length:F2}s");
                return clip.length;
            }
        }
    
        Debug.LogWarning("[EnemyAI] 未找到名称包含 'attack' 的剪辑，使用默认时长 1s");
        return 1f;
    }
}
