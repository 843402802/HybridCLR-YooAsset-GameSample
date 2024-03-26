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
        EventCenter.Instance.EventListenner("重新开始游戏", RestartGame);
        DontDestroyOnLoad(gameObject);
    }

    private void RestartGame()
    {
        LoadScene("Demo_Scene");
    }

    public async void LoadScene(string sceneName)
    {
        Debug.Log("加载主场景");
        //获取资源包
        bool suspendLoad = false;
        SceneHandle handle = YooAssets.LoadSceneAsync(sceneName, LoadSceneMode.Single, suspendLoad);
        while (!handle.IsDone)
        {
            float progress = Mathf.Clamp01(handle.Progress);
            await Task.Yield(); // 让出当前帧的执行权，等待下一帧继续执行
            EventCenter.Instance.EventTrigger("场景加载进度", progress / 0.9f);// 场景加载的进度范围在0到0.9之间
        }
        EventCenter.Instance.EventTrigger("场景加载完成");

    }
}

