using System;

namespace Lysandra.Core.Signals
{
    /// <summary>
    /// Interface de base pour tous les signaux du système
    /// Tous les signaux doivent être des structs pour une allocation minimale
    /// </summary>
    public interface ISignal { }

    /// <summary>
    /// Interface pour les gestionnaires de signaux
    /// </summary>
    /// <typeparam name="T">Type de signal géré</typeparam>
    public interface ISignalHandler<T> where T : struct, ISignal
    {
        /// <summary>
        /// Méthode appelée lorsqu'un signal est émis
        /// </summary>
        /// <param name="signal">Signal reçu</param>
        void OnSignal(T signal);
    }

    /// <summary>
    /// Interface pour les émetteurs de signaux
    /// </summary>
    public interface ISignalEmitter
    {
        /// <summary>
        /// Émet un signal via le SignalBus
        /// </summary>
        /// <typeparam name="T">Type de signal</typeparam>
        /// <param name="signal">Signal à émettre</param>
        void Emit<T>(T signal) where T : struct, ISignal;
    }
}