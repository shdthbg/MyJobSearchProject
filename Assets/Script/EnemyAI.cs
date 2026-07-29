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

    void Awake()
    {
        identity = GetComponent<UnitIdentity>();
        myUnitID = identity.unitID;
        apManager = GetComponent<UnitAPManager>();
        moveCtrl = GetComponent<NavMeshMoveCtrl>();
    }
    void Start()
    {
        animationCtrl = GetComponent<NavMeshMoveCtrl>().CAniCtrl;
    }

    void OnEnable()
    {
        EventBus.AddEventListener<int>(E_EventType.TurnStart,OnTurnStart);
    }
    void OnDisable()
    {
        EventBus.RemoveEventListener<int>(E_EventType.TurnStart,OnTurnStart);
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
                    animationCtrl.Idle1ToAttack = false;   // 结束攻击动画
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

        moveCtrl.moveEndPos = targetPlayer.position;
        moveCtrl.Move();

        currentState = E_EnemyState.Moving;
        animationCtrl.Idle1ToWalk = true;
    }

    private void DoAttack()
    {
        attackTimer = 0.8f; // 根据实际攻击动画长度调整
        BattleManager.Instance.TrySpendCurrentUnitAP(myUnitID,2);

        EventBus.EventTrigger(E_EventType.Attacked,new AttackData
        {
            attackerID = myUnitID,
            targetID = targetPlayer.GetComponent<UnitIdentity>().unitID,
            damage = attackDamage
        });

        currentState = E_EnemyState.Attacking;
        animationCtrl.Idle1ToWalk = false;//后续攻击动画也接入这里
        if (moveCtrl.isMoving) moveCtrl.StopMove();
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
                DoAttack();
                return;
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
}
