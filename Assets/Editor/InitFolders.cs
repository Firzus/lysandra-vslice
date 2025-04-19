#if UNITY_EDITOR

using UnityEditor;

public static class InitFolders
{
    [MenuItem("Tools/Lysandra/Create Base Folders")]
    public static void Create()
    {
        string[] paths = {
       "Assets/Code/Core", "Assets/Code/Movement", "Assets/Code/Combat",
       "Assets/Data/Settings", "Assets/Prefabs", "Assets/Scenes/Persistent",
       "Assets/Tests/EditMode", "Assets/Tests/PlayMode"
     };
        foreach (var p in paths) if (!AssetDatabase.IsValidFolder(p))
                AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(p)!, System.IO.Path.GetFileName(p));
        AssetDatabase.Refresh();
    }
}

#endif