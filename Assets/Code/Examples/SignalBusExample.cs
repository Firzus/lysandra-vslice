using UnityEngine;
using Lysandra.Core.Signals;
using Lysandra.Core.Services;

namespace Lysandra.Examples
{
    /// <summary>
    /// Exemple de composant utilisant le système de SignalBus
    /// </summary>
    public class SignalBusExample : MonoBehaviour
    {
        // Référence au SignalBus (peut être injectée via ServiceLocator ou assignée directement)
        private SignalBus _signalBus;
        
        [Header("Configuration")]
        [SerializeField] private int _startHealth = 100;
        [SerializeField] private int _maxHealth = 100;
        [SerializeField] private bool _logEvents = true;
        
        // Données internes
        private int _currentHealth;
        private bool _isDead = false;
        private string _currentStateName = "Idle";
        
        private void Awake()
        {
            // Initialiser les valeurs
            _currentHealth = _startHealth;
            
            // Récupérer le SignalBus depuis le ServiceLocator
            _signalBus = ServiceLocator.Instance.Get<SignalBus>();
        }
        
        private void Start()
        {
            // S'abonner aux signaux pertinents
            _signalBus.AddListener<CommonSignals.AttackHit>(OnAttackHit);
            _signalBus.AddListener<CommonSignals.GamePaused>(OnGamePaused);
            
            // Émettre un signal pour mettre à jour l'UI avec les stats initiales
            EmitHealthUpdateSignal();
        }
        
        private void OnDestroy()
        {
            // Se désabonner des signaux pour éviter les fuites mémoire
            if (_signalBus != null)
            {
                _signalBus.RemoveListener<CommonSignals.AttackHit>(OnAttackHit);
                _signalBus.RemoveListener<CommonSignals.GamePaused>(OnGamePaused);
            }
        }
        
        private void Update()
        {
            // Exemples d'interactions pour tester le système
            if (Input.GetKeyDown(KeyCode.H))
            {
                TakeDamage(10);
            }
            
            if (Input.GetKeyDown(KeyCode.R))
            {
                Heal(20);
            }
            
            if (Input.GetKeyDown(KeyCode.Space))
            {
                PerformAttack();
            }
            
            if (Input.GetKeyDown(KeyCode.S))
            {
                ChangeState("Running");
            }
            
            if (Input.GetKeyDown(KeyCode.D))
            {
                // Émet un signal de débug (pour tester le tracking de performance)
                for (int i = 0; i < 10; i++)
                {
                    _signalBus.Emit(new CommonSignals.LogDebugEvent
                    {
                        Category = "Test",
                        Message = $"Test message {i}",
                        Level = CommonSignals.LogDebugEvent.LogLevel.Info
                    });
                }
            }
        }
        
        // Gestionnaire pour les signaux de type AttackHit
        private void OnAttackHit(CommonSignals.AttackHit signal)
        {
            // Si notre objet est la cible, prendre des dégâts
            if (signal.Target == gameObject)
            {
                TakeDamage((int)signal.DamageDealt);
                
                Log($"Je suis touché par {signal.Attacker.name} pour {signal.DamageDealt} dégâts!");
            }
        }
        
        // Gestionnaire pour les signaux de pause du jeu
        private void OnGamePaused(CommonSignals.GamePaused signal)
        {
            Log($"Jeu {(signal.IsPaused ? "mis en pause" : "repris")}");
            
            // Désactiver les comportements pendant la pause
            enabled = !signal.IsPaused;
        }
        
        // Exemple de méthode qui prend des dégâts et émet des signaux
        public void TakeDamage(int amount)
        {
            if (_isDead) return;
            
            _currentHealth -= amount;
            
            // Émettre un signal de dégâts
            _signalBus.Emit(new CommonSignals.PlayerDamaged
            {
                DamageAmount = amount,
                HitPosition = transform.position,
                DamageSource = null // Normalement, la source des dégâts
            });
            
            // Mettre à jour l'UI de santé
            EmitHealthUpdateSignal();
            
            // Vérifier si le joueur est mort
            if (_currentHealth <= 0)
            {
                Die();
            }
        }
        
        // Exemple de méthode de soin
        public void Heal(int amount)
        {
            if (_isDead) return;
            
            _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth);
            
            // Mettre à jour l'UI de santé
            EmitHealthUpdateSignal();
            
            Log($"Soigné de {amount}, santé actuelle: {_currentHealth}");
        }
        
        // Exemple de méthode qui effectue une attaque et émet un signal
        public void PerformAttack()
        {
            Log("Attaque!");
            
            // Émettre un signal d'attaque
            _signalBus.Emit(new CommonSignals.AttackStarted
            {
                Attacker = gameObject,
                AttackName = "BasicAttack",
                AttackPower = 20
            });
            
            // Simuler une attaque réussie après un certain délai
            Invoke(nameof(SimulateHit), 0.2f);
        }
        
        // Simuler un coup réussi
        private void SimulateHit()
        {
            // Émettre un signal de hit avec un hitstop
            _signalBus.Emit(new CommonSignals.AttackHit
            {
                Attacker = gameObject,
                Target = null, // Normalement, la cible touchée
                DamageDealt = 20,
                HitPosition = transform.position + transform.forward,
                IsCritical = Random.value > 0.7f
            });
            
            // Créer un effet de hitstop (freeze temporaire) pour les coups puissants
            _signalBus.Emit(new CommonSignals.HitStop
            {
                Duration = 0.1f,
                TimeScale = 0.2f
            });
        }
        
        // Exemple de changement d'état (pour un système FSM)
        public void ChangeState(string newState)
        {
            string previousState = _currentStateName;
            _currentStateName = newState;
            
            // Émettre un signal de changement d'état
            _signalBus.Emit(new CommonSignals.PlayerStateChanged
            {
                PreviousState = previousState,
                CurrentState = newState,
                StateTime = Time.time
            });
            
            Log($"Changement d'état: {previousState} -> {newState}");
        }
        
        // Mort du personnage
        private void Die()
        {
            if (_isDead) return;
            
            _isDead = true;
            _currentHealth = 0;
            
            // Émettre un signal de mort
            _signalBus.Emit(new CommonSignals.PlayerDied
            {
                DeathPosition = transform.position,
                KilledBy = null // Normalement, l'entité qui a tué
            });
            
            Log("Mort!");
        }
        
        // Helper pour mettre à jour l'UI de santé via signal
        private void EmitHealthUpdateSignal()
        {
            _signalBus.Emit(new CommonSignals.UpdateHealthUI
            {
                CurrentValue = _currentHealth,
                MaxValue = _maxHealth,
                Animate = true
            });
            
            // Émettre également un signal plus général de stats joueur
            _signalBus.Emit(new CommonSignals.PlayerStatsChanged
            {
                Health = _currentHealth,
                MaxHealth = _maxHealth,
                Stamina = 100, // Exemple, à remplacer par la vraie valeur
                MaxStamina = 100 // Exemple, à remplacer par la vraie valeur
            });
        }
        
        // Helper pour logger les messages si activé
        private void Log(string message)
        {
            if (_logEvents)
            {
                Debug.Log($"[SignalBusExample] {message}");
            }
        }
    }
}