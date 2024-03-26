using HybridCLR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using YooAsset;

/// <summary>
/// 启动器：
/// 1.下载资源，用YooAsset资源框架进行下载
///     1）资源文件，ab包等
///     2）热更新dll
/// </summary>
public class LaunchManager : MonoBehaviour
{
    /// <summary>
    /// 资源系统运行模式
    /// </summary>
    public EPlayMode PlayMode = EPlayMode.EditorSimulateMode;
    void Awake()
    {
#if !UNITY_EDITOR
         //打包后默认远程下载模式
         PlayMode = EPlayMode.HostPlayMode;
#endif
        Application.targetFrameRate = 60;
        Application.runInBackground = true;
        Screen.SetResolution(600, 800, false);
        DontDestroyOnLoad(this.gameObject);
    }

    IEnumerator Start()
    {
        // 初始化事件系统
        //UniEvent.Initalize();
        //1.初始化
        Debug.Log("1. 初始化");
        // 初始化资源系统
        YooAssets.Initialize();

        // 创建默认的资源包
        var package = YooAssets.CreatePackage("DefaultPackage");

        // 设置该资源包为默认的资源包，可以使用YooAssets相关加载接口加载该资源包内容。
        YooAssets.SetDefaultPackage(package);

        //2.资源系统的运行模式
        Debug.Log("2. 资源系统的运行模式");
        if (PlayMode == EPlayMode.EditorSimulateMode)
        {
            var initParameters = new EditorSimulateModeParameters();//EditorSimulateModeParameters继承自InitializeParameters
            string simulateManifestFilePath = EditorSimulateModeHelper.SimulateBuild(EDefaultBuildPipeline.BuiltinBuildPipeline.ToString(), "DefaultPackage");
            initParameters.SimulateManifestFilePath = simulateManifestFilePath;
            yield return package.InitializeAsync(initParameters);
        }
        else if (PlayMode == EPlayMode.OfflinePlayMode)
        {
            var initParameters = new OfflinePlayModeParameters();//OfflinePlayModeParameters继承自InitializeParameters
            initParameters.DecryptionServices = new FileOffsetDecryption();//需要补充这个
            yield return package.InitializeAsync(initParameters);
        }
        else if (PlayMode == EPlayMode.HostPlayMode)
        {
            // 注意：GameQueryServices.cs 太空战机的脚本类，详细见StreamingAssetsHelper.cs
            string defaultHostServer = "http://127.0.0.1/CDN/Bundle";
            string fallbackHostServer = "http://127.0.0.1/CDN/Bundle";
            var initParameters = new HostPlayModeParameters();//HostPlayModeParameters继承自InitializeParameters
            initParameters.BuildinQueryServices = new GameQueryServices();//内置资源查询服务接口
            initParameters.DecryptionServices = new FileOffsetDecryption();//如果资源包在构建的时候有加密，需要提供实现IDecryptionServices接口的实例类。
            initParameters.RemoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);//远端服务器查询服务接口
            var initOperation = package.InitializeAsync(initParameters);
            yield return initOperation;

            if (initOperation.Status == EOperationStatus.Succeed)
            {
                Debug.Log("资源包初始化成功！");
            }
            else
            {
                Debug.LogError($"资源包初始化失败：{initOperation.Error}");
            }
        }
        else if (PlayMode == EPlayMode.WebPlayMode)
        {
            string defaultHostServer = "http://127.0.0.1/CDN/WebGL/V1.0";
            string fallbackHostServer = "http://127.0.0.1/CDN/WebGL/V1.0";
            var initParameters = new WebPlayModeParameters();//WebPlayModeParameters继承自InitializeParameters
            initParameters.BuildinQueryServices = new GameQueryServices();
            initParameters.RemoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
            var initOperation = package.InitializeAsync(initParameters);
            yield return initOperation;

            if (initOperation.Status == EOperationStatus.Succeed)
            {
                Debug.Log("资源包初始化成功！");
            }
            else
            {
                Debug.LogError($"资源包初始化失败：{initOperation.Error}");
            }
        }

        //3.获取资源版本：UpdatePackageVersionAsync
        Debug.Log("3. 获取资源版本");
        var operation = package.UpdatePackageVersionAsync();
        yield return operation;

