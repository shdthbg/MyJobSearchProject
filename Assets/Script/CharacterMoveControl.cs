using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterMoveControl : MonoBehaviour
{
    public float moveSpeed = 2f;//移动速度
    private Vector3 targetPosition;//移动目标位置
    private float stopDistance = 0.01f;//停止范围，用于抹除浮点数的不精确尾数
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 moveDirection = (targetPosition - transform.position).normalized;//移动方向
        if (Vector3.Distance(transform.position, targetPosition) > stopDistance)
        {
            transform.position += moveDirection * moveSpeed * Time.deltaTime;
        }
    }
}
