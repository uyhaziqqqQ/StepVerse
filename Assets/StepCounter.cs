using UnityEngine;
using System;

public class StepCounter : MonoBehaviour
{
    // Singleton instance
    public static StepCounter Instance { get; private set; }

    [Header("Architecture Bindings")]
    [SerializeField] private HomeScreenController homeUI;

    [Header("Sensor Calibration")]
    public float stepThreshold = 1.2f;
    public float stepDelay = 0.5f;

    [Header("Power-Ups")]
    private float _stepMultiplier = 1.0f; // Defaults to 1 (normal steps)
    private float _multiplierTimer = 0f;
    private bool _isMultiplierActive = false;

    private int _currentSteps = 0;
    private float _timer = 0f;

    private const string STEP_KEY = "StepVerse_Steps";
    private const string DATE_KEY = "StepVerse_Date";

    private void Awake()
    {
        // Enforce the Singleton pattern to guarantee only one tracker exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Preserves this object across all scenes
    }

    private void Start()
    {
        EvaluateTemporalState();
        InitializePersistentData();
        PushDataToInterface();
    }

    private void Update()
    {
        // 1. Hardware Sensor Evaluation
        float accelerationMagnitude = Input.acceleration.magnitude;

        if (accelerationMagnitude > stepThreshold && _timer > stepDelay)
        {
            RegisterPhysicalStep();
            _timer = 0f;
        }

        // 2. PC Simulation Hook (Crucial for Unity Editor testing)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SimulateSteps(50);
        }

        if (_isMultiplierActive)
        {
            _multiplierTimer -= Time.deltaTime; // Time.deltaTime acts as a real-time stopwatch

            if (_multiplierTimer <= 0)
            {
                // Time's up! Turn off the power-up.
                _isMultiplierActive = false;
                _stepMultiplier = 1.0f;
                Debug.Log("⏱️ 2x Steps Power-Up has expired.");
            }
        }

#if UNITY_EDITOR
        // Break Streak Cheat
        if (Input.GetKeyDown(KeyCode.B))
        {
            PlayerPrefs.SetInt("LostStreak", 45); // Pretend we just lost a 45 day streak
            PlayerPrefs.SetInt("CurrentStreak", 0);
            Debug.Log("CHEAT: Simulated a lost 45-day streak!");
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            DateTime yesterday = DateTime.Now.AddDays(-1);
            PlayerPrefs.SetString(DATE_KEY, yesterday.ToString("yyyy-MM-dd"));
            PlayerPrefs.Save();
            Debug.Log($"⏱️ TIME TRAVEL: Set last played date to Yesterday ({yesterday.ToString("yyyy-MM-dd")}). Walk to trigger a new day!");
        }

        // TIME TRAVEL 2: Trick the game into thinking 2 DAYS PASSED (Breaks streak)
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            DateTime twoDaysAgo = DateTime.Now.AddDays(-2);
            PlayerPrefs.SetString(DATE_KEY, twoDaysAgo.ToString("yyyy-MM-dd"));
            PlayerPrefs.Save();
            Debug.Log($"⏱️ TIME TRAVEL: Set last played date to 2 Days Ago ({twoDaysAgo.ToString("yyyy-MM-dd")}). Walk to trigger a broken streak!");
        }
#endif

        _timer += Time.deltaTime;
    }

    private void RegisterPhysicalStep()
    {
        // Calculate the multiplied step (1 step * 2.0x multiplier = 2 steps!)
        int stepsToAdd = Mathf.RoundToInt(1 * _stepMultiplier);
        _currentSteps += stepsToAdd;

        CommitDataToStorage();
        PushDataToInterface();
    }

    private void SimulateSteps(int amount)
    {
        // Apply the multiplier to your cheat codes too!
        int stepsToAdd = Mathf.RoundToInt(amount * _stepMultiplier);
        _currentSteps += stepsToAdd;

        CommitDataToStorage();
        PushDataToInterface();
        Debug.Log($"[Simulated] Added {stepsToAdd} steps. Total: {_currentSteps}");
    }

    private void CommitDataToStorage()
    {
        PlayerPrefs.SetInt(STEP_KEY, _currentSteps);
        PlayerPrefs.SetString(DATE_KEY, DateTime.Now.ToString("yyyy-MM-dd"));
        PlayerPrefs.Save();
    }

    private void InitializePersistentData()
    {
        if (PlayerPrefs.HasKey(STEP_KEY))
        {
            _currentSteps = PlayerPrefs.GetInt(STEP_KEY);
        }
    }

    private void EvaluateTemporalState()
    {
        string storedDate = PlayerPrefs.GetString(DATE_KEY, "");
        string todayDate = DateTime.Now.ToString("yyyy-MM-dd");

        // Temporal mismatch indicates a new chronological day.
        if (!string.IsNullOrEmpty(storedDate) && storedDate != todayDate)
        {
            DateTime lastPlayed = DateTime.Parse(storedDate);
            DateTime today = DateTime.Now;
            int daysMissed = (today.Date - lastPlayed.Date).Days;

            int currentStreak = PlayerPrefs.GetInt("CurrentStreak", 0);

            // Did they play yesterday? (Exactly 1 day missed)
            if (daysMissed == 1)
            {
                currentStreak++; // Add 1 to their streak!
                PlayerPrefs.SetInt("CurrentStreak", currentStreak);
                Debug.Log($"🔥 Streak increased! Current Streak: {currentStreak}");
            }
            // Did they miss 2 or more days? (Streak Broken!)
            else if (daysMissed >= 2)
            {
                if (currentStreak > 0)
                {
                    // Save the broken streak into memory so the item can restore it later!
                    PlayerPrefs.SetInt("LostStreak", currentStreak);
                    Debug.Log($"💔 Streak of {currentStreak} broken and saved to LostStreak.");
                }

                PlayerPrefs.SetInt("CurrentStreak", 0); // Reset current streak to 0
            }

            // Always reset the daily steps
            _currentSteps = 0;
            PlayerPrefs.SetInt(STEP_KEY, 0);
            PlayerPrefs.SetString(DATE_KEY, todayDate);
            PlayerPrefs.Save();
        }
    }

    private void PushDataToInterface()
    {
        // Route the calculation output directly into the UI Toolkit controller
        if (homeUI != null)
        {
            homeUI.UpdateStepData(_currentSteps);
        }
    }

    public void ActivateStepMultiplier(float multiplierValue, float durationInMinutes)
    {
        _stepMultiplier = multiplierValue;
        _multiplierTimer = durationInMinutes * 60f; // Convert minutes to seconds
        _isMultiplierActive = true;

        Debug.Log($"⚡ Power-Up Activated: {_stepMultiplier}x Steps for {durationInMinutes} minutes!");
    }


}