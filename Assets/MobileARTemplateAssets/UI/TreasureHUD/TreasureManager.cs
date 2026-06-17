using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using System;
using System.Collections;
using System.Collections.Generic; // <--- Required for our UI snapshot!
using UnityEngine.Android;


[System.Serializable]
public struct SafeLocation
{
    public string locationName;
    public double latitude;
    public double longitude;
}

[RequireComponent(typeof(UIDocument))]
public class TreasureManager : MonoBehaviour
{
    public static TreasureManager Instance { get; private set; }

    private float _originalHotZoneRadius;
    private float _radarTimer = 0f;
    private bool _isRadarActive = false;

    // --- UI Toolkit Elements ---
    private VisualElement _hudContainer;
    private VisualElement _alertPanel;
    private Label _radarText;
    private UnityEngine.UIElements.Button _btnARScan;

    [Header("UI GameObject Link")]
    public GameObject mapUI;

    [Header("New Gyro-AR Elements")]
    public Camera gyroCamera;
    public RawImage cameraBackground;
    public GameObject videoCanvas;
    public GameObject mapCamera;
    public GameObject cesiumMap;
    public GameObject treasureModel;
    public Animator chestAnimator;
    public GameObject chestGlowFX;
    public ParticleSystem chestSparksFX;

    [Header("Game State")]
    public bool isGameplayActive = false;

    [Header("Game Design Settings")]
    public float hotZoneRadius = 50f;
    public float captureRadius = 5f;

    [Header("Spawn Settings")]
    public bool useIndoorTesting = true;
    public SafeLocation[] safeLocations;

    private bool isTreasureSpawned = false;
    private bool isTrackingChest = false;
    private double treasureLat;
    private double treasureLon;

    private bool inARMode = false;
    private WebCamTexture backCam;
    private GameObject gyroContainer;
    private float _arTouchCooldown = 0f;

    // --- NEW: UI Toolkit State Saver ---
    private Dictionary<UIDocument, StyleEnum<DisplayStyle>> _uiStates = new Dictionary<UIDocument, StyleEnum<DisplayStyle>>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _hudContainer = root.Q<VisualElement>("HudContainer");
        _alertPanel = root.Q<VisualElement>("AlertPanel");
        _radarText = root.Q<Label>("RadarText");
        _btnARScan = root.Q<UnityEngine.UIElements.Button>("BtnARScan");

