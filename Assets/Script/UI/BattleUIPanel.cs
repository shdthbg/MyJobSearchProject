using UnityEngine;
using UnityEngine.UI;

public class BattleUIPanel : BasePanel
{
    [Header("UI 引用（Inspector 拖拽）")]
    [SerializeField] private Text turnIndicator;
    [SerializeField] private Text apDisplay;

    private int watchingUnitID = -1;

    protected override void OnShow()
    {
        EventBus.AddEventListener<int>(E_EventType.TurnStart, OnTurnStart);
        EventBus.AddEventListener<UnitAPData>(E_EventType.APChanged, OnAPChanged);
    }

    protected override void OnHide()
    {
        EventBus.RemoveEventListener<int>(E_EventType.TurnStart, OnTurnStart);
        EventBus.RemoveEventListener<UnitAPData>(E_EventType.APChanged, OnAPChanged);
    }

    private void OnTurnStart(int unitID)
    {
        if (BattleManager.Instance == null) return;

        watchingUnitID = unitID;

        bool isPlayer = BattleManager.Instance.IsCurrentUnitPlayer();
        turnIndicator.text = isPlayer ? "你的回合" : "敌人回合";
        turnIndicator.color = isPlayer ? Color.green : Color.red;

        // 初始显示当前 AP（TurnStart 时 ResetAP 会触发 APChanged，这行作为保底）
        RefreshAPDisplay();
    }

    private void OnAPChanged(UnitAPData data)
    {
        if (data.unitID != watchingUnitID) return;  // 只看当前行动单位
        apDisplay.text = $"AP: {data.currentAP}/{data.maxAP}";
    }

    private void RefreshAPDisplay()
    {
        var obj = BattleManager.Instance?.GetUnitObject(watchingUnitID);
        if (obj != null)
        {
            var ap = obj.GetComponent<UnitAPManager>();
            apDisplay.text = $"AP: {ap.currentAP}/{ap.maxAP}";
        }
    }
}