using UnityEngine;
using Firebase;
using System.Threading.Tasks;

public class FirebaseInit : MonoBehaviour
{
    // Using async Start allows us to use 'await'
    async void Start()
    {
        Debug.Log("Checking dependencies...");

        // This line waits for Firebase to finish checking everything
        var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();

        if (dependencyStatus == DependencyStatus.Available)
        {
            // This SHOULD now appear in your console
            Debug.Log("Success! Firebase is ready and authenticated.");

            // Initialize the default app instance
            FirebaseApp app = FirebaseApp.DefaultInstance;
        }
        else
        {
            Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
        }
    }
}