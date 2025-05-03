using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lysandra.Core.Services
{
    /// <summary>
    /// Service Locator centralisé pour tous les services du jeu.
    /// Architecture standard AAA pour une gestion propre des dépendances.
    /// </summary>
    public class ServiceLocator
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private readonly Dictionary<Type, List<Type>> _interfaceImplementations = new Dictionary<Type, List<Type>>();

        // Singleton instance (thread-safe)
        private static readonly Lazy<ServiceLocator> _instance = new Lazy<ServiceLocator>(() => new ServiceLocator());
        public static ServiceLocator Instance => _instance.Value;

        // Constructeur privé pour le singleton
        private ServiceLocator() { }

        /// <summary>
        /// Enregistre un service dans le locator
        /// </summary>
        /// <typeparam name="T">Type d'interface du service</typeparam>
        /// <param name="service">Instance du service</param>
        /// <param name="overrideExisting">Si true, remplace un service existant du même type</param>
        public void Register<T>(T service, bool overrideExisting = false)
        {
            Type type = typeof(T);

            if (_services.ContainsKey(type))
            {
                if (!overrideExisting)
                {
                    Debug.LogWarning($"[ServiceLocator] Service {type.Name} déjà enregistré");
                    return;
                }

                _services.Remove(type);
            }

            _services.Add(type, service);

            // Supprimer le log qui n'est pas utile en production
            // Debug.Log($"[ServiceLocator] Service enregistré: {type.Name}");
        }

        /// <summary>
        /// Récupère un service du locator
        /// </summary>
        /// <typeparam name="T">Type du service</typeparam>
        /// <returns>Instance du service</returns>
        /// <exception cref="InvalidOperationException">Si le service n'est pas trouvé</exception>
        public T Get<T>() where T : class
        {
            Type serviceType = typeof(T);

            if (!_services.TryGetValue(serviceType, out var service))
            {
                throw new InvalidOperationException($"Service de type {serviceType.Name} non trouvé");
            }

            return (T)service;
        }

        /// <summary>
        /// Essaie de récupérer un service sans lever d'exception
        /// </summary>
        /// <typeparam name="T">Type du service</typeparam>
        /// <param name="service">Service si trouvé, null sinon</param>
        /// <returns>True si le service existe</returns>
        public bool TryGet<T>(out T service) where T : class
        {
            Type serviceType = typeof(T);

            if (_services.TryGetValue(serviceType, out var foundService))
            {
                service = (T)foundService;
                return true;
            }

            service = null;
            return false;
        }

        /// <summary>
        /// Récupère toutes les implémentations d'une interface
        /// </summary>
        /// <typeparam name="T">Type d'interface</typeparam>
        /// <returns>Liste des implémentations de l'interface</returns>
        public IEnumerable<T> GetAll<T>() where T : class
        {
            Type interfaceType = typeof(T);
            var result = new List<T>();

            if (_interfaceImplementations.TryGetValue(interfaceType, out var implementations))
            {
                foreach (var implType in implementations)
                {
                    if (_services.TryGetValue(implType, out var service))
                    {
                        result.Add((T)service);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Supprime un service du locator
        /// </summary>
        /// <typeparam name="T">Type du service</typeparam>
        /// <returns>True si le service a été supprimé</returns>
        public bool Unregister<T>() where T : class
        {
            Type serviceType = typeof(T);

            if (_services.TryGetValue(serviceType, out var service))
            {
                _services.Remove(serviceType);

                // Enlever les références d'interface
                foreach (var interfaceType in serviceType.GetInterfaces())
                {
                    if (_interfaceImplementations.TryGetValue(interfaceType, out var implementations))
                    {
                        implementations.Remove(serviceType);

                        // Si c'est la dernière implémentation, supprimer aussi l'interface
                        if (implementations.Count == 0)
                        {
                            _interfaceImplementations.Remove(interfaceType);
                            _services.Remove(interfaceType);
                        }
                        // Sinon, mettre à jour l'implémentation par défaut si nécessaire
                        else if (_services.TryGetValue(interfaceType, out var currentImpl) && currentImpl == service)
                        {
                            if (implementations.Count > 0)
                            {
                                var firstImplType = implementations[0];
                                _services[interfaceType] = _services[firstImplType];
                            }
                        }
                    }
                }

                // Debug.Log($"[ServiceLocator] Service désenregistré: {serviceType.Name}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Vérifie si un service est enregistré
        /// </summary>
        /// <typeparam name="T">Type du service</typeparam>
        /// <returns>True si le service existe</returns>
        public bool Contains<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Réinitialise le ServiceLocator (utile pour les tests ou les changements de scène)
        /// </summary>
        public void Reset()
        {
            _services.Clear();
            _interfaceImplementations.Clear();
            // Debug.Log("[ServiceLocator] Réinitialisé");
        }
    }

    /// <summary>
    /// Extensions du ServiceLocator pour faciliter l'injection de dépendances
    /// </summary>
    public static class ServiceLocatorExtensions
    {
        /// <summary>
        /// Injecte automatiquement les dépendances dans un objet
        /// </summary>
        public static void InjectDependencies(this ServiceLocator locator, object target)
        {
            var type = target.GetType();

            // Injecter les propriétés
            foreach (var property in type.GetProperties())
            {
                if (property.CanWrite && property.IsDefined(typeof(InjectAttribute), true))
                {
                    var propertyType = property.PropertyType;
                    if (locator.TryGet(propertyType, out var service))
                    {
                        property.SetValue(target, service);
                    }
                }
            }

            // Injecter les champs
            foreach (var field in type.GetFields())
            {
                if (field.IsDefined(typeof(InjectAttribute), true))
                {
                    var fieldType = field.FieldType;
                    if (locator.TryGet(fieldType, out var service))
                    {
                        field.SetValue(target, service);
                    }
                }
            }
        }

        /// <summary>
        /// Version générique du TryGet qui accepte un type à l'exécution
        /// </summary>
        private static bool TryGet(this ServiceLocator locator, Type serviceType, out object service)
        {
            try
            {
                // Utiliser la réflexion pour appeler la méthode générique TryGet<T>
                var method = typeof(ServiceLocator).GetMethod("TryGet");
                var genericMethod = method.MakeGenericMethod(serviceType);

                var parameters = new object[1];
                var result = (bool)genericMethod.Invoke(locator, parameters);
                service = parameters[0];
                return result;
            }
            catch
            {
                service = null;
                return false;
            }
        }
    }

    /// <summary>
    /// Attribut pour marquer les propriétés à injecter
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class InjectAttribute : Attribute { }
}