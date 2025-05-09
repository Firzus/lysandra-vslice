using System;
using System.Collections.Generic;

namespace Project.Core.FSM
{
    /// <summary>
    /// Interface de base pour un état de FSM
    /// </summary>
    public interface IState
    {
        string Name { get; }
        void OnEnter();
        void OnExit();
        void Tick();
    }

    /// <summary>
    /// Classe générique de FSM avec historique des transitions
    /// </summary>
    public class StateMachine<TState> where TState : IState
    {
        public TState CurrentState { get; private set; }
        public IReadOnlyList<TransitionRecord> History => _history;
        private List<TransitionRecord> _history = new List<TransitionRecord>();

        public void SetState(TState newState, string trigger = "Manual")
        {
            if (CurrentState != null)
                CurrentState.OnExit();
            var prev = CurrentState;
            CurrentState = newState;
            CurrentState.OnEnter();
            _history.Add(new TransitionRecord
            {
                Timestamp = DateTime.Now,
                From = prev?.Name ?? "<None>",
                To = newState.Name,
                Trigger = trigger
            });
            if (_history.Count > 32) _history.RemoveAt(0); // Limite l'historique
        }

        public void Tick()
        {
            CurrentState?.Tick();
        }

        public struct TransitionRecord
        {
            public DateTime Timestamp;
            public string From;
            public string To;
            public string Trigger;
        }
    }
}
