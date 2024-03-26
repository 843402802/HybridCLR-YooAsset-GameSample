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
/// ��������
/// 1.������Դ����YooAsset��Դ��ܽ�������
///     1����Դ�ļ���ab����
///     2���ȸ���dll
/// </summary>
public class LaunchManager : MonoBehaviour
{
    /// <summary>
    /// ��Դϵͳ����ģʽ
    /// </summary>
    public EPlayMode PlayMode = EPlayMode.EditorSimulateMode;
    void Awake()
    {
#if !UNITY_EDITOR
         //�����Ĭ��Զ������ģʽ
         PlayMode = EPlayMode.HostPlayMode;
#endif
        Application.targetFrameRate = 60;
        Application.runInBackground = true;
        Screen.SetResolution(600, 800, false);
        DontDestroyOnLoad(this.gameObject);
    }

    IEnumerator Start()
    {
        // ��ʼ���¼�ϵͳ
        //UniEvent.Initalize();
        //1.��ʼ��
        Debug.Log("1. ��ʼ��");
        // ��ʼ����Դϵͳ
        YooAssets.Initialize();

        // ����Ĭ�ϵ���Դ��
        var package = YooAssets.CreatePackage("DefaultPackage");

        // ���ø���Դ��ΪĬ�ϵ���Դ��������ʹ��YooAssets��ؼ��ؽӿڼ��ظ���Դ�����ݡ�
        YooAssets.SetDefaultPackage(package);

        //2.��Դϵͳ������ģʽ
        Debug.Log("2. ��Դϵͳ������ģʽ");
        if (PlayMode == EPlayMode.EditorSimulateMode)
        {
            var initParameters = new EditorSimulateModeParameters();//EditorSimulateModeParameters�̳���InitializeParameters
            string simulateManifestFilePath = EditorSimulateModeHelper.SimulateBuild(EDefaultBuildPipeline.BuiltinBuildPipeline.ToString(), "DefaultPackage");
            initParameters.SimulateManifestFilePath = simulateManifestFilePath;
            yield return package.InitializeAsync(initParameters);
        }
        else if (PlayMode == EPlayMode.OfflinePlayMode)
        {
            var initParameters = new OfflinePlayModeParameters();//OfflinePlayModeParameters�̳���InitializeParameters
            initParameters.DecryptionServices = new FileOffsetDecryption();//��Ҫ�������
            yield return package.InitializeAsync(initParameters);
        }
        else if (PlayMode == EPlayMode.HostPlayMode)
        {
            // ע�⣺GameQueryServices.cs ̫��ս���Ľű��࣬��ϸ��StreamingAssetsHelper.cs
            string defaultHostServer = "http://127.0.0.1/CDN/Bundle";
            string fallbackHostServer = "http://127.0.0.1/CDN/Bundle";
            var initParameters = new HostPlayModeParameters();//HostPlayModeParameters�̳���InitializeParameters
            initParameters.BuildinQueryServices = new GameQueryServices();//������Դ��ѯ����ӿ�
            initParameters.DecryptionServices = new FileOffsetDecryption();//�����Դ���ڹ�����ʱ���м��ܣ���Ҫ�ṩʵ��IDecryptionServices�ӿڵ�ʵ���ࡣ
            initParameters.RemoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);//Զ�˷�������ѯ����ӿ�
            var initOperation = package.InitializeAsync(initParameters);
            yield return initOperation;

            if (initOperation.Status == EOperationStatus.Succeed)
            {
                Debug.Log("��Դ����ʼ���ɹ���");
            }
            else
            {
                Debug.LogError($"��Դ����ʼ��ʧ�ܣ�{initOperation.Error}");
            }
        }
        else if (PlayMode == EPlayMode.WebPlayMode)
        {
            string defaultHostServer = "http://127.0.0.1/CDN/WebGL/V1.0";
            string fallbackHostServer = "http://127.0.0.1/CDN/WebGL/V1.0";
            var initParameters = new WebPlayModeParameters();//WebPlayModeParameters�̳���InitializeParameters
            initParameters.BuildinQueryServices = new GameQueryServices();
            initParameters.RemoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
            var initOperation = package.InitializeAsync(initParameters);
            yield return initOperation;

            if (initOperation.Status == EOperationStatus.Succeed)
            {
                Debug.Log("��Դ����ʼ���ɹ���");
            }
            else
            {
                Debug.LogError($"��Դ����ʼ��ʧ�ܣ�{initOperation.Error}");
            }
        }

        //3.��ȡ��Դ�汾��UpdatePackageVersionAsync
        Debug.Log("3. ��ȡ��Դ�汾");
        var operation = package.UpdatePackageVersionAsync();
        yield return operation;

