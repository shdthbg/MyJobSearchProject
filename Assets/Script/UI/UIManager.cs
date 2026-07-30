using System;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("面板引用（Inspector 拖拽）")]
    [SerializeField] private BasePanel explorationPanel;
    [SerializeField] private BasePanel battlePanel;
    [SerializeField] private BattleResultPanel resultPanel;

    private BasePanel currentPanel;

    // === 缓存 Action 引用，避免 lambda 无法注销 ===
    private Action<List<(int, float, GameObject)>> onBattleStart;
    private Action onBattleEnd;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // 初始化时创建好 Action，保证 Add 和 Remove 用的是同一个引用
        onBattleStart = _ => ShowPanel(battlePanel);
        onBattleEnd   = () => ShowPanel(explorationPanel);
    }

    void OnEnable()
    {
        EventBus.AddEventListener(E_EventType.BattleStart, onBattleStart);
        EventBus.AddEventListener(E_EventType.BattleEnd,   onBattleEnd);
    }

    void OnDisable()
    {
        EventBus.RemoveEventListener(E_EventType.BattleStart, onBattleStart);
        EventBus.RemoveEventListener(E_EventType.BattleEnd,   onBattleEnd);
    }

    void Start()
    {
        ShowPanel(explorationPanel);
    }

    public void ShowPanel(BasePanel target)
    {
        if (currentPanel == target) return;
        currentPanel?.Hide();
        currentPanel = target;
        currentPanel?.Show();
    }

    public void ShowResult(bool isVictory)
    {
        var rp = resultPanel;
        if (rp != null) rp.SetResult(isVictory);
        ShowPanel(resultPanel);
    }

    public T GetPanel<T>() where T : BasePanel
        => GetComponentInChildren<T>(includeInactive: true);
}