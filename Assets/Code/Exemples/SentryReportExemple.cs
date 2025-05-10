using UnityEngine;
using Sentry;

public class CrashTest : MonoBehaviour
{
    public void SendManualReport()
    {
        SentrySdk.CaptureMessage("Rapport manuel envoyé depuis le jeu !");
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            Debug.Log("Test rapport Sentry");

            // Send a manual report
            SentrySdk.CaptureMessage("Rapport manuel envoyé depuis le jeu !");
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            Debug.Log("Test crash Sentry");

            // Simulate a crash
            SentrySdk.CaptureException(new System.Exception("Test crash Sentry"));
        }
    }
}