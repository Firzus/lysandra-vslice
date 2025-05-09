using Project.Tools.Debugging.Runtime;
using UnityEditor;

[CustomEditor(typeof(DebugOverlay))]
public class DebugOverlayEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Ce composant affiche uniquement les FPS et la mémoire en runtime.", MessageType.Info);
    }
}
