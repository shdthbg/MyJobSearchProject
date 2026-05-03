using UnityEngine;

public class ClickSelector : MonoBehaviour
{
    [SerializeField] private InputManager inputManager;
    [SerializeField] private LayerMask characterLayer;
    [SerializeField] private LayerMask walkableLayer;
    [SerializeField] private CamFollow cameraFollow;   // 拖拽赋值

    private GameObject selectedCharacter;               // 当前选中的角色（可由 SelectEvent 更新）

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
            // 可选：右键点击事件用于取消选中
        }
        // 订阅 SelectEvent 以保持 selectedCharacter 同步
        if (SelectEvent.Instance != null)
        {
            SelectEvent.Instance.OnCharacterSelected += OnCharacterSelectedFromEvent;
            SelectEvent.Instance.OnCharacterDeselected += OnCharacterDeselectedFromEvent;
        }
    }

    void OnDisable()
    {
        if (inputManager != null)
        {
            inputManager.OnLeftClick -= HandleLeftClick;
            inputManager.OnAltClick -= HandleAltClick;
        }
        if (SelectEvent.Instance != null)
        {
            SelectEvent.Instance.OnCharacterSelected -= OnCharacterSelectedFromEvent;
            SelectEvent.Instance.OnCharacterDeselected -= OnCharacterDeselectedFromEvent;
        }
    }

    // 左键点击处理
    void HandleLeftClick(RaycastHit hit)
    {
        // 1. 检测是否点击到角色
        if (IsInLayer(hit.collider.gameObject, characterLayer))
        {
            SelectEvent.Instance?.TriggerCharacterSelected(hit.collider.gameObject);
            return;
        }

        // 2. 检测是否点击到可行走地面
        if (IsInLayer(hit.collider.gameObject, walkableLayer))
        {
            if (selectedCharacter != null)
            {
                selectedCharacter.GetComponent<CharacterMoveControl>()
                    ?.SetTargetPosition(hit.point);
            }
            // 点击地面：不取消选中（保持当前角色）
            return;
        }

        // 3. 点击到其他层（UI、障碍物等）→ 取消选中
        SelectEvent.Instance?.DeselectCurrentCharacter();
    }

    // Alt + 左键点击处理（强制移动到地面，即使无选中角色？原逻辑：有选中角色时移动）
    void HandleAltClick(RaycastHit hit)
    {
        if (IsInLayer(hit.collider.gameObject, walkableLayer))
        {
            if (selectedCharacter != null)
            {
                selectedCharacter.GetComponent<CharacterMoveControl>()
                    ?.SetTargetPosition(hit.point);
            }
        }
        // Alt 点击非地面不做处理
    }

    // 通过 SelectEvent 回调更新本地 selectedCharacter
    void OnCharacterSelectedFromEvent(GameObject obj) => selectedCharacter = obj;
    void OnCharacterDeselectedFromEvent(GameObject obj)
    {
        if (obj == selectedCharacter)
            selectedCharacter = null;
    }

    // 辅助方法：检查 GameObject 是否在指定 LayerMask 中
    bool IsInLayer(GameObject obj, LayerMask mask) =>
        ((1 << obj.layer) & mask) != 0;
}