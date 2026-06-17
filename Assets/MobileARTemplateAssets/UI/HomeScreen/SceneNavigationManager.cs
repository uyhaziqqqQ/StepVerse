using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNavigationManager : MonoBehaviour
{
    // We make this a Singleton so any UI button can easily call it
    public static SceneNavigationManager Instance { get; private set; }

    private void Awake()
    {
        // Standard Singleton enforcement
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // No need for DontDestroyOnLoad here if it's attached to the same 
        // GameObject as your StepCounter, which already protects it!
    }

    /// <summary>
    /// Call this from your Dashboard UI to launch the AR/Map environment.
    /// </summary>
    public void LaunchGameScene()
    {
        Debug.Log("Suspending UI. Initializing AR and Map Environment...");
        SceneManager.LoadScene("GameScene");
    }

    /// <summary>
    /// Call this from a "Back" button inside your GameScene to return to the UI.
    /// </summary>
    public void ReturnToDashboard()
    {
        Debug.Log("Suspending Map. Returning to Main Dashboard...");
        SceneManager.LoadScene("MainMenu");
    }
}