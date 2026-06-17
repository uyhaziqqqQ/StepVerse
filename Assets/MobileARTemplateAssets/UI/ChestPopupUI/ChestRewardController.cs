using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

[RequireComponent(typeof(UIDocument))]
public class ChestRewardController : MonoBehaviour
{
    public static ChestRewardController Instance;

    private VisualElement _overlay;
    private Label _txtTreats;
    private Label _txtPowerUps;
    private Button _btnClaim;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _overlay = root.Q<VisualElement>("RewardOverlay");
        _txtTreats = root.Q<Label>("TxtTreats");
        _txtPowerUps = root.Q<Label>("TxtPowerUps");
        _btnClaim = root.Q<Button>("BtnClaim");

        if (_btnClaim != null)
        {
            _btnClaim.clicked += () => 
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("claim-rewards");
                _overlay.AddToClassList("hidden"); // Hide when claimed
            };
        }
    }

    // Call this from your AR Chest when tapped!
    public void OpenChest()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("popup-chest-sound");

        if (InventoryManager.Instance == null) return;

        // 1. ROLL TREATS (Flat 10% chance for 10, 20, 30... 100)
        // Random.Range(1, 11) picks a number 1 through 10. Multiply by 10 = 10 to 100 Treats!
        int treatsWon = UnityEngine.Random.Range(1, 11) * 10;
        InventoryManager.Instance.TotalTreats += treatsWon;

        // 2. ROLL POWER-UP QUANTITY (Weighted Distribution)
        int powerUpRoll = UnityEngine.Random.Range(1, 101); // 1 to 100
        int amountToGive = 1;

        if (powerUpRoll <= 50) amountToGive = 1;      // 50% chance
        else if (powerUpRoll <= 70) amountToGive = 2; // 20% chance
        else if (powerUpRoll <= 85) amountToGive = 3; // 15% chance
        else if (powerUpRoll <= 95) amountToGive = 4; // 10% chance
        else amountToGive = 5;                        // 5% chance

        // 3. ROLL SPECIFIC POWER-UPS (Pure Random)
        Dictionary<string, int> wonItems = new Dictionary<string, int>();
        string[] availablePowerUps = { "Level Boost", "Streak Save", "Double Steps", "Loot Radar", "Treat Piñata", "Auto-Fetch" };

        for (int i = 0; i < amountToGive; i++)
        {
            int itemIndex = UnityEngine.Random.Range(0, availablePowerUps.Length);
            string itemWon = availablePowerUps[itemIndex];

            // Add to the visual list
            if (wonItems.ContainsKey(itemWon)) wonItems[itemWon]++;
            else wonItems[itemWon] = 1;

            // Add to the actual inventory
            switch (itemWon)
            {
                case "Level Boost": InventoryManager.Instance.LvlBoostCount++; break;
                case "Streak Save": InventoryManager.Instance.StreakSaveCount++; break;
                case "Double Steps": InventoryManager.Instance.DoubleStepsCount++; break;
                case "Loot Radar": InventoryManager.Instance.LootRadarCount++; break;
                case "Treat Piñata": InventoryManager.Instance.TreatPinataCount++; break;
                case "Auto-Fetch": InventoryManager.Instance.AutoFetchCount++; break;
            }
        }

        InventoryManager.Instance.SaveInventory();

        // 4. UPDATE UI & SHOW POPUP
        _txtTreats.text = $"+{treatsWon} Treats";

        string powerUpText = "";
        foreach (var item in wonItems)
        {
            powerUpText += $"- {item.Value}x {item.Key}\n";
        }
        _txtPowerUps.text = powerUpText;

        _overlay.RemoveFromClassList("hidden"); // Show the popup!
        Debug.Log("Chest Opened! Loot awarded.");
    }
}