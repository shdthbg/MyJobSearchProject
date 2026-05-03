using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class InputManager : MonoBehaviour
{
    // 通过事件将点击结果发出
    public event Action<RaycastHit> OnLeftClick;     // 左键点击
    public event Action<RaycastHit> OnRightClick;    // 右键点击
    public event Action<RaycastHit> OnAltClick;      // Alt+左键

    [SerializeField] private Camera mainCamera;       // 用于射线

    void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
    }

    void Update()
    {
        // 检测左键
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            {
                if (Input.GetKey(KeyCode.LeftAlt))
                    OnAltClick?.Invoke(hit);
                else
                    OnLeftClick?.Invoke(hit);
            }
        }
    }
}
