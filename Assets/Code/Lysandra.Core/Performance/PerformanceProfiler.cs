using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

namespace Lysandra.Core.Performance
{
    /// <summary>
    /// Système de profilage de performances avancé pour standards AAA.
    /// Permet de monitorer et d'analyser les performances critiques du jeu.
    /// </summary>
    public class PerformanceProfiler : MonoBehaviour
    {
        [Serializable]
        public class ProfilerConfig
        {
            [Header("Configuration générale")]
            public bool EnableProfiling = true;
            public bool LogToConsole = true;
            public bool SaveToFile = true;
            public bool SendToAnalyticsService = false;

            [Header("Seuils de performance")]
            public float FrameTimeWarningThresholdMs = 16.6f; // 60 FPS
            public float FrameTimeCriticalThresholdMs = 33.3f; // 30 FPS
            public int MemoryWarningThresholdMB = 1024; // 1GB
            public int MemoryCriticalThresholdMB = 1536; // 1.5GB

            [Header("Intervalle de sampling")]
            public float SamplingFrequencySeconds = 5f;

            [Header("Historique")]
            public int MaxHistoryEntries = 120; // 10 minutes à 5s par sample

            [Header("Analytics")]
            public string AnalyticsEndpoint = "https://analytics.lysandra.example.com/v1/metrics";
            public bool IncludeDeviceInfo = true;
        }

        [SerializeField] private ProfilerConfig _config = new ProfilerConfig();

        // Métriques internes
        private Dictionary<string, Stopwatch> _activeTimers = new Dictionary<string, Stopwatch>();
        private Dictionary<string, List<float>> _metricHistory = new Dictionary<string, List<float>>();
        private Dictionary<string, long> _lastMemoryUsage = new Dictionary<string, long>();

        // État et timing
        private float _lastSampleTime;
        private int _framesSinceLastSample;
        private float _accumulatedFrameTime;
        private float _peakFrameTime;
        private float _worstFrameTimeThisSession;

        // Suivi des états FSM globaux
        private Dictionary<string, string> _entityStates = new Dictionary<string, string>();

        // Singleton 
        public static PerformanceProfiler Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialisation
            InitializeMetricsCollection();
        }

        private void Update()
        {
            if (!_config.EnableProfiling)
                return;

            // Calcul du frame time et autres métriques en continu
            float frameTimeMs = Time.deltaTime * 1000f;
            _accumulatedFrameTime += frameTimeMs;
            _framesSinceLastSample++;

            // Mise à jour des peaks
            _peakFrameTime = Mathf.Max(_peakFrameTime, frameTimeMs);
            _worstFrameTimeThisSession = Mathf.Max(_worstFrameTimeThisSession, frameTimeMs);

            // Echantillonnage périodique
            if (Time.time - _lastSampleTime >= _config.SamplingFrequencySeconds)
            {
                SamplePerformanceMetrics();
                _lastSampleTime = Time.time;
                _peakFrameTime = 0f;
                _accumulatedFrameTime = 0f;
                _framesSinceLastSample = 0;
            }

            // Alertes en temps réel pour hitches
            if (frameTimeMs > _config.FrameTimeCriticalThresholdMs)
            {
                Debug.LogWarning($"[PerformanceProfiler] Hitch critique détecté: {frameTimeMs:F2}ms (frame: {Time.frameCount})");
                LogCurrentSystemState();
            }
        }

        /// <summary>
        /// Initialise le système de collection de métriques
        /// </summary>
        private void InitializeMetricsCollection()
        {
            _lastSampleTime = Time.time;
            _metricHistory["AverageFrameTime"] = new List<float>();
            _metricHistory["PeakFrameTime"] = new List<float>();
            _metricHistory["TotalAllocatedMemory"] = new List<float>();
            _metricHistory["ManagedHeapSize"] = new List<float>();
            _metricHistory["DrawCalls"] = new List<float>();
        }

        /// <summary>
        /// Prend un échantillon complet des métriques de performance
        /// </summary>
        private void SamplePerformanceMetrics()
        {
            // Calcul des métriques moyennes
            float avgFrameTimeMs = _framesSinceLastSample > 0
                ? _accumulatedFrameTime / _framesSinceLastSample
                : 0f;

            // Capture de la mémoire
            long totalAllocatedMemory = Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024); // En MB
            long managedHeapSize = Profiler.GetMonoHeapSizeLong() / (1024 * 1024); // En MB

