using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleInputHandler : MonoBehaviour
{
    [SerializeField] private InputManager inputManager;
    [SerializeField] private LayerMask characterLayer;
    [SerializeField] private LayerMask walkableLayer;
    [SerializeField] private CamFollow cameraFollow;
    [SerializeField] private float playerAttackRange = 2f;   // 玩家攻击范围

    void Awake()
    {
        if (cameraFollow == null)
            cameraFollow = FindObjectOfType<CamFollow>();
    }

    void OnEnable()
    {
        if (inputManager != null)
        {
            inputManager.OnLeftClick += HandleLeftClick;
            inputManager.OnAltClick += HandleAltClick;
        }
    }

    void OnDisable()
    {
        if (inputManager != null)
        {
            inputManager.OnLeftClick -= HandleLeftClick;
            inputManager.OnAltClick -= HandleAltClick;
        }
    }

    void Update()
    {
        if (!BattleManager.Instance.IsBattleActive) return;

        int currentID = BattleManager.Instance.GetBattleQueue?.GetNowUnit() ?? -1;
        if (currentID == -1) return;

        if (!BattleManager.Instance.IsCurrentUnitPlayer())
        {
            Debug.Log("当前不是玩家单位");
            return;
        }

        GameObject currentUnit = BattleManager.Instance.GetUnitObject(currentID);
        if (currentUnit == null) return;

        // 攻击快捷键（数字键2）——攻击最近敌人
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            GameObject target = FindNearestEnemy(currentUnit);
            if (target == null)
            {
                Debug.Log("范围内没有敌人");
                return;
            }

            float distance = Vector3.Distance(currentUnit.transform.position, target.transform.position);
            if (distance > playerAttackRange)
            {
                Debug.Log($"距离太远，无法攻击（{distance:F2} > {playerAttackRange}）");
                return;
            }

            if (BattleManager.Instance.TrySpendCurrentUnitAP(currentID, 2))
            {
                PerformAttack(currentUnit, target, currentID);
            }
            else
            {
                Debug.Log("AP不足，无法攻击");
            }
        }
        // 手动结束回合（空格）
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            EventBus.EventTrigger(E_EventType.TurnEnd, currentID);
        }
    }

    void HandleLeftClick(RaycastHit hit)
    {
        if (!BattleManager.Instance.IsBattleActive) return;

        int currentID = BattleManager.Instance.GetBattleQueue?.GetNowUnit() ?? -1;
        if (currentID == -1) return;
        if (!BattleManager.Instance.IsCurrentUnitPlayer()) return;

        GameObject currentUnit = BattleManager.Instance.GetUnitObject(currentID);
        if (currentUnit == null) return;

        NavMeshMoveCtrl moveCtrl = currentUnit.GetComponent<NavMeshMoveCtrl>();
        if (moveCtrl == null) return;
        if (moveCtrl.isMoving) moveCtrl.StopMove();   // 移动中忽略点击

        // 点击到敌人 → 攻击
        if (IsInLayer(hit.collider.gameObject, characterLayer))
        {
            UnitIdentity targetId = hit.collider.GetComponent<UnitIdentity>();
            if (targetId != null && !targetId.isPlayer)
            {
                float distance = Vector3.Distance(currentUnit.transform.position, hit.collider.transform.position);
                if (distance > playerAttackRange)
                {
                    Debug.Log($"距离太远，无法攻击（{distance:F2} > {playerAttackRange}）");
                    return;
                }

                if (BattleManager.Instance.TrySpendCurrentUnitAP(currentID, 2))
                {
                    var attackHandler = currentUnit.GetComponent<PlayerAttackHandler>();
                    if (attackHandler != null)
                    {
                        attackHandler.DoAttack(new AttackData
                        {
                            attackerID = currentID,
                            targetID = targetId.unitID,
                            damage = 15   // 暂时固定伤害，未来从武器获取
                        });
                    }
                }
                else
                {
                    Debug.Log("AP不足，无法攻击");
                }
                return;
            }
        }

        // 点击到可行走地面 → 移动
        if (IsInLayer(hit.collider.gameObject, walkableLayer))
        {
            if(!BattleManager.Instance.TrySpendCurrentUnitAP(currentID, 1))
            {
                Debug.Log("AP不足，无法移动");
                return;
            }
            
            //Debug.Log($"[BattleInput] 收到移动点击，目标={hit.point}");
            //Debug.Log($"[BattleInput] TrySpendAP 结果={BattleManager.Instance.TrySpendCurrentUnitAP(currentID, 1)}");
            //Debug.Log($"[BattleInput] moveCtrl.isMoving={moveCtrl.isMoving}, navAgent.isStopped={moveCtrl.navAgent?.isStopped}");
            
            moveCtrl.moveEndPos = hit.point;
            moveCtrl.Move();
            
            //Debug.Log($"[BattleInput] Move()调用后: isMoving={moveCtrl.isMoving}, isStopped={moveCtrl.navAgent?.isStopped}");
        }
    }

    void HandleAltClick(RaycastHit _)
    {
        if (!BattleManager.Instance.IsBattleActive) return;

        int currentID = BattleManager.Instance.GetBattleQueue?.GetNowUnit() ?? -1;
        if (currentID == -1) return;
        if (!BattleManager.Instance.IsCurrentUnitPlayer()) return;

        GameObject currentUnit = BattleManager.Instance.GetUnitObject(currentID);
        if (currentUnit == null) return;

        NavMeshMoveCtrl moveCtrl = currentUnit.GetComponent<NavMeshMoveCtrl>();
        if (moveCtrl == null || moveCtrl.isMoving) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, walkableLayer))
        {
            if(!BattleManager.Instance.TrySpendCurrentUnitAP(currentID, 1))
            {
                Debug.Log("AP不足，无法移动");
                return;
            }
            moveCtrl.moveEndPos = hit.point;
            moveCtrl.Move();
        }
    }

    // ---------- 攻击执行与工具方法 ----------

    /// <summary>
    /// 执行攻击动作：发事件、播动画、延迟结束回合
    /// </summary>
    private void PerformAttack(GameObject attacker, GameObject target, int attackerID)
    {
        int targetID = target.GetComponent<UnitIdentity>().unitID;
        EventBus.EventTrigger(E_EventType.Attacked, new AttackData
        {
            attackerID = attackerID,
            targetID = targetID,
            damage = 15   // 暂时固定伤害，未来从武器获取
        });

        // 播放攻击动画（待机→攻击）
        var aniCtrl = attacker.GetComponent<NavMeshMoveCtrl>().CAniCtrl;
        if (aniCtrl != null)
        {
            aniCtrl.Idle1ToWalk = false;
            aniCtrl.Idle1ToAttack = true;
        }

        StartCoroutine(EndTurnAfterDelay(attackerID, 1f));
    }

    /// <summary>
    /// 延迟结束当前单位的回合，并清理攻击动画
    /// </summary>
    private IEnumerator EndTurnAfterDelay(int unitID, float delay)
    {
        yield return new WaitForSeconds(delay);

        // 战斗已结束则不再触发回合结束
        if (!BattleManager.Instance.IsBattleActive)
            yield break;

        GameObject unit = BattleManager.Instance.GetUnitObject(unitID);
        if (unit != null)
        {
            var aniCtrl = unit.GetComponent<NavMeshMoveCtrl>().CAniCtrl;
            if (aniCtrl != null)
            {
                aniCtrl.Idle1ToAttack = false;
                aniCtrl.WalkToAttack = false;
            }
        }

        EventBus.EventTrigger(E_EventType.TurnEnd, unitID);
    }

    /// <summary>
    /// 查找离玩家最近的存活敌人
    /// 注意：BattleManager 需提供 public Dictionary<int, GameObject> GetUnitObjMap() 方法
    /// </summary>
    private GameObject FindNearestEnemy(GameObject player)
    {
        float minDist = Mathf.Infinity;
        GameObject nearest = null;

        // 遍历战斗中的所有单位，筛选敌人
        // 确保 BattleManager 中已添加：public Dictionary<int, GameObject> GetUnitObjMap() => unitObjMap;
        Dictionary<int, GameObject> allUnits = BattleManager.Instance.GetUnitObjMap();
        if (allUnits == null) return null;

        foreach (var kvp in allUnits)
        {
            GameObject obj = kvp.Value;
            if (obj == null || !obj.activeSelf) continue;

            UnitIdentity identity = obj.GetComponent<UnitIdentity>();
            if (identity != null && !identity.isPlayer)   // 敌人
            {
                float dist = Vector3.Distance(player.transform.position, obj.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = obj;
                }
            }
        }
        return nearest;
    }

    // 层检测辅助
    bool IsInLayer(GameObject obj, LayerMask mask) =>
        ((1 << obj.layer) & mask) != 0;
}