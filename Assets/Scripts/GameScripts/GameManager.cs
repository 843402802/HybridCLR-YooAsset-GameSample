using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;

public class GameManager : SingleMonoBase<GameManager>
{
    private void Awake()
    {
        EventCenter.Instance.EventListenner("���¿�ʼ��Ϸ", RestartGame);
        DontDestroyOnLoad(gameObject);
    }

    private void RestartGame()
    {
        LoadScene("Demo_Scene");
    }

    public async void LoadScene(string sceneName)
    {
        Debug.Log("����������");
        //��ȡ��Դ��
        bool suspendLoad = false;
        SceneHandle handle = YooAssets.LoadSceneAsync(sceneName, LoadSceneMode.Single, suspendLoad);
        while (!handle.IsDone)
        {
            float progress = Mathf.Clamp01(handle.Progress);
            await Task.Yield(); // �ó���ǰ֡��ִ��Ȩ���ȴ���һ֡����ִ��
            EventCenter.Instance.EventTrigger("�������ؽ���", progress / 0.9f);// �������صĽ��ȷ�Χ��0��0.9֮��
        }
        EventCenter.Instance.EventTrigger("�����������");

    }
}

