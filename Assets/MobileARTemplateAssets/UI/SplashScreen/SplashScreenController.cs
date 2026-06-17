using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class SplashScreenController : MonoBehaviour
{
    [SerializeField] private ProfileScreenController profileScreenController;
    [SerializeField] private HomeScreenController homeScreenController;
    [SerializeField] private GameObject globalUIGameObject; // <-- ADD THIS

    private Button _btnStartJourney;
    private Button _btnSignUp;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        _btnStartJourney = root.Q<Button>("BtnStartJourney");
        _btnSignUp = root.Q<Button>("BtnSignUp");

        if (_btnStartJourney != null) _btnStartJourney.clicked += OnStartJourneyClicked;
        if (_btnSignUp != null) _btnSignUp.clicked += OnSignUpClicked;
    }

    private void OnDisable()
    {
        if (_btnStartJourney != null) _btnStartJourney.clicked -= OnStartJourneyClicked;
        if (_btnSignUp != null) _btnSignUp.clicked -= OnSignUpClicked;
    }

    private void OnStartJourneyClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("game-start");

        // Evaluate persistent local state
        bool hasRegistered = PlayerPrefs.GetInt("IsRegistered", 0) == 1;

        if (hasRegistered)
        {
            string savedName = PlayerPrefs.GetString("Username", "Walker");
            Debug.Log($"Persistent state verified. Welcome back, {savedName}. Initializing Main Dashboard...");

            // --- TRANSITION LOGIC ---
            if (homeScreenController != null) homeScreenController.ShowHomeLayer();
            if (TreasureManager.Instance != null) TreasureManager.Instance.isGameplayActive = true;
            if (globalUIGameObject != null) globalUIGameObject.SetActive(true); // <-- ADD THIS
            gameObject.SetActive(false); // Turn off this splash screen
        }
        else
        {
            Debug.LogWarning("Access Denied: Unregistered device state detected.");

            // Intercept and force the registration flow
            if (profileScreenController != null)
            {
                profileScreenController.ShowScreen();
            }
        }
    }

    private void OnSignUpClicked()
    {
        if (profileScreenController != null)
        {
            profileScreenController.ShowScreen();
        }
    }
}