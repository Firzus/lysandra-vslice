using System;
using UnityEngine;
using Project.Core.FSM;

namespace Project.Tools.Examples
{
    /// <summary>
    /// Exemple d'utilisation de la FSM sur un MonoBehaviour
    /// </summary>
    public class PlayerFSMExample : MonoBehaviour
    {
        [Header("Exemple FSM - Utilisation")]
        [Tooltip("Appuyez sur [Espace] pour passer à l'état Run, sur [I] pour revenir à Idle. L'état courant et l'historique sont visibles dans la fenêtre FSM Viewer (menu Tools/Debug/FSM Viewer).")]
        [SerializeField, TextArea(2, 4)]
        private string info = "Appuyez sur [Espace] pour passer à l'état Run, sur [I] pour revenir à Idle.\nL'état courant et l'historique sont visibles dans la fenêtre FSM Viewer (menu Tools/Debug/FSM Viewer).";

        private StateMachine<IState> _fsm;
        private IdleState _idle;
        private RunState _run;

        private void Awake()
        {
            _idle = new IdleState();
            _run = new RunState();
            _fsm = new StateMachine<IState>();
            _fsm.SetState(_idle, "Init");
        }

        private void Update()
        {
            // Changement d'état fictif pour l'exemple
            if (Input.GetKeyDown(KeyCode.Space))
                _fsm.SetState(_run, "Space");
            if (Input.GetKeyDown(KeyCode.I))
                _fsm.SetState(_idle, "I");
            _fsm.Tick();
        }

        // États exemples
        private class IdleState : IState
        {
            public string Name => "Idle";
            public void OnEnter() { Debug.Log("Enter Idle"); }
            public void OnExit() { Debug.Log("Exit Idle"); }
            public void Tick() { }
        }
        private class RunState : IState
        {
            public string Name => "Run";
            public void OnEnter() { Debug.Log("Enter Run"); }
            public void OnExit() { Debug.Log("Exit Run"); }
            public void Tick() { }
        }

        // Expose la FSM pour le debug
        public StateMachine<IState> GetFSM() => _fsm;
    }
}
