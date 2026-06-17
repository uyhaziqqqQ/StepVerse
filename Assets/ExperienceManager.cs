using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using Firebase.Auth;
using System.Collections.Generic;
using System;
using System.Collections;

public class ExperienceManager : MonoBehaviour
{
    public static ExperienceManager Instance;

    [Header("XP Math Settings")]
    [SerializeField] private float baseXP = 1000f;
    [SerializeField] private float scalingFactor = 1.4f;

    [Header("Current Player Stats")]
    public int playerLevel = 1;
    public float playerXP = 0f;

    [Header("Current Pet Stats")]
    public int petLevel = 1;
    public float petXP = 0f;

    private string _userId;
    private FirebaseFirestore _db;

    private void Awake()
    {
        // Simple Singleton so other scripts can easily talk to this one
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private IEnumerator Start()
    {
        // 1. Wait a few seconds for your Login script to finish authenticating 'tyty'
        yield return new WaitForSeconds(3f);

        // 2. Wake up the database
        _db = FirebaseFirestore.DefaultInstance;

        // 3. Grab the REAL, secure User ID directly from Firebase
        if (FirebaseAuth.DefaultInstance != null && FirebaseAuth.DefaultInstance.CurrentUser != null)
        {
            _userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
            Debug.Log("☁️ ExperienceManager securely linked to UID: " + _userId);

            // Now that we know exactly who we are, load our stats!
            LoadStatsFromFirebase();
        }
        else
        {
            Debug.LogError("❌ ExperienceManager failed to find a logged-in user!");
        }
    }

    private void Update()
    {
        // DEVELOPER CHEAT: Press the Spacebar to instantly gain 500 XP AND 500 Steps!
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("CHEAT ACTIVATED: Added 500 XP and 500 Steps!");

            // 1. Add the XP
            AddPlayerXP(500f);

            // 2. Add 500 steps to all three of our tracking buckets!
            PlayerPrefs.SetInt("daily_steps", PlayerPrefs.GetInt("daily_steps", 0) + 500);
            PlayerPrefs.SetInt("weekly_steps", PlayerPrefs.GetInt("weekly_steps", 0) + 500);
            PlayerPrefs.SetInt("monthly_steps", PlayerPrefs.GetInt("monthly_steps", 0) + 500);
            PlayerPrefs.Save(); // Force the device to save the new step counts

            // 3. Find the Home Screen and update the UI
            HomeScreenController homeUI = UnityEngine.Object.FindFirstObjectByType<HomeScreenController>();
            if (homeUI != null)
            {
                homeUI.UpdateLevelUI();
            }
        }
    }

    // --- THE MATH ---
    public float GetRequiredXP(int currentLevel)
    {
        // The Formula: Required XP = BaseXP * (CurrentLevel ^ ScalingFactor)
        return baseXP * Mathf.Pow(currentLevel, scalingFactor);
    }

    // --- PLAYER LEVELING LOGIC ---
    public void AddPlayerXP(float amount)
    {
        playerXP += amount;
        bool leveledUp = false;

        float requiredXP = GetRequiredXP(playerLevel);

        // Keep leveling up if they earned a massive amount of XP at once!
        while (playerXP >= requiredXP)
        {
            playerXP -= requiredXP; // Carry over leftover XP
            playerLevel++;
            requiredXP = GetRequiredXP(playerLevel);
            leveledUp = true;

            Debug.Log($"🎉 PLAYER LEVEL UP! You are now Level {playerLevel}!");
            // TODO: Trigger UI Particle Effects or Pop-ups here!
        }

        if (leveledUp && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("level-up");
        }

        // Only save to the cloud AFTER all the fast math is done
        SaveStatsToFirebase();
    }

    // --- PET LEVELING LOGIC ---
    public void AddPetXP(float amount)
    {
        petXP += amount;
        bool leveledUp = false;

        float requiredXP = GetRequiredXP(petLevel);

        while (petXP >= requiredXP)
        {
            petXP -= requiredXP;
            petLevel++;
            requiredXP = GetRequiredXP(petLevel);
            leveledUp = true;

            Debug.Log($"🐾 PET LEVEL UP! Your companion is now Level {petLevel}!");
        }

        if (leveledUp && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("level-up");
        }

        SaveStatsToFirebase();
    }

    // --- FIREBASE CLOUD SAVING ---
    private void SaveStatsToFirebase()
    {
        if (_db == null || string.IsNullOrEmpty(_userId)) return;

        // Package our stats into a dictionary
        Dictionary<string, object> stats = new Dictionary<string, object>
        {
            { "playerLevel", playerLevel },
            { "playerXP", playerXP },
            { "petLevel", petLevel },
            { "petXP", petXP }
        };

        // Send to Firestore database -> Collection "Users" -> Document "Haziq123"
        DocumentReference docRef = _db.Collection("Users").Document(_userId);
        docRef.SetAsync(stats, SetOptions.MergeAll).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted) Debug.Log("☁️ Stats saved securely to Firebase!");
            else Debug.LogError("❌ Firebase Save Failed: " + task.Exception);
        });
    }

    private void LoadStatsFromFirebase()
    {
        if (_db == null || string.IsNullOrEmpty(_userId)) return;

        DocumentReference docRef = _db.Collection("Users").Document(_userId);
        docRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                DocumentSnapshot snap = task.Result;

                // Extract data from the cloud, defaulting to 1 or 0 if it doesn't exist yet
                playerLevel = snap.ContainsField("playerLevel") ? Convert.ToInt32(snap.GetValue<long>("playerLevel")) : 1;
                playerXP = snap.ContainsField("playerXP") ? Convert.ToSingle(snap.GetValue<double>("playerXP")) : 0f;

                petLevel = snap.ContainsField("petLevel") ? Convert.ToInt32(snap.GetValue<long>("petLevel")) : 1;
                petXP = snap.ContainsField("petXP") ? Convert.ToSingle(snap.GetValue<double>("petXP")) : 0f;

                Debug.Log("☁️ Downloaded Stats from Firebase! Player Level: " + playerLevel);

                // TODO: Tell your HomeScreenController to update its UI bars!
            }
        });
    }
}