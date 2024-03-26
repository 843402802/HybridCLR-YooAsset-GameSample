using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class PatchWindow : MonoBehaviour
{
    private Slider progressSlider;
    private Transform messgeBox;
    private Text txt_content;
    private Text txt_tips;
    private Button btn_ok;
    private void Awake()
    {
        EventCenter.Instance.EventListenner<float>("更新下载进度", UpdateDownloadProgress);
        EventCenter.Instance.EventListenner<float>("场景加载进度", UpdateLoadSceneProgress);
        EventCenter.Instance.EventListenner("游戏结束", GameOver);
        EventCenter.Instance.EventListenner("场景加载完成", HideUIWindow);

        messgeBox = transform.Find("UIWindow/MessgeBox");
        txt_content = messgeBox.Find("txt_content").GetComponent<Text>();
        progressSlider = transform.Find("UIWindow/Slider").GetComponent<Slider>();
        txt_tips = progressSlider.transform.Find("txt_tips").GetComponent<Text>();
        btn_ok = messgeBox.Find("btn_ok").GetComponent<Button>();
        btn_ok.onClick.AddListener(RestartGame);

        DontDestroyOnLoad(gameObject);
    }

    private void UpdateDownloadProgress(float progress)
    {
        progressSlider.value = progress;
        txt_tips.text = $"下载进度 : {progress * 100:N2}%";
    }
    private void UpdateLoadSceneProgress(float progress)
    {
        progressSlider.value = progress;
        txt_tips.text = $"场景加载进度 : {progress * 100:N2}%";
    }

    private void GameOver()
    {
        transform.Find("UIWindow").gameObject.SetActive(true);
        ShowMessageBox("重新开始游戏");
    }

    private void ShowMessageBox(string content)
    {
        txt_content.text = content;
        progressSlider.gameObject.SetActive(false);
        messgeBox.gameObject.SetActive(true);
    }

    private void RestartGame()  
    {
        progressSlider.gameObject.SetActive(true);
        messgeBox.gameObject.SetActive(false);
        EventCenter.Instance.EventTrigger("重新开始游戏");
    }

    private void HideUIWindow()
    {
        transform.Find("UIWindow").gameObject.SetActive(false);
    }
}
