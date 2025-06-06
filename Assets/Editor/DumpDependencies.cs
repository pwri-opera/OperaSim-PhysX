// Assets/Editor/DumpDependencies.cs
using UnityEditor;
using UnityEngine;
using System.IO;

public static class DumpDependencies
{
    // 選択アセットの依存ファイルを Console に出力
    [MenuItem("Assets/Dependency/Dump to Console")]
    private static void DumpConsole()
    {
        foreach (var obj in Selection.objects)
            Dump(obj, null);                    // 第 2 引数 null = 標準出力
    }

    // コマンドライン用：引数1=アセットパス, 引数2=出力ファイル
    // 例）Unity.exe -batchmode -quit -executeMethod DumpDependencies.Run "Assets/Prefabs/Foo.prefab" deps.txt
    public static void Run()
    {
        var args = System.Environment.GetCommandLineArgs();
        if (args.Length < 3) { Debug.LogError("Need asset path"); return; }
        var asset = args[args.Length-2];
        var outfile = args[args.Length-1];
        Dump(AssetDatabase.LoadMainAssetAtPath(asset), outfile);
    }

    private static void Dump(Object root, string outPath)
    {
        var rootPath = AssetDatabase.GetAssetPath(root);
        var deps = AssetDatabase.GetDependencies(rootPath, true);   // true = 間接依存も全部
        if (outPath == null)
        {
            Debug.Log($"=== Dependencies of {rootPath} ===");
            foreach (var d in deps) Debug.Log(d);
        }
        else
        {
            File.WriteAllLines(outPath, deps);
            Debug.Log($"Saved {deps.Length} lines -> {outPath}");
        }
    }
}
