using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class BattleQueue
{
    private List<int> nowRoundQueue = new();    //当前回合队列
    private List<int> readyQueue = new();       //准备队列(两个队列的int都是角色ID)
    private Dictionary<int, float> unitSpeeds;  // ID -> 速度
    private bool isActive;                      //当前是否在进行回合循环

    // 当新的回合开始时触发（参数：当前回合的单位顺序列表）
    public event Action<List<int>> OnRoundStart;
    // 当所有单位行动完毕，即将开始新回合时触发
    public event Action OnAllUnitsActed;
    // 当前行动单位
    public event Action<int> OnUnitTurnStart;
    // 战斗结束
    public event Action OnBattleEnd;

    /// <summary>
    /// 初始化队列
    /// </summary>
    /// <param name="participantsInBattle">参战单位的ID与速度的值元组列表 </param>
    public void InitQueue(List<(int id,float speed)> participantsInBattle)
    {
        unitSpeeds = participantsInBattle.ToDictionary(t => t.id, t => t.speed);
        readyQueue.Clear();
        nowRoundQueue.Clear();
        for(int i = 0; i < participantsInBattle.Count; i++)
        {
            int id = participantsInBattle[i].id;
            InsertSorted(nowRoundQueue,id);
        }
        if(nowRoundQueue != null && nowRoundQueue.Count != 0)
        {
            List<int> getNowOrder = GetNowOrder();
            OnRoundStart?.Invoke(getNowOrder);
            OnUnitTurnStart?.Invoke(nowRoundQueue[0]);
            isActive = true;
        }
    }

    /// <summary>
    /// 获取行动队列队首的ID
    /// </summary>
    /// <returns>异常</returns>获取到的ID
    public int GetNowUnit()
    {
        if(nowRoundQueue == null || nowRoundQueue.Count == 0)
        {
            Debug.LogWarning("当前队列为空，无法获取行动单位");
            return -1;
        }
        return nowRoundQueue[0];
    }

    /// <summary>
    /// 从当前回合队伍中移出目标单位，并压入准备队列，无论是否是队首均移出（涵盖完成行动或者被技能冻结两种情况）
    /// </summary>
    /// <param name="unitID">将被移出的单位</param>
    public void OnTurnEnd(int unitID)
    {
        if (!nowRoundQueue.Remove(unitID))
        {
            Debug.LogWarning($"出队失败：单位 {unitID} 不在当前队列中");
            return;
        }

        InsertSorted(readyQueue, unitID);
        if (nowRoundQueue.Count > 0)
        {
            OnUnitTurnStart?.Invoke(nowRoundQueue[0]);
        }
        else
        {
            OnAllUnitsActed?.Invoke();
            FinishRound();
        }
        
    }

    /// <summary>
    /// 单位死亡，从两个队列移出
    /// </summary>
    /// <param name="unitID">"死亡"的单位</param>
    public void RemoveUnit(int unitID)
    {
        if(unitID == GetNowUnit())
        {
            nowRoundQueue.Remove(unitID);
            if (nowRoundQueue.Count > 0)
            {
                OnUnitTurnStart?.Invoke(nowRoundQueue[0]);
            }
            else
            {
                // 队列为空，进入回合结束逻辑
                OnAllUnitsActed?.Invoke();
                FinishRound();
            }
        }
    }

    /// <summary>
    /// 回合结束，如果当前队列空并且准备队列非空，将准备队列置为当前队列，新建准备队列
    /// </summary>
    private void FinishRound()
    {
        if(nowRoundQueue == null || nowRoundQueue.Count == 0)
        {
            if(readyQueue == null || readyQueue.Count == 0)
            {
                Debug.LogWarning("双队列均为空");
                return;
            }
            else
            {
                nowRoundQueue = readyQueue;
                readyQueue = new List<int>();
                List<int> getNowOrder = GetNowOrder();
                OnRoundStart?.Invoke(getNowOrder);
                OnUnitTurnStart?.Invoke(nowRoundQueue[0]);
                return;
            }
        }
        return;
    }

    /// <summary>
    /// 新单位加入战斗
    /// </summary>
    /// <param name="unitID">新单位ID</param>
    /// <param name="speed">新单位速度</param>
    public void AddUnit(int unitID, float speed)
    {
        unitSpeeds.Add(unitID , speed);
        InsertSorted(readyQueue,unitID);
    }

    /// <summary>
    /// 用于获取当前回合仍未完成行动的单位顺序（只读）
    /// </summary>
    /// <returns>返回一份新地址存放的当前队列的拷贝</returns>
    public List<int> GetNowOrder()
    {
        List<int>getNowOrder = nowRoundQueue.ToList();
        return getNowOrder;
    }

    /// <summary>
    /// 用于获取下一回合的单位顺序（只读）
    /// </summary>
    /// <returns>返回一份新地址存放的准备队列的拷贝</returns>
    public List<int> GetReadyOrder()
    {
        List<int>getReadyOrder = readyQueue.ToList();
        return getReadyOrder;
    }

    /// <summary>
    /// 判断当前队列是否为空
    /// </summary>
    /// <returns></returns>
    public bool IsRoundComplete()
    {
        bool isRoundComplete = (nowRoundQueue == null || nowRoundQueue.Count == 0);
        return isRoundComplete;
    }

    /// <summary>
    /// 用于战斗结束之后清空数据 
    /// </summary>
    public void BattleQueueClear()
    {
        unitSpeeds.Clear();
        nowRoundQueue.Clear();
        readyQueue.Clear();
        OnBattleEnd?.Invoke();
    }

    /// <summary>
    /// 降序插入工具
    /// </summary>
    /// <param name="list">被插入的列表</param>
    /// <param name="unitID">插入的单位ID</param>
    private void InsertSorted(List<int> list , int unitID)
    {
        float speed = unitSpeeds[unitID];
        int index = list.FindIndex(id => unitSpeeds[id] < speed);
        if(index <0 )
            index = list.Count;//如果上一步返回-1，也就是没有找到速度小于当前单位速度的话，插入在末尾
        list.Insert(index,unitID);//在索引处插入ID
    }
}
