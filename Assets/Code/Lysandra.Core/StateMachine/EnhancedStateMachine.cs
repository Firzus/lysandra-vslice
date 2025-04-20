using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Lysandra.Core
{
    /// <summary>
    /// StateMachine avancée avec support de transitions asynchrones, debugging,
    /// transitions conditionnelles, et historique pour les jeux AAA.
    /// </summary>
    /// <typeparam name="T">Type d'entité propriétaire de la StateMachine</typeparam>
    public class EnhancedStateMachine<T> where T : MonoBehaviour
    {
        // Configuration des options de la StateMachine
        [Serializable]
        public struct Config
        {
            public bool EnableHistory;
            public int HistoryCapacity;
            public bool EnableTransitionEvents;
            public bool EnableProfiling;
            public bool EnableVisualDebugging;
        }

        // Contexte pour les transitions
        public class StateTransitionContext
        {
            public IState<T> PreviousState { get; internal set; }
            public IState<T> NextState { get; internal set; }
            public float TransitionTime { get; internal set; }
            public bool WasForced { get; internal set; }
            public object TransitionData { get; set; }
        }

        // Propriétés publiques
        public IState<T> CurrentState { get; private set; }
        public IState<T> PreviousState { get; private set; }
        public float TimeInCurrentState { get; private set; }
        public bool IsTransitioning { get; private set; }
        public T Owner { get; private set; }
        public Config SMConfig { get; private set; }

        // Événements
        public event Action<StateTransitionContext> OnStateTransitionStarted;
        public event Action<StateTransitionContext> OnStateTransitionCompleted;
        public event Action<IState<T>> OnStateEnter;
        public event Action<IState<T>> OnStateExit;

        // Stockage des états disponibles
        private Dictionary<Type, IState<T>> _availableStates = new Dictionary<Type, IState<T>>();
        private Dictionary<string, IState<T>> _statesByName = new Dictionary<string, IState<T>>();
        private List<StateTransition> _transitionHistory = new List<StateTransition>();
        private Stopwatch _stateTimer = new Stopwatch();
        private CancellationTokenSource _transitionCancellationSource;
        private bool _isInitialized;

        // Structure pour garder l'historique des transitions
        private struct StateTransition
        {
            public Type FromState;
            public Type ToState;
            public float TransitionDuration;
            public DateTime Timestamp;
        }

        /// <summary>
        /// Initialise la StateMachine avec son propriétaire et configuration
        /// </summary>
        /// <param name="owner">L'entité propriétaire de la FSM</param>
        /// <param name="config">Configuration optionnelle</param>
        public EnhancedStateMachine(T owner, Config? config = null)
        {
            Owner = owner;
            SMConfig = config ?? new Config
            {
                EnableHistory = true,
                HistoryCapacity = 30,
                EnableTransitionEvents = true,
                EnableProfiling = true,
                EnableVisualDebugging = Debug.isDebugBuild
            };
        }

        /// <summary>
        /// Enregistre un nouvel état dans la machine
        /// </summary>
        /// <typeparam name="TState">Type de l'état à enregistrer</typeparam>
        /// <returns>Instance de l'état créé</returns>
        public TState RegisterState<TState>() where TState : IState<T>, new()
        {
            Type stateType = typeof(TState);

            if (_availableStates.ContainsKey(stateType))
            {
                Debug.LogWarning($"[StateMachine] État déjà enregistré: {stateType.Name}");
                return (TState)_availableStates[stateType];
            }

            var state = new TState();
            state.Initialize(Owner, this);

            _availableStates[stateType] = state;
            _statesByName[state.GetName()] = state;

            return state;
        }

        /// <summary>
        /// Démarre la StateMachine avec l'état initial spécifié
        /// </summary>
        /// <typeparam name="TState">Type de l'état initial</typeparam>
        public void SetInitialState<TState>() where TState : IState<T>
        {
            if (CurrentState != null)
            {
                Debug.LogWarning("[StateMachine] État initial déjà défini, utiliser ChangeState() pour changer d'état.");
                return;
            }

            Type stateType = typeof(TState);

            if (!_availableStates.TryGetValue(stateType, out var state))
            {
                Debug.LogError($"[StateMachine] État non enregistré: {stateType.Name}");
                return;
            }

            CurrentState = state;
            _stateTimer.Restart();
            CurrentState.Enter(default);

            if (SMConfig.EnableTransitionEvents)
            {
                OnStateEnter?.Invoke(CurrentState);
            }

            _isInitialized = true;

            if (SMConfig.EnableVisualDebugging)
            {
                Debug.Log($"[StateMachine] État initial défini: {CurrentState.GetName()}");
            }
        }

        /// <summary>
        /// Change l'état courant de façon synchrone
        /// </summary>
        /// <typeparam name="TState">Type du nouvel état</typeparam>
        /// <param name="transitionData">Données optionnelles pour la transition</param>
        public void ChangeState<TState>(object transitionData = null) where TState : IState<T>
        {
            Type stateType = typeof(TState);

            if (!_availableStates.TryGetValue(stateType, out var nextState))
            {
                Debug.LogError($"[StateMachine] État non enregistré: {stateType.Name}");
                return;
            }

            if (IsTransitioning)
            {
                // Annulation de toute transition en cours
                _transitionCancellationSource?.Cancel();
                _transitionCancellationSource = null;
                IsTransitioning = false;
            }

            PerformStateChange(nextState, transitionData, false);
        }

        /// <summary>
        /// Change l'état courant de façon asynchrone avec gestion des transitions
        /// </summary>
        /// <typeparam name="TState">Type du nouvel état</typeparam>
        /// <param name="transitionData">Données optionnelles pour la transition</param>
        /// <param name="forceInstant">Force une transition instantanée</param>
        /// <returns>Task complétée une fois la transition terminée</returns>
        public async Task ChangeStateAsync<TState>(object transitionData = null, bool forceInstant = false)
            where TState : IState<T>
        {
            Type stateType = typeof(TState);

            if (!_availableStates.TryGetValue(stateType, out var nextState))
            {
                Debug.LogError($"[StateMachine] État non enregistré: {stateType.Name}");
                return;
            }

            if (IsTransitioning && !forceInstant)
            {
                // Annulation de toute transition en cours
                _transitionCancellationSource?.Cancel();
            }

            _transitionCancellationSource = new CancellationTokenSource();
            var cancellationToken = _transitionCancellationSource.Token;
            IsTransitioning = true;

            var context = new StateTransitionContext
            {
                PreviousState = CurrentState,
                NextState = nextState,
                TransitionTime = 0f,
                WasForced = forceInstant,
                TransitionData = transitionData
            };

            if (SMConfig.EnableTransitionEvents)
            {
                OnStateTransitionStarted?.Invoke(context);
            }

            try
            {
                if (!forceInstant && CurrentState != null)
                {
                    var exitDuration = CurrentState.GetExitDuration(nextState);

                    if (exitDuration > 0)
                    {
                        var startTime = Time.time;
                        float progress = 0f;

                        while (progress < 1f && !cancellationToken.IsCancellationRequested)
                        {
                            progress = (Time.time - startTime) / exitDuration;
                            context.TransitionTime = progress * exitDuration;
                            await Task.Yield();
                        }

                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }
                    }
                }

                PerformStateChange(nextState, transitionData, forceInstant);

                if (!forceInstant)
                {
                    var enterDuration = nextState.GetEnterDuration(CurrentState);

                    if (enterDuration > 0)
                    {
                        var startTime = Time.time;
                        float progress = 0f;

                        while (progress < 1f && !cancellationToken.IsCancellationRequested)
                        {
                            progress = (Time.time - startTime) / enterDuration;
                            context.TransitionTime = progress * enterDuration;
                            await Task.Yield();
                        }

                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }
                    }
                }
            }
            finally
            {
                _transitionCancellationSource = null;
                IsTransitioning = false;

                if (SMConfig.EnableTransitionEvents)
                {
                    OnStateTransitionCompleted?.Invoke(context);
                }
            }
        }

        /// <summary>
        /// Mise à jour de la StateMachine - à appeler dans Update() du owner
        /// </summary>
        public void Tick()
        {
            if (!_isInitialized || CurrentState == null)
                return;

            TimeInCurrentState = (float)_stateTimer.Elapsed.TotalSeconds;
            CurrentState.Tick();
        }

        /// <summary>
        /// Mise à jour de la physique - à appeler dans FixedUpdate() du owner
        /// </summary>
        public void FixedTick()
        {
            if (!_isInitialized || CurrentState == null)
                return;

            CurrentState.FixedTick();
        }

        /// <summary>
        /// Récupère l'état de type spécifié s'il est enregistré
        /// </summary>
        public TState GetState<TState>() where TState : IState<T>
        {
            if (_availableStates.TryGetValue(typeof(TState), out var state))
            {
                return (TState)state;
            }

            return default;
        }

        /// <summary>
        /// Retourne l'historique des transitions récentes
        /// </summary>
        public IReadOnlyList<(string FromState, string ToState, float Duration, DateTime When)> GetTransitionHistory()
        {
            if (!SMConfig.EnableHistory)
                return Array.Empty<(string, string, float, DateTime)>();

            var result = new List<(string, string, float, DateTime)>(_transitionHistory.Count);

            foreach (var transition in _transitionHistory)
            {
                result.Add((
                    transition.FromState?.Name ?? "None",
                    transition.ToState.Name,
                    transition.TransitionDuration,
                    transition.Timestamp
                ));
            }

            return result;
        }

        /// <summary>
        /// Vérifie si l'état courant est du type spécifié
        /// </summary>
        public bool IsInState<TState>() where TState : IState<T>
        {
            return CurrentState != null && CurrentState.GetType() == typeof(TState);
        }

        /// <summary>
        /// Vérifie si un état est disponible pour cette StateMachine
        /// </summary>
        public bool HasState<TState>() where TState : IState<T>
        {
            return _availableStates.ContainsKey(typeof(TState));
        }

        /// <summary>
        /// Exécute la transition effective entre états
        /// </summary>
        private void PerformStateChange(IState<T> nextState, object transitionData, bool forced)
        {
            if (CurrentState == nextState && !forced)
                return;

            var previousState = CurrentState;
            float transitionStartTime = Time.time;

            // Exit de l'état courant
            if (CurrentState != null)
            {
                if (SMConfig.EnableTransitionEvents)
                {
                    OnStateExit?.Invoke(CurrentState);
                }

                CurrentState.Exit(nextState);
            }

            // Mise à jour des références
            PreviousState = CurrentState;
            CurrentState = nextState;

            // Enregistrement dans l'historique
            if (SMConfig.EnableHistory && PreviousState != null)
            {
                var transition = new StateTransition
                {
                    FromState = PreviousState.GetType(),
                    ToState = CurrentState.GetType(),
                    TransitionDuration = Time.time - transitionStartTime,
                    Timestamp = DateTime.UtcNow
                };

                _transitionHistory.Add(transition);

                if (_transitionHistory.Count > SMConfig.HistoryCapacity)
                {
                    _transitionHistory.RemoveAt(0);
                }
            }

            // Redémarrage du timer d'état
            _stateTimer.Restart();
            TimeInCurrentState = 0f;

            // Enter dans le nouvel état
            CurrentState.Enter(transitionData);

            if (SMConfig.EnableTransitionEvents)
            {
                OnStateEnter?.Invoke(CurrentState);
            }

            if (SMConfig.EnableVisualDebugging)
            {
                string fromState = PreviousState != null ? PreviousState.GetName() : "None";
                Debug.Log($"[StateMachine] Transition: {fromState} -> {CurrentState.GetName()}");
            }
        }
    }
}