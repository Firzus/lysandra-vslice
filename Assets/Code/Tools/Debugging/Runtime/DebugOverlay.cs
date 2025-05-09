using UnityEngine;
using System.Diagnostics;

namespace Project.Tools.Debugging.Runtime
{
    /// <summary>
    /// Affiche un overlay debug runtime avec FPS, mémoire, et état courant de la FSM du joueur principal.
    /// </summary>
    public class DebugOverlay : MonoBehaviour
    {
        public MonoBehaviour fsmSource; // À assigner dans l'inspecteur, doit exposer GetFSM()
        private float _deltaTime;
        private GUIStyle _style;

        // Reflection caching
        private System.Reflection.MethodInfo _getFsmMethod;
        private System.Reflection.PropertyInfo _currentStateProp;
        private System.Reflection.PropertyInfo _stateNameProp;
        private object _cachedFsmInstance;
        private System.Type _cachedFsmSourceType;
        private System.Type _lastStateType;
        private bool _fsmNullWarningShown = false;

        private void Awake()
        {
            _style = new GUIStyle();
            _style.fontSize = 16;
            _style.normal.textColor = Color.white;
            _style.richText = true;
            CacheReflection();
        }

        private void CacheReflection(bool logWarning = true)
        {
            _getFsmMethod = null;
            _currentStateProp = null;
            _stateNameProp = null;
            _cachedFsmInstance = null;
            _cachedFsmSourceType = null;
            if (fsmSource == null)
            {
                if (logWarning && !_fsmNullWarningShown)
                {
                    UnityEngine.Debug.LogWarning("[DebugOverlay] fsmSource non assigné.");
                    _fsmNullWarningShown = true;
                }
                return;
            }
            _cachedFsmSourceType = fsmSource.GetType();
            _getFsmMethod = _cachedFsmSourceType.GetMethod("GetFSM");
            if (_getFsmMethod == null)
            {
                if (logWarning && !_fsmNullWarningShown)
                {
                    UnityEngine.Debug.LogWarning($"[DebugOverlay] Méthode GetFSM() non trouvée sur {fsmSource.GetType().Name}.");
                    _fsmNullWarningShown = true;
                }
                return;
            }
            _cachedFsmInstance = _getFsmMethod.Invoke(fsmSource, null);
            if (_cachedFsmInstance == null)
            {
                if (logWarning && !_fsmNullWarningShown)
                {
                    UnityEngine.Debug.LogWarning("[DebugOverlay] GetFSM() a retourné null.");
                    _fsmNullWarningShown = true;
                }
                return;
            }
            var fsmType = _cachedFsmInstance.GetType();
            _currentStateProp = fsmType.GetProperty("CurrentState");
            if (_currentStateProp == null)
            {
                if (logWarning && !_fsmNullWarningShown)
                {
                    UnityEngine.Debug.LogWarning($"[DebugOverlay] Propriété CurrentState non trouvée sur {fsmType.Name}.");
                    _fsmNullWarningShown = true;
                }
                return;
            }
            var stateObj = _currentStateProp.GetValue(_cachedFsmInstance, null);
            if (stateObj == null)
            {
                if (logWarning && !_fsmNullWarningShown)
                {
                    UnityEngine.Debug.LogWarning("[DebugOverlay] CurrentState est null.");
                    _fsmNullWarningShown = true;
                }
                return;
            }
            _stateNameProp = stateObj.GetType().GetProperty("Name");
            if (_stateNameProp == null)
            {
                if (logWarning && !_fsmNullWarningShown)
                {
                    UnityEngine.Debug.LogWarning($"[DebugOverlay] Propriété Name non trouvée sur {stateObj.GetType().Name}.");
                    _fsmNullWarningShown = true;
                }
            }
            // Si tout est OK, reset le flag warning
            _fsmNullWarningShown = false;
        }

        private void OnValidate()
        {
            // Re-cache if fsmSource is changed in inspector
            CacheReflection();
        }

        private void OnEnable()
        {
            CacheReflection();
        }

        private void Update()
        {
            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
            // If fsmSource changes at runtime, re-cache
            if (fsmSource != null && fsmSource.GetType() != _cachedFsmSourceType)
            {
                CacheReflection();
            }
            // Si la FSM n'est pas encore trouvée, on réessaie silencieusement
            if (_cachedFsmInstance == null)
            {
                CacheReflection(false);
            }
        }

        private void OnGUI()
        {
            float fps = 1.0f / _deltaTime;
            long mem = Process.GetCurrentProcess().PrivateMemorySize64 / (1024 * 1024);
            string fsmState = "<None>";
            if (_cachedFsmInstance != null && _currentStateProp != null)
            {
                var currentState = _currentStateProp.GetValue(_cachedFsmInstance, null);
                if (currentState != null)
                {
                    var stateType = currentState.GetType();
                    if (_stateNameProp == null || _lastStateType != stateType)
                    {
                        _stateNameProp = stateType.GetProperty("Name");
                        _lastStateType = stateType;
                    }
                    if (_stateNameProp != null)
                    {
                        var name = _stateNameProp.GetValue(currentState, null) as string;
                        if (!string.IsNullOrEmpty(name))
                            fsmState = name;
                    }
                }
            }
            string text = $"<b>FPS:</b> {fps:F1}\n<b>Mémoire:</b> {mem} MB\n<b>FSM:</b> {fsmState}";
            GUI.Label(new Rect(16, 16, 350, 80), text, _style);
        }
    }
}
