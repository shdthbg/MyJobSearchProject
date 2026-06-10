using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class AttackSystem : MonoBehaviour
{
    void OnEnable()
    {
        EventBus.AddEventListener<AttackData>(E_EventType.Attacked,OnAttack);
    }
    void OnDisable()
    {
        EventBus.RemoveEventListener<AttackData>(E_EventType.Attacked,OnAttack);
    }
    private void OnAttack(AttackData req)
    {
        GameObject targetObj = BattleManager.Instance.GetUnitObject(req.targetID);
        if(targetObj == null)
        {
            Debug.LogWarning($"攻击失败：目标 {req.targetID} 不存在");
            return;
        }
        HealthComponent targetHealth = targetObj.GetComponent<HealthComponent>();
        if(targetHealth == null)
        {
            Debug.LogWarning($"攻击失败：目标 {req.targetID} 没有 HealthComponent");
            return;
        }
        targetHealth.TakeDamage(req.damage);
    }
}
