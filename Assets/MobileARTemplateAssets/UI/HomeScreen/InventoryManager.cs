using UnityEngine;
using System;

public class InventoryManager : MonoBehaviour
{
    // This makes it a Singleton, meaning ANY script can talk to it easily!
    public static InventoryManager Instance { get; private set; }

    public event Action OnInventoryUpdated;

    [Header("Inventory Counts")]
    public int LvlBoostCount;
    public int StreakSaveCount;
    public int DoubleStepsCount;
    public int LootRadarCount;
    public int TreatPinataCount;
    public int AutoFetchCount;

    [Header("Currency")]
    public int TotalTreats;

    private void Awake()
    {
        // Setup the Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keeps this alive between scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        LoadInventory();
    }

    // Call this whenever a player buys or finds an item
    public void SaveInventory()
    {
        PlayerPrefs.SetInt("Inv_LvlBoost", LvlBoostCount);
        PlayerPrefs.SetInt("Inv_StreakSave", StreakSaveCount);
        PlayerPrefs.SetInt("Inv_DoubleSteps", DoubleStepsCount);
        PlayerPrefs.SetInt("Inv_LootRadar", LootRadarCount);
        PlayerPrefs.SetInt("Inv_TreatPinata", TreatPinataCount);
        PlayerPrefs.SetInt("Inv_AutoFetch", AutoFetchCount);
        PlayerPrefs.SetInt("Inv_TotalTreats", TotalTreats);
        PlayerPrefs.Save();

        OnInventoryUpdated?.Invoke();
    }

    [ContextMenu("Force Save Inspector Values")]
    public void ForceSaveInspectorValues()
    {
        SaveInventory();
        Debug.Log("Force saved new inventory numbers to the hard drive!");
    }

    // Automatically runs when the game starts to load their saved items
    public void LoadInventory()
    {
        // The '0' means if they have never played before, they start with 0 of that item.
        // (Tip: Change the 0 to a 5 for testing so you have items to play with!)
        LvlBoostCount = PlayerPrefs.GetInt("Inv_LvlBoost", 2);
        StreakSaveCount = PlayerPrefs.GetInt("Inv_StreakSave", 5);
        DoubleStepsCount = PlayerPrefs.GetInt("Inv_DoubleSteps", 1);
        LootRadarCount = PlayerPrefs.GetInt("Inv_LootRadar", 3);
        TreatPinataCount = PlayerPrefs.GetInt("Inv_TreatPinata", 0);
        AutoFetchCount = PlayerPrefs.GetInt("Inv_AutoFetch", 1);
        TotalTreats = PlayerPrefs.GetInt("Inv_TotalTreats", 100);
    }
}