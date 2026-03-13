using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MouseClickControl : MonoBehaviour
{
    Ray mouseRay;
    public LayerMask charactorMask;//角色遮罩，用于鼠标选角的射线判定
    public LayerMask landMashMask;//地面网格遮罩，用于鼠标点击地面位置的射线判定
    RaycastHit chaCatch;//检测角色的射线检测到的角色
    RaycastHit landCarch;//检测地面网格的射线检测到的点
    Vector3 targetpoint;//用于关联玩家角色的目标位置
    private CharacterMoveControl selectedCharacter; // 选中的角色

    void Start()
    {

    }

    void Update()
    {
        mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Input.GetMouseButtonDown(0))
        {
            RaycastStart();
        }
    }

    public void RaycastStart()
    {
        if (Physics.Raycast(mouseRay, out chaCatch, Mathf.Infinity, charactorMask))
        {
            selectedCharacter = chaCatch.collider.GetComponent<CharacterMoveControl>();
            Debug.Log("选中角色：" + chaCatch.collider.gameObject.name);
        }
        else
        {
            if (Physics.Raycast(mouseRay, out landCarch, Mathf.Infinity, landMashMask))
            {
                if (selectedCharacter == null)
                {
                    Debug.LogError("没有选中角色");
                    return;
                }
                // 给选中的角色设置移动目标
                selectedCharacter.SetTargetPosition(landCarch.point);
                Debug.Log("点击地面，目标位置：" + landCarch.point);
            }
        }
    }
}
/*
 遇到的bug和解决办法：
 */