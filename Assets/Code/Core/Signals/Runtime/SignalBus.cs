using System;
using System.Collections.Generic;
using UnityEngine;
using Project.Core.Services;

#if UNITY_EDITOR
using UnityEditor; // Ajout de la référence à UnityEditor
#endif

namespace Project.Core.Signals
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
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

#if UNITY_EDITOR
        // Interface de débug avec style glassmorphism simplifié
        private void OnGUI()
        {
            if (!_showDebugWindow) return;

            // Définir les dimensions et position de la fenêtre
            int width = 450;
            int height = 280;
            int x = Screen.width - width - 10;
            int y = 10;

            // Couleurs de base pour le style glassmorphism
            Color mainBackgroundColor = new Color(0.12f, 0.14f, 0.18f, 0.92f); // Fond principal plus opaque
            Color panelBorderColor = new Color(0.3f, 0.35f, 0.45f, 0.6f); // Bordure

            // Style pour l'en-tête
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.normal.textColor = new Color(0.9f, 0.9f, 1.0f, 0.95f);
            headerStyle.fontSize = 12;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.alignment = TextAnchor.MiddleLeft;
            headerStyle.margin = new RectOffset(5, 5, 8, 8);

            // Style pour les labels standard
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.normal.textColor = new Color(0.85f, 0.85f, 0.9f, 0.9f);
            labelStyle.fontSize = 11;

            // Style pour les boutons
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.normal.background = CreateColorTexture(new Color(0.2f, 0.22f, 0.25f, 0.9f));
            buttonStyle.hover.background = CreateColorTexture(new Color(0.25f, 0.27f, 0.3f, 0.9f));
            buttonStyle.normal.textColor = new Color(0.9f, 0.9f, 0.95f);
            buttonStyle.border = new RectOffset(3, 3, 3, 3); // Bordures améliorées
            buttonStyle.margin = new RectOffset(2, 2, 2, 2);

            // Style pour les boutons ON
            GUIStyle switchOnStyle = new GUIStyle(GUI.skin.button);
            switchOnStyle.normal.background = CreateColorTexture(new Color(0.2f, 0.5f, 0.3f, 0.9f));
            switchOnStyle.hover.background = CreateColorTexture(new Color(0.22f, 0.55f, 0.33f, 0.9f));
            switchOnStyle.normal.textColor = new Color(1f, 1f, 1f, 0.95f);
            switchOnStyle.fontStyle = FontStyle.Bold;
            switchOnStyle.fixedWidth = 80;
            switchOnStyle.fontSize = 10;
            switchOnStyle.border = new RectOffset(3, 3, 3, 3); // Bordures améliorées

            // Style pour les boutons OFF
            GUIStyle switchOffStyle = new GUIStyle(GUI.skin.button);
            switchOffStyle.normal.background = CreateColorTexture(new Color(0.5f, 0.2f, 0.2f, 0.9f));
            switchOffStyle.hover.background = CreateColorTexture(new Color(0.55f, 0.22f, 0.22f, 0.9f));
            switchOffStyle.normal.textColor = new Color(1f, 1f, 1f, 0.9f);
            switchOffStyle.fontStyle = FontStyle.Bold;
            switchOffStyle.fixedWidth = 80;
            switchOffStyle.fontSize = 10;
            switchOffStyle.border = new RectOffset(3, 3, 3, 3); // Bordures améliorées

            // Style pour la zone défilante (sans background)
            GUIStyle scrollViewStyle = new GUIStyle(GUI.skin.scrollView);
            scrollViewStyle.normal.background = null; // Sans background

            // Style pour les items de signal
            GUIStyle signalStyle = new GUIStyle(GUI.skin.label);
            signalStyle.normal.textColor = new Color(0.85f, 0.85f, 0.9f);
            signalStyle.fontSize = 10;
            signalStyle.margin = new RectOffset(8, 8, 2, 2);

            // Style pour les signaux lents
            GUIStyle slowSignalStyle = new GUIStyle(signalStyle);
            slowSignalStyle.normal.textColor = new Color(1f, 0.85f, 0.5f);

            // Style pour le panneau principal avec bordures arrondies
            GUIStyle panelStyle = new GUIStyle();
            panelStyle.normal.background = CreateRoundedRectTexture(32, 32, mainBackgroundColor, panelBorderColor, 8); // 8px de rayon pour les coins

            // Dessiner l'ombre portée légère pour donner de la profondeur
            GUI.Box(new Rect(x + 3, y + 3, width, height), "", GUI.skin.box);

            // Dessiner le panneau principal avec les coins arrondis
            GUI.Box(new Rect(x, y, width, height), "", panelStyle);

            // Zone de contenu
            GUILayout.BeginArea(new Rect(x, y, width, height));

            // Titre
            GUILayout.Space(10);
            GUILayout.Label("Signal Monitor", headerStyle);

            // Ligne de séparation
            Rect separatorRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(1));
            EditorGUI.DrawRect(separatorRect, new Color(0.4f, 0.45f, 0.5f, 0.5f));
            GUILayout.Space(5);

            // Entête avec boutons de contrôle
            GUILayout.BeginHorizontal();
            GUILayout.Space(5);

            // Bouton Reset
            if (GUILayout.Button("Reset", buttonStyle, GUILayout.Width(60), GUILayout.Height(22)))
            {
                SignalPerformanceTracker.Reset();
            }

            GUILayout.FlexibleSpace();

            // Bouton "Tracking" de style switch
            string trackingLabel = _enablePerformanceTracking ? "Track: ON" : "Track: OFF";
            GUIStyle trackingStyle = _enablePerformanceTracking ? switchOnStyle : switchOffStyle;

            if (GUILayout.Button(trackingLabel, trackingStyle, GUILayout.Height(22)))
            {
                _enablePerformanceTracking = !_enablePerformanceTracking;
                SignalPerformanceTracker.TrackingEnabled = _enablePerformanceTracking;
            }

            GUILayout.Space(8);

            // Bouton "Logging" de style switch
            string logLabel = _logSignals ? "Log: ON" : "Log: OFF";
            GUIStyle logStyle = _logSignals ? switchOnStyle : switchOffStyle;

            if (GUILayout.Button(logLabel, logStyle, GUILayout.Height(22)))
            {
                _logSignals = !_logSignals;
            }

            GUILayout.Space(5);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Statistiques principales (sans background)
            GUILayout.BeginVertical();
            GUILayout.Space(3);
            GUILayout.Label($"Total: {SignalPerformanceTracker.TotalSignalsEmitted} signals • {SignalPerformanceTracker.SignalStatsCollection.Count} types • {_channelsByType.Count + _runtimeChannelsByType.Count} channels", labelStyle);
            GUILayout.Space(3);
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Titre de la section signaux récents
            GUILayout.Label("Recent Signals:", headerStyle);

            // Zone déroulante pour les signaux récents
            _debugScrollPosition = GUILayout.BeginScrollView(_debugScrollPosition, scrollViewStyle, GUILayout.Height(150));

            var recentSignals = SignalPerformanceTracker.RecentSignals;
            int count = recentSignals.Count;

            // N'afficher que les 20 derniers signaux maximum, du plus récent au plus ancien
            int startIndex = Mathf.Max(0, count - 20);

            // Si aucun signal, afficher un message
            if (count == 0)
            {
                GUILayout.Space(5);
                GUILayout.Label("  No signals recorded yet.", signalStyle);
            }

            for (int i = count - 1; i >= startIndex; i--)
            {
                var record = recentSignals[i];

                string signalText = $"• {record.SignalType.Name}: {record.EmissionTimeMs:F2}ms ({record.ListenersCount} listeners)";

                // Utiliser un style différent pour les signaux lents
                GUILayout.Label(signalText, record.EmissionTimeMs > 1.0f ? slowSignalStyle : signalStyle);

                // Ajouter un petit espace entre les signaux pour une meilleure lisibilité
                GUILayout.Space(2);
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

        // Helper pour créer une texture avec des coins arrondis
        private Texture2D CreateRoundedRectTexture(int width, int height, Color fillColor, Color borderColor, int radius)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

            // Remplir de transparent
            Color[] colors = new Color[width * height];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.clear;
            }
            tex.SetPixels(colors);

            // Remplir le centre
            for (int y = radius; y < height - radius; y++)
            {
                for (int x = radius; x < width - radius; x++)
                {
                    tex.SetPixel(x, y, fillColor);
                }

                // Bordures gauche et droite (sans les coins)
                for (int x = 0; x < radius; x++)
                {
                    tex.SetPixel(x, y, borderColor);
                }
                for (int x = width - radius; x < width; x++)
                {
                    tex.SetPixel(x, y, borderColor);
                }
            }

            // Bordures haut et bas (sans les coins)
            for (int y = 0; y < radius; y++)
            {
                for (int x = radius; x < width - radius; x++)
                {
                    tex.SetPixel(x, y, borderColor);
                    tex.SetPixel(x, height - 1 - y, borderColor);
                }
            }

            // Dessiner les coins arrondis
            float radiusSquared = radius * radius;

            // Coin supérieur gauche
            for (int y = 0; y < radius; y++)
            {
                for (int x = 0; x < radius; x++)
                {
                    // Distance depuis le point au centre du coin arrondi
                    float distanceSquared = (radius - x - 0.5f) * (radius - x - 0.5f) +
                                            (radius - y - 0.5f) * (radius - y - 0.5f);
                    if (distanceSquared < radiusSquared)
                    {
                        tex.SetPixel(x, y, borderColor);
                    }
                }
            }

            // Coin supérieur droit
            for (int y = 0; y < radius; y++)
            {
                for (int x = width - radius; x < width; x++)
                {
                    float distanceSquared = (x - (width - radius) + 0.5f) * (x - (width - radius) + 0.5f) +
                                            (radius - y - 0.5f) * (radius - y - 0.5f);
                    if (distanceSquared < radiusSquared)
                    {
                        tex.SetPixel(x, y, borderColor);
                    }
                }
            }

            // Coin inférieur gauche
            for (int y = height - radius; y < height; y++)
            {
                for (int x = 0; x < radius; x++)
                {
                    float distanceSquared = (radius - x - 0.5f) * (radius - x - 0.5f) +
                                            (y - (height - radius) + 0.5f) * (y - (height - radius) + 0.5f);
                    if (distanceSquared < radiusSquared)
                    {
                        tex.SetPixel(x, y, borderColor);
                    }
                }
            }

            // Coin inférieur droit
            for (int y = height - radius; y < height; y++)
            {
                for (int x = width - radius; x < width; x++)
                {
                    float distanceSquared = (x - (width - radius) + 0.5f) * (x - (width - radius) + 0.5f) +
                                            (y - (height - radius) + 0.5f) * (y - (height - radius) + 0.5f);
                    if (distanceSquared < radiusSquared)
                    {
                        tex.SetPixel(x, y, borderColor);
                    }
                }
            }

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
                // Log conditionnel avec plus de contexte pour être réellement utile
                // Debug.Log($"[SignalBus] Signal émis: {signalType.Name}");
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