using UnityEngine;
using System.Collections.Generic;

public class BattleTestStarter : MonoBehaviour
{
    public List<GameObject> testUnits; // 在 Inspector 中拖入 PlayerUnit 和 EnemyUnit

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            StartTestBattle();
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // 获取当前行动单位ID（如果战斗已激活）
            if (BattleManager.Instance.IsBattleActive && BattleManager.Instance.GetBattleQueue != null)
            {
                int currentID = BattleManager.Instance.GetBattleQueue.GetNowUnit();
                if (currentID != -1)
                {
                    Debug.Log($"[Test] 按下空格，发布 TurnEnd，单位ID：{currentID}");
                    EventBus.EventTrigger(E_EventType.TurnEnd, currentID);
                }
            }
        }
    }

    void StartTestBattle()
    {
        var participants = new List<(int id, float speed, GameObject obj)>();
        foreach (var unit in testUnits)
        {
            var identity = unit.GetComponent<UnitIdentity>();
            participants.Add((identity.unitID, identity.speed, unit));
        }

        var manager = GetComponent<BattleManager>();
        manager.StartBattle(participants);
    }
}