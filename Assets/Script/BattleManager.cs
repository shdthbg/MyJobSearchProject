using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    private BattleQueue battleQueue;
    private Dictionary<int,GameObject> unitObjMap;  //单位ID到场景对象的映射 
    private bool isBattleActive;                    //战斗是否进行中
    public static BattleManager Instance { get; private set; }
    public bool IsBattleActive => isBattleActive;
    public BattleQueue GetBattleQueue =>battleQueue;
    void Awake()
    {
        // 单例初始化（确保全局唯一）
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 跨场景保留
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartBattle(List<(int id,float speed,GameObject obj)> participants)
    {
        if(isBattleActive) EndBattle();
        unitObjMap = new();
        foreach(var p in participants)
        {
            if (!unitObjMap.ContainsKey(p.id))
            {
                unitObjMap.Add(p.id,p.obj);
            }
            else
            {
                Debug.LogWarning($"单位ID {p.id} 重复，已忽略");
            }
        }
        battleQueue = new();
        //将battleQueue的四个事件挂接("翻译")到EventBus
        battleQueue.OnRoundStart += OnRoundStartBridge;
        battleQueue.OnUnitTurnStart += OnUnitTurnStartBridge;
        battleQueue.OnAllUnitsActed += OnAllUnitsActedBridge;
        battleQueue.OnBattleEnd += OnBattleEndBridge;
        //订阅TurnEnd和UnitDied
        EventBus.AddEventListener<int>(E_EventType.TurnEnd,OnExternalTurnEnd);
        EventBus.AddEventListener<int>(E_EventType.UnitDied,OnExternalUnitDied);
        
        EventBus.EventTrigger(E_EventType.BattleStart,participants);     
        //把id->speed 数据压入队列初始化方法
        var speedIDs = new List<(int id ,float speed)>();
        foreach(var p in participants)
        {
            speedIDs.Add((p.id,p.speed));
        }
        battleQueue.InitQueue(speedIDs);
        isBattleActive = true;  
    }

    public void EndBattle()
    {
        //取消battleQueue的四个事件的挂接
        battleQueue.OnRoundStart -= OnRoundStartBridge;
        battleQueue.OnUnitTurnStart -= OnUnitTurnStartBridge;
        battleQueue.OnAllUnitsActed -= OnAllUnitsActedBridge;
        battleQueue.OnBattleEnd -= OnBattleEndBridge;
        //取消TurnEnd和UnitDied的订阅
        EventBus.RemoveEventListener<int>(E_EventType.TurnEnd,OnExternalTurnEnd);
        EventBus.RemoveEventListener<int>(E_EventType.UnitDied,OnExternalUnitDied);
        unitObjMap.Clear();
        battleQueue.BattleQueueClear();
        isBattleActive =false;
        EventBus.EventTrigger(E_EventType.BattleEnd);
    }
    private void OnExternalTurnEnd(int unitID)
    {
        if(!isBattleActive) return;
        battleQueue.OnTurnEnd(unitID);
        Debug.Log($"[BattleManager] 收到外部 TurnEnd 事件，单位ID：{unitID}");
    }
    private void OnExternalUnitDied(int unitID)
    {
        if(!isBattleActive) return;
        battleQueue.RemoveUnit(unitID);
        Debug.Log($"[BattleManager] 收到外部 UnitDied 事件，单位ID：{unitID}");
    }

    private void OnRoundStartBridge(List<int> order)
    {
        EventBus.EventTrigger(E_EventType.RoundStart,order);
        Debug.Log($"[BattleManager] 桥接 OnRoundStart，顺序：{string.Join(",", order)}");
    }
    private void OnUnitTurnStartBridge(int unitID)
    {
        EventBus.EventTrigger(E_EventType.TurnStart, unitID);
        Debug.Log($"[BattleManager] 桥接 OnUnitTurnStart，单位ID：{unitID}");
    }
    private void OnAllUnitsActedBridge()
    {
        EventBus.EventTrigger(E_EventType.AllUnitsActed);
        Debug.Log($"[BattleManager] 桥接 OnAllUnitsActed");
    }
    private void OnBattleEndBridge()
    {
        EventBus.EventTrigger(E_EventType.BattleEnd);
    }
}


