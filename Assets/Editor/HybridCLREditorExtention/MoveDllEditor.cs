using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class MoveDllEditor : EditorWindow
{
    [MenuItem("HybridCLR/移动热更脚本到Assets\\AssetsPackage\\Codes中")]
    static void MoveDll()
    {
        //项目工程名称
        string projectPath = Application.dataPath.Replace("/Assets", "");
        //目标平台路径
        string sourcePath = $"{projectPath}/HybridCLRData/HotUpdateDlls/{EditorUserBuildSettings.activeBuildTarget}/HotUpdate.dll";
        string destinationPath = $"{Application.dataPath}/AssetsPackage/Codes/HotUpdate.dll.txt";
        // 检查源文件是否存在
        if (!File.Exists(sourcePath))
        {
            Debug.LogError("Source file not found: " + sourcePath);
            return;
        }
        // 复制文件
        File.Copy(sourcePath, destinationPath, true);
        AssetDatabase.Refresh();
        Debug.Log("HotUpdate.dll moved to: " + destinationPath);
    }

    [MenuItem("HybridCLR/移动补充元数据dll至StreamingAssets中")]
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
            // 检查源文件是否存在
            if (!File.Exists(sourcePath))
            {
                Debug.LogError("Source file not found: " + sourcePath);
                return;
            }
            // 复制文件
            File.Copy(sourcePath, destinationPath, true);
            AssetDatabase.Refresh();
            Debug.Log($"{item}.dll moved to: " + destinationPath);
        }
    }
}
