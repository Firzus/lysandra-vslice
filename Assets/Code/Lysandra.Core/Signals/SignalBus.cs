using System;
using System.Collections.Generic;
using UnityEngine;
using Lysandra.Core.Services;

namespace Lysandra.Core.Signals
{
    /// <summary>
    /// SignalBus central pour la communication entre systèmes
    /// Implémente le pattern pub/sub avec un système de canaux type-safe
    /// </summary>
    [DefaultExecutionOrder(-9000)] // S'exécute très tôt dans le cycle
    public class SignalBus : MonoBehaviour, ISignalEmitter
    {
        [Header("Configuration")]
        [SerializeField] private bool _persistAcrossScenes = true;
        [SerializeField] private bool _enablePerformanceTracking = true;
        [SerializeField] private bool _logSignals = false;

        [Header("Debug")]
        [SerializeField] private bool _showDebugWindow = false;

        // Registre des canaux par type de signal
        private readonly Dictionary<Type, SignalChannelBase> _channelsByType = new();

        // Registre des canaux runtime (non-génériques) par type de signal
        private readonly Dictionary<Type, RuntimeSignalChannel> _runtimeChannelsByType = new();

        // Singleton accessible via ServiceLocator
        private static SignalBus _instance;
        public static SignalBus Instance => _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            if (_persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }

            // Activation du tracking de performance
            SignalPerformanceTracker.TrackingEnabled = _enablePerformanceTracking;

            // Enregistrer dans le ServiceLocator pour une injection facile
            ServiceLocator.Instance.Register<SignalBus>(this, true);
            ServiceLocator.Instance.Register<ISignalEmitter>(this, true);

            Debug.Log("[SignalBus] Initialisé");
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

#if UNITY_EDITOR
        // Interface de débug (Editor Only)
        private void OnGUI()
        {
            if (!_showDebugWindow) return;

            GUI.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            int width = 500;
            int height = 300;
            int x = Screen.width - width - 10;
            int y = 10;

            GUILayout.BeginArea(new Rect(x, y, width, height), "SignalBus Monitor", GUI.skin.window);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Reset Stats", GUILayout.Width(120)))
            {
                SignalPerformanceTracker.Reset();
            }

            _enablePerformanceTracking = GUILayout.Toggle(_enablePerformanceTracking, "Enable Tracking");
            SignalPerformanceTracker.TrackingEnabled = _enablePerformanceTracking;

            _logSignals = GUILayout.Toggle(_logSignals, "Log Signals");

            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.Label($"Total Signals: {SignalPerformanceTracker.TotalSignalsEmitted}");
            GUILayout.Label($"Signal Types: {SignalPerformanceTracker.SignalStatsCollection.Count}");
            int totalChannels = _channelsByType.Count + _runtimeChannelsByType.Count;
            GUILayout.Label($"Registered Channels: {totalChannels} ({_channelsByType.Count} editor, {_runtimeChannelsByType.Count} runtime)");

            GUILayout.Label("Recent Signals:");
            foreach (var record in SignalPerformanceTracker.RecentSignals)
            {
                if (record.EmissionTimeMs > 1.0f)
                {
                    // Colorer en rouge les signaux lents
                    GUI.contentColor = Color.red;
                }
                else
                {
                    GUI.contentColor = Color.white;
                }

                GUILayout.Label($"- {record.SignalType.Name}: {record.EmissionTimeMs:F2}ms");
                GUI.contentColor = Color.white;
            }

            GUILayout.EndArea();
        }
#endif

        /// <summary>
        /// Enregistre un canal de signal dans le bus
        /// </summary>
        /// <param name="channel">Canal à enregistrer</param>
        public void RegisterChannel<T>(SignalChannel<T> channel) where T : struct, ISignal
        {
            Type signalType = typeof(T);

            // Désinscrire un éventuel canal runtime existant
            if (_runtimeChannelsByType.ContainsKey(signalType))
            {
                _runtimeChannelsByType.Remove(signalType);
            }

            if (!_channelsByType.ContainsKey(signalType))
            {
                _channelsByType[signalType] = channel;
                Debug.Log($"[SignalBus] Canal enregistré pour le signal: {signalType.Name}");
            }
            else
            {
                Debug.LogWarning($"[SignalBus] Un canal pour {signalType.Name} est déjà enregistré");
            }
        }

