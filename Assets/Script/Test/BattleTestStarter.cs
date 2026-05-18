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