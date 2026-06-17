using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections.Generic;
using Firebase.Auth;
using System;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance;

    private FirebaseFirestore _db;
    private string _userId;

    // A simple blueprint to hold the data we download
    [System.Serializable]
    public class Mission
    {
        public string id;
        public string title;
        public float targetAmount;
        public float rewardXP;
        public string frequency;
        public bool isClaimed;

        // --- NEW ADVANCED RULES ---
        public string trackingType;
        public int startHour;
        public int endHour;
    }

    [Header("Live Missions")]
    public List<Mission> activeMissions = new List<Mission>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Wait 3.5 seconds to ensure Firebase Auth and ExperienceManager are fully loaded first
        Invoke(nameof(Initialize), 3.5f);
    }

    private void Initialize()
    {
        if (FirebaseAuth.DefaultInstance != null && FirebaseAuth.DefaultInstance.CurrentUser != null)
        {
            _userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
            _db = FirebaseFirestore.DefaultInstance;

            DownloadMissions();
        }
    }

    public void DownloadMissions()
    {
        if (_db == null) return;

        Debug.Log("📡 Downloading Master Mission List...");

        _db.Collection("Missions").GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result != null)
            {
                activeMissions.Clear();
                QuerySnapshot snapshot = task.Result;

                foreach (DocumentSnapshot doc in snapshot.Documents)
                {
                    Dictionary<string, object> data = doc.ToDictionary();

                    // Safely extract the data, providing defaults if fields are missing!
                    Mission newMission = new Mission
                    {
                        id = doc.Id,
                        title = data.ContainsKey("title") ? data["title"].ToString() : "Unknown Mission",
                        targetAmount = data.ContainsKey("targetAmount") ? Convert.ToSingle(data["targetAmount"]) : 0f,
                        rewardXP = data.ContainsKey("rewardXP") ? Convert.ToSingle(data["rewardXP"]) : 0f,
                        frequency = data.ContainsKey("frequency") ? data["frequency"].ToString() : "daily",

                        // NEW ADVANCED DATA:
                        trackingType = data.ContainsKey("trackingType") ? data["trackingType"].ToString() : "daily_steps",
                        startHour = data.ContainsKey("startHour") ? Convert.ToInt32(data["startHour"]) : 0,
                        endHour = data.ContainsKey("endHour") ? Convert.ToInt32(data["endHour"]) : 24,

                        isClaimed = false
                    };

                    activeMissions.Add(newMission);
                }
                Debug.Log($"✅ Successfully loaded {activeMissions.Count} missions from Firebase!");
            }
        });
    }
}