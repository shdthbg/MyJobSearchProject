using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum E_EventType
{
    BattleStart,        // 参数：BattleStartData（参战单位列表、回合队列等信息）
    BattleEnd,          // 参数：无
    TurnStart,          // 参数：GameObject（当前行动单位）
    TurnEnd,            // 参数：GameObject（刚结束行动的单位）
    UnitMoved,          // 参数：UnitMoveData（单位、目标位置、已消耗的移动距离）
    UnitAttacked,       // 参数：UnitAttackData（攻击者、目标、伤害值）
    UnitDied,           // 参数：GameObject（死亡单位）
    EnemySpotted,       // 参数：EnemySpottedData（发现者、被发现者、触发距离）
    BattleQueueUpdated, // 参数：List<GameObject>（当前队列顺序）
    AnimNotify          // 参数：AnimNotifyData（动画播放完毕等通用通知）
}
