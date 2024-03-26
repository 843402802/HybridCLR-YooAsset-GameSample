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
/// �¼�����,ÿ���¼�����ֻ��ע��һ���¼�������
/// </summary>
public class EventCenter : SingleBase<EventCenter>
{
    /// <summary>
    /// key���¼�������
    /// value: ��Ӧ���Ǽ������¼���ί�з���
    /// </summary>
    private Dictionary<string, IEventInfo> eventDic = new Dictionary<string, IEventInfo>();

    /// <summary>
    /// �����¼�(�����Ͳ���)
    /// </summary>
    /// <param name="eventName">�¼�������</param>
    /// <param name="action">����������¼��ķ���</param>
    public void EventListenner<T>(string eventName, UnityAction<T> action)
    {
        //��û�ж�Ӧ���¼�����
        //��
        if (eventDic.ContainsKey(eventName))
        {
            //ί�� һ�Զ�
            (eventDic[eventName] as EventInfo<T>).actions += action;
        }
        else//û��
        {
            eventDic[eventName] = new EventInfo<T>(action);
        }
    }

    /// <summary>
    /// �����¼���������
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="action"></param>
    public void EventListenner(string eventName, UnityAction action)
    {
        //��û�ж�Ӧ���¼�����
        //��
        if (eventDic.ContainsKey(eventName))
        {
            //ί�� һ�Զ�
            (eventDic[eventName] as EventInfo).actions += action;
        }
        else//û��
        {
            eventDic[eventName] = new EventInfo(action);
        }
    }

    /// <summary>
    /// �¼������������Ͳ�����
    /// </summary>
    /// <param name="eventName">�Ǹ����ֵ��¼�������</param>
    public void EventTrigger<T>(string eventName, T info)
    {
        if (eventDic.ContainsKey(eventName))
        {
            // eventDic[eventName]?.Invoke(info);
            if ((eventDic[eventName] as EventInfo<T>).actions != null)
                (eventDic[eventName] as EventInfo<T>).actions(info);//ִ��ί�к���
        }
    }

    /// <summary>
    /// �¼��������������Ͳ�����
    /// </summary>
    /// <param name="eventName"></param>
    public void EventTrigger(string eventName)
    {
        if (eventDic.ContainsKey(eventName))
        {
            // eventDic[eventName]?.Invoke(info);
            if ((eventDic[eventName] as EventInfo).actions != null)
                (eventDic[eventName] as EventInfo).actions();//ִ��ί�к���
        }
    }

    /// <summary>
    /// �Ƴ���Ӧ�¼�
    /// </summary>
    /// <param name="eventName">�¼�������</param>
    /// <param name="action">��Ӧ֮����ӵ�ί�к���</param>
    public void RemoveEvent<T>(string eventName, UnityAction<T> action)
    {
        if (eventDic.ContainsKey(eventName))
        {
            (eventDic[eventName] as EventInfo<T>).actions -= action;
        }
    }

    /// <summary>
    /// �Ƴ���Ӧ�¼�
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
    /// �Ƴ�ĳ���¼�
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
