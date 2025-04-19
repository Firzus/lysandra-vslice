#if UNITY_EDITOR

using UnityEditor;
public static class InitFolders
{
    [MenuItem("Tools/Lysandra/Init Base Folders")]
    public static void Create()
    {
        string[] paths = {
      "Assets/Code", "Assets/Data", "Assets/Art",
      "Assets/Audio", "Assets/Prefabs", "Assets/Scenes", "Assets/Tests"
    };
        foreach (var p in paths)
            if (!AssetDatabase.IsValidFolder(p))
                AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(p) ?? "Assets",
                                           System.IO.Path.GetFileName(p));
        AssetDatabase.Refresh();
    }
}

#endif
