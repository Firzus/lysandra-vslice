using UnityEngine;

namespace Project.Tools.Debugging.Runtime
{
    /// <summary>
    /// Affiche un overlay debug runtime avec FPS et mémoire.
    /// </summary>
    public class DebugOverlay : MonoBehaviour
    {
        private float _deltaTime;
        private GUIStyle _style;

        private void Awake()
        {
            _style = new GUIStyle();
            _style.fontSize = 16;
            _style.normal.textColor = Color.white;
            _style.richText = true;
        }

        private void Update()
        {
            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
        }

        private void OnGUI()
        {
            float fps = 1.0f / _deltaTime;
            long mem = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024);
            string text = $"<b>FPS:</b> {fps:F1}\n<b>Mémoire:</b> {mem} MB";
            GUI.Label(new Rect(16, 16, 350, 50), text, _style);
        }
    }
}
