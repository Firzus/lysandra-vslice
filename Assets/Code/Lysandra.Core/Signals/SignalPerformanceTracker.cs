using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Lysandra.Core.Signals
{
    /// <summary>
    /// Classe statique pour suivre et mesurer les performances du système de signaux
    /// </summary>
    public static class SignalPerformanceTracker
    {
        // Constantes de configuration
        private const int MaxStoredSignals = 1000;  // Nombre maximum de signaux mémorisés pour l'historique
        private const float SlowSignalThresholdMs = 1.0f;  // Seuil pour considérer un signal comme "lent" (en ms)

        // Données de suivi des performances
        private static readonly Dictionary<Type, SignalStats> _signalStats = new Dictionary<Type, SignalStats>();
        private static readonly List<SignalRecord> _recentSignals = new List<SignalRecord>(MaxStoredSignals);
        private static readonly Stopwatch _stopwatch = new Stopwatch();
        private static readonly Dictionary<int, SignalEmissionData> _activeEmissions = new Dictionary<int, SignalEmissionData>();
        private static int _nextEmissionId = 0; // Compteur pour générer des IDs d'émission uniques

        private static int _totalSignalsEmitted = 0;
        private static bool _trackingEnabled = true;

        /// <summary>
        /// Active ou désactive le suivi des performances
        /// </summary>
        public static bool TrackingEnabled
        {
            get => _trackingEnabled;
            set => _trackingEnabled = value;
        }

        /// <summary>
        /// Nombre total de signaux émis depuis le démarrage
        /// </summary>
        public static int TotalSignalsEmitted => _totalSignalsEmitted;

        /// <summary>
        /// Histogramme des temps d'exécution des signaux (pour visualisation)
        /// </summary>
        public static Dictionary<Type, SignalStats> SignalStatsCollection => _signalStats;

        /// <summary>
        /// Liste des signaux récents pour l'analyse
        /// </summary>
        public static IReadOnlyList<SignalRecord> RecentSignals => _recentSignals;

        /// <summary>
        /// Notifie le tracker du début d'une émission de signal
        /// </summary>
        /// <param name="channel">Canal émetteur</param>
        /// <param name="signal">Signal émis</param>
        /// <returns>ID unique pour cette émission de signal</returns>
        public static int BeginSignalEmission(SignalChannelBase channel, ISignal signal)
        {
            if (!_trackingEnabled) return -1;

            // Génère un ID unique pour cette émission
            int emissionId = System.Threading.Interlocked.Increment(ref _nextEmissionId);

            var data = new SignalEmissionData
            {
                SignalType = signal.GetType(),
                Channel = channel,
                Signal = signal,
                StartTime = Time.realtimeSinceStartup,
                FrameNumber = Time.frameCount,
                CallStack = GetCallStack()
            };

            _stopwatch.Restart();
            _activeEmissions[emissionId] = data;

            return emissionId;
        }

        /// <summary>
        /// Notifie le tracker de la fin d'une émission de signal
        /// </summary>
        /// <param name="emissionId">ID unique généré par BeginSignalEmission</param>
        /// <param name="channel">Canal émetteur</param>
        /// <param name="signal">Signal émis</param>
        /// <param name="listenersCount">Nombre de listeners notifiés</param>
        public static void EndSignalEmission(int emissionId, SignalChannelBase channel, int listenersCount)
        {
            if (!_trackingEnabled || emissionId < 0) return;

            _stopwatch.Stop();
            float elapsedMs = _stopwatch.ElapsedTicks / (float)TimeSpan.TicksPerMillisecond;

            if (_activeEmissions.TryGetValue(emissionId, out var data))
            {
                _activeEmissions.Remove(emissionId);

                // Mettre à jour les statistiques pour ce type de signal
                Type signalType = data.SignalType;
                if (!_signalStats.TryGetValue(signalType, out var stats))
                {
                    stats = new SignalStats(signalType);
                    _signalStats[signalType] = stats;
                }

                stats.LogEmission(elapsedMs, listenersCount);

                // Enregistrer dans l'historique récent
                var record = new SignalRecord
                {
                    SignalType = signalType,
                    ChannelName = channel.name,
                    EmissionTimeMs = elapsedMs,
                    ListenersCount = listenersCount,
                    Timestamp = data.StartTime,
                    FrameNumber = data.FrameNumber,
                    CallStack = data.CallStack
                };

                _recentSignals.Add(record);
                if (_recentSignals.Count > MaxStoredSignals)
                {
                    _recentSignals.RemoveAt(0);
                }

                _totalSignalsEmitted++;

                // Log warning if the signal processing took too long
                if (elapsedMs > SlowSignalThresholdMs)
                {
                    Debug.LogWarning($"<color=#FF7F00>[SignalPerformance] Signal lent détecté: {signalType.Name} - {elapsedMs:F2}ms avec {listenersCount} listeners</color>");
                }
            }
        }

        /// <summary>
        /// Méthode de compatibilité avec l'ancien système
        /// </summary>
        public static void EndSignalEmission(SignalChannelBase channel, ISignal signal, int listenersCount)
        {
            // Cette méthode est maintenue pour la compatibilité avec le code existant
            Debug.LogWarning("[SignalPerformanceTracker] Using deprecated EndSignalEmission method. This will not track signals correctly.");
        }

        /// <summary>
        /// Récupérer la stack trace pour le débogage (version courte)
        /// </summary>
        private static string GetCallStack()
        {
            var stackTrace = new System.Diagnostics.StackTrace(true);
            var frames = stackTrace.GetFrames();

            if (frames == null || frames.Length < 3)
                return "Unknown";

            StringBuilder sb = new StringBuilder();

            // Skip les frames de ce système, cherche les 3 frames utiles qui montrent l'émetteur
            int count = 0;
            bool foundEmitter = false;

            for (int i = 2; i < frames.Length && count < 3; i++)
            {
                var frame = frames[i];
                var method = frame.GetMethod();
                if (method == null) continue;

                // On exclut les frames du système de signaux lui-même
                if (method.DeclaringType?.Namespace == "Lysandra.Core.Signals" &&
                    !foundEmitter)
                {
                    continue;
                }

                foundEmitter = true;
                string fileName = frame.GetFileName();
                if (string.IsNullOrEmpty(fileName))
                    fileName = "UnknownFile";
                else
                    fileName = System.IO.Path.GetFileName(fileName);

                sb.AppendLine($"{method.DeclaringType?.Name}.{method.Name} ({fileName}:{frame.GetFileLineNumber()})");
                count++;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Réinitialise toutes les statistiques de tracking
        /// </summary>
        public static void Reset()
        {
            _signalStats.Clear();
            _recentSignals.Clear();
            _activeEmissions.Clear();
            _totalSignalsEmitted = 0;
            _nextEmissionId = 0;
        }

        /// <summary>
        /// Génère un rapport de performance des signaux
        /// </summary>
        public static string GenerateReport(bool detailed = false)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"===== RAPPORT DE PERFORMANCE SIGNALBUS =====");
            sb.AppendLine($"Total des signaux émis: {_totalSignalsEmitted}");
            sb.AppendLine($"Types de signaux: {_signalStats.Count}");
            sb.AppendLine();

            // Liste des types de signaux triés par nombre d'occurrences
            var sortedStats = new List<SignalStats>(_signalStats.Values);
            sortedStats.Sort((a, b) => b.EmissionCount.CompareTo(a.EmissionCount));

            sb.AppendLine("TOP SIGNAUX PAR FRÉQUENCE:");
            for (int i = 0; i < Math.Min(sortedStats.Count, 10); i++)
            {
                var stat = sortedStats[i];
                sb.AppendLine($"{i + 1}. {stat.SignalTypeName}: {stat.EmissionCount} émissions, " +
                             $"moy: {stat.AverageTimeMs:F3}ms, max: {stat.MaxTimeMs:F3}ms");
            }

            sb.AppendLine();
            sb.AppendLine("TOP SIGNAUX LES PLUS LENTS (TEMPS MOYEN):");
            sortedStats.Sort((a, b) => b.AverageTimeMs.CompareTo(a.AverageTimeMs));
            for (int i = 0; i < Math.Min(sortedStats.Count, 5); i++)
            {
                var stat = sortedStats[i];
                sb.AppendLine($"{i + 1}. {stat.SignalTypeName}: {stat.AverageTimeMs:F3}ms moy, " +
                             $"{stat.EmissionCount} émissions, {stat.AverageListenersCount:F1} listeners");
            }

            if (detailed && _recentSignals.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("5 DERNIERS SIGNAUX:");
                for (int i = _recentSignals.Count - 1; i >= Math.Max(0, _recentSignals.Count - 5); i--)
                {
                    var record = _recentSignals[i];
                    sb.AppendLine($"- {record.SignalType.Name} via {record.ChannelName}: " +
                                 $"{record.EmissionTimeMs:F3}ms, {record.ListenersCount} listeners, " +
                                 $"frame {record.FrameNumber}");

                    if (!string.IsNullOrEmpty(record.CallStack))
                    {
                        sb.AppendLine($"  Call Stack: {record.CallStack}");
                    }
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Données de suivi d'une émission en cours
        /// </summary>
        private class SignalEmissionData
        {
            public Type SignalType;
            public SignalChannelBase Channel;
            public ISignal Signal;
            public float StartTime;
            public int FrameNumber;
            public string CallStack;
        }

        /// <summary>
        /// Structure pour stocker un enregistrement d'émission de signal
        /// </summary>
        public struct SignalRecord
        {
            public Type SignalType;
            public string ChannelName;
            public float EmissionTimeMs;
            public int ListenersCount;
            public float Timestamp;
            public int FrameNumber;
            public string CallStack;
        }

        /// <summary>
        /// Statistiques d'un type de signal
        /// </summary>
        public class SignalStats
        {
            public Type SignalType { get; }
            public string SignalTypeName => SignalType.Name;

            public int EmissionCount { get; private set; }
            public float TotalTimeMs { get; private set; }
            public float MinTimeMs { get; private set; } = float.MaxValue;
            public float MaxTimeMs { get; private set; }
            public float AverageTimeMs => EmissionCount > 0 ? TotalTimeMs / EmissionCount : 0;

            public int TotalListenersCount { get; private set; }
            public float AverageListenersCount => EmissionCount > 0 ? (float)TotalListenersCount / EmissionCount : 0;

            public SignalStats(Type signalType)
            {
                SignalType = signalType;
            }

            public void LogEmission(float timeMs, int listenersCount)
            {
                EmissionCount++;
                TotalTimeMs += timeMs;
                MinTimeMs = Math.Min(MinTimeMs, timeMs);
                MaxTimeMs = Math.Max(MaxTimeMs, timeMs);
                TotalListenersCount += listenersCount;
            }
        }
    }
}