        if (operation.Status == EOperationStatus.Succeed)
        {

            string packageVersion = operation.PackageVersion;
            Debug.Log($"Updated package Version : {packageVersion}");

            //4.更新资源清单：对于联机运行模式，在获取到资源版本号之后，就可以更新资源清单了：UpdatePackageManifestAsync
            //联机运行模式
            //通过传入的清单版本，优先比对当前激活清单的版本，如果相同就直接返回成功。如果有差异就从缓存里去查找匹配的清单，如果缓存里不存在，就去远端下载并保存到沙盒里。最后加载沙盒内匹配的清单文件。
            Debug.Log("4. 更新资源清单");
            bool savePackageVersion = true;
            var operation2 = package.UpdatePackageManifestAsync(packageVersion, savePackageVersion);
            yield return operation2;

            if (operation2.Status == EOperationStatus.Succeed)
            {
                //5.资源包下载
                Debug.Log("5. 资源包下载");
                yield return Download();
#if UNITY_EDITOR
                Debug.Log("6.编辑器模式下，直接查找程序集");
                // 编辑器模式下，直接进入
                Assembly hotUpdateAss = System.AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "HotUpdate");
                Type type = hotUpdateAss.GetType("HotUpdateEntry");
                type.GetMethod("EntryGame").Invoke(null, null);
#else
                //6.加载热更代码,YooAsset2.0版本rawfile文件需要用RawFile管线单独打包,否则用LoadRawFileAsync时候会报错
                //把热更代码改成txt后缀用textasset类型读取可解决
                Debug.Log("6.加载补充元数据与热更代码");
                //补充元数据
                yield return LoadMetadataForAOTAssemblies();
                //热更代码
                var codeHandle = package.LoadAssetAsync<TextAsset>("HotUpdate.dll");
                yield return codeHandle;
                TextAsset textAsset = codeHandle.AssetObject as UnityEngine.TextAsset;
                Assembly hotUpdateAss = Assembly.Load(textAsset.bytes);
                Type type = hotUpdateAss.GetType("HotUpdateEntry");
                type.GetMethod("EntryGame").Invoke(null, null);
#endif
                ////7.加载场景，启用可寻址功能（Enable Addressable）后，不用写全路径，直接写资源名称即可
                //string location = "TestScene";
                //var sceneMode = UnityEngine.SceneManagement.LoadSceneMode.Single;
                //bool suspendLoad = false;
                //SceneHandle handle = package.LoadSceneAsync(location, sceneMode, suspendLoad);
                //yield return handle;
                //Debug.Log($"Scene name is {handle.SceneObject.name}");
                //package.UnloadUnusedAssets();
            }
            else
            {
                //更新失败
                Debug.LogError(operation.Error);
            }
        }
        else
        {
            //更新失败
            Debug.LogError(operation.Error);
        }
    }

    /// <summary>
    /// 为aot assembly加载原始metadata， 这个代码放aot或者热更新都行。
    /// 一旦加载后，如果AOT泛型函数对应native实现不存在，则自动替换为解释模式执行
    /// </summary>
    private IEnumerator LoadMetadataForAOTAssemblies()
    {
        List<string> AOTMetaAssemblyFiles = new List<string>()
        {
            "mscorlib.dll.bytes",
            "System.dll.bytes",
            "System.Core.dll.bytes"
        };

        HomologousImageMode mode = HomologousImageMode.SuperSet;
        foreach (var aotDllName in AOTMetaAssemblyFiles)
        {
            using (UnityWebRequest www = UnityWebRequest.Get($"{Application.streamingAssetsPath}/{aotDllName}"))
            {
                yield return www.SendWebRequest();
                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError(www.error);
                }
                else
                {
                    byte[] dllBytes = www.downloadHandler.data;
                    // 加载assembly对应的dll，会自动为它hook。一旦aot泛型函数的native函数不存在，用解释器版本代码
                    LoadImageErrorCode err = RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, mode);
                    Debug.Log($"LoadMetadataForAOTAssembly:{aotDllName}. mode:{mode} ret:{err}");
                }
            }
        }
    }

    IEnumerator Download()
    {
        int downloadingMaxNum = 10;
        int failedTryAgain = 3;
        var package = YooAssets.GetPackage("DefaultPackage");

        //创建资源下载器，下载所有资源
        var downloader = package.CreateResourceDownloader(downloadingMaxNum, failedTryAgain);

        //没有需要下载的资源
        if (downloader.TotalDownloadCount == 0)
        {
            Debug.Log("TotalDownloadCount = 0");

            yield break;
        }

        //需要下载的文件总数和总大小
        int totalDownloadCount = downloader.TotalDownloadCount;
        long totalDownloadBytes = downloader.TotalDownloadBytes;

        //注册回调方法
        downloader.OnDownloadErrorCallback = OnDownloadErrorFunction;
        downloader.OnDownloadProgressCallback = OnDownloadProgressUpdateFunction;
        downloader.OnDownloadOverCallback = OnDownloadOverFunction;
        downloader.OnStartDownloadFileCallback = OnStartDownloadFileFunction;

        //开启下载
        downloader.BeginDownload();
        yield return downloader;

        //检测下载结果
        if (downloader.Status == EOperationStatus.Succeed)
        {
            Debug.Log("Finish");
        }
        else
        {
            Debug.Log("DownLoad Failed");
        }
    }

    private void OnStartDownloadFileFunction(string fileName, long sizeBytes)
    {
        Debug.Log("fileName:" + fileName + ",sizeBytes:" + sizeBytes);
    }

    private void OnDownloadOverFunction(bool isSucceed)
    {
        Debug.Log("isSucceed");
        EventCenter.Instance.EventTrigger("下载完成");
    }

    private void OnDownloadProgressUpdateFunction(int totalDownloadCount, int currentDownloadCount, long totalDownloadBytes, long currentDownloadBytes)
    {
        Debug.Log("totalDownloadCount:" + totalDownloadCount + ",currentDownloadCount" + currentDownloadCount + ",totalDownloadBytes:" + totalDownloadBytes + ",currentDownloadBytes" + currentDownloadBytes);
        double progress = (double)currentDownloadBytes / totalDownloadBytes;
        EventCenter.Instance.EventTrigger("更新下载进度", (float)progress);
    }

    private void OnDownloadErrorFunction(string fileName, string error)
    {
        Debug.Log("DownloadError:" + fileName + ",error:" + error);
    }

    /// <summary>
    /// 资源文件偏移加载解密类
    /// </summary>
    private class FileOffsetDecryption : IDecryptionServices
    {
        /// <summary>
        /// 同步方式获取解密的资源包对象
        /// 注意：加载流对象在资源包对象释放的时候会自动释放
        /// </summary>
        AssetBundle IDecryptionServices.LoadAssetBundle(DecryptFileInfo fileInfo, out Stream managedStream)
        {
            managedStream = null;
            return AssetBundle.LoadFromFile(fileInfo.FileLoadPath, fileInfo.ConentCRC, GetFileOffset());
        }

        /// <summary>
        /// 异步方式获取解密的资源包对象
        /// 注意：加载流对象在资源包对象释放的时候会自动释放
        /// </summary>
        AssetBundleCreateRequest IDecryptionServices.LoadAssetBundleAsync(DecryptFileInfo fileInfo, out Stream managedStream)
        {
            managedStream = null;
            return AssetBundle.LoadFromFileAsync(fileInfo.FileLoadPath, fileInfo.ConentCRC, GetFileOffset());
        }

        private static ulong GetFileOffset()
        {
            return 32;
        }

    }

    /// <summary>
    /// 远端资源地址查询服务类
    /// </summary>
    private class RemoteServices : IRemoteServices
    {
        private readonly string _defaultHostServer;
        private readonly string _fallbackHostServer;

        public RemoteServices(string defaultHostServer, string fallbackHostServer)
        {
            _defaultHostServer = defaultHostServer;
            _fallbackHostServer = fallbackHostServer;
        }
        string IRemoteServices.GetRemoteMainURL(string fileName)
        {
            return $"{_defaultHostServer}/{fileName}";
        }
        string IRemoteServices.GetRemoteFallbackURL(string fileName)
        {
            return $"{_fallbackHostServer}/{fileName}";
        }
    }
}