using UnityEngine;

namespace Lysandra.Core
{
    /// <summary>
    /// Interface de base pour tous les états utilisés dans la EnhancedStateMachine.
    /// Fournit toutes les méthodes essentielles pour un état de qualité AAA.
    /// </summary>
    /// <typeparam name="T">Type d'entité propriétaire de l'état</typeparam>
    public interface IState<T> where T : MonoBehaviour
    {
        /// <summary>
        /// Initialise l'état avec son propriétaire et sa state machine
        /// </summary>
        /// <param name="owner">Entité propriétaire</param>
        /// <param name="stateMachine">StateMachine parent</param>
        void Initialize(T owner, EnhancedStateMachine<T> stateMachine);
        
        /// <summary>
        /// Appelé quand l'état devient actif
        /// </summary>
        /// <param name="transitionData">Données optionnelles pour la transition</param>
        void Enter(object transitionData);
        
        /// <summary>
        /// Appelé chaque frame quand l'état est actif
        /// </summary>
        void Tick();
        
        /// <summary>
        /// Appelé à chaque FixedUpdate quand l'état est actif
        /// </summary>
        void FixedTick();
        
        /// <summary>
        /// Appelé quand l'état devient inactif
        /// </summary>
        /// <param name="nextState">État qui va devenir actif</param>
        void Exit(IState<T> nextState);
        
        /// <summary>
        /// Retourne le nom lisible de l'état pour le debugging et l'UI
        /// </summary>
        string GetName();
        
        /// <summary>
        /// Retourne la durée de sortie de l'état vers un autre
        /// </summary>
        /// <param name="nextState">État vers lequel on transite</param>
        /// <returns>Durée en secondes</returns>
        float GetExitDuration(IState<T> nextState);
        
        /// <summary>
        /// Retourne la durée d'entrée dans l'état
        /// </summary>
        /// <param name="previousState">État précédent</param>
        /// <returns>Durée en secondes</returns>
        float GetEnterDuration(IState<T> previousState);
        
        /// <summary>
        /// Vérifie si une transition vers un autre état est possible
        /// </summary>
        /// <param name="nextState">État candidat pour la transition</param>
        /// <returns>True si la transition est valide</returns>
        bool CanTransitionTo(IState<T> nextState);
        
        /// <summary>
        /// Vérifie si l'état peut être interrompu par un autre
        /// </summary>
        /// <param name="interruptingState">État qui tente d'interrompre</param>
        /// <returns>True si l'interruption est permise</returns>
        bool CanBeInterruptedBy(IState<T> interruptingState);

        /// <summary>
        /// Configure des options spécifiques à l'état via un ScriptableObject
        /// </summary>
        /// <param name="config">Configuration à appliquer</param>
        void ConfigureFromScriptableObject(ScriptableObject config);
    }
}