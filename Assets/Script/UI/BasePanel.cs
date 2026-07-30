using UnityEngine;

public abstract class BasePanel : MonoBehaviour
{
    [SerializeField] protected GameObject panelRoot;
    public bool IsVisible { get; private set; }

    protected virtual void Awake()
    {
        if (panelRoot == null) panelRoot = gameObject;
    }

    protected virtual void Start()
    {
        panelRoot.SetActive(false);  // 默认隐藏
    }

    public virtual void Show()
    {
        if (IsVisible) return;
        IsVisible = true;
        panelRoot.SetActive(true);
        OnShow();
    }

    public virtual void Hide()
    {
        if (!IsVisible) return;
        IsVisible = false;
        OnHide();
        panelRoot.SetActive(false);
    }

    /// <summary>子类重写：面板打开时注册事件</summary>
    protected virtual void OnShow() { }

    /// <summary>子类重写：面板关闭时注销事件</summary>
    protected virtual void OnHide() { }
}
