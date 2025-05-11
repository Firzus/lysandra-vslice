using UnityEngine;

namespace Project.Core.Services
{
    /// <summary>
    /// Adaptateur MonoBehaviour pour le ServiceLocator.
    /// Permet d'initialiser le ServiceLocator et de le lier au cycle de vie d'Unity.
    /// </summary>
    [DefaultExecutionOrder(-9900)] // Exécution très tôt dans le cycle Unity
    public class ServiceLocatorBehaviour : MonoBehaviour
    {
        [SerializeField] private bool _persistAcrossScenes = true;

        private static ServiceLocatorBehaviour _instance;

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

            // Supprimer le log qui n'est plus nécessaire avec le Signal Monitor
            // Debug.Log("[ServiceLocatorBehaviour] Initialisé et prêt à l'emploi");
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                // Suppression du log de destruction non nécessaire
                // if (_logServiceOperations)
                // {
                //     Debug.Log("[ServiceLocatorBehaviour] Destruction. Réinitialisation du ServiceLocator.");
                // }

                // Réinitialiser le ServiceLocator quand cet objet est détruit
                // Utile lors des changements de scène ou quand le jeu se termine
                ServiceLocator.Instance.Reset();
                _instance = null;
            }
        }
    }
}