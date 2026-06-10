using System.Collections;
using System.Collections.Generic;
using System.Security.Principal;
using Unity.VisualScripting;
using UnityEngine;

public class HealthComponent : MonoBehaviour
{
    [SerializeField] private int maxHP;
    [SerializeField] private int currentHP;
    private UnitIdentity identity;
    void Awake()
    {
        currentHP = maxHP;
        identity = GetComponent<UnitIdentity>();
        if (identity == null)
            Debug.LogError($"{name} 缺少 UnitIdentity 组件！");
    }
    public void TakeDamage(int damage)
    {
        if (currentHP <= 0) return; // 已死亡

        currentHP = Mathf.Max(0, currentHP - damage);

        // 通知血量变化（无论死活，UI都需要更新最后一帧）
        EventBus.EventTrigger<(int,int,int)>(E_EventType.HealthChanged, (identity.unitID, currentHP, maxHP));

        if (currentHP == 0)
            EventBus.EventTrigger(E_EventType.UnitDied, identity.unitID);
    }
}