        /// <summary>
        /// Récupère un canal existant pour un type de signal donné ou en crée un si nécessaire
        /// </summary>
        /// <typeparam name="T">Type de signal</typeparam>
        /// <param name="createIfMissing">Si true, crée un canal runtime s'il n'existe pas</param>
        /// <returns>Le canal ou null si non trouvé et createIfMissing est false</returns>
        public object GetOrCreateChannel<T>(bool createIfMissing = true) where T : struct, ISignal
        {
            Type signalType = typeof(T);

            // 1. Chercher un canal créé dans l'éditeur (prioritaire)
            if (_channelsByType.TryGetValue(signalType, out var editorChannel))
            {
                return editorChannel as SignalChannel<T>;
            }

            // 2. Chercher un canal runtime
            if (_runtimeChannelsByType.TryGetValue(signalType, out var runtimeChannel))
            {
                return runtimeChannel;
            }

            // 3. Créer un canal runtime si demandé
            if (createIfMissing)
            {
                var newChannel = ScriptableObject.CreateInstance<RuntimeSignalChannel>();
                newChannel.name = $"{signalType.Name}Channel_Runtime";
                newChannel.Initialize(signalType);
                _runtimeChannelsByType[signalType] = newChannel;

                Debug.Log($"[SignalBus] Canal runtime créé pour le signal: {signalType.Name}");
                return newChannel;
            }

            return null;
        }

        /// <summary>
        /// Émet un signal via le bus
        /// </summary>
        /// <typeparam name="T">Type de signal</typeparam>
        /// <param name="signal">Signal à émettre</param>
        public void Emit<T>(T signal) where T : struct, ISignal
        {
            Type signalType = typeof(T);

            if (_logSignals)
            {
                Debug.Log($"[SignalBus] Signal émis: {signalType.Name}");
            }

            // 1. Chercher un canal créé dans l'éditeur
            if (_channelsByType.TryGetValue(signalType, out var editorChannel))
            {
                if (editorChannel is SignalChannel<T> typedChannel)
                {
                    typedChannel.Emit(signal);
                    return;
                }
            }

            // 2. Chercher un canal runtime
            if (_runtimeChannelsByType.TryGetValue(signalType, out var runtimeChannel))
            {
                runtimeChannel.EmitTyped(signal);
                return;
            }

            // 3. Créer un canal runtime
            var newChannel = ScriptableObject.CreateInstance<RuntimeSignalChannel>();
            newChannel.Initialize(signalType);
            _runtimeChannelsByType[signalType] = newChannel;

            Debug.Log($"[SignalBus] Canal runtime créé pour le signal: {signalType.Name}");
            newChannel.EmitTyped(signal);
        }

        /// <summary>
        /// S'abonne à un signal
        /// </summary>
        /// <typeparam name="T">Type de signal</typeparam>
        /// <param name="callback">Callback à appeler</param>
        /// <param name="createChannelIfMissing">Créer un canal si nécessaire</param>
        public void AddListener<T>(Action<T> callback, bool createChannelIfMissing = true) where T : struct, ISignal
        {
            Type signalType = typeof(T);

            // 1. Chercher un canal créé dans l'éditeur
            if (_channelsByType.TryGetValue(signalType, out var editorChannel))
            {
                if (editorChannel is SignalChannel<T> typedChannel)
                {
                    typedChannel.AddListener(callback);
                    return;
                }
            }

            // 2. Chercher un canal runtime
            if (_runtimeChannelsByType.TryGetValue(signalType, out var runtimeChannel))
            {
                runtimeChannel.AddTypedListener(callback);
                return;
            }

            // 3. Créer un canal runtime si demandé
            if (createChannelIfMissing)
            {
                var newChannel = ScriptableObject.CreateInstance<RuntimeSignalChannel>();
                newChannel.Initialize(signalType);
                _runtimeChannelsByType[signalType] = newChannel;

                Debug.Log($"[SignalBus] Canal runtime créé pour le signal: {signalType.Name}");
                newChannel.AddTypedListener(callback);
            }
        }

