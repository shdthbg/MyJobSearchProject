using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BattleProximityDetector : MonoBehaviour
{
    private bool isActive = false;  
    SphereCollider trigger;
    private Coroutine detectionCoroutine;
    HashSet<EnemySensor> enemiesInRange = new();//进入触发器的带敌人判断脚本的敌人表
    public float engageDistance = 20f;//战斗触发阈值
    public float triggerRadius = 20f;//触发器半径
    // Start is called before the first frame update
    void Awake()
    {
        trigger = gameObject.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = triggerRadius;
        trigger.center = Vector3.zero;
        trigger.enabled = false;
    }
    void Start()
    {
        if (SelectEvent.Instance != null)
        {
            SelectEvent.Instance.OnCharacterSelected += OnCharacterSelected;
        }
        else
            Debug.LogError("SelectEvent.Instance 为 null，请确保 SelectEvent 脚本已挂载并处于活动状态");
    }

    void OnDestroy()
    {
        if(detectionCoroutine != null)
        {
            StopCoroutine(detectionCoroutine);
            detectionCoroutine = null;
        }
        SelectEvent.Instance.OnCharacterSelected -= OnCharacterSelected;
    }
    private void OnCharacterSelected(GameObject selectedChar)
    {
        if(selectedChar == this.gameObject)
        {
            SetActive(true);
        }
        else
        {
            SetActive(false);
        }
    }
    /// <summary>
    /// 核心逻辑，用于设置触发器/开启或关闭协程
    /// </summary>
    /// <param name="active"></param>
    private void SetActive(bool active)
    {
        isActive = active;
        if(isActive)
        {
            trigger.enabled = true;
            if(detectionCoroutine == null)
            {
                detectionCoroutine = StartCoroutine(DetectionRoutine());
            }

        }
        else
        {
            trigger.enabled = false;
            if(detectionCoroutine != null)
            {
                StopCoroutine(detectionCoroutine);
                detectionCoroutine = null;
            }
            enemiesInRange.Clear();
        }
    }
    private IEnumerator DetectionRoutine()
    {
        while (isActive)
        {
            yield return new WaitForSeconds(1f);
            PerformDetection();
        }
    }
    
    /// <summary>
    /// 负责判断触发器范围内的敌人是否满足入战条件，并启动战斗。
    /// </summary>
    private void PerformDetection()
    {
        if(BattleManager.Instance.IsBattleActive)
        {
            List<EnemySensor> toAdd = new List<EnemySensor>();
            foreach (var enemy in enemiesInRange)
            {
                if (enemy != null && enemy.CheckEngageDistance(transform, engageDistance))
                    toAdd.Add(enemy);
            }
            
            foreach (var enemy in toAdd)
            {
                UnitIdentity idComp = enemy.GetComponent<UnitIdentity>();
                if (idComp != null)
                {
                    BattleManager.Instance.AddUnitToBattle(idComp.unitID, idComp.speed, enemy.gameObject);
                    enemiesInRange.Remove(enemy);
                }
            }
            return;
        }
        List<EnemySensor> readyEnemis = new();
        foreach (var i in enemiesInRange)
        {
            if(i == null) continue;
            if (i.CheckEngageDistance(gameObject.transform, engageDistance))
            {
                readyEnemis.Add(i);
            }
        }
        var participants = new List<(int id,float speed,GameObject obj)>();
        foreach(var i in UnitIdentity.playerUnits)
        {
            if(i == null)continue;
            var indentity = i.GetComponent<UnitIdentity>();
            if(indentity != null)
                participants.Add((indentity.unitID,indentity.speed,i));
        }
        foreach(var i in readyEnemis)
        {
            var indentity = i.GetComponent<UnitIdentity>();
            if(indentity != null)
            participants.Add((indentity.unitID,indentity.speed,i.gameObject));
        }

        Debug.Log($"[BPD] 本轮满足条件敌人数量={readyEnemis.Count}");

        if (readyEnemis.Count == 0) return;
        BattleManager.Instance.StartBattle(participants);
    }
    void OnTriggerEnter(Collider other)
    {
        EnemySensor otherSensor = other.GetComponent<EnemySensor>();
        if (otherSensor != null)
        {
            enemiesInRange.Add(otherSensor);
        }
        Debug.Log($"{other.name} 进入触发器，距离={Vector3.Distance(transform.position, other.transform.position)}");
    }
    void OnTriggerExit(Collider other)
    {
        EnemySensor otherSensor = other.GetComponent<EnemySensor>();
        if (otherSensor != null)
        {
            enemiesInRange.Remove(otherSensor);
        }
        Debug.Log($"{other.name} 离开触发器，距离={Vector3.Distance(transform.position, other.transform.position)}");
    }
}
