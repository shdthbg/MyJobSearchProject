using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))] // 强制依赖导航代理，避免遗漏
public class NavMeshMoveCtrl : MonoBehaviour
{

    [Header("移动配置")]
    [Tooltip("每次点击需要移动的距离（米）")]
    public float moveDistance = 10; // 可在Inspector调整移动距离
    // 导航代理
    private NavMeshAgent navAgent;
    // 移动起点（记录开始移动时的角色位置）
    private Vector3 moveStartPos;
    // 是否处于“指定距离移动”状态
    public bool isMoving = false;
    public Vector3 moveEndPos;//移动终点

    [Tooltip("距离检测容错值（避免精度问题导致超距）")]
    public float distanceTolerance = 0.2f;
    [Tooltip("角色移动速度")]
    public float moveSpeed = 3f;

    // 新增：累计行走距离（核心）
    private float totalWalkedDistance = 0;
    // 记录上一帧的位置（用于计算每帧移动距离）
    private Vector3 lastFramePosition;

    public BaseAniCtrl CAniCtrl;//子物体动画控制器脚本

    void Awake()
    {
        // 1. 获取导航代理组件（自动添加，无需手动拖拽）
        navAgent = GetComponent<NavMeshAgent>();

        // 2. 初始化导航代理参数（匹配烘焙的NavMesh参数）
        navAgent.isStopped = true; // 强制停止
        navAgent.ResetPath(); // 清空所有路径（避免残留
        navAgent.speed = moveSpeed;
        navAgent.stoppingDistance = 0; // 关闭导航自带的停止距离（我们自己控制）
        navAgent.autoBraking = true; // 停止时自动刹车，避免滑步
        if (CAniCtrl == null)//如果没有拖拽赋值子物体脚本就获取子物体脚本
        {
            CAniCtrl = GetComponentInChildren<BaseAniCtrl>();
        }
    }
    public void CheckMoveDistance()
    {
        if (!isMoving) return;

        // 1. 计算【本帧移动的距离】（平面距离，忽略Y轴）
        Vector3 currentPos = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 lastPos = new Vector3(lastFramePosition.x, 0, lastFramePosition.z);
        float frameDistance = Vector3.Distance(currentPos, lastPos);

        // 2. 累加到总行走距离
        totalWalkedDistance += frameDistance;

        // 3. 打印日志：总行走距离 vs 目标距离
        Debug.Log($"【距离检测】已走：{totalWalkedDistance:F2}m / 目标：{moveDistance}m | 本帧走了：{frameDistance:F3}m");

        // 4. 达到目标距离（含容错）则停止
        if (totalWalkedDistance >= moveDistance - distanceTolerance)
        {
            StopMove();
            Debug.Log($"【移动停止】实际行走：{totalWalkedDistance:F2}m，达到目标距离 {moveDistance}m");
        }
        if(Vector3.Distance(transform.position, moveEndPos) <= distanceTolerance)
        {
            Debug.Log("到达目标点");
            StopMove();
        }
        // 5. 更新上一帧位置（供下一帧计算）
        lastFramePosition = transform.position;
    }

    public void StopMove() 
    {   
        isMoving = false;
        navAgent.isStopped = true;
        CAniCtrl.Idle1ToWalk = false;

    }
    public void Move()
    {
        Debug.Log("开始移动");
        // 1. 验证终点是否在导航网格上
        if (!NavMesh.SamplePosition(moveEndPos, out NavMeshHit hit, 1f, NavMesh.AllAreas))
        {
            Debug.LogWarning("【移动失败】目标点不在可行走区域");
        }
        isMoving = true;
        moveStartPos = transform.position;

        //初始化记录
        totalWalkedDistance = 0;
        lastFramePosition = transform.position;

        navAgent.isStopped = false;
        navAgent.SetDestination(moveEndPos);
        CAniCtrl.Idle1ToWalk = true;

    }


}
