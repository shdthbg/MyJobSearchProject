using System.Collections;
using UnityEngine;

public class PlayerAttackHandler : MonoBehaviour
{
    private BaseAniCtrl aniCtrl;
    private AttackData pendingAttack;
    private Coroutine attackCoroutine;

    void Awake()
    {
        var animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            aniCtrl = animator.GetComponent<BaseAniCtrl>();
            if (aniCtrl != null)
                aniCtrl.AttackHitTriggered += OnAttackHit;
        }
    }

    void OnDestroy()
    {
        if (aniCtrl != null)
            aniCtrl.AttackHitTriggered -= OnAttackHit;
        if (attackCoroutine != null)
            StopCoroutine(attackCoroutine);
    }

    public void DoAttack(AttackData data)
    {
        pendingAttack = data;

        var moveCtrl = GetComponent<NavMeshMoveCtrl>();
        if (moveCtrl != null && moveCtrl.isMoving)
            moveCtrl.StopMove();

        if (aniCtrl != null)
            aniCtrl.Idle1ToAttack = true;

        // 读取攻击动画长度，启动收尾协程
        float clipLength = GetAttackClipLength();
        attackCoroutine = StartCoroutine(EndAttackRoutine(clipLength));
    }

    private void OnAttackHit()
    {
        if (pendingAttack.targetID == 0) return;
        EventBus.EventTrigger(E_EventType.Attacked, pendingAttack);
        Debug.Log($"[PlayerAttackHandler] 击打点触发！目标={pendingAttack.targetID}，伤害={pendingAttack.damage}");
        pendingAttack = default;
    }

    private IEnumerator EndAttackRoutine(float clipLength)
    {
        yield return new WaitForSeconds(clipLength);

        if (aniCtrl != null)
            aniCtrl.Idle1ToAttack = false;

        // 获取自己的 UnitIdentity，触发回合结束
        var identity = GetComponent<UnitIdentity>();
        if (identity != null)
            EventBus.EventTrigger(E_EventType.TurnEnd, identity.unitID);
    }

    private float GetAttackClipLength()
    {
        if (aniCtrl == null) return 1f;
        var animator = aniCtrl.GetComponent<Animator>();
        if (animator == null || animator.runtimeAnimatorController == null) return 1f;

        foreach (var clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name.IndexOf("attack", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return clip.length;
        }
        return 1f;
    }
}