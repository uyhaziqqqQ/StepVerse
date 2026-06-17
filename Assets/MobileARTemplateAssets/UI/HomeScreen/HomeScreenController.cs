using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class HomeScreenController : MonoBehaviour
{
    [Header("Navigation Links")]
    [SerializeField] private UserProfileController userProfileTab;
    [SerializeField] private UserProfileController profileScreenController;
    [SerializeField] private StatsScreenController statsScreenController;
    [Header("Map Integration")]
    [SerializeField] private RenderTexture mapTexture;
    private VisualElement _mapRenderSurface;
    [Header("Alert Popup")]
    private VisualElement _infoAlertModal;
    private Label _txtAlertTitle, _txtAlertDesc;
    private VisualElement _imgInfoIcon;

    [Header("Inventory Labels")]
    private Label _txtCountLvlBoost, _txtCountStreakSave, _txtCountDoubleSteps;
    private Label _txtCountLootRadar, _txtCountTreatPinata, _txtCountAutoFetch;

    [Header("Confirmation Popup")]
    private VisualElement _confirmAlertModal;
    private Label _txtConfirmTitle, _txtConfirmDesc;
    private VisualElement _imgConfirmIcon;
    private Button _btnConfirmYes, _btnConfirmNo;

    [Header("Inventory Use Buttons")]
    private Button _btnUseTreatPinata;
    private Button _btnUseLvlBoost;
    private Button _btnUseStreakSave;
    private Button _btnUseDoubleSteps;
    private Button _btnUseLootRadar;
    private Button _btnUseAutoFetch;

    // This is the magic variable! It stores the specific code we want to run if they click 'Yes'.
    private System.Action _onConfirmAction;
    private int _lastKnownSteps = 0;
    private VisualElement _rootContainer;
    private Button _btnTopProfile;
    private Button _btnStepStats;
    private VisualElement _inventoryModal;
    private Button _btnInventory, _btnCloseInventory;

    private Label _txtGreeting, _txtStepCount, _txtDistance, _txtDailyGoal, _txtPlayerRank;
    private VisualElement _dailyProgressBar;
    private Label _txtPlayerLevel;
    private VisualElement _xpProgressBar;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _rootContainer = root.Q<VisualElement>("HomeScreenRoot");

        _txtGreeting = root.Q<Label>("TxtGreeting");
        _txtStepCount = root.Q<Label>("TxtStepCount");
        _txtDistance = root.Q<Label>("TxtDistance");
        _txtDailyGoal = root.Q<Label>("TxtDailyGoal");
        _dailyProgressBar = root.Q<VisualElement>("DailyProgressBar");
        _txtPlayerRank = root.Q<Label>("TxtPlayerRank");

        _txtPlayerLevel = root.Q<Label>("TxtPlayerLevel");
        _xpProgressBar = root.Q<VisualElement>("XpProgressBar");

        _btnTopProfile = root.Q<Button>("BtnTopProfile");
        _btnTopProfile.clicked += OpenProfileScreen;
        _btnStepStats = root.Q<Button>("BtnStepStats");

        _mapRenderSurface = root.Q<VisualElement>("MapRenderSurface");
        _inventoryModal = root.Q<VisualElement>("InventoryModal");
        _btnInventory = root.Q<Button>("BtnInventory");
        _btnCloseInventory = root.Q<Button>("BtnCloseInventory");

        if (_btnInventory != null) _btnInventory.clicked += OpenInventory;
        if (_btnCloseInventory != null) _btnCloseInventory.clicked += CloseInventory;

        // Plug the video feed into the UI background!
        if (_mapRenderSurface != null && mapTexture != null)
        {
            _mapRenderSurface.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(mapTexture));
        }

        Button btnExplore = root.Q<Button>("BtnExplore");
        if (btnExplore != null) btnExplore.clicked += () => { if (SceneNavigationManager.Instance != null) SceneNavigationManager.Instance.LaunchGameScene(); };
        if (_btnStepStats != null) _btnStepStats.clicked += HandleStepStatsClicked;

        UpdateLevelUI();

        _infoAlertModal = root.Q<VisualElement>("InfoAlertModal");
        _txtAlertTitle = root.Q<Label>("TxtAlertTitle");
        _txtAlertDesc = root.Q<Label>("TxtAlertDesc");
        _imgInfoIcon = root.Q<VisualElement>("ImgInfoIcon");

        // "Tap anywhere to close" magic! We put a click listener on the entire dark background.
        if (_infoAlertModal != null)
        {
            _infoAlertModal.RegisterCallback<ClickEvent>(ev => CloseInfoAlert());
        }

        // Wire up the 6 specific Info buttons (Passing the custom text!)
        Button btnInfoLvlBoost = root.Q<Button>("BtnInfoLvlBoost");
        if (btnInfoLvlBoost != null) btnInfoLvlBoost.clicked += () => ShowInfoAlert("Lvl Boost", "Instantly skips the grind by raising your pet 3 levels!", "icon-star");

        Button btnInfoStreakSave = root.Q<Button>("BtnInfoStreakSave");
        if (btnInfoStreakSave != null) btnInfoStreakSave.clicked += () => ShowInfoAlert("Streak Save", "Protects your daily streak! If you miss a day, this will automatically be consumed.", "icon-fire");

        Button btnInfoDoubleSteps = root.Q<Button>("BtnInfoDoubleSteps");
        if (btnInfoDoubleSteps != null) btnInfoDoubleSteps.clicked += () => ShowInfoAlert("2x Steps", "For the next 30 minutes, every physical step you take counts as two!", "icon-lighting");

        Button btnInfoLootRadar = root.Q<Button>("BtnInfoLootRadar");
        if (btnInfoLootRadar != null) btnInfoLootRadar.clicked += () => ShowInfoAlert("Loot Radar", "Expands your GPS map radius for 1 hour so you can reach distant chests from home.", "icon-satelite");

        Button btnInfoTreatPinata = root.Q<Button>("BtnInfoTreatPinata");
        if (btnInfoTreatPinata != null) btnInfoTreatPinata.clicked += () => ShowInfoAlert("Treat Piñata", "Instantly drops a massive burst of Treats into your inventory!", "icon-party");

        Button btnInfoAutoFetch = root.Q<Button>("BtnInfoAutoFetch");
        if (btnInfoAutoFetch != null) btnInfoAutoFetch.clicked += () => ShowInfoAlert("Auto-Fetch", "For 24 hours, your pet will collect items while your phone is locked in your pocket!", "icon-robot");

        _txtCountLvlBoost = root.Q<Label>("TxtCountLvlBoost");
        _txtCountStreakSave = root.Q<Label>("TxtCountStreakSave");
        _txtCountDoubleSteps = root.Q<Label>("TxtCountDoubleSteps");

        _txtCountLootRadar = root.Q<Label>("TxtCountLootRadar");
        _txtCountTreatPinata = root.Q<Label>("TxtCountTreatPinata");
        _txtCountAutoFetch = root.Q<Label>("TxtCountAutoFetch");

        _confirmAlertModal = root.Q<VisualElement>("ConfirmAlertModal");
        _txtConfirmTitle = root.Q<Label>("TxtConfirmTitle");
        _txtConfirmDesc = root.Q<Label>("TxtConfirmDesc");
        _imgConfirmIcon = root.Q<VisualElement>("ImgConfirmIcon");
        _btnConfirmYes = root.Q<Button>("BtnConfirmYes");
        _btnConfirmNo = root.Q<Button>("BtnConfirmNo");

        _btnUseTreatPinata = root.Q<Button>("BtnUseTreatPinata");
        if (_btnUseTreatPinata != null) _btnUseTreatPinata.clicked += UseTreatPinata;

        _btnUseLvlBoost = root.Q<Button>("BtnUseLvlBoost");
        if (_btnUseLvlBoost != null) _btnUseLvlBoost.clicked += UseLvlBoost;

        _btnUseStreakSave = root.Q<Button>("BtnUseStreakSave");
        if (_btnUseStreakSave != null) _btnUseStreakSave.clicked += UseStreakSave;

        _btnUseDoubleSteps = root.Q<Button>("BtnUseDoubleSteps");
        if (_btnUseDoubleSteps != null) _btnUseDoubleSteps.clicked += UseDoubleSteps;

        _btnUseLootRadar = root.Q<Button>("BtnUseLootRadar");
        if (_btnUseLootRadar != null) _btnUseLootRadar.clicked += UseLootRadar;

        _btnUseAutoFetch = root.Q<Button>("BtnUseAutoFetch");
        if (_btnUseAutoFetch != null) _btnUseAutoFetch.clicked += UseAutoFetch;

        if (_btnConfirmYes != null) _btnConfirmYes.clicked += ExecuteConfirmAction;
        if (_btnConfirmNo != null) _btnConfirmNo.clicked += CloseConfirmAlert;

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryUpdated += RefreshBackpackUI;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe the stats and profile buttons
        if (_btnStepStats != null) _btnStepStats.clicked -= HandleStepStatsClicked;
        if (_btnTopProfile != null) _btnTopProfile.clicked -= OpenProfileScreen;

        // Unsubscribe the new inventory buttons!
        if (_btnInventory != null) _btnInventory.clicked -= OpenInventory;
        if (_btnCloseInventory != null) _btnCloseInventory.clicked -= CloseInventory;

        if(_btnUseTreatPinata != null) _btnUseTreatPinata.clicked -= UseTreatPinata;
        
        if (_btnUseLvlBoost != null) _btnUseLvlBoost.clicked -= UseLvlBoost;

        
        if (_btnUseStreakSave != null) _btnUseStreakSave.clicked -= UseStreakSave;

        
        if (_btnUseDoubleSteps != null) _btnUseDoubleSteps.clicked -= UseDoubleSteps;

        
        if (_btnUseLootRadar != null) _btnUseLootRadar.clicked -= UseLootRadar;


        if (_btnUseAutoFetch != null) _btnUseAutoFetch.clicked -= UseAutoFetch;

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryUpdated -= RefreshBackpackUI;
        }
    }

    private string GetRankName(int level)
    {
        if (level < 10) return "Junior Walker";
        if (level < 25) return "Senior Explorer";
        if (level < 50) return "Master Adventurer";
        return "Legendary Step Master";
    }

    public void ShowHomeLayer()
    {
        // Target the inner container specifically!
        if (_rootContainer != null)
        {
            _rootContainer.style.display = DisplayStyle.Flex;
            _rootContainer.RemoveFromClassList("screen-hidden-right"); // Strip away the hidden class
        }

        string username = PlayerPrefs.GetString("Username", "Walker");
        if (_txtGreeting != null) _txtGreeting.text = $"Good Morning, {username}!";

        UpdateStepData(_lastKnownSteps);
        UpdateLevelUI();
    }

    public void HideHomeLayer()
    {
        if (_rootContainer != null)
        {
            _rootContainer.style.display = DisplayStyle.None;
        }
    }

    private void OpenProfileScreen()
    {
        // Hide the current Home screen
        HideHomeLayer();

        // Show the profile screen! (Make sure the variable name matches what you called it at the top of your script)
        if (profileScreenController != null)
        {
            profileScreenController.ShowTab(); // Or whatever your show method is called!
        }
    }

    private void OpenInventory()
    {
        // Get the fresh numbers!
        RefreshBackpackUI();

        // Turn it on and slide it up
        _inventoryModal.style.display = DisplayStyle.Flex;
        _inventoryModal.schedule.Execute(() => _inventoryModal.RemoveFromClassList("modal-hidden")).StartingIn(10);
    }

    private void CloseInventory()
    {
        _inventoryModal.AddToClassList("modal-hidden"); // Slide it down and fade out
        // Wait for the 0.4s animation to finish before turning the display fully off
        _inventoryModal.schedule.Execute(() => _inventoryModal.style.display = DisplayStyle.None).StartingIn(400);
    }

    private void ShowInfoAlert(string title, string description, string iconClass = "")
    {
        if (AudioManager.Instance != null && (iconClass == "icon-error" || iconClass == "icon-stop"))
        {
            AudioManager.Instance.PlaySFX("error-sound");
        }

        // 1. Swap the text out
        _txtAlertTitle.text = title;
        _txtAlertDesc.text = description;

        if (_imgInfoIcon != null)
        {
            _imgInfoIcon.ClearClassList();
            _imgInfoIcon.AddToClassList("alert-icon");
            if (!string.IsNullOrEmpty(iconClass))
                _imgInfoIcon.AddToClassList(iconClass);
        }

        // 2. Turn the screen on and fade it in
        _infoAlertModal.style.display = DisplayStyle.Flex;
        _infoAlertModal.schedule.Execute(() => _infoAlertModal.RemoveFromClassList("alert-hidden")).StartingIn(10);
    }

    private void CloseInfoAlert()
    {
        // 1. Fade it out
        _infoAlertModal.AddToClassList("alert-hidden");

        // 2. Turn the display off after the CSS fade finishes (0.2 seconds)
        _infoAlertModal.schedule.Execute(() => _infoAlertModal.style.display = DisplayStyle.None).StartingIn(200);
    }

    public void UpdateStepData(int currentSteps)
    {
        _lastKnownSteps = currentSteps;
        if (_txtStepCount != null) _txtStepCount.text = currentSteps.ToString("N0");

        float dailyGoal = PlayerPrefs.GetFloat("DailyStepGoal", 10000f);
        if (_txtDailyGoal != null) _txtDailyGoal.text = dailyGoal.ToString("N0");

        if (_dailyProgressBar != null)
        {
            float progressPercentage = Mathf.Clamp01((float)currentSteps / dailyGoal) * 100f;
            _dailyProgressBar.style.width = Length.Percent(progressPercentage);
        }
    }

    public void UpdateDistanceData(float totalMetersWalked)
    {
        float kilometers = totalMetersWalked / 1000f;
        if (_txtDistance != null) _txtDistance.text = $"{kilometers:F1} km travelled";
        UpdateLevelUI();
    }

    public void UpdateLevelUI()
    {
        if (ExperienceManager.Instance == null) return;
        int currentLevel = ExperienceManager.Instance.playerLevel;
        float currentXP = ExperienceManager.Instance.playerXP;
        float requiredXP = ExperienceManager.Instance.GetRequiredXP(currentLevel);

        if (_txtPlayerLevel != null) _txtPlayerLevel.text = $"Lvl {currentLevel}";
        if (_xpProgressBar != null) _xpProgressBar.style.width = Length.Percent(Mathf.Clamp01(currentXP / requiredXP) * 100f);
        if (_txtPlayerRank != null) _txtPlayerRank.text = GetRankName(currentLevel);
    }

    private void ShowConfirmAlert(string title, string description, string iconClass, System.Action actionToRun)
    {
        _txtConfirmTitle.text = title;
        _txtConfirmDesc.text = description;
        _onConfirmAction = actionToRun; // Save the instructions for later

        if (_imgConfirmIcon != null)
        {
            _imgConfirmIcon.ClearClassList();
            _imgConfirmIcon.AddToClassList("alert-icon");
            if (!string.IsNullOrEmpty(iconClass))
                _imgConfirmIcon.AddToClassList(iconClass);
        }

        _confirmAlertModal.style.display = DisplayStyle.Flex;
        _confirmAlertModal.schedule.Execute(() => _confirmAlertModal.RemoveFromClassList("alert-hidden")).StartingIn(10);
    }

    private void CloseConfirmAlert()
    {
        _confirmAlertModal.AddToClassList("alert-hidden");
        _confirmAlertModal.schedule.Execute(() => _confirmAlertModal.style.display = DisplayStyle.None).StartingIn(200);
    }

    private void ExecuteConfirmAction()
    {
        // If they click 'Yes', run whatever code we stored in the variable, then close the popup!
        _onConfirmAction?.Invoke();
        CloseConfirmAlert();
    }

    // --- POWER UP LOGIC ---

    private void UseTreatPinata()
    {
        if (InventoryManager.Instance.TreatPinataCount > 0)
        {
            ShowConfirmAlert(
                "Smash the Piñata?",
                "Do you want to crack open 1x Treat Piñata? It's going to rain 500 Treats!",
                "icon-party",
                () =>
                {
                    // 1. Deduct the item
                    InventoryManager.Instance.TreatPinataCount--;

                    // 2. THE EFFECT: Add the currency to the wallet bank account!
                    InventoryManager.Instance.TotalTreats += 500;

                    // 3. Save everything to the phone's hard drive
                    InventoryManager.Instance.SaveInventory();

                    // 4. Refresh both UI counters on the screen instantly
                    _txtCountTreatPinata.text = "x" + InventoryManager.Instance.TreatPinataCount;
                    

                    // 5. Celebration success popup
                    ShowInfoAlert("Sweet Success!", "The Piñata burst! You gained 500 Treats! Go buy your pet something nice.", "icon-party");
                }
            );
        }
        else
        {
            ShowInfoAlert("Out of Items", "You're fresh out of Piñatas! Keep walking to earn more.", "icon-error");
        }
    }

    private void UseLvlBoost()
    {
        // 1. Check if they have the item
        if (InventoryManager.Instance.LvlBoostCount > 0)
        {
            // 2. SAFETY CHECK: Is the pet already maxed out?
            if (PetManager.Instance != null && PetManager.Instance.PetLevel >= PetManager.MAX_LEVEL)
            {
                ShowInfoAlert("Max Level!", "Your pet is already at the maximum level! Save this boost for your next pet.", "icon-stop");
                return; // Stop the code right here. Don't consume the item!
            }

            ShowConfirmAlert("Level Up?", "Use 1x Lvl Boost to instantly jump 3 levels?", "icon-star", () =>
            {
                // 3. Deduct from inventory & Save
                InventoryManager.Instance.LvlBoostCount--;
                InventoryManager.Instance.SaveInventory();
                _txtCountLvlBoost.text = "x" + InventoryManager.Instance.LvlBoostCount;

                // 4. THE EFFECT: Add the levels!
                if (PetManager.Instance != null)
                {
                    PetManager.Instance.AddLevels(3);
                }

                // 5. Success popup
                ShowInfoAlert("Power Overwhelming!", "Your pet instantly gained 3 levels!", "icon-star");
            });
        }
        else
        {
            ShowInfoAlert("Out of Items", "No Level Boosts left!", "icon-error");
        }
    }

    private void UseStreakSave()
    {
        if (InventoryManager.Instance.StreakSaveCount > 0)
        {
            // 1. Check if they actually have a broken streak to restore!
            int lostStreak = PlayerPrefs.GetInt("LostStreak", 0);

            if (lostStreak == 0)
            {
                ShowInfoAlert("No Broken Streak", "Your streak is currently active or you haven't lost one yet. Keep walking!", "icon-check");
                return;
            }

            ShowConfirmAlert("Restore Streak?", $"Use 1x Streak Save to instantly recover your lost {lostStreak}-day streak?", "icon-fire", () =>
            {
                // 2. Deduct item & Save
                InventoryManager.Instance.StreakSaveCount--;
                InventoryManager.Instance.SaveInventory();

                // 3. THE EFFECT: Put the Lost Streak back into the Current Streak!
                PlayerPrefs.SetInt("CurrentStreak", lostStreak);
                PlayerPrefs.SetInt("LostStreak", 0); // Clear the lost streak memory
                PlayerPrefs.Save();

                // 4. Update UI
                _txtCountStreakSave.text = "x" + InventoryManager.Instance.StreakSaveCount;

                ShowInfoAlert("Streak Restored!", $"Welcome back! Your {lostStreak}-day streak has been successfully recovered.", "icon-fire");
            });
        }
        else ShowInfoAlert("Out of Items", "No Streak Saves left!", "icon-error");
    }

    private void UseDoubleSteps()
    {
        if (InventoryManager.Instance.DoubleStepsCount > 0)
        {
            ShowConfirmAlert("Double Steps?", "Activate 2x Steps for the next 30 minutes?", "icon-lighting", () =>
            {
                InventoryManager.Instance.DoubleStepsCount--;
                InventoryManager.Instance.SaveInventory();
                _txtCountDoubleSteps.text = "x" + InventoryManager.Instance.DoubleStepsCount;

                if (StepCounter.Instance != null)
                {
                    StepCounter.Instance.ActivateStepMultiplier(2.0f, 30f);
                }

                ShowInfoAlert("Speed Boost!", "Every step you take is now worth DOUBLE for 30 mins!", "icon-lighting");
            });
        }
        else ShowInfoAlert("Out of Items", "No Double Steps left!", "icon-error");
    }

    private void UseLootRadar()
    {
        if (InventoryManager.Instance.LootRadarCount > 0)
        {
            ShowConfirmAlert("Expand Radar?", "Use 1x Loot Radar to triple (3x) your detection radius for 60 minutes?", "icon-satelite", () =>
            {
                // 1. Deduct item & Save
                InventoryManager.Instance.LootRadarCount--;
                InventoryManager.Instance.SaveInventory();
                _txtCountLootRadar.text = "x" + InventoryManager.Instance.LootRadarCount;

                // 2. THE EFFECT: Tell the Treasure Manager to multiply the radius by 3 for 60 minutes!
                if (TreasureManager.Instance != null)
                {
                    TreasureManager.Instance.ActivateRadar(3.0f, 60f);
                }

                // 3. Success popup
                ShowInfoAlert("Radar Active!", "Your map radius has expanded. Go grab those distant chests!", "icon-satelite");
            });
        }
        else ShowInfoAlert("Out of Items", "No Loot Radars left!", "icon-error");
    }

    private void UseAutoFetch()
    {
        if (InventoryManager.Instance.AutoFetchCount > 0)
        {
            ShowConfirmAlert("Deploy Bot?", "Use 1x Auto-Fetch? Your pet will collect items automatically for 24 hours.", "icon-robot", () =>
            {
                InventoryManager.Instance.AutoFetchCount--;
                InventoryManager.Instance.SaveInventory();
                _txtCountAutoFetch.text = "x" + InventoryManager.Instance.AutoFetchCount;

                // TODO: Call PetManager.Instance.EnableAutoFetch(24); // 24 hours

                ShowInfoAlert("Bot Deployed!", "You can lock your phone. Your pet will do the work for 24 hours!", "icon-robot");
            });
        }
        else ShowInfoAlert("Out of Items", "No Auto-Fetch bots left!", "icon-error");
    }

    private void RefreshBackpackUI()
    {
        // Ask the Brain for the latest numbers and update the UI
        if (InventoryManager.Instance != null)
        {
            _txtCountLvlBoost.text = "x" + InventoryManager.Instance.LvlBoostCount;
            _txtCountStreakSave.text = "x" + InventoryManager.Instance.StreakSaveCount;
            _txtCountDoubleSteps.text = "x" + InventoryManager.Instance.DoubleStepsCount;

            _txtCountLootRadar.text = "x" + InventoryManager.Instance.LootRadarCount;
            _txtCountTreatPinata.text = "x" + InventoryManager.Instance.TreatPinataCount;
            _txtCountAutoFetch.text = "x" + InventoryManager.Instance.AutoFetchCount;
        }
    }

    private void HandleStepStatsClicked() { if (statsScreenController != null) statsScreenController.ShowScreen(); }
}