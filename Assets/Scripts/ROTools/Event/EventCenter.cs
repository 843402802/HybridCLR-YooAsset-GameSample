using System.Collections.Generic;
using UnityEngine.Events;

public interface IEventInfo { }

public class EventInfo<T> : IEventInfo
{
    public UnityAction<T> actions;

    public EventInfo(UnityAction<T> action)
    {
        actions += action;
    }
}

public class EventInfo : IEventInfo
{
    public UnityAction actions;

    public EventInfo(UnityAction action)
    {
        actions += action;
    }
}

/// <summary>
/// 事件中心,每个事件名称只能注册一个事件监听器
/// </summary>
public class EventCenter : SingleBase<EventCenter>
{
    /// <summary>
    /// key：事件的名字
    /// value: 对应的是监听该事件的委托方法
    /// </summary>
    private Dictionary<string, IEventInfo> eventDic = new Dictionary<string, IEventInfo>();

    /// <summary>
    /// 监听事件(带泛型参数)
    /// </summary>
    /// <param name="eventName">事件的名字</param>
    /// <param name="action">用来处理该事件的方法</param>
    public void EventListenner<T>(string eventName, UnityAction<T> action)
    {
        //有没有对应的事件监听
        //有
        if (eventDic.ContainsKey(eventName))
        {
            //委托 一对多
            (eventDic[eventName] as EventInfo<T>).actions += action;
        }
        else//没有
        {
            eventDic[eventName] = new EventInfo<T>(action);
        }
    }

    /// <summary>
    /// 监听事件不带参数
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="action"></param>
    public void EventListenner(string eventName, UnityAction action)
    {
        //有没有对应的事件监听
        //有
        if (eventDic.ContainsKey(eventName))
        {
            //委托 一对多
            (eventDic[eventName] as EventInfo).actions += action;
        }
        else//没有
        {
            eventDic[eventName] = new EventInfo(action);
        }
    }

    /// <summary>
    /// 事件触发（带泛型参数）
    /// </summary>
    /// <param name="eventName">那个名字的事件触发了</param>
    public void EventTrigger<T>(string eventName, T info)
    {
        if (eventDic.ContainsKey(eventName))
        {
            // eventDic[eventName]?.Invoke(info);
            if ((eventDic[eventName] as EventInfo<T>).actions != null)
                (eventDic[eventName] as EventInfo<T>).actions(info);//执行委托函数
        }
    }

    /// <summary>
    /// 事件触发（不带泛型参数）
    /// </summary>
    /// <param name="eventName"></param>
    public void EventTrigger(string eventName)
    {
        if (eventDic.ContainsKey(eventName))
        {
            // eventDic[eventName]?.Invoke(info);
            if ((eventDic[eventName] as EventInfo).actions != null)
                (eventDic[eventName] as EventInfo).actions();//执行委托函数
        }
    }

    /// <summary>
    /// 移除对应事件
    /// </summary>
    /// <param name="eventName">事件的名字</param>
    /// <param name="action">对应之间添加的委托函数</param>
    public void RemoveEvent<T>(string eventName, UnityAction<T> action)
    {
        if (eventDic.ContainsKey(eventName))
        {
            (eventDic[eventName] as EventInfo<T>).actions -= action;
        }
    }

    /// <summary>
    /// 移除对应事件
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="action"></param>
    public void RemoveEvent(string eventName, UnityAction action)
    {
        if (eventDic.ContainsKey(eventName))
        {
            (eventDic[eventName] as EventInfo).actions -= action;
        }
    }

    /// <summary>
    /// 移除某类事件
    /// </summary>
    /// <param name="eventName"></param>
    public void ClearEvents(string eventName)
    {
        if (eventDic.ContainsKey(eventName))
        {
            eventDic.Remove(eventName);
        }
    }
}
