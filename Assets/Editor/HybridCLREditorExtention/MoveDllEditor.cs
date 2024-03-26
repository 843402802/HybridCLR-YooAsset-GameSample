using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class MoveDllEditor : EditorWindow
{
    [MenuItem("HybridCLR/�ƶ��ȸ��ű���Assets\\AssetsPackage\\Codes��")]
    static void MoveDll()
    {
        //��Ŀ��������
        string projectPath = Application.dataPath.Replace("/Assets", "");
        //Ŀ��ƽ̨·��
        string sourcePath = $"{projectPath}/HybridCLRData/HotUpdateDlls/{EditorUserBuildSettings.activeBuildTarget}/HotUpdate.dll";
        string destinationPath = $"{Application.dataPath}/AssetsPackage/Codes/HotUpdate.dll.txt";
        // ���Դ�ļ��Ƿ����
        if (!File.Exists(sourcePath))
        {
            Debug.LogError("Source file not found: " + sourcePath);
            return;
        }
        // �����ļ�
        File.Copy(sourcePath, destinationPath, true);
        AssetDatabase.Refresh();
        Debug.Log("HotUpdate.dll moved to: " + destinationPath);
    }

    [MenuItem("HybridCLR/�ƶ�����Ԫ����dll��StreamingAssets��")]
    static void MoveAOTMetaAssemblyFiles()
    {
        string projectPath = Application.dataPath.Replace("/Assets", "");
        List<string> AOTMetaAssemblyFiles = new List<string>()
        {
            "mscorlib",
            "System",
            "System.Core"
        };
        foreach (var item in AOTMetaAssemblyFiles)
        {
            string sourcePath = $"{projectPath}/HybridCLRData/AssembliesPostIl2CppStrip/{EditorUserBuildSettings.activeBuildTarget}/{item}.dll";
            string destinationPath = $"{Application.streamingAssetsPath}/{item}.dll.bytes";
            // ���Դ�ļ��Ƿ����
            if (!File.Exists(sourcePath))
            {
                Debug.LogError("Source file not found: " + sourcePath);
                return;
            }
            // �����ļ�
            File.Copy(sourcePath, destinationPath, true);
            AssetDatabase.Refresh();
            Debug.Log($"{item}.dll moved to: " + destinationPath);
        }
    }
}
