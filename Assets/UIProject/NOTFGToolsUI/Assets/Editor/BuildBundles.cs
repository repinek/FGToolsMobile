using System.IO;
using UnityEditor;
using UnityEngine;

public class BuildBundles : MonoBehaviour
{
    [MenuItem("Assets/Build AssetBundles")]
    static void Build()
    {
        var output = "Assets/AssetBundles";
        if(!Directory.Exists(output))
            Directory.CreateDirectory(output);
        BuildPipeline.BuildAssetBundles(output, BuildAssetBundleOptions.None, BuildTarget.Android);
    }
}
