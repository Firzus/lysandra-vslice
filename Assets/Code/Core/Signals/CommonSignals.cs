using UnityEngine;

namespace Lysandra.Core.Signals
{
    /// <summary>
    /// Classe contenant des signaux communs utilisés dans tout le projet
    /// </summary>
    public static class CommonSignals
    {
        #region Signaux du Core System
        
        /// <summary>
        /// Signal émis lorsque le jeu est prêt (tous les managers chargés)
        /// </summary>
        public struct GameReady : ISignal { }
        
        /// <summary>
        /// Signal émis lorsqu'une scène commence à être chargée
        /// </summary>
        public struct SceneLoadStarted : ISignal
        {
            public string SceneName;
            public float EstimatedLoadTime;
        }
        
        /// <summary>
        /// Signal émis lorsqu'une scène est complètement chargée
        /// </summary>
        public struct SceneLoadCompleted : ISignal
        {
            public string SceneName;
            public float ActualLoadTime;
        }
        
        /// <summary>
        /// Signal émis lorsque le jeu est mis en pause
        /// </summary>
        public struct GamePaused : ISignal
        {
            public bool IsPaused;
        }
        
        #endregion
        
        #region Signaux du Player
        
        /// <summary>
        /// Signal émis lorsque le joueur change d'état (FSM)
        /// </summary>
        public struct PlayerStateChanged : ISignal
        {
            public string PreviousState;
            public string CurrentState;
            public float StateTime;
        }
        
        /// <summary>
        /// Signal émis lorsque les stats du joueur changent
        /// </summary>
        public struct PlayerStatsChanged : ISignal
        {
            public float Health;
            public float MaxHealth;
            public float Stamina;
            public float MaxStamina;
        }
        
        /// <summary>
        /// Signal émis lorsque le joueur prend des dégâts
        /// </summary>
        public struct PlayerDamaged : ISignal
        {
            public float DamageAmount;
            public Vector3 HitPosition;
            public GameObject DamageSource;
        }
        
        /// <summary>
        /// Signal émis lorsque le joueur meurt
        /// </summary>
        public struct PlayerDied : ISignal
        {
            public GameObject KilledBy;
            public Vector3 DeathPosition;
        }
        
        #endregion
        
        #region Signaux de Combat
        
        /// <summary>
        /// Signal émis lorsqu'une attaque commence
        /// </summary>
        public struct AttackStarted : ISignal
        {
            public GameObject Attacker;
            public string AttackName;
            public float AttackPower;
        }
        
        /// <summary>
        /// Signal émis lorsqu'une attaque touche
        /// </summary>
        public struct AttackHit : ISignal
        {
            public GameObject Attacker;
            public GameObject Target;
            public float DamageDealt;
            public Vector3 HitPosition;
            public bool IsCritical;
        }
        
        /// <summary>
        /// Signal émis pour déclencher un hitstop (ralentissement temporaire)
        /// </summary>
        public struct HitStop : ISignal
        {
            public float Duration;
            public float TimeScale;
        }
        
        #endregion
        
        #region Signaux d'UI
        
        /// <summary>
        /// Signal pour afficher un message à l'écran
        /// </summary>
        public struct ShowMessage : ISignal
        {
            public string Message;
            public float Duration;
            public MessageType Type;
            
            public enum MessageType
            {
                Info,
                Warning,
                Error,
                Achievement
            }
        }
        
        /// <summary>
        /// Signal pour ouvrir/fermer un menu
        /// </summary>
        public struct ToggleMenu : ISignal
        {
            public string MenuName;
            public bool IsOpen;
        }
        
        /// <summary>
        /// Signal pour mettre à jour la barre de vie
        /// </summary>
        public struct UpdateHealthUI : ISignal
        {
            public float CurrentValue;
            public float MaxValue;
            public bool Animate;
        }
        
        /// <summary>
        /// Signal pour mettre à jour la barre de stamina
        /// </summary>
        public struct UpdateStaminaUI : ISignal
        {
            public float CurrentValue;
            public float MaxValue;
            public bool Animate;
        }
        
        #endregion
        
        #region Signaux de Debug
        
        /// <summary>
        /// Signal pour enregistrer un événement de debug
        /// </summary>
        public struct LogDebugEvent : ISignal
        {
            public string Category;
            public string Message;
            public LogLevel Level;
            
            public enum LogLevel
            {
                Verbose,
                Info,
                Warning,
                Error
            }
        }
        
        /// <summary>
        /// Signal pour demander un rapport de performance
        /// </summary>
        public struct RequestPerformanceReport : ISignal
        {
            public bool Detailed;
        }
        
        #endregion
    }
}