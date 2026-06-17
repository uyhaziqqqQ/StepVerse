using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

public class MissionsScreenController : MonoBehaviour
{
    public VisualTreeAsset missionCardTemplate;

    [Header("Mission Icons")]
    public Texture2D dailyIcon;    // Drag walking.png here in Inspector!
    public Texture2D weeklyIcon;   // Drag training.png (or runner) here in Inspector!
    public Texture2D monthlyIcon;  // Drag earth.png here in Inspector!

    private Label _txtDailyTimer, _txtWeeklyTimer, _txtMonthlyTimer;
    private VisualElement _dailyContainer, _weeklyContainer, _monthlyContainer;
    private VisualElement _root;

    private void OnEnable()
    {
        _root = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("MissionsRoot");
        if (_root == null) return;

        _txtDailyTimer = _root.Q<Label>("TxtDailyTimer");
        _txtWeeklyTimer = _root.Q<Label>("TxtWeeklyTimer");
        _txtMonthlyTimer = _root.Q<Label>("TxtMonthlyTimer");

        _dailyContainer = _root.Q<VisualElement>("DailyContainer");
        _weeklyContainer = _root.Q<VisualElement>("WeeklyContainer");
        _monthlyContainer = _root.Q<VisualElement>("MonthlyContainer");

        InvokeRepeating(nameof(UpdateTimers), 0f, 1f);
        HideTab();
    }

    public void ShowTab()
    {
        if (_root != null) _root.style.display = DisplayStyle.Flex;
        RefreshMissionData();
    }

    public void HideTab()
    {
        if (_root != null) _root.style.display = DisplayStyle.None;
    }

    private void UpdateTimers()
    {
        DateTime now = DateTime.Now;

        // Daily Timer (Midnight Tonight)
        TimeSpan dailyTime = now.Date.AddDays(1) - now;
        // Removed the emoji from here!
        if (_txtDailyTimer != null) _txtDailyTimer.text = $"RESETS IN {dailyTime.Hours}H {dailyTime.Minutes}M";

        // Weekly Timer (Next Sunday Midnight)
        int daysUntilSunday = ((int)DayOfWeek.Sunday - (int)now.DayOfWeek + 7) % 7;
        if (daysUntilSunday == 0) daysUntilSunday = 7;
        TimeSpan weeklyTime = now.Date.AddDays(daysUntilSunday) - now;
        // Removed the emoji from here!
        if (_txtWeeklyTimer != null) _txtWeeklyTimer.text = $"ENDS IN {weeklyTime.Days}D {weeklyTime.Hours}H";

        // Monthly Timer (1st of Next Month)
        DateTime nextMonth = new DateTime(now.Year, now.Month, 1).AddMonths(1);
        TimeSpan monthlyTime = nextMonth - now;
        // Removed the emoji from here!
        if (_txtMonthlyTimer != null) _txtMonthlyTimer.text = $"ENDS IN {monthlyTime.Days}D {monthlyTime.Hours}H";
    }

    public void RefreshMissionData()
    {
        if (MissionManager.Instance == null || missionCardTemplate == null) return;

        _dailyContainer.Clear();
        _weeklyContainer.Clear();
        _monthlyContainer.Clear();

        List<MissionManager.Mission> allMissions = MissionManager.Instance.activeMissions;

        // 1. Separate into buckets
        List<MissionManager.Mission> dailies = allMissions.FindAll(m => m.frequency == "daily");
        List<MissionManager.Mission> weeklies = allMissions.FindAll(m => m.frequency == "weekly");
        List<MissionManager.Mission> monthlies = allMissions.FindAll(m => m.frequency == "monthly");

        // 2. Generate unique seeds based on the date so they lock in!
        DateTime now = DateTime.Now;
        int dailySeed = now.Year * 1000 + now.DayOfYear;
        int weeklySeed = now.Year * 52 + (now.DayOfYear / 7);
        int monthlySeed = now.Year * 12 + now.Month;

        // 3. Shuffle and pick amounts (3 Daily, 1 Weekly, 1 Monthly)
        List<MissionManager.Mission> pickedDailies = GetRandomMissions(dailies, 3, dailySeed);
        List<MissionManager.Mission> pickedWeeklies = GetRandomMissions(weeklies, 1, weeklySeed);
        List<MissionManager.Mission> pickedMonthlies = GetRandomMissions(monthlies, 1, monthlySeed);

        int currentSteps = PlayerPrefs.GetInt("DailySteps", 6000);

        // 4. Spawn them with their respective colors
        // We now pass your Texture2D images instead of the emoji strings!
        foreach (var m in pickedDailies) SpawnCard(m, _dailyContainer, currentSteps, "border-yellow", "bg-yellow", dailyIcon);
        foreach (var m in pickedWeeklies) SpawnCard(m, _weeklyContainer, currentSteps, "border-purple", "bg-purple", weeklyIcon);
        foreach (var m in pickedMonthlies) SpawnCard(m, _monthlyContainer, currentSteps, "border-orange", "bg-orange", monthlyIcon);
    }

