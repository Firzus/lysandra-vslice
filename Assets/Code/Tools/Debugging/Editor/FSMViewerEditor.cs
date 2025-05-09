using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Project.Core.FSM;

namespace Project.Tools.Debugging.Editor
{
    /// <summary>
    /// Fenêtre d'éditeur pour visualiser les FSM (Finite State Machines) en temps réel
    /// Affiche l'état courant, l'historique des transitions et permet l'inspection des FSM actives
    /// </summary>
    public class FSMViewerEditor : EditorWindow
    {
        private Vector2 _scrollPosition;
        private int _selectedFSMIndex = 0;
        private string[] _fsmNames = Array.Empty<string>();
        private List<object> _activeFSMs = new List<object>(); // À remplacer par le vrai type StateMachine<T>
        private bool _autoRefresh = true;

        [MenuItem("Tools/Debug/FSM Viewer")]
        public static void ShowWindow()
        {
            var window = GetWindow<FSMViewerEditor>("FSM Viewer");
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
            if (_autoRefresh)
                Repaint();
        }

        private void RefreshFSMList()
        {
            // Recherche tous les MonoBehaviours de la scène qui exposent une méthode GetFSM()
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

        private void OnGUI()
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
    }
}
