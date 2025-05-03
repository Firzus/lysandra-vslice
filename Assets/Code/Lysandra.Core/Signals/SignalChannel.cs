using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lysandra.Core.Signals
{
    /// <summary>
    /// Classe de base pour tous les canaux de signaux
    /// </summary>
    public abstract class SignalChannelBase : ScriptableObject
    {
        [SerializeField, TextArea(1, 3)]
        private string description = "Description du signal";

        [SerializeField] private string category = "Général";
        [SerializeField] private Color channelColor = Color.white;

        public virtual string Description => description;
        public virtual string Category => category;
        public virtual Color ChannelColor => channelColor;

        /// <summary>
        /// Type de signal géré par ce canal
        /// </summary>
        public abstract Type SignalType { get; }

        /// <summary>
        /// Méthode abstraite pour émettre un signal via ce canal
        /// </summary>
        /// <param name="signal">Signal à émettre (sera casté au bon type)</param>
        public abstract void Emit(ISignal signal);

        /// <summary>
        /// Méthode abstraite pour émettre un signal non typé via ce canal
        /// </summary>
        public abstract void EmitEmpty();
    }

    /// <summary>
    /// Canal de signal typé pour une communication type-safe entre systèmes
    /// ScriptableObject pour permettre la configuration dans l'éditeur et les références persistantes
    /// </summary>
    [CreateAssetMenu(fileName = "NewSignalChannel", menuName = "Lysandra/Signals/Signal Channel")]
    public class SignalChannel<T> : SignalChannelBase where T : struct, ISignal
    {
        private readonly List<Action<T>> _listeners = new();
        private readonly List<ISignalHandler<T>> _handlers = new();

        private int _listenersCount => _listeners.Count + _handlers.Count;

        public override Type SignalType => typeof(T);

        /// <summary>
        /// Émet un signal via ce canal
        /// </summary>
        /// <param name="signal">Signal à émettre</param>
        public void Emit(T signal)
        {
            // Notifier la surveillance des performances avant l'émission
            int emissionId = SignalPerformanceTracker.BeginSignalEmission(this, signal);

            // Appeler tous les listeners
            for (int i = 0; i < _listeners.Count; i++)
            {
                try
                {
                    _listeners[i]?.Invoke(signal);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SignalChannel] Exception lors de l'émission du signal {typeof(T).Name}: {ex}");
                }
            }

            // Appeler tous les handlers
            for (int i = 0; i < _handlers.Count; i++)
            {
                try
                {
                    _handlers[i]?.OnSignal(signal);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SignalChannel] Exception lors de l'émission du signal {typeof(T).Name}: {ex}");
                }
            }

            // Notifier la surveillance des performances après l'émission
            SignalPerformanceTracker.EndSignalEmission(emissionId, this, _listenersCount);
        }

        /// <summary>
        /// Émet un signal via ce canal (version non typée)
        /// </summary>
        /// <param name="signal">Signal à émettre</param>
        public override void Emit(ISignal signal)
        {
            if (signal is T typedSignal)
            {
                Emit(typedSignal);
            }
            else
            {
                Debug.LogError($"[SignalChannel] Type de signal incompatible: {signal.GetType().Name} n'est pas {typeof(T).Name}");
            }
        }

        /// <summary>
        /// Émet un signal vide (utile pour les signaux simples qui sont juste des triggers)
        /// </summary>
        public override void EmitEmpty()
        {
            Emit(default);
        }

        /// <summary>
        /// S'abonne à ce canal de signal avec un callback
        /// </summary>
        /// <param name="callback">Callback à appeler lors de l'émission</param>
        public void AddListener(Action<T> callback)
        {
            if (callback != null && !_listeners.Contains(callback))
            {
                _listeners.Add(callback);
            }
        }

        /// <summary>
        /// Se désabonne de ce canal de signal
        /// </summary>
        /// <param name="callback">Callback à désabonner</param>
        public void RemoveListener(Action<T> callback)
        {
            if (callback != null)
            {
                _listeners.Remove(callback);
            }
        }

        /// <summary>
        /// S'abonne à ce canal de signal avec un handler
        /// </summary>
        /// <param name="handler">Handler à notifier lors de l'émission</param>
        public void AddHandler(ISignalHandler<T> handler)
        {
            if (handler != null && !_handlers.Contains(handler))
            {
                _handlers.Add(handler);
            }
        }

        /// <summary>
        /// Se désabonne de ce canal de signal
        /// </summary>
        /// <param name="handler">Handler à désabonner</param>
        public void RemoveHandler(ISignalHandler<T> handler)
        {
            if (handler != null)
            {
                _handlers.Remove(handler);
            }
        }

        /// <summary>
        /// Nettoie tous les abonnements (utile lors des changements de scène)
        /// </summary>
        public void ClearListeners()
        {
            _listeners.Clear();
            _handlers.Clear();
        }
    }
}