    private void SpawnCard(MissionManager.Mission mission, VisualElement container, int fallbackSteps, string borderColorClass, string bgColorClass, Texture2D iconImage)
    {
        VisualElement newCard = missionCardTemplate.Instantiate();

        // Find visual elements
        VisualElement cardRoot = newCard.Q<VisualElement>("CardRoot");
        VisualElement iconBox = newCard.Q<VisualElement>("IconBox");
        VisualElement fillBar = newCard.Q<VisualElement>("FillBar");
        Button btnClaim = newCard.Q<Button>("BtnClaim");
        Label txtClaim = btnClaim.Q<Label>();

        // Apply dynamic colors
        cardRoot.AddToClassList(borderColorClass);
        iconBox.AddToClassList(bgColorClass);
        fillBar.AddToClassList(bgColorClass);

        // --- 1. DETERMINE WHICH STEPS TO CHECK ---
        int stepsToCheck = 0;
        if (mission.trackingType == "monthly_steps")
            stepsToCheck = PlayerPrefs.GetInt("monthly_steps", 0);
        else if (mission.trackingType == "weekly_steps")
            stepsToCheck = PlayerPrefs.GetInt("weekly_steps", 0);
        else
            stepsToCheck = PlayerPrefs.GetInt("daily_steps", 0);

        int currentHour = DateTime.Now.Hour;
        bool isWithinTimeWindow = (currentHour >= mission.startHour && currentHour < mission.endHour);

        // --- 2. THE CORRECTED TEXT LAYOUT ---

        // Grab the VisualElement we made and apply the dynamic image!
        VisualElement missionIcon = newCard.Q<VisualElement>("MissionIcon");
        if (missionIcon != null && iconImage != null)
        {
            missionIcon.style.backgroundImage = new StyleBackground(iconImage);
        }

        newCard.Q<Label>("TxtTitle").text = mission.title;

        // Lock the left side strictly to 0, and right side strictly to the Target!
        newCard.Q<Label>("TxtCurrent").text = "0";
        newCard.Q<Label>("TxtTarget").text = mission.targetAmount.ToString("N0");

        // Clamp the display steps so it doesn't say "Progress: 6000 / 1500"
        int displaySteps = Mathf.Min(stepsToCheck, (int)mission.targetAmount);

        // Put the exact progress in the description text where it's easy to read
        string timeLimit = (mission.startHour > 0 || mission.endHour < 24) ? $"\n⏰ Available {mission.startHour}:00 - {mission.endHour}:00" : "";
        newCard.Q<Label>("TxtDesc").text = $"Progress: {displaySteps:N0} / {mission.targetAmount:N0} Steps\nReward: {mission.rewardXP} XP{timeLimit}";

        // --- 3. THE DYNAMIC ANIMATION BAR ---
        fillBar.style.width = Length.Percent(0);

        float percent = Mathf.Clamp01((float)stepsToCheck / mission.targetAmount) * 100f;

        newCard.schedule.Execute(() => {
            fillBar.style.width = Length.Percent(percent);
        }).StartingIn(100);

        // --- 4. CLAIM LOGIC ---
        if (mission.isClaimed)
        {
            btnClaim.style.display = DisplayStyle.None;
        }
        else if (!isWithinTimeWindow)
        {
            btnClaim.style.display = DisplayStyle.Flex;
            btnClaim.SetEnabled(false);
            // Removed ⏰ emoji from here so it doesn't double up
            txtClaim.text = "Time Locked";
            btnClaim.style.backgroundColor = new StyleColor(Color.gray);
        }
        else if (stepsToCheck >= mission.targetAmount)
        {
            btnClaim.style.display = DisplayStyle.Flex;
            btnClaim.SetEnabled(true);
            btnClaim.clicked += () => ClaimReward(mission, btnClaim);
        }

        container.Add(newCard);
    }

    private void ClaimReward(MissionManager.Mission mission, Button buttonPressed)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("claim-rewards");
        if (ExperienceManager.Instance != null) ExperienceManager.Instance.AddPlayerXP(mission.rewardXP);
        mission.isClaimed = true;
        buttonPressed.style.display = DisplayStyle.None;
    }

    // A "Fisher-Yates" Deterministic Shuffle
    private List<MissionManager.Mission> GetRandomMissions(List<MissionManager.Mission> source, int count, int seed)
    {
        if (source.Count == 0) return new List<MissionManager.Mission>();

        System.Random rnd = new System.Random(seed);
        List<MissionManager.Mission> shuffled = new List<MissionManager.Mission>(source);

        int n = shuffled.Count;
        while (n > 1)
        {
            n--;
            int k = rnd.Next(n + 1);
            var value = shuffled[k];
            shuffled[k] = shuffled[n];
            shuffled[n] = value;
        }

        return shuffled.GetRange(0, Mathf.Min(count, shuffled.Count));
    }
}