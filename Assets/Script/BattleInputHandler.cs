using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleInputHandler : MonoBehaviour
{
    [SerializeField] private InputManager inputManager;
    [SerializeField] private LayerMask characterLayer;
    [SerializeField] private LayerMask walkableLayer;
    [SerializeField] private CamFollow cameraFollow;

    void Awake()
    {
        if (cameraFollow == null)
            cameraFollow = FindObjectOfType<CamFollow>();
    }

    void OnEnable()
    {
        if (inputManager != null)
        {
            inputManager.OnLeftClick += HandleLeftClick;
            inputManager.OnAltClick += HandleAltClick;
        }
    }

    void OnDisable()
    {
        if (inputManager != null)
        {
            inputManager.OnLeftClick -= HandleLeftClick;
            inputManager.OnAltClick -= HandleAltClick;
        }
    }

    void Update()
    {
        if (!BattleManager.Instance.IsBattleActive) return;

        int currentID = BattleManager.Instance.GetBattleQueue?.GetNowUnit() ?? -1;
        if (currentID == -1) return;

        if (!BattleManager.Instance.IsCurrentUnitPlayer())
        {
            Debug.Log("当前不是玩家单位 ");
            return;
        }

        // 攻击快捷键（数字键2）
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            bool success = BattleManager.Instance.TrySpendCurrentUnitAP(currentID, 2);
            if (!success) Debug.Log("AP不足，无法攻击");
        }
        // 手动结束回合（空格）
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            EventBus.EventTrigger(E_EventType.TurnEnd, currentID);
        }
    }

    void HandleLeftClick(RaycastHit hit)
    {
        // 战斗未激活时不处理（由 ClickSelector 处理自由探索）
        if (!BattleManager.Instance.IsBattleActive) return;

        int currentID = BattleManager.Instance.GetBattleQueue?.GetNowUnit() ?? -1;
        if (currentID == -1) return;
        if (!BattleManager.Instance.IsCurrentUnitPlayer()) return;

        GameObject currentUnit = BattleManager.Instance.GetUnitObject(currentID);
        if (currentUnit == null) return;

        NavMeshMoveCtrl moveCtrl = currentUnit.GetComponent<NavMeshMoveCtrl>();
        if (moveCtrl == null) return;

        // 如果单位正在移动，忽略此次点击
        if (moveCtrl.isMoving) return;

        // 点击到可行走地面 → 战斗移动
        if (IsInLayer(hit.collider.gameObject, walkableLayer))
        {
            moveCtrl.moveEndPos = hit.point;
            moveCtrl.Move();
            BattleManager.Instance.TrySpendCurrentUnitAP(currentID, 1);
        }
        // 未来：点击到敌人 → 攻击（消耗2AP）
        // else if (IsInLayer(hit.collider.gameObject, characterLayer))
        // {
        //     // 攻击逻辑...
        // }
    }

    void HandleAltClick(RaycastHit _)
    {
        // 战斗未激活时不处理
        if (!BattleManager.Instance.IsBattleActive) return;

        int currentID = BattleManager.Instance.GetBattleQueue?.GetNowUnit() ?? -1;
        if (currentID == -1) return;
        if (!BattleManager.Instance.IsCurrentUnitPlayer()) return;

        GameObject currentUnit = BattleManager.Instance.GetUnitObject(currentID);
        if (currentUnit == null) return;

        NavMeshMoveCtrl moveCtrl = currentUnit.GetComponent<NavMeshMoveCtrl>();
        if (moveCtrl == null) return;

        if (moveCtrl.isMoving) return;

        // 重新从鼠标位置发射射线，仅检测 walkableLayer
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, walkableLayer))
        {
            moveCtrl.moveEndPos = hit.point;
            moveCtrl.Move();
            BattleManager.Instance.TrySpendCurrentUnitAP(currentID, 1);
        }
    }

    // 辅助方法
    bool IsInLayer(GameObject obj, LayerMask mask) =>
        ((1 << obj.layer) & mask) != 0;


    
}