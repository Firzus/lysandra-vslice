using Project.Tools.Debugging.Runtime;
using UnityEditor;

[CustomEditor(typeof(DebugOverlay))]
public class DebugOverlayEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var overlay = (DebugOverlay)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("État courant de la FSM", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(overlay.CurrentFsmStateName);
    }
}
