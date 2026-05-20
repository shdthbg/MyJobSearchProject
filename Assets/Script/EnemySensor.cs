using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class EnemySensor : MonoBehaviour
{
    private NavMeshPath cachedPath;
    void Awake()
    {
        cachedPath = new();
    }
    /// <summary>
    /// 用于确认敌人到主控玩家的寻路距离
    /// </summary>
    /// <param name="playerTransform">玩家的transform</param>
    /// <param name="distanceThreshold">入战距离阈值</param>
    /// <returns></returns>
    public bool CheckEngageDistance(Transform playerTransform,float distanceThreshold)
    {
        NavMeshHit self,player;
        float pathLength = 0;
        if(!NavMesh.SamplePosition(playerTransform.position, out player,1f,NavMesh.AllAreas) ||
            !NavMesh.SamplePosition(this.transform.position, out self,1f,NavMesh.AllAreas)) return false;
        else
        {
            NavMesh.CalculatePath(self.position,player.position,NavMesh.AllAreas,cachedPath);
            if(cachedPath.status == NavMeshPathStatus.PathPartial || cachedPath.status == NavMeshPathStatus.PathInvalid)
                return false;
            else
            {
                if (cachedPath.corners.Length >= 2)
                {
                    for(int i = 1; i < cachedPath.corners.Length; i++)
                    {
                        pathLength+=Vector3.Distance(cachedPath.corners[i-1],cachedPath.corners[i]);
                    }
                }
                else
                {
                    pathLength = Vector3.Distance(self.position,player.position);
                }
                bool isWithin = pathLength <= distanceThreshold;
                Debug.Log($"[EnemySensor {name}] 路径长度={pathLength:F2}m, 阈值={distanceThreshold}m, 拐点数={cachedPath.corners.Length}, 结果={isWithin}");
                return pathLength <= distanceThreshold;
            }
        }
    }
}
