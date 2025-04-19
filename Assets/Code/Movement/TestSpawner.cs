using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class TestSpawner : MonoBehaviour
{
    [SerializeField] string key = "Crate";   // Adresse ou label

    void Start()
    {
        Addressables.InstantiateAsync(key, transform)
                    .Completed += OnDone;
    }
    void OnDone(AsyncOperationHandle<GameObject> op)
    {
        if (op.Status == AsyncOperationStatus.Succeeded)
            Debug.Log($"Spawn OK: {op.Result.name}");
    }
}
