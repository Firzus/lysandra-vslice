using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Project.Core.FSM;

namespace Project.Tools.Debugging.Editor
{
    /// <summary>
    /// Fenêtre d'éditeur extensible pour visualiser différents panels de debug (FSM, mémoire, input, etc.)
    /// </summary>
    public class DebugViewerEditor : EditorWindow
    {
        private enum DebugPanel { FSM, /* Ajoute ici d'autres panels à l'avenir */ }
        private DebugPanel _selectedPanel = DebugPanel.FSM;

        // FSM Panel
        private Vector2 _scrollPosition;
        private int _selectedFSMIndex = 0;
        private string[] _fsmNames = Array.Empty<string>();
        private List<object> _activeFSMs = new List<object>();
        private bool _autoRefresh = true;

        [MenuItem("Tools/Debug/Debug Viewer")]
        public static void ShowWindow()
        {
            var window = GetWindow<DebugViewerEditor>("Debug Viewer");
            window.minSize = new Vector2(500, 350);
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            RefreshFSMList();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (_autoRefresh && _selectedPanel == DebugPanel.FSM)
                Repaint();
        }

        private void OnGUI()
        {
            // Onglets panels debug
            _selectedPanel = (DebugPanel)GUILayout.Toolbar((int)_selectedPanel, new[] { "FSM" /*, "Mémoire", "Input", etc. */ });
            EditorGUILayout.Space();

            switch (_selectedPanel)
            {
                case DebugPanel.FSM:
                    DrawFSMPanel();
                    break;
                    // case DebugPanel.Memory:
                    //     DrawMemoryPanel();
                    //     break;
                    // case DebugPanel.Input:
                    //     DrawInputPanel();
                    //     break;
            }
        }

        // --- FSM PANEL ---
        private void DrawFSMPanel()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Rafraîchir", EditorStyles.toolbarButton))
                RefreshFSMList();
            _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto Refresh", EditorStyles.toolbarButton);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (_fsmNames.Length == 0)
            {
                EditorGUILayout.HelpBox("Aucune FSM active détectée dans la scène.", MessageType.Info);
                return;
            }

            _selectedFSMIndex = EditorGUILayout.Popup("FSM à inspecter", _selectedFSMIndex, _fsmNames);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            DrawFSMDetails(_selectedFSMIndex);
            EditorGUILayout.EndScrollView();
        }

        private void RefreshFSMList()
        {
            var allBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            var fsms = new List<StateMachine<IState>>();
            var names = new List<string>();

            foreach (var behaviour in allBehaviours)
            {
                var method = behaviour.GetType().GetMethod("GetFSM");
                if (method != null && method.ReturnType == typeof(StateMachine<IState>))
                {
                    if (method.Invoke(behaviour, null) is StateMachine<IState> fsm)
                    {
                        fsms.Add(fsm);
                        names.Add($"{behaviour.GetType().Name} ({behaviour.gameObject.name})");
                    }
                }
            }
            _activeFSMs = fsms.Cast<object>().ToList();
            _fsmNames = names.ToArray();
            if (_selectedFSMIndex >= _fsmNames.Length) _selectedFSMIndex = 0;
        }

        private void DrawFSMDetails(int index)
        {
            if (index < 0 || index >= _activeFSMs.Count)
            {
                EditorGUILayout.LabelField("Aucune FSM sélectionnée.");
                return;
            }
            var fsm = _activeFSMs[index] as StateMachine<IState>;
            if (fsm == null)
            {
                EditorGUILayout.LabelField("Erreur d'accès à la FSM.");
                return;
            }
            EditorGUILayout.LabelField("État courant :", fsm.CurrentState?.Name ?? "<None>", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Historique des transitions :", EditorStyles.boldLabel);
            if (fsm.History.Count == 0)
            {
                EditorGUILayout.LabelField("(Aucune transition)");
            }
            else
            {
                foreach (var record in fsm.History)
                {
                    EditorGUILayout.LabelField($"[{record.Timestamp:HH:mm:ss}] {record.From} → {record.To} (trigger: {record.Trigger})");
                }
            }
        }

        // --- Ajoute ici d'autres panels de debug (ex: mémoire, input, etc.) ---
    }
}
