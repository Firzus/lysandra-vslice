using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Core.Signals
{
    /// <summary>
    /// Version non-générique du SignalChannel pour l'instanciation à l'exécution
    /// Résout le problème des ScriptableObjects génériques qui ne sont pas supportés par Unity
    /// </summary>
    public class RuntimeSignalChannel : SignalChannelBase
    {
        private readonly Dictionary<Type, object> _listenersByType = new();
        private readonly Dictionary<Type, object> _handlersByType = new();

        private Type _signalType;
        private string _category = "Runtime";
        private Color _channelColor = Color.gray;

        public override Type SignalType => _signalType;
        public override string Description => $"RuntimeChannel pour {_signalType?.Name ?? "Unknown"} signals";
        public override string Category => _category;
        public override Color ChannelColor => _channelColor;

        /// <summary>
        /// Initialise ce canal pour un type spécifique de signal
        /// </summary>
        public void Initialize(Type signalType)
        {
            _signalType = signalType;
            string channelName = $"{signalType.Name}Channel_Runtime";
            name = channelName;
        }

        /// <summary>
        /// Configure les propriétés visuelles du canal
        /// </summary>
        public void Configure(string category, Color color)
        {
            _category = category;
            _channelColor = color;
        }

        /// <summary>
        /// Ajoute un listener typé pour ce canal
        /// </summary>
        public void AddTypedListener<T>(Action<T> callback) where T : struct, ISignal
        {
            Type type = typeof(T);
            if (_signalType != type)
            {
                Debug.LogError($"[RuntimeSignalChannel] Type mismatch: canal pour {_signalType.Name}, mais listener pour {type.Name}");
                return;
            }

            if (!_listenersByType.TryGetValue(type, out object listeners))
            {
                listeners = new List<Action<T>>();
                _listenersByType[type] = listeners;
            }

            var typedListeners = (List<Action<T>>)listeners;
            if (callback != null && !typedListeners.Contains(callback))
            {
                typedListeners.Add(callback);
            }
        }

        /// <summary>
        /// Supprime un listener typé de ce canal
        /// </summary>
        public void RemoveTypedListener<T>(Action<T> callback) where T : struct, ISignal
        {
            Type type = typeof(T);
            if (_signalType != type) return;

            if (_listenersByType.TryGetValue(type, out object listeners))
            {
                var typedListeners = (List<Action<T>>)listeners;
                typedListeners.Remove(callback);
            }
        }

        /// <summary>
        /// Ajoute un handler typé pour ce canal
        /// </summary>
        public void AddTypedHandler<T>(ISignalHandler<T> handler) where T : struct, ISignal
        {
            Type type = typeof(T);
            if (_signalType != type)
            {
                Debug.LogError($"[RuntimeSignalChannel] Type mismatch: canal pour {_signalType.Name}, mais handler pour {type.Name}");
                return;
            }

            if (!_handlersByType.TryGetValue(type, out object handlers))
            {
                handlers = new List<ISignalHandler<T>>();
                _handlersByType[type] = handlers;
            }

            var typedHandlers = (List<ISignalHandler<T>>)handlers;
            if (handler != null && !typedHandlers.Contains(handler))
            {
                typedHandlers.Add(handler);
            }
        }

        /// <summary>
        /// Supprime un handler typé de ce canal
        /// </summary>
        public void RemoveTypedHandler<T>(ISignalHandler<T> handler) where T : struct, ISignal
        {
            Type type = typeof(T);
            if (_signalType != type) return;

            if (_handlersByType.TryGetValue(type, out object handlers))
            {
                var typedHandlers = (List<ISignalHandler<T>>)handlers;
                typedHandlers.Remove(handler);
            }
        }

        /// <summary>
        /// Émet un signal typé via ce canal
        /// </summary>
        public void EmitTyped<T>(T signal) where T : struct, ISignal
        {
            Type type = typeof(T);
            if (_signalType != type)
            {
                Debug.LogError($"[RuntimeSignalChannel] Type mismatch: canal pour {_signalType.Name}, mais émission de {type.Name}");
                return;
            }

            // Performance tracking
            int emissionId = SignalPerformanceTracker.BeginSignalEmission(this, signal);

            int listenerCount = 0;

            // Notifier les listeners typés
            if (_listenersByType.TryGetValue(type, out object listenersObj))
            {
                var listeners = (List<Action<T>>)listenersObj;
                listenerCount += listeners.Count;

                for (int i = 0; i < listeners.Count; i++)
                {
                    try
                    {
                        listeners[i]?.Invoke(signal);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[RuntimeSignalChannel] Exception lors de l'émission du signal {type.Name}: {ex}");
                    }
                }
            }

            // Notifier les handlers typés
            if (_handlersByType.TryGetValue(type, out object handlersObj))
            {
                var handlers = (List<ISignalHandler<T>>)handlersObj;
                listenerCount += handlers.Count;

                for (int i = 0; i < handlers.Count; i++)
                {
                    try
                    {
                        handlers[i]?.OnSignal(signal);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[RuntimeSignalChannel] Exception lors de l'émission du signal {type.Name}: {ex}");
                    }
                }
            }

            // Performance tracking
            SignalPerformanceTracker.EndSignalEmission(emissionId, this, listenerCount);
        }

        /// <summary>
        /// Implémentation de Emit de la classe de base
        /// </summary>
        public override void Emit(ISignal signal)
        {
            if (signal == null)
            {
                Debug.LogError("[RuntimeSignalChannel] Tentative d'émission d'un signal null");
                return;
            }

            Type signalType = signal.GetType();
            if (signalType != _signalType)
            {
                Debug.LogError($"[RuntimeSignalChannel] Type mismatch: canal pour {_signalType.Name}, mais émission de {signalType.Name}");
                return;
            }

            // Utilise la réflexion pour appeler la méthode EmitTyped avec le bon type
            var method = GetType().GetMethod("EmitTyped").MakeGenericMethod(signalType);
            method.Invoke(this, new object[] { signal });
        }

        /// <summary>
        /// Émet un signal vide (trigger)
        /// </summary>
        public override void EmitEmpty()
        {
            if (_signalType == null)
            {
                Debug.LogError("[RuntimeSignalChannel] Tentative d'émission d'un signal vide sans type défini");
                return;
            }

            // Crée une instance vide du type de signal
            ISignal emptySignal = (ISignal)Activator.CreateInstance(_signalType);
            Emit(emptySignal);
        }

        /// <summary>
        /// Nettoie tous les listeners et handlers pour ce canal
        /// </summary>
        public void ClearAllListeners()
        {
            foreach (var listenerList in _listenersByType.Values)
            {
                var method = listenerList.GetType().GetMethod("Clear");
                method?.Invoke(listenerList, null);
            }

            foreach (var handlerList in _handlersByType.Values)
            {
                var method = handlerList.GetType().GetMethod("Clear");
                method?.Invoke(handlerList, null);
            }

            _listenersByType.Clear();
            _handlersByType.Clear();
        }

        /// <summary>
        /// Obtient le nombre total d'abonnés à ce canal
        /// </summary>
        public int GetTotalListenerCount()
        {
            int count = 0;

            foreach (var listenerList in _listenersByType.Values)
            {
                var countProp = listenerList.GetType().GetProperty("Count");
                if (countProp != null)
                {
                    count += (int)countProp.GetValue(listenerList);
                }
            }

            foreach (var handlerList in _handlersByType.Values)
            {
                var countProp = handlerList.GetType().GetProperty("Count");
                if (countProp != null)
                {
                    count += (int)countProp.GetValue(handlerList);
                }
            }

            return count;
        }
    }
}