using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EventBus
{
    //用于存储事件的字典
    private static Dictionary<E_EventType,Delegate> eventDic = new Dictionary<E_EventType, Delegate>();

    public static void AddEventListener<T>(E_EventType eventID, Action<T> handler)
    {
        if (eventDic.ContainsKey(eventID))
        {
            eventDic[eventID] = Delegate.Combine(eventDic[eventID],handler);
        }
        else
        {
            eventDic.Add(eventID,handler);
        }
    }

    public static void RemoveEventListener<T>(E_EventType eventID, Action<T> handler)
    {
        if (eventDic.ContainsKey(eventID))
        {
            Delegate Newdelegate = Delegate.Remove(eventDic[eventID],handler);
            if(Newdelegate == null)
            {
                //没有剩余订阅者删除键
                eventDic.Remove(eventID);
            }
            else
            {
                eventDic[eventID] = Newdelegate;
            }
        }
    }
    public static void EventTrigger<T>(E_EventType eventID, T args)
    {
        if (eventDic.ContainsKey(eventID))
        {
            Action<T> action = eventDic[eventID] as Action<T>;
            if(action != null)
            {
                action.Invoke(args);
            }
            else
            {
                Debug.Log("委托类型不匹配");
            }
        }
    }
    
    public static void AddEventListener(E_EventType eventID ,Action handler)
    {
        if (eventDic.ContainsKey(eventID))
        {
            eventDic[eventID] = Delegate.Combine(eventDic[eventID],handler);
        }
        else
        {
            eventDic.Add(eventID,handler);
        }
    }

    public static void RemoveEventListener(E_EventType eventID, Action handler)
    {
        if (eventDic.ContainsKey(eventID))
        {
            Delegate Newdelegate = Delegate.Remove(eventDic[eventID],handler);
            if(Newdelegate == null)
            {
                //没有剩余订阅者删除键
                eventDic.Remove(eventID);
            }
            else
            {
                eventDic[eventID] = Newdelegate;
            }
        }
    }

    public static void EventTrigger(E_EventType eventID)
    {
        if (eventDic.ContainsKey(eventID))
        {
            Action action = eventDic[eventID] as Action;
            if(action != null)
            {
                action.Invoke();
            }
            else
            {
                Debug.Log("委托类型不匹配");
            }
        }
    }
}

