using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;

namespace Project.Core.Signals
{
    /// <summary>
    /// Système de suivi des performances du système de signaux
    /// Permet d'analyser les émissions de signaux et d'identifier les goulots d'étranglement
    /// </summary>
    public static class SignalPerformanceTracker
    {
        // Statistiques par type de signal
        public class SignalStats
        {
            public Type SignalType { get; set; }
            public int EmissionCount { get; set; }
            public float TotalTimeMs { get; set; }
            public float MaxTimeMs { get; set; }
            public int TotalListeners { get; set; }

            // Calculé à la demande
            public float AverageTimeMs => EmissionCount > 0 ? TotalTimeMs / EmissionCount : 0;
            public float AverageListenersPerEmission => EmissionCount > 0 ? (float)TotalListeners / EmissionCount : 0;
        }

        // Enregistrement d'une émission de signal
        public class SignalEmissionRecord
        {
            public int Id { get; set; }
            public Type SignalType { get; set; }
            public object SignalObject { get; set; }
            public float EmissionTimeMs { get; set; }
            public int ListenersCount { get; set; }
            public DateTime Timestamp { get; set; }
            public string CallStack { get; set; }
        }

        // Configuration 
        private static bool _trackingEnabled = true;
        public static bool TrackingEnabled
        {
            get => _trackingEnabled;
            set => _trackingEnabled = value;
        }

        private static bool _collectStackTraces = true;
        public static bool CollectStackTraces
        {
            get => _collectStackTraces;
            set => _collectStackTraces = value;
        }

        // Données de suivi
        private static readonly Dictionary<Type, SignalStats> _signalStatsCollection = new Dictionary<Type, SignalStats>();
        private static readonly List<SignalEmissionRecord> _recentSignals = new List<SignalEmissionRecord>();
        private static readonly Dictionary<int, Stopwatch> _activeEmissions = new Dictionary<int, Stopwatch>();
        private static readonly object _lock = new object();
        private static int _nextEmissionId = 1;
        private static int _totalSignalsEmitted;

        // Limites 
        private const int MaxRecentSignals = 100;

        // Accesseurs publics
        public static IReadOnlyDictionary<Type, SignalStats> SignalStatsCollection => _signalStatsCollection;
        public static IReadOnlyList<SignalEmissionRecord> RecentSignals => _recentSignals;
        public static int TotalSignalsEmitted => _totalSignalsEmitted;

        /// <summary>
        /// Commence le suivi d'une émission de signal
        /// </summary>
        /// <returns>ID de l'émission pour la récupération ultérieure</returns>
        public static int BeginSignalEmission<T>(SignalChannelBase channel, T signal) where T : struct, ISignal
        {
            if (!_trackingEnabled) return 0;

            lock (_lock)
            {
                int emissionId = _nextEmissionId++;
                _totalSignalsEmitted++;

                // Démarrer le chrono
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                _activeEmissions[emissionId] = stopwatch;

                // Préparer l'enregistrement
                var record = new SignalEmissionRecord
                {
                    Id = emissionId,
                    SignalType = typeof(T),
                    SignalObject = signal,
                    Timestamp = DateTime.Now
                };

                // Collecter la stack trace si activé
                if (_collectStackTraces)
                {
                    try
                    {
                        var trace = new StackTrace(2, true); // Ignorer BeginSignalEmission et EmitTyped
                        record.CallStack = trace.ToString();
                    }
                    catch
                    {
                        record.CallStack = "Stack trace collection failed";
                    }
                }

                // Ajouter aux signaux récents (avec limite)
                _recentSignals.Add(record);
                if (_recentSignals.Count > MaxRecentSignals)
                {
                    _recentSignals.RemoveAt(0);
                }

                return emissionId;
            }
        }

        /// <summary>
        /// Termine le suivi d'une émission de signal
        /// </summary>
        public static void EndSignalEmission(int emissionId, SignalChannelBase channel, int listenersCount)
        {
            if (!_trackingEnabled || emissionId == 0) return;

            lock (_lock)
            {
                if (!_activeEmissions.TryGetValue(emissionId, out var stopwatch))
                {
                    return;
                }

                // Arrêter le chrono
                stopwatch.Stop();
                float elapsedMs = stopwatch.ElapsedMilliseconds + (stopwatch.ElapsedTicks % 10000) / 10000f;
                _activeEmissions.Remove(emissionId);

                // Trouver l'enregistrement correspondant
                var record = _recentSignals.Find(r => r.Id == emissionId);
                if (record != null)
                {
                    record.EmissionTimeMs = elapsedMs;
                    record.ListenersCount = listenersCount;
                }

                // Mettre à jour les statistiques par type
                var signalType = channel.SignalType;
                if (!_signalStatsCollection.TryGetValue(signalType, out var stats))
                {
                    stats = new SignalStats { SignalType = signalType };
                    _signalStatsCollection[signalType] = stats;
                }

                stats.EmissionCount++;
                stats.TotalTimeMs += elapsedMs;
                stats.MaxTimeMs = Mathf.Max(stats.MaxTimeMs, elapsedMs);
                stats.TotalListeners += listenersCount;
            }
        }

        /// <summary>
        /// Réinitialise toutes les statistiques de performance
        /// </summary>
        public static void Reset()
        {
            lock (_lock)
            {
                _signalStatsCollection.Clear();
                _recentSignals.Clear();
                _activeEmissions.Clear();
                _totalSignalsEmitted = 0;
                _nextEmissionId = 1;
            }
        }

        /// <summary>
        /// Génère un rapport de performance des signaux
        /// </summary>
        /// <param name="detailed">Inclure des détails par signal</param>
        public static string GenerateReport(bool detailed = false)
        {
            lock (_lock)
            {
                var sb = new StringBuilder();
                sb.AppendLine("=== RAPPORT DE PERFORMANCE DES SIGNAUX ===");
                sb.AppendLine($"Total des signaux émis: {_totalSignalsEmitted}");
                sb.AppendLine($"Types de signaux uniques: {_signalStatsCollection.Count}");
                sb.AppendLine();

                if (detailed && _signalStatsCollection.Count > 0)
                {
                    sb.AppendLine("--- DÉTAILS PAR TYPE DE SIGNAL ---");

                    // Trier par nombre d'émissions (plus fréquents en premier)
                    var sortedStats = new List<SignalStats>(_signalStatsCollection.Values);
                    sortedStats.Sort((a, b) => b.EmissionCount.CompareTo(a.EmissionCount));

                    foreach (var stats in sortedStats)
                    {
                        sb.AppendLine($"• {stats.SignalType.Name}:");
                        sb.AppendLine($"  - Émissions: {stats.EmissionCount}");
                        sb.AppendLine($"  - Temps moyen: {stats.AverageTimeMs:F3} ms");
                        sb.AppendLine($"  - Temps max: {stats.MaxTimeMs:F3} ms");
                        sb.AppendLine($"  - Écouteurs par émission: {stats.AverageListenersPerEmission:F1}");
                    }

                    sb.AppendLine();
                }

                if (_recentSignals.Count > 0)
                {
                    sb.AppendLine("--- 5 DERNIERS SIGNAUX ---");
                    int startIdx = Math.Max(0, _recentSignals.Count - 5);
                    for (int i = _recentSignals.Count - 1; i >= startIdx; i--)
                    {
                        var signal = _recentSignals[i];
                        sb.AppendLine($"• {signal.SignalType.Name}: {signal.EmissionTimeMs:F3} ms, {signal.ListenersCount} écouteurs");
                    }
                }

                return sb.ToString();
            }
        }
    }
}