        if (operation.Status == EOperationStatus.Succeed)
        {

            string packageVersion = operation.PackageVersion;
            Debug.Log($"Updated package Version : {packageVersion}");

            //4.������Դ�嵥��������������ģʽ���ڻ�ȡ����Դ�汾��֮�󣬾Ϳ��Ը�����Դ�嵥�ˣ�UpdatePackageManifestAsync
            //��������ģʽ
            //ͨ��������嵥�汾�����ȱȶԵ�ǰ�����嵥�İ汾�������ͬ��ֱ�ӷ��سɹ�������в���ʹӻ�����ȥ����ƥ����嵥����������ﲻ���ڣ���ȥԶ�����ز����浽ɳ���������ɳ����ƥ����嵥�ļ���
            Debug.Log("4. ������Դ�嵥");
            bool savePackageVersion = true;
            var operation2 = package.UpdatePackageManifestAsync(packageVersion, savePackageVersion);
            yield return operation2;

            if (operation2.Status == EOperationStatus.Succeed)
            {
                //5.��Դ������
                Debug.Log("5. ��Դ������");
                yield return Download();
#if UNITY_EDITOR
                Debug.Log("6.�༭��ģʽ�£�ֱ�Ӳ��ҳ���");
                // �༭��ģʽ�£�ֱ�ӽ���
                Assembly hotUpdateAss = System.AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "HotUpdate");
                Type type = hotUpdateAss.GetType("HotUpdateEntry");
                type.GetMethod("EntryGame").Invoke(null, null);
#else
                //6.�����ȸ�����,YooAsset2.0�汾rawfile�ļ���Ҫ��RawFile���ߵ������,������LoadRawFileAsyncʱ��ᱨ��
                //���ȸ�����ĳ�txt��׺��textasset���Ͷ�ȡ�ɽ��
                Debug.Log("6.���ز���Ԫ�������ȸ�����");
                //����Ԫ����
                yield return LoadMetadataForAOTAssemblies();
                //�ȸ�����
                var codeHandle = package.LoadAssetAsync<TextAsset>("HotUpdate.dll");
                yield return codeHandle;
                TextAsset textAsset = codeHandle.AssetObject as UnityEngine.TextAsset;
                Assembly hotUpdateAss = Assembly.Load(textAsset.bytes);
                Type type = hotUpdateAss.GetType("HotUpdateEntry");
                type.GetMethod("EntryGame").Invoke(null, null);
#endif
                ////7.���س��������ÿ�Ѱַ���ܣ�Enable Addressable���󣬲���дȫ·����ֱ��д��Դ���Ƽ���
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
                //����ʧ��
                Debug.LogError(operation.Error);
            }
        }
        else
        {
            //����ʧ��
            Debug.LogError(operation.Error);
        }
    }

    /// <summary>
    /// Ϊaot assembly����ԭʼmetadata�� ��������aot�����ȸ��¶��С�
    /// һ�����غ����AOT���ͺ�����Ӧnativeʵ�ֲ����ڣ����Զ��滻Ϊ����ģʽִ��
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
                    // ����assembly��Ӧ��dll�����Զ�Ϊ��hook��һ��aot���ͺ�����native���������ڣ��ý������汾����
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

        //������Դ������������������Դ
        var downloader = package.CreateResourceDownloader(downloadingMaxNum, failedTryAgain);

        //û����Ҫ���ص���Դ
        if (downloader.TotalDownloadCount == 0)
        {
            Debug.Log("TotalDownloadCount = 0");

            yield break;
        }

        //��Ҫ���ص��ļ��������ܴ�С
        int totalDownloadCount = downloader.TotalDownloadCount;
        long totalDownloadBytes = downloader.TotalDownloadBytes;

        //ע��ص�����
        downloader.OnDownloadErrorCallback = OnDownloadErrorFunction;
        downloader.OnDownloadProgressCallback = OnDownloadProgressUpdateFunction;
        downloader.OnDownloadOverCallback = OnDownloadOverFunction;
        downloader.OnStartDownloadFileCallback = OnStartDownloadFileFunction;

        //��������
        downloader.BeginDownload();
        yield return downloader;

        //������ؽ��
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
        EventCenter.Instance.EventTrigger("�������");
    }

    private void OnDownloadProgressUpdateFunction(int totalDownloadCount, int currentDownloadCount, long totalDownloadBytes, long currentDownloadBytes)
    {
        Debug.Log("totalDownloadCount:" + totalDownloadCount + ",currentDownloadCount" + currentDownloadCount + ",totalDownloadBytes:" + totalDownloadBytes + ",currentDownloadBytes" + currentDownloadBytes);
        double progress = (double)currentDownloadBytes / totalDownloadBytes;
        EventCenter.Instance.EventTrigger("�������ؽ���", (float)progress);
    }

    private void OnDownloadErrorFunction(string fileName, string error)
    {
        Debug.Log("DownloadError:" + fileName + ",error:" + error);
    }

    /// <summary>
    /// ��Դ�ļ�ƫ�Ƽ��ؽ�����
    /// </summary>
    private class FileOffsetDecryption : IDecryptionServices
    {
        /// <summary>
        /// ͬ����ʽ��ȡ���ܵ���Դ������
        /// ע�⣺��������������Դ�������ͷŵ�ʱ����Զ��ͷ�
        /// </summary>
        AssetBundle IDecryptionServices.LoadAssetBundle(DecryptFileInfo fileInfo, out Stream managedStream)
        {
            managedStream = null;
            return AssetBundle.LoadFromFile(fileInfo.FileLoadPath, fileInfo.ConentCRC, GetFileOffset());
        }

        /// <summary>
        /// �첽��ʽ��ȡ���ܵ���Դ������
        /// ע�⣺��������������Դ�������ͷŵ�ʱ����Զ��ͷ�
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
    /// Զ����Դ��ַ��ѯ������
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