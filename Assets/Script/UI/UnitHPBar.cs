using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// 角色头顶 World Space 血条。
/// 挂在角色 Prefab 的 HPBarCanvas 子对象上，自动监听 HealthChanged 事件更新显示。
/// </summary>
public class UnitHPBar : MonoBehaviour
{
    [Header("UI 引用（Inspector 拖拽）")]
    [SerializeField] private Image fillImage;      // HPFill 的 Image 组件
    [SerializeField] private Image bgImage;        // HPBackground 的 Image 组件（可选）
    [SerializeField] private Text hpText;          // 可选，不拖则不显示数字

    [Header("设置")]
    [SerializeField] private Color playerColor = new Color(0.3f, 0.9f, 0.3f);  // 玩家绿
    [SerializeField] private Color enemyColor  = new Color(0.9f, 0.2f, 0.2f);  // 敌人红
    [SerializeField] private bool faceCamera = true;

    private UnitIdentity identity;
    private Action<(int, int, int)> onHealthChanged;

    // ==================== 生命周期 ====================

    void Awake()
    {
        identity = GetComponentInParent<UnitIdentity>();
        if (identity == null)
        {
            Debug.LogError($"[UnitHPBar] {name}: 找不到父级 UnitIdentity，HPBar 不会工作");
            enabled = false;
            return;
        }

        // 自动生成白色 Sprite（解决内置 UISprite 圆角变形问题）
        EnsureWhiteSprite(fillImage);
        EnsureWhiteSprite(bgImage);

        // 根据阵营自动着色
        SetBarColor(identity.isPlayer ? playerColor : enemyColor);

        // 初始化显示
       
        // 订阅事件（缓存 Action 引用，确保能正确 Remove）
        onHealthChanged = OnHealthChanged;
        EventBus.AddEventListener<(int, int, int)>(E_EventType.HealthChanged, onHealthChanged);
    }
    void Start()
    {
        var health = GetComponentInParent<HealthComponent>();
        if (health != null)
            UpdateDisplay(health.currentHP, health.maxHP);

    }
    void OnDestroy()
    {
        if (onHealthChanged != null)
            EventBus.RemoveEventListener<(int, int, int)>(E_EventType.HealthChanged, onHealthChanged);
    }

    void LateUpdate()
    {
        if (faceCamera && Camera.main != null)
        {
            transform.LookAt(Camera.main.transform);
            transform.forward = -transform.forward;
        }
    }

    // ==================== 事件回调 ====================

    private void OnHealthChanged((int unitID, int currentHP, int maxHP) data)
    {
        if (identity == null || data.unitID != identity.unitID) return;
        UpdateDisplay(data.currentHP, data.maxHP);
    }

    // ==================== 核心逻辑 ====================

    private void UpdateDisplay(int current, int max)
    {
        float ratio = max > 0 ? (float)current / max : 0f;

        if (fillImage != null)
            fillImage.fillAmount = ratio;

        if (hpText != null)
            hpText.text = $"{current}/{max}";
    }

    public void SetBarColor(Color color)
    {
        if (fillImage != null) fillImage.color = color;
    }

    // ==================== 工具方法 ====================

    /// <summary>
    /// 如果 Image 没有 Source Image，自动生成 4×4 纯白方块（不变形）。
    /// </summary>
    private void EnsureWhiteSprite(Image img)
    {
        if (img == null || img.sprite != null) return;

        var tex = new Texture2D(4, 4);
        var colors = new Color[16];
        for (int i = 0; i < 16; i++)
            colors[i] = Color.white;
        tex.SetPixels(colors);
        tex.Apply();

        img.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
    }
}