using UnityEngine;
using UnityEngine.UIElements;
using System;

[RequireComponent(typeof(UIDocument))]
public class UserProfileController : MonoBehaviour
{
    private VisualElement _rootContainer;

    // --- Interactive & Settings Elements ---
    private Label _txtUserName;
    private Label _txtGoalValue;
    private Slider _goalSlider;
    private Button _btnSignOut;
    private Toggle _tglSound;
    private Toggle _tglMusic;

    // --- NEW: Dynamic Statistic Elements ---
    private Label _txtProfileSubtitle;
    private Label _txtTotalSteps;
    private Label _txtCurrentPet;
    private Label _txtCurrentPetForm;
    private Label _txtDayStreak;

    private const string GOAL_KEY = "DailyStepGoal";

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _rootContainer = root.Q<VisualElement>("UserProfileRoot");

        // Query Interactive Elements
        _txtUserName = root.Q<Label>("TxtUserName");
        _txtGoalValue = root.Q<Label>("TxtGoalValue");
        _goalSlider = root.Q<Slider>("GoalSlider");
        _btnSignOut = root.Q<Button>("BtnSignOut");
        _tglSound = root.Q<Toggle>("TglSound");
        _tglMusic = root.Q<Toggle>("TglMusic");

        // Query Statistic Elements
        _txtProfileSubtitle = root.Q<Label>("TxtProfileSubtitle");
        _txtTotalSteps = root.Q<Label>("TxtTotalSteps");
        _txtCurrentPet = root.Q<Label>("TxtCurrentPet");
        _txtCurrentPetForm = root.Q<Label>("TxtCurrentPetForm");
        _txtDayStreak = root.Q<Label>("TxtDayStreak");

        // Event Bindings
        if (_goalSlider != null)
        {
            _goalSlider.RegisterValueChangedCallback(evt =>
            {
                // Format the text visually (e.g., 8,000)
                if (_txtGoalValue != null) _txtGoalValue.text = evt.newValue.ToString("N0");

                // Save the exact number to the device's hard drive
                PlayerPrefs.SetFloat(GOAL_KEY, evt.newValue);
                PlayerPrefs.Save();
            });
        }

        if (_tglSound != null) _tglSound.RegisterValueChangedCallback(evt => 
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetSoundEnabled(evt.newValue);
        });
        if (_tglMusic != null) _tglMusic.RegisterValueChangedCallback(evt => 
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetMusicEnabled(evt.newValue);
        });
        if (_btnSignOut != null) _btnSignOut.clicked += HandleSignOut;
    }

    public void ShowTab()
    {
        // Target the absolute root of the document and force it to render
        GetComponent<UIDocument>().rootVisualElement.style.display = DisplayStyle.Flex;

        // Refresh all data dynamically every time the tab is opened
        RefreshProfileData();
    }

    public void HideTab()
    {
        // Force the entire document to hide, completely bypassing USS classes
        GetComponent<UIDocument>().rootVisualElement.style.display = DisplayStyle.None;
    }

    public void RefreshProfileData()
    {
        // 1. Load persistent username
        if (_txtUserName != null) _txtUserName.text = PlayerPrefs.GetString("Username", "Explorer");

        // 2. Load persistent goal (default to 10000 if it's their first time)
        if (_goalSlider != null)
        {
            float savedGoal = PlayerPrefs.GetFloat(GOAL_KEY, 10000f);
            _goalSlider.SetValueWithoutNotify(savedGoal); // Set without triggering the save callback
            if (_txtGoalValue != null) _txtGoalValue.text = savedGoal.ToString("N0");
        }

        // 3. Level & Join Date
        int playerLevel = ExperienceManager.Instance != null ? ExperienceManager.Instance.playerLevel : 1;
        string joinDate = PlayerPrefs.GetString("JoinDate", DateTime.Now.ToString("MMM yyyy"));
        if (_txtProfileSubtitle != null) _txtProfileSubtitle.text = $"Level {playerLevel} • Joined {joinDate}";

        // 4. Update Audio Toggles
        if (AudioManager.Instance != null)
        {
            if (_tglSound != null) _tglSound.SetValueWithoutNotify(AudioManager.Instance.isSoundEnabled);
            if (_tglMusic != null) _tglMusic.SetValueWithoutNotify(AudioManager.Instance.isMusicEnabled);
        }

        // 5. Total Lifetime Steps (Formatted with 'k' suffix if over 1,000)
        int totalSteps = PlayerPrefs.GetInt("TotalLifetimeSteps", 0);
        if (_txtTotalSteps != null)
        {
            if (totalSteps >= 1000)
                _txtTotalSteps.text = (totalSteps / 1000f).ToString("0.#") + "k";
            else
                _txtTotalSteps.text = totalSteps.ToString();
        }

        // 5. Current Pet Info
        string activePetID = PlayerPrefs.GetString("ActivePetID", "Chimpon");
        int petLevel = PetManager.Instance != null ? PetManager.Instance.PetLevel : 1;

        string formName = "Junior Form";
        if (petLevel >= 25) formName = "Evolved Form";
        else if (petLevel >= 10) formName = "Growth Form";

        if (_txtCurrentPet != null) _txtCurrentPet.text = activePetID;
        if (_txtCurrentPetForm != null) _txtCurrentPetForm.text = formName;

        // 6. Day Streak
        int streak = PlayerPrefs.GetInt("CurrentStreak", 0);
        if (_txtDayStreak != null) _txtDayStreak.text = streak.ToString();
    }

    private void HandleSignOut()
    {
        Debug.Log("Signing out...");
        PlayerPrefs.SetInt("IsRegistered", 0);
        PlayerPrefs.Save();

        // TODO: Transition back to Splash Screen
    }
}