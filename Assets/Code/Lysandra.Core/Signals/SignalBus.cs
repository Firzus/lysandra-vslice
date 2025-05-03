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
        // Interface de débug minimaliste et efficace (Editor Only)
        private void OnGUI()
        {
            if (!_showDebugWindow) return;

            // Définir un style épuré
            int width = 450;
            int height = 270;
            int x = Screen.width - width - 10;
            int y = 10;
            
            // Créer les styles personnalisés pour l'interface
            GUIStyle windowStyle = new GUIStyle(GUI.skin.window);
            windowStyle.normal.background = CreateColorTexture(new Color(0.1f, 0.1f, 0.12f, 0.85f));
            
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.normal.textColor = Color.white;
            headerStyle.fontSize = 12;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.margin = new RectOffset(5, 5, 5, 5);
            
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            labelStyle.fontSize = 11;
            
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.normal.textColor = Color.white;
            buttonStyle.normal.background = CreateColorTexture(new Color(0.25f, 0.25f, 0.3f, 1f));
            buttonStyle.hover.background = CreateColorTexture(new Color(0.3f, 0.3f, 0.35f, 1f));
            
            GUIStyle switchOnStyle = new GUIStyle(GUI.skin.button);
            switchOnStyle.normal.textColor = Color.white;
            switchOnStyle.normal.background = CreateColorTexture(new Color(0.2f, 0.6f, 0.3f, 1f));
            switchOnStyle.hover.background = CreateColorTexture(new Color(0.25f, 0.65f, 0.35f, 1f));
            switchOnStyle.fixedWidth = 80;
            
            GUIStyle switchOffStyle = new GUIStyle(GUI.skin.button);
            switchOffStyle.normal.textColor = Color.white;
            switchOffStyle.normal.background = CreateColorTexture(new Color(0.5f, 0.2f, 0.2f, 1f));
            switchOffStyle.hover.background = CreateColorTexture(new Color(0.55f, 0.25f, 0.25f, 1f));
            switchOffStyle.fixedWidth = 80;
            
            GUIStyle scrollViewStyle = new GUIStyle(GUI.skin.scrollView);
            scrollViewStyle.normal.background = CreateColorTexture(new Color(0.15f, 0.15f, 0.17f, 0.8f));
            
            GUIStyle signalStyle = new GUIStyle(GUI.skin.label);
            signalStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            signalStyle.fontSize = 10;
            signalStyle.margin = new RectOffset(5, 5, 1, 1);
            
            GUIStyle slowSignalStyle = new GUIStyle(signalStyle);
            slowSignalStyle.normal.textColor = new Color(1f, 0.6f, 0.2f);

            // Dessiner la fenêtre
            GUILayout.BeginArea(new Rect(x, y, width, height), "Signal Monitor", windowStyle);

            // Entête avec boutons de contrôle
            GUILayout.BeginHorizontal();
            
            // Bouton Reset
            if (GUILayout.Button("Reset", buttonStyle, GUILayout.Width(70)))
            {
                SignalPerformanceTracker.Reset();
            }
            
            GUILayout.FlexibleSpace();
            
            // Bouton "Tracking" de style switch
            string trackingLabel = _enablePerformanceTracking ? "Track: ON" : "Track: OFF";
            GUIStyle trackingStyle = _enablePerformanceTracking ? switchOnStyle : switchOffStyle;
            
            if (GUILayout.Button(trackingLabel, trackingStyle))
            {
                _enablePerformanceTracking = !_enablePerformanceTracking;
                SignalPerformanceTracker.TrackingEnabled = _enablePerformanceTracking;
            }
            
            GUILayout.Space(10);
            
            // Bouton "Logging" de style switch
            string logLabel = _logSignals ? "Log: ON" : "Log: OFF";
            GUIStyle logStyle = _logSignals ? switchOnStyle : switchOffStyle;
            
            if (GUILayout.Button(logLabel, logStyle))
            {
                _logSignals = !_logSignals;
            }
            
            GUILayout.EndHorizontal();
            
            GUILayout.Space(8);
            
            // Statistiques principales
            GUILayout.BeginVertical();
            GUILayout.Label($"Total: {SignalPerformanceTracker.TotalSignalsEmitted} signals • {SignalPerformanceTracker.SignalStatsCollection.Count} types • {_channelsByType.Count + _runtimeChannelsByType.Count} channels", labelStyle);
            GUILayout.EndVertical();
            
            GUILayout.Space(5);
            
            // Zone déroulante pour les signaux récents
            GUILayout.Label("Recent Signals:", headerStyle);
            _debugScrollPosition = GUILayout.BeginScrollView(_debugScrollPosition, scrollViewStyle, GUILayout.Height(170));
            
            var recentSignals = SignalPerformanceTracker.RecentSignals;
            int count = recentSignals.Count;
            
            // N'afficher que les 20 derniers signaux maximum, du plus récent au plus ancien
            int startIndex = Mathf.Max(0, count - 20);
            
            // Si aucun signal, afficher un message
            if (count == 0)
            {
                GUILayout.Label("  No signals recorded yet.", signalStyle);
            }
            
            for (int i = count - 1; i >= startIndex; i--)
            {
                var record = recentSignals[i];
                string signalText = $"• {record.SignalType.Name}: {record.EmissionTimeMs:F2}ms ({record.ListenersCount} listeners)";
                
                // Utiliser un style différent pour les signaux lents
                GUILayout.Label(signalText, record.EmissionTimeMs > 1.0f ? slowSignalStyle : signalStyle);
            }
            
            GUILayout.EndScrollView();
            
            GUILayout.EndArea();
        }
        
        // Helper pour créer une texture de couleur unie
        private Texture2D CreateColorTexture(Color color)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        // Variable pour la position de défilement
        private Vector2 _debugScrollPosition;
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