        /// <summary>
        /// Se désabonne d'un signal
        /// </summary>
        /// <typeparam name="T">Type de signal</typeparam>
        /// <param name="callback">Callback à désabonner</param>
        public void RemoveListener<T>(Action<T> callback) where T : struct, ISignal
        {
            Type signalType = typeof(T);

            // 1. Chercher un canal créé dans l'éditeur
            if (_channelsByType.TryGetValue(signalType, out var editorChannel))
            {
                if (editorChannel is SignalChannel<T> typedChannel)
                {
                    typedChannel.RemoveListener(callback);
                    return;
                }
            }

            // 2. Chercher un canal runtime
            if (_runtimeChannelsByType.TryGetValue(signalType, out var runtimeChannel))
            {
                runtimeChannel.RemoveTypedListener(callback);
            }
        }

        /// <summary>
        /// S'abonne à un signal via un handler
        /// </summary>
        /// <typeparam name="T">Type de signal</typeparam>
        /// <param name="handler">Handler à notifier</param>
        /// <param name="createChannelIfMissing">Créer un canal si nécessaire</param>
        public void AddHandler<T>(ISignalHandler<T> handler, bool createChannelIfMissing = true) where T : struct, ISignal
        {
            Type signalType = typeof(T);

            // 1. Chercher un canal créé dans l'éditeur
            if (_channelsByType.TryGetValue(signalType, out var editorChannel))
            {
                if (editorChannel is SignalChannel<T> typedChannel)
                {
                    typedChannel.AddHandler(handler);
                    return;
                }
            }

            // 2. Chercher un canal runtime
            if (_runtimeChannelsByType.TryGetValue(signalType, out var runtimeChannel))
            {
                runtimeChannel.AddTypedHandler(handler);
                return;
            }

            // 3. Créer un canal runtime si demandé
            if (createChannelIfMissing)
            {
                var newChannel = ScriptableObject.CreateInstance<RuntimeSignalChannel>();
                newChannel.Initialize(signalType);
                _runtimeChannelsByType[signalType] = newChannel;

                Debug.Log($"[SignalBus] Canal runtime créé pour le signal: {signalType.Name}");
                newChannel.AddTypedHandler(handler);
            }
        }

        /// <summary>
        /// Se désabonne d'un signal
        /// </summary>
        /// <typeparam name="T">Type de signal</typeparam>
        /// <param name="handler">Handler à désabonner</param>
        public void RemoveHandler<T>(ISignalHandler<T> handler) where T : struct, ISignal
        {
            Type signalType = typeof(T);

            // 1. Chercher un canal créé dans l'éditeur
            if (_channelsByType.TryGetValue(signalType, out var editorChannel))
            {
                if (editorChannel is SignalChannel<T> typedChannel)
                {
                    typedChannel.RemoveHandler(handler);
                    return;
                }
            }

            // 2. Chercher un canal runtime
            if (_runtimeChannelsByType.TryGetValue(signalType, out var runtimeChannel))
            {
                runtimeChannel.RemoveTypedHandler(handler);
            }
        }

        /// <summary>
        /// Réinitialise le SignalBus (utile pour les changements de scène)
        /// </summary>
        public void Reset()
        {
            _channelsByType.Clear();

            // Nettoyer les canaux runtime
            foreach (var channel in _runtimeChannelsByType.Values)
            {
                channel.ClearAllListeners();
                Destroy(channel);
            }
            _runtimeChannelsByType.Clear();

            SignalPerformanceTracker.Reset();
        }

        /// <summary>
        /// Génère un rapport de performance des signaux
        /// </summary>
        public string GeneratePerformanceReport(bool detailed = false)
        {
            return SignalPerformanceTracker.GenerateReport(detailed);
        }
    }
}