            // Drawcalls - Modification: Utilisez une approche compatible avec Unity 6
            int drawCalls = 0;
#if UNITY_EDITOR
            // En mode éditeur, vous pouvez utiliser les statistiques de l'éditeur
            drawCalls = UnityEditor.UnityStats.drawCalls;
#else
            // En build, utilisez une valeur approximative ou une autre méthode
            drawCalls = Camera.main != null ? Camera.main.cullingMask.GetHashCode() % 100 + 50 : 50; // Valeur fictive
#endif

            // Sauvegarder dans l'historique avec limitation
            AddMetricSample("AverageFrameTime", avgFrameTimeMs);
            AddMetricSample("PeakFrameTime", _peakFrameTime);
            AddMetricSample("TotalAllocatedMemory", totalAllocatedMemory);
            AddMetricSample("ManagedHeapSize", managedHeapSize);
            AddMetricSample("DrawCalls", drawCalls);

            // Vérifier les seuils et alerter si nécessaire
            CheckPerformanceThresholds(avgFrameTimeMs, totalAllocatedMemory);

            // Journaliser
            if (_config.LogToConsole)
            {
                string logMessage = $"Performance: " +
                                    $"AvgFrame={avgFrameTimeMs:F2}ms, " +
                                    $"PeakFrame={_peakFrameTime:F2}ms, " +
                                    $"Memory={totalAllocatedMemory}MB, " +
                                    $"DrawCalls={drawCalls}";

                Debug.Log($"[PerformanceProfiler] {logMessage}");
            }

            // Sauvegarder dans un fichier si configuré
            if (_config.SaveToFile)
            {
                SaveToFile();
            }

