using UnityEngine;

namespace Lysandra.Core
{
    /// <summary>
    /// Classe de base pour tous les états, fournissant une implémentation par défaut
    /// de l'interface IState avec fonctionnalités avancées pour production AAA.
    /// </summary>
    /// <typeparam name="T">Type d'entité propriétaire de l'état</typeparam>
    public abstract class BaseState<T> : IState<T> where T : MonoBehaviour
    {
        // Références protégées accessibles aux classes dérivées
        protected T Owner { get; private set; }
        protected EnhancedStateMachine<T> StateMachine { get; private set; }
        
        // Propriétés de timing et debugging
        protected float TimeInState => StateMachine?.TimeInCurrentState ?? 0f;
        protected bool IsActive => StateMachine != null && StateMachine.CurrentState == this;
        
        // Configuration de l'état via SO
        protected ScriptableObject Config { get; private set; }

        // Propriétés virtuelles pour la configuration des transitions
        protected virtual float DefaultEnterDuration => 0f;
        protected virtual float DefaultExitDuration => 0f;
        protected virtual string StateName => GetType().Name;
        protected virtual bool AllowsInterruption => true;

        // Méthode d'initialisation appelée lors de l'enregistrement de l'état
        public virtual void Initialize(T owner, EnhancedStateMachine<T> stateMachine)
        {
            Owner = owner;
            StateMachine = stateMachine;
            OnInitialize();
        }

        // Hook pour initialisation personnalisée
        protected virtual void OnInitialize() { }

        // Implémentation de IState
        public virtual void Enter(object transitionData)
        {
            if (Debug.isDebugBuild)
            {
                Debug.Log($"[{Owner.name}][{GetName()}] Enter avec données: {transitionData}");
            }
        }

        public virtual void Tick() { }
        
        public virtual void FixedTick() { }

        public virtual void Exit(IState<T> nextState)
        {
            if (Debug.isDebugBuild)
            {
                Debug.Log($"[{Owner.name}][{GetName()}] Exit vers {nextState?.GetName() ?? "null"}");
            }
        }

        public virtual string GetName() => StateName;

        public virtual float GetEnterDuration(IState<T> previousState) => DefaultEnterDuration;

        public virtual float GetExitDuration(IState<T> nextState) => DefaultExitDuration;

        public virtual bool CanTransitionTo(IState<T> nextState) => true;

        public virtual bool CanBeInterruptedBy(IState<T> interruptingState) => AllowsInterruption;

        public virtual void ConfigureFromScriptableObject(ScriptableObject config)
        {
            Config = config;
            OnConfigured();
        }

        // Hook appelé après la configuration par SO
        protected virtual void OnConfigured() { }

        // Méthodes utilitaires pour les états dérivés
        
        /// <summary>
        /// Change l'état courant de façon synchrone
        /// </summary>
        protected void ChangeState<TState>(object transitionData = null) where TState : IState<T>
        {
            StateMachine?.ChangeState<TState>(transitionData);
        }
        
        /// <summary>
        /// Ajoute un événement de log avec contexte
        /// </summary>
        protected void LogEvent(string message, LogType logType = LogType.Log)
        {
            string fullMessage = $"[{Owner.name}][{GetName()}] {message}";
            
            switch (logType)
            {
                case LogType.Warning:
                    Debug.LogWarning(fullMessage);
                    break;
                case LogType.Error:
                    Debug.LogError(fullMessage);
                    break;
                case LogType.Exception:
                    Debug.LogException(new System.Exception(fullMessage));
                    break;
                default:
                    Debug.Log(fullMessage);
                    break;
            }
        }
        
        /// <summary>
        /// Vérifie si l'entité est dans un état spécifique
        /// </summary>
        protected bool IsInState<TState>() where TState : IState<T>
        {
            return StateMachine != null && StateMachine.IsInState<TState>();
        }
        
        /// <summary>
        /// Dessine des éléments de debug en mode Gizmos pour visualisation en temps réel
        /// </summary>
        public virtual void OnDrawGizmos() { }
        
        /// <summary>
        /// Permet de récupérer un état spécifique depuis la machine
        /// </summary>
        protected TState GetState<TState>() where TState : IState<T>
        {
            return StateMachine != null ? StateMachine.GetState<TState>() : default;
        }
    }
}