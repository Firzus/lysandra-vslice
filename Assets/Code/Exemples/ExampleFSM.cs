using UnityEngine;
using Project.Core.FSM;
using Project.Core.Signals;
using Project.Core.Services;

namespace Project.Tools.Examples
{
    /// <summary>
    /// Exemple d'utilisation de la FSM sur un MonoBehaviour
    /// </summary>
    public class PlayerFSMExample : MonoBehaviour
    {
        private StateMachine<IState> _fsm;
        private IdleState _idle;
        private RunState _run;
        private SignalBus _signalBus;

        private void Awake()
        {
            _idle = new IdleState();
            _run = new RunState();
            _fsm = new StateMachine<IState>();
            _fsm.SetState(_idle, "Init");
            // Récupère le SignalBus via le ServiceLocator (ou singleton)
            _signalBus = ServiceLocator.Instance.Get<SignalBus>();
        }

        private void Update()
        {
            // Changement d'état fictif pour l'exemple
            if (Input.GetKeyDown(KeyCode.Space))
                ChangeState(_run, "Space");
            if (Input.GetKeyDown(KeyCode.I))
                ChangeState(_idle, "I");
            _fsm.Tick();
        }

        private void ChangeState(IState newState, string trigger)
        {
            var prev = _fsm.CurrentState?.Name ?? "<None>";
            _fsm.SetState(newState, trigger);
            // Émet un signal PlayerStateChanged à chaque transition
            _signalBus?.Emit(new CommonSignals.PlayerStateChanged
            {
                PreviousState = prev,
                CurrentState = newState.Name,
                StateTime = Time.time
            });
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
