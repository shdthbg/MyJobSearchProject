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
    public ClickSelector clickSelector;
    public Dictionary<int, GameObject> GetUnitObjMap() => unitObjMap;
    
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
        clickSelector = FindObjectOfType<ClickSelector>();
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
        
        //确保每个参与者都有unitAPManger
        foreach(var p in unitObjMap)
        {
            UnitAPManager unitAPManager = p.Value.GetComponent<UnitAPManager>();
            if(unitAPManager == null)
            {
                p.Value.AddComponent<UnitAPManager>();
            }
            var cmc = p.Value.GetComponent<CharacterMoveControl>();
            if(cmc != null) 
            {
                cmc.SetTargetPosition(cmc.transform.position); // 初始化目标位置为当前位置
                cmc.enabled = false;// 禁用CharacterMoveControl，避免与战斗移动冲突
            }
        }
        //把id->speed 数据压入队列初始化方法
        var speedIDs = new List<(int id ,float speed)>();
        foreach(var p in participants)
        {
            speedIDs.Add((p.id,p.speed));
        }
        battleQueue.InitQueue(speedIDs);
        isBattleActive = true;  
        if(clickSelector != null)
        {
            clickSelector.enabled = false;
        }
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

        battleQueue.BattleQueueClear();

        foreach(var kvp in unitObjMap)
        {
            var cmc = kvp.Value.GetComponent<CharacterMoveControl>();
            if(cmc != null) 
            {
                cmc.enabled = true;// 恢复CharacterMoveControl
            }
        }

        unitObjMap.Clear();
        isBattleActive =false;
        EventBus.EventTrigger(E_EventType.BattleEnd);
        if(clickSelector != null)
        {
            clickSelector.enabled = true;
        }
        Debug.Log("[BattleManager] 战斗结束，已退出战斗模式，返回自由探索模式");
    }

    public void AddUnitToBattle(int id,float speed,GameObject obj)
    {
        if (unitObjMap.ContainsKey(id))
        {
            Debug.LogWarning($"单位 {id} 已在战斗中，无法重复加入");
            return;
        }

        unitObjMap.Add(id, obj);
        if (obj.GetComponent<UnitAPManager>() == null)
            obj.AddComponent<UnitAPManager>();
            
            obj.GetComponent<UnitAPManager>().ResetAP(); // 确保新加入单位的AP被初始化
        battleQueue.AddUnit(id, speed);
        Debug.Log($"[BattleManager] 新敌人 {id} 加入战斗");
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
        // 如果死亡单位正好是当前行动单位，先强制结束其回合（队列会切换到下一个单位）
        int currentID = battleQueue.GetNowUnit();
        if (currentID == unitID)
        {
            battleQueue.OnTurnEnd(unitID); // 这会在队列中标记单位完成，并触发下一单位的 TurnStart
        }

        battleQueue.RemoveUnit(unitID);
        Debug.Log($"[BattleManager] 收到外部 UnitDied 事件，单位ID：{unitID}");

         //从映射表中获取并销毁游戏对象
        if (unitObjMap.TryGetValue(unitID, out GameObject deadObj))
        {
            unitObjMap.Remove(unitID);               // 防止后续访问空引用
            Destroy(deadObj, 0.5f);                  // 延迟0.5秒销毁，给死亡特效/动画留时间（当前占位）
            Debug.Log($"[BattleManager] 单位 {unitID} 游戏对象已销毁");
        }
          // 3. 检查战斗结束条件（一方全灭）
        bool hasPlayer = false;
        bool hasEnemy = false;
        foreach (var kvp in unitObjMap)
        {
            UnitIdentity identity = kvp.Value.GetComponent<UnitIdentity>();
            if (identity != null)
            {
                if (identity.isPlayer) hasPlayer = true;
                else hasEnemy = true;
            }
        }
        if (!hasPlayer)
        {
            Debug.Log("[BattleManager] 所有玩家单位死亡，战斗失败");
            EndBattle();
        }
        else if (!hasEnemy)
        {
            Debug.Log("[BattleManager] 所有敌人死亡，战斗胜利！");
            EndBattle();
        }
        
    }

    private void OnRoundStartBridge(List<int> order)
    {
        EventBus.EventTrigger(E_EventType.RoundStart,order);
        Debug.Log($"[BattleManager] 桥接 OnRoundStart，顺序：{string.Join(",", order)}");
    }
    private void OnUnitTurnStartBridge(int unitID)
    {
        if (!unitObjMap.TryGetValue(unitID, out GameObject currentObj)) return;
        currentObj.GetComponent<UnitAPManager>().ResetAP();
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
    public bool TrySpendCurrentUnitAP(int unitID, int cost)
    {
        if (!unitObjMap.TryGetValue(unitID, out GameObject obj))
        {
            Debug.LogWarning($"[BattleManager] TrySpendAP: 单位 {unitID} 不在映射表中");
            return false;
        }
        var apMgr = obj.GetComponent<UnitAPManager>();
        if (apMgr == null)
        {
            Debug.LogError($"[BattleManager] TrySpendAP: 单位 {unitID} 缺少 UnitAPManager 组件");
            return false;
        }
        return apMgr.TrySpendAP(cost);
        }
    
    public bool IsCurrentUnitPlayer()
    {
        int currentID = battleQueue?.GetNowUnit()??-1;
        if(currentID == -1)return false;
        if(unitObjMap.TryGetValue(currentID,out GameObject obj))
        {
           var identity = obj.GetComponent<UnitIdentity>();
           return identity != null && identity.isPlayer; 
        }
        return false;
    }
    public GameObject GetUnitObject(int unitID)
    {
        unitObjMap.TryGetValue(unitID, out GameObject obj);
        return obj;  // 不存在时返回 null
    }

}