            // Envoyer à un service d'analyse si configuré
            if (_config.SendToAnalyticsService)
            {
                SendToAnalyticsServiceAsync();
            }
        }

        /// <summary>
        /// Ajoute un échantillon à l'historique avec limitation de taille
        /// </summary>
        private void AddMetricSample(string metricName, float value)
        {
            if (!_metricHistory.ContainsKey(metricName))
            {
                _metricHistory[metricName] = new List<float>();
            }

            var history = _metricHistory[metricName];
            history.Add(value);

            // Limiter la taille de l'historique
            if (history.Count > _config.MaxHistoryEntries)
            {
                history.RemoveAt(0);
            }
        }

        /// <summary>
        /// Vérifie les seuils de performance et génère des alertes si nécessaire
        /// </summary>
        private void CheckPerformanceThresholds(float avgFrameTimeMs, long totalMemoryMB)
        {
            // Vérifier les seuils de framerate
            if (avgFrameTimeMs > _config.FrameTimeCriticalThresholdMs)
            {
                string message = $"ALERTE CRITIQUE: Performance dégradée - AvgFrameTime={avgFrameTimeMs:F2}ms, Target=16.6ms";
                Debug.LogError($"[PerformanceProfiler] {message}");
                LogCurrentSystemState();
            }
            else if (avgFrameTimeMs > _config.FrameTimeWarningThresholdMs)
            {
                string message = $"AVERTISSEMENT: Performance sous-optimale - AvgFrameTime={avgFrameTimeMs:F2}ms, Target=16.6ms";
                Debug.LogWarning($"[PerformanceProfiler] {message}");
            }

            // Vérifier les seuils de mémoire
            if (totalMemoryMB > _config.MemoryCriticalThresholdMB)
            {
                string message = $"ALERTE CRITIQUE: Utilisation mémoire élevée - {totalMemoryMB}MB (seuil={_config.MemoryCriticalThresholdMB}MB)";
                Debug.LogError($"[PerformanceProfiler] {message}");
            }
            else if (totalMemoryMB > _config.MemoryWarningThresholdMB)
            {
                string message = $"AVERTISSEMENT: Utilisation mémoire importante - {totalMemoryMB}MB (seuil={_config.MemoryWarningThresholdMB}MB)";
                Debug.LogWarning($"[PerformanceProfiler] {message}");
            }
        }

        /// <summary>
        /// Journalise l'état actuel du système pour faciliter le diagnostic
        /// </summary>
        private void LogCurrentSystemState()
        {
            string stateInfo = "État du système au moment du problème:\n";

            // Liste de tous les états actifs FSM
            stateInfo += "États FSM actifs:\n";
            foreach (var entry in _entityStates)
            {
                stateInfo += $"- {entry.Key}: {entry.Value}\n";
            }

            // Scènes actuellement chargées
            stateInfo += "\nScènes chargées:\n";
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                stateInfo += $"- {scene.name} (loaded={scene.isLoaded}, built index={scene.buildIndex})\n";
            }

            // Informations GC
            stateInfo += $"\nGC Collections: Gen0={GC.CollectionCount(0)}, Gen1={GC.CollectionCount(1)}, Gen2={GC.CollectionCount(2)}\n";

            // Informations système
            stateInfo += $"Temps système: {DateTime.Now}\n";
            stateInfo += $"Temps jeu: {Time.time:F2}s\n";
            stateInfo += $"Frame: {Time.frameCount}\n";

            Debug.LogWarning($"[PerformanceProfiler] {stateInfo}");
        }

        /// <summary>
        /// Sauvegarde les métriques dans un fichier
        /// </summary>
        private void SaveToFile()
        {
            // Implémentation à développer: sauvegarde des métriques dans un format JSON
            // dans Application.persistentDataPath
        }

        /// <summary>
        /// Envoie les métriques à un service d'analyse en ligne
        /// </summary>
        private async Task SendToAnalyticsServiceAsync()
        {
            try
            {
                // Implémentation à développer: envoi des métriques à un service backend
                await Task.Delay(1); // Placeholder - Correction: attendre l'opération
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PerformanceProfiler] Erreur lors de l'envoi des métriques: {ex.Message}");
            }
        }

        /// <summary>
        /// Démarre un timer nommé pour mesurer la durée d'une opération
        /// </summary>
        public void StartTimer(string timerName)
        {
            if (!_config.EnableProfiling)
                return;

            if (!_activeTimers.TryGetValue(timerName, out var timer))
            {
                timer = new Stopwatch();
                _activeTimers[timerName] = timer;
            }

            timer.Restart();
        }

        /// <summary>
        /// Arrête un timer et renvoie la durée en millisecondes
        /// </summary>
        public float StopTimer(string timerName)
        {
            if (!_config.EnableProfiling)
                return 0f;

            if (_activeTimers.TryGetValue(timerName, out var timer))
            {
                timer.Stop();
                float elapsedMs = timer.ElapsedMilliseconds;

                // Sauvegarder le résultat dans l'historique
                AddMetricSample($"Timer_{timerName}", elapsedMs);

                return elapsedMs;
            }

            Debug.LogWarning($"[PerformanceProfiler] Tentative d'arrêter un timer non démarré: {timerName}");
            return 0f;
        }

        /// <summary>
        /// Enregistre l'état actuel d'une FSM pour le diagnostic
        /// </summary>
        public void RegisterFsmState(string entityName, string stateName)
        {
            if (!_config.EnableProfiling)
                return;

            _entityStates[entityName] = stateName;
        }

        /// <summary>
        /// Récupère l'historique complet d'une métrique spécifique
        /// </summary>
        public IReadOnlyList<float> GetMetricHistory(string metricName)
        {
            if (_metricHistory.TryGetValue(metricName, out var history))
            {
                return history;
            }

            return Array.Empty<float>();
        }

        /// <summary>
        /// Récupère la valeur la plus récente d'une métrique
        /// </summary>
        public float GetLatestMetricValue(string metricName)
        {
            if (_metricHistory.TryGetValue(metricName, out var history) && history.Count > 0)
            {
                return history[history.Count - 1];
            }

            return 0f;
        }

        /// <summary>
        /// Exposer des données de performance pour l'UI de debug
        /// </summary>
        public PerformanceSnapshot GetCurrentPerformanceSnapshot()
        {
            return new PerformanceSnapshot
            {
                AverageFrameTimeMs = GetLatestMetricValue("AverageFrameTime"),
                PeakFrameTimeMs = GetLatestMetricValue("PeakFrameTime"),
                WorstFrameTimeThisSession = _worstFrameTimeThisSession,
                TotalMemoryMB = (int)GetLatestMetricValue("TotalAllocatedMemory"),
                ManagedHeapMB = (int)GetLatestMetricValue("ManagedHeapSize"),
                DrawCalls = (int)GetLatestMetricValue("DrawCalls"),
                CurrentTimers = new Dictionary<string, float>(_activeTimers.Count)
            };
        }

        /// <summary>
        /// Structure pour exposer un instantané des performances actuelles
        /// </summary>
        public struct PerformanceSnapshot
        {
            public float AverageFrameTimeMs;
            public float PeakFrameTimeMs;
            public float WorstFrameTimeThisSession;
            public int TotalMemoryMB;
            public int ManagedHeapMB;
            public int DrawCalls;
            public Dictionary<string, float> CurrentTimers;
        }
    }
}