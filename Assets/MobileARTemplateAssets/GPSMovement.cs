using UnityEngine;
using UnityEngine.Android; // Required for Android GPS Permissions
using CesiumForUnity;
using System.Collections;
using System;

public class GPSMovement : MonoBehaviour
{
    [Header("UI Toolkit Integration")]
    [Tooltip("Link the Dashboard Controller here.")]
    [SerializeField] private HomeScreenController homeUI;

    [Header("Settings")]
    public float updateInterval = 2.0f; // 2 seconds saves battery compared to 1

    private double _lastLat = 0;
    private double _lastLon = 0;
    private float _totalDistanceWalked = 0f;

    private const string DISTANCE_KEY = "StepVerse_Distance";
    private const string DATE_KEY = "StepVerse_Date";

    private void Start()
    {
        EvaluateTemporalState();
        LoadDistance();
        StartCoroutine(InitializeLocationService());
    }

    private IEnumerator InitializeLocationService()
    {
        // 1. Request Android Permissions
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);
            yield return new WaitForSeconds(2.0f); // Give user time to click "Allow"
        }

        // 2. Check if hardware is enabled
        if (!Input.location.isEnabledByUser)
        {
            Debug.LogWarning("GPS is disabled by the user hardware settings.");
            yield break;
        }

        // 3. Start Service (Accuracy: 5m, Update distance: 5m)
        Input.location.Start(5f, 5f);

        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (maxWait < 1 || Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.LogError("GPS initialization failed or timed out.");
            yield break;
        }

        Debug.Log("GPS Initialized Successfully. Starting tracking loop...");
        StartCoroutine(UpdatePositionLoop());
    }

    private IEnumerator UpdatePositionLoop()
    {
        while (true)
        {
            if (Input.location.status == LocationServiceStatus.Running)
            {
                double latitude = Input.location.lastData.latitude;
                double longitude = Input.location.lastData.longitude;
                double altitude = 200; // Fixed altitude for map rendering

                // --- DISTANCE TRACKING LOGIC ---
                if (_lastLat != 0 && _lastLon != 0)
                {
                    float distanceMoved = CalculateHaversineDistance(_lastLat, _lastLon, latitude, longitude);

                    // Filter out GPS "jitter" (Only count movement between 1m and 20m per update)
                    if (distanceMoved > 1.0f && distanceMoved < 20.0f)
                    {
                        _totalDistanceWalked += distanceMoved;
                        SaveDistance();

                        // --- NEW: REWARD XP FOR WALKING! ---
                        // If 1km = 100xp, then 1 meter = 0.1xp. 
                        float xpReward = distanceMoved * 0.1f;
                        if (ExperienceManager.Instance != null)
                        {
                            ExperienceManager.Instance.AddPlayerXP(xpReward);
                        }

                        if (homeUI != null)
                        {
                            homeUI.UpdateDistanceData(_totalDistanceWalked);
                        }
                    }
                }

                _lastLat = latitude;
                _lastLon = longitude;

                // --- DYNAMIC SCENE HANDLING ---
                // We dynamically search for the Cesium map. If we are in the MainMenu, this will be null and safely skipped.
                // If we load into the GameScene, it will find the map and teleport it!
                CesiumGeoreference activeMap = UnityEngine.Object.FindFirstObjectByType<CesiumGeoreference>();
                if (activeMap != null)
                {
                    activeMap.SetOriginLongitudeLatitudeHeight(longitude, latitude, altitude);
                }
            }
            yield return new WaitForSeconds(updateInterval);
        }
    }

    private float CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        float R = 6378137f; // Earth's radius in meters
        float dLat = (float)((lat2 - lat1) * Mathf.PI / 180.0);
        float dLon = (float)((lon2 - lon1) * Mathf.PI / 180.0);

        float a = Mathf.Sin(dLat / 2) * Mathf.Sin(dLat / 2) +
                  Mathf.Cos((float)(lat1 * Mathf.PI / 180.0)) * Mathf.Cos((float)(lat2 * Mathf.PI / 180.0)) *
                  Mathf.Sin(dLon / 2) * Mathf.Sin(dLon / 2);

        float c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));
        return R * c;
    }

    // --- PERSISTENCE LOGIC ---
    private void SaveDistance()
    {
        PlayerPrefs.SetFloat(DISTANCE_KEY, _totalDistanceWalked);
        PlayerPrefs.Save();
    }

    private void LoadDistance()
    {
        _totalDistanceWalked = PlayerPrefs.GetFloat(DISTANCE_KEY, 0f);
        if (homeUI != null) homeUI.UpdateDistanceData(_totalDistanceWalked);
    }

    private void EvaluateTemporalState()
    {
        string storedDate = PlayerPrefs.GetString(DATE_KEY, "");
        string todayDate = DateTime.Now.ToString("yyyy-MM-dd");

        if (!string.IsNullOrEmpty(storedDate) && storedDate != todayDate)
        {
            Debug.Log("New Day! Resetting GPS Distance.");
            _totalDistanceWalked = 0f;
            SaveDistance();
        }
    }
}