        if (_btnARScan != null)
        {
            _btnARScan.clicked += EnterARMode;
        }
    }

    void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        _originalHotZoneRadius = hotZoneRadius;

        // --- CESIUM FIX ---
        if (mapCamera != null)
        {
            Camera cam = mapCamera.GetComponent<Camera>();
            if (cam != null)
            {
                cam.tag = "MainCamera";
                cam.farClipPlane = 50000f;
                cam.cullingMask = -1; // Force camera to render ALL layers (including Cesium tiles)
                
                // Ensure camera is high enough to see terrain even if GPS altitude varies
                if (cam.transform.position.y < 500f) 
                {
                    cam.transform.position = new Vector3(cam.transform.position.x, 800f, cam.transform.position.z);
                }

                cam.clearFlags = CameraClearFlags.SolidColor;
                if (UnityEngine.ColorUtility.TryParseHtmlString("#EAF4D3", out UnityEngine.Color mapBgCol))
                {
                    cam.backgroundColor = mapBgCol;
                }
            }
        }

        if (SystemInfo.supportsGyroscope) Input.gyro.enabled = true;
    }

    void Update()
    {
        // 1. GYRO AR MODE LOGIC
        if (inARMode)
        {
            if (_arTouchCooldown > 0)
            {
                _arTouchCooldown -= Time.deltaTime;
                return;
            }

            if (Input.gyro.enabled && gyroCamera != null)
            {
                Quaternion gyroAttitude = Input.gyro.attitude;
                gyroCamera.transform.localRotation = new Quaternion(gyroAttitude.x, gyroAttitude.y, -gyroAttitude.z, -gyroAttitude.w);
            }

            if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            {
                Vector3 inputPos = Input.GetMouseButtonDown(0) ? Input.mousePosition : (Vector3)Input.GetTouch(0).position;
                Ray ray = gyroCamera.ScreenPointToRay(inputPos);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.transform.gameObject == treasureModel || hit.transform.IsChildOf(treasureModel.transform))
                    {
                        StartCoroutine(CatchTreasureRoutine());
                    }
                }
            }
            return;
        }

        // 2. LOOT RADAR TIMER
        if (_isRadarActive)
        {
            _radarTimer -= Time.deltaTime;
            if (_radarTimer <= 0)
            {
                _isRadarActive = false;
                hotZoneRadius = _originalHotZoneRadius;
                Debug.Log("📡 Loot Radar expired. Radius returning to normal.");
            }
        }

        // 3. NORMAL GPS MATH
        if (!isGameplayActive) return;
        if (Input.location.status != LocationServiceStatus.Running) return;

        double currentLat = Input.location.lastData.latitude;
        double currentLon = Input.location.lastData.longitude;

        if (!isTreasureSpawned)
        {
            if (useIndoorTesting)
            {
                treasureLat = currentLat + 0.00001;
                treasureLon = currentLon;
                TriggerDiscoveryPopup();
            }
            else if (safeLocations.Length > 0)
            {
                foreach (var loc in safeLocations)
                {
                    float distToSafeZone = CalculateHaversineDistance(currentLat, currentLon, loc.latitude, loc.longitude);
                    if (distToSafeZone <= 80f)
                    {
                        treasureLat = loc.latitude;
                        treasureLon = loc.longitude;
                        TriggerDiscoveryPopup();
                        break;
                    }
                }
            }
        }
        else if (isTrackingChest)
        {
            CheckDistanceToTreasure(currentLat, currentLon);
        }
    }

    private void TriggerDiscoveryPopup()
    {
        isTreasureSpawned = true;
        isTrackingChest = false;

        if (ChestDiscoveryController.Instance != null)
        {
            ChestDiscoveryController.Instance.ShowPopup(
                onAccept: () =>
                {
                    isTrackingChest = true;
                    if (_hudContainer != null) _hudContainer.RemoveFromClassList("hidden");
                },
                onDecline: () =>
                {
                    isTreasureSpawned = false;
                }
            );
        }
    }

    public void EnterARMode()
    {
        // 1. Hide HUD & Map
        if (_hudContainer != null) _hudContainer.AddToClassList("hidden");
        if (mapCamera != null) mapCamera.SetActive(false);
        if (cesiumMap != null) cesiumMap.SetActive(false);

        // --- SMART UI TOOLKIT HIDE ---
        if (mapUI != null)
        {
            _uiStates.Clear();
            UIDocument[] docs = mapUI.GetComponentsInChildren<UIDocument>();
            foreach (var doc in docs)
            {
                if (doc.rootVisualElement != null)
                {
                    // Save the current CSS display state
                    _uiStates[doc] = doc.rootVisualElement.style.display;
                    // Hide it safely
                    doc.rootVisualElement.style.display = DisplayStyle.None;
                }
            }
        }

        // 2. Turn on our Top-Layer AR Canvas and Camera
        if (videoCanvas != null) videoCanvas.SetActive(true);
        if (gyroCamera != null) gyroCamera.gameObject.SetActive(true);

        StartCoroutine(StartCameraRoutine());
    }

    private System.Collections.IEnumerator StartCameraRoutine()
    {
        // 1. Permission Check
#if UNITY_ANDROID
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
            yield return new WaitUntil(() => UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera));
        }
#endif

        // 2. Find Back Lens
        string backCamName = "";
        foreach (var device in WebCamTexture.devices)
        {
            if (!device.isFrontFacing) { backCamName = device.name; break; }
        }

        // 3. Request 60 FPS to kill the lag!
        if (backCamName != "")
        {
            backCam = new WebCamTexture(backCamName, 1280, 720, 60);
        }
        else
        {
            backCam = new WebCamTexture(1280, 720, 60);
        }

        // 4. Attach to UI and Boot Lens
        if (cameraBackground != null)
        {
            cameraBackground.color = Color.white;
            cameraBackground.texture = backCam;
        }
        backCam.Play();

        // 5. Wait for lens to push pixels
        int retries = 0;
        while (backCam.width < 100 && retries < 20)
        {
            yield return new WaitForSeconds(0.1f);
            retries++;
        }

        yield return new WaitForEndOfFrame();
        FixCameraOrientation();

        // 6. Connect Gyroscope & FORCE 60Hz REFRESH RATE
        Input.gyro.enabled = true;
        Input.gyro.updateInterval = 0.0167f;

        if (gyroContainer == null) gyroContainer = GameObject.Find("AR_Container");
        if (gyroContainer != null)
        {
            gyroContainer.transform.position = transform.position;
            gyroContainer.transform.rotation = Quaternion.Euler(90f, 90f, 0f);
        }

        yield return null;

        if (treasureModel != null && gyroCamera != null)
        {
            treasureModel.SetActive(true);
            treasureModel.transform.position = gyroCamera.transform.position + (gyroCamera.transform.forward * 2.0f);
            treasureModel.transform.position += new Vector3(0, -0.9f, 0);

            Vector3 targetPosition = gyroCamera.transform.position;
            targetPosition.y = treasureModel.transform.position.y;
            treasureModel.transform.LookAt(targetPosition);
            treasureModel.transform.Rotate(0f, 0f, 0f);
        }

        inARMode = true;
        _arTouchCooldown = 0.5f;

        if (AudioManager.Instance != null) AudioManager.Instance.PlayBGM("ARMODE-bgm");
    }

    void FixCameraOrientation()
    {
        if (backCam == null || !backCam.isPlaying || videoCanvas == null) return;

        cameraBackground.rectTransform.localEulerAngles = new Vector3(0, 0, -backCam.videoRotationAngle);

        RectTransform canvasRect = videoCanvas.GetComponent<RectTransform>();
        float cWidth = canvasRect.rect.width;
        float cHeight = canvasRect.rect.height;

        if (backCam.videoRotationAngle == 90 || backCam.videoRotationAngle == 270)
        {
            cameraBackground.rectTransform.sizeDelta = new Vector2(cHeight, cWidth);
        }
        else
        {
            cameraBackground.rectTransform.sizeDelta = new Vector2(cWidth, cHeight);
        }

        float scaleY = backCam.videoVerticallyMirrored ? -1.2f : 1.2f;
        cameraBackground.rectTransform.localScale = new Vector3(1.2f, scaleY, 1f);
    }

    void CheckDistanceToTreasure(double currentLat, double currentLon)
    {
        float distance = CalculateHaversineDistance(currentLat, currentLon, treasureLat, treasureLon);
        if (_radarText != null) _radarText.text = "Target Distance: " + distance.ToString("F1") + "m";

        if (distance <= hotZoneRadius)
        {
            if (_alertPanel != null) _alertPanel.RemoveFromClassList("hidden");

            if (distance <= captureRadius)
            {
                if (_radarText != null) _radarText.text = "Target Distance: VERY HOT! Scan Area!";
                if (_btnARScan != null) _btnARScan.RemoveFromClassList("hidden");
            }
            else
            {
                if (_btnARScan != null) _btnARScan.AddToClassList("hidden");
            }
        }
        else
        {
            if (_alertPanel != null) _alertPanel.AddToClassList("hidden");
            if (_btnARScan != null) _btnARScan.AddToClassList("hidden");
        }
    }

    private IEnumerator CatchTreasureRoutine()
    {
        inARMode = false; // Stop tracking touches

        if (chestAnimator != null) chestAnimator.SetTrigger("Open");
        else
        {
            Animator childAnim = treasureModel.GetComponentInChildren<Animator>();
            if (childAnim != null) childAnim.SetTrigger("Open");
        }

        // The Light/Glow fix
        if (chestGlowFX != null)
        {
            Light glowLight = chestGlowFX.GetComponent<Light>();
            if (glowLight != null)
            {
                glowLight.intensity = 10f;
                glowLight.range = 1.5f;
            }
            chestGlowFX.SetActive(true);
        }

        if (chestSparksFX != null) chestSparksFX.Play();

        yield return new WaitForSeconds(1.5f);

        if (ChestRewardController.Instance != null) ChestRewardController.Instance.OpenChest();

        yield return new WaitForSeconds(2.0f);

        if (chestAnimator != null)
        {
            chestAnimator.ResetTrigger("Open");
            chestAnimator.Rebind();
            chestAnimator.Update(0f);
        }
        else
        {
            Animator childAnim = treasureModel.GetComponentInChildren<Animator>();
            if (childAnim != null)
            {
                childAnim.ResetTrigger("Open");
                childAnim.Rebind();
                childAnim.Update(0f);
            }
        }

        if (chestGlowFX != null) chestGlowFX.SetActive(false);

        treasureModel.SetActive(false);
        if (gyroCamera != null) gyroCamera.gameObject.SetActive(false);
        if (videoCanvas != null) videoCanvas.SetActive(false);
        if (backCam != null && backCam.isPlaying) backCam.Stop();

        if (mapCamera != null) mapCamera.SetActive(true);
        if (cesiumMap != null) cesiumMap.SetActive(true);

        // --- SMART UI TOOLKIT RESTORE ---
        if (mapUI != null)
        {
            foreach (var kvp in _uiStates)
            {
                if (kvp.Key != null && kvp.Key.rootVisualElement != null)
                {
                    // Restore exact tab states
                    kvp.Key.rootVisualElement.style.display = kvp.Value;
                }
            }
        }

        isTreasureSpawned = false;
        isTrackingChest = false;

        if (AudioManager.Instance != null) AudioManager.Instance.PlayBGM("MAINTHEME-bgm");
    }

    float CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        float R = 6378137f;
        float dLat = (float)((lat2 - lat1) * Mathf.PI / 180.0);
        float dLon = (float)((lon2 - lon1) * Mathf.PI / 180.0);
        float a = Mathf.Sin(dLat / 2) * Mathf.Sin(dLat / 2) + Mathf.Cos((float)(lat1 * Mathf.PI / 180.0)) * Mathf.Cos((float)(lat2 * Mathf.PI / 180.0)) * Mathf.Sin(dLon / 2) * Mathf.Sin(dLon / 2);
        float c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));
        return R * c;
    }

    public void ActivateRadar(float multiplier, float durationMinutes)
    {
        hotZoneRadius = _originalHotZoneRadius * multiplier;
        _radarTimer = durationMinutes * 60f;
        _isRadarActive = true;
        Debug.Log($"📡 Loot Radar Active! Hot Zone expanded to {hotZoneRadius}m for {durationMinutes} mins.");
    }
}