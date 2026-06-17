using UnityEngine;
using System;

public class PetManager : MonoBehaviour
{
    public static PetManager Instance;

    public const int MAX_LEVEL = 25;

    public int PetLevel { get; private set; }
    public int PetXP { get; private set; }

    // NOTE: TreatsAvailable has been completely removed! We use InventoryManager now.

    public event Action OnPetDataChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        LoadPetData();
    }

    private void LoadPetData()
    {
        PetLevel = PlayerPrefs.GetInt("PetLevel", 1);
        PetXP = PlayerPrefs.GetInt("PetXP", 0);
    }

    public void SavePetData()
    {
        PlayerPrefs.SetInt("PetLevel", PetLevel);
        PlayerPrefs.SetInt("PetXP", PetXP);
        PlayerPrefs.Save();
    }

    public int GetXPRequiredForNextLevel()
    {
        // Math: Level 1 = 100XP, Level 2 = 200XP, Level 10 = 1000XP
        return PetLevel * 100;
    }

    public void FeedPet()
    {
        // 1. Ask the InventoryManager if we have treats
        if (InventoryManager.Instance != null && InventoryManager.Instance.TotalTreats > 0)
        {
            // 2. Deduct the treat and save the inventory
            InventoryManager.Instance.TotalTreats--;
            InventoryManager.Instance.SaveInventory();

            // 3. Give the pet XP and save the pet data
            PetXP += 50; // Each treat gives 50 XP
            CheckLevelUp();
            SavePetData();

            // 4. Update the UI
            OnPetDataChanged?.Invoke();
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("pet-eat");
        }
        else
        {
            Debug.Log("Out of Treats!");
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("error-sound");
        }
    }

    private void CheckLevelUp()
    {
        int requiredXP = GetXPRequiredForNextLevel();
        if (PetXP >= requiredXP)
        {
            PetXP -= requiredXP; // Roll over extra XP
            PetLevel++;
            Debug.Log($"🐾 Pet Leveled Up to Level {PetLevel}!");

            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("level-up");

            // Recursive check just in case they gained enough XP to level up twice!
            CheckLevelUp();
        }
    }

    public void AddLevels(int amount)
    {
        PetLevel += amount;

        // Safety catch: Don't let them go past the max level!
        if (PetLevel > MAX_LEVEL)
        {
            PetLevel = MAX_LEVEL;
        }

        SavePetData();
        OnPetDataChanged?.Invoke(); // Tell the UI to update the 3D model!
        Debug.Log($"🚀 Used Level Boost! Pet jumped to Level {PetLevel}!");
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("level-up");
    }

    // Secret cheat codes updated to use InventoryManager
    private void Update()
    {
        // Normal Cheat: Add 5 Treats
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.TotalTreats += 5;
                InventoryManager.Instance.SaveInventory();
                OnPetDataChanged?.Invoke();
                Debug.Log("CHEAT: +5 Treats Added!");
            }
        }

        // MEGA CHEAT: Instantly jump to Level 25 to test the Evolved Form!
        if (Input.GetKeyDown(KeyCode.Y))
        {
            PetLevel = 25;
            PetXP = 0;

            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.TotalTreats += 100;
                InventoryManager.Instance.SaveInventory();
            }

            SavePetData();
            OnPetDataChanged?.Invoke();
            Debug.Log("MEGA CHEAT: Leveled up to 25 instantly!");
        }

        // RESET CHEAT: Drop back to Level 1 to test leveling!
        if (Input.GetKeyDown(KeyCode.R))
        {
            PetLevel = 1;
            PetXP = 0;
            SavePetData();
            OnPetDataChanged?.Invoke();
            Debug.Log("CHEAT: Pet Reset to Level 1!");
        }
    }
}