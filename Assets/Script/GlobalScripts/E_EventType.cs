using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum E_EventType
{
    BattleStart,        // 参数：BattleStartData（参战单位列表、回合队列等信息）
    BattleEnd,          // 参数：无
    RoundStart,         // 当新的回合开始时触发（参数：当前回合的单位顺序列表）
    TurnStart,          // 参数：ID（当前行动单位）
    TurnEnd,            // 参数：ID（刚结束行动的单位）
    AllUnitsActed,      // 当所有单位行动完毕，即将开始新回合时触发
    UnitMoved,          // 参数：UnitMoveData（单位、目标位置、已消耗的移动距离）
    Attacked,           // 参数：AttackData；结构体，含攻击方id，目标id和伤害;
    HealthChanged,      // 参数：(int，int，int)分别是id/当前HP/最大HP
    UnitDied,           // 参数：ID（死亡单位）
    BattleQueueUpdated, // 参数：List<GameObject>（当前队列顺序）
    AnimNotify          // 参数：AnimNotifyData（动画播放完毕等通用通知）
}
