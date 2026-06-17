using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

[RequireComponent(typeof(UIDocument))]
public class PetScreenController : MonoBehaviour
{
    [System.Serializable]
    public class PetFamily
    {
        public string petID;
        [Header("3D Models (Main Display)")]
        public GameObject babyModel;
        public GameObject teenModel;
        public GameObject adultModel;

        [Header("2D Sprites (Bottom Icons)")]
        public Sprite babySprite;
        public Sprite teenSprite;
        public Sprite adultSprite;
    }

    [Header("Pet Database")]
    public List<PetFamily> petDatabase;
    public Sprite lockedEvoSprite;

    [Header("3D Render Settings")]
    [SerializeField] private RenderTexture petTexture;
    [SerializeField] private Transform petPivot;
    [SerializeField] private float rotationSpeed = 0.5f;

    private VisualElement _root;
    private VisualElement _petRenderSurface;
    private VisualElement _evoIcon1, _evoIcon2, _evoIcon3;
    private Label _txtPetName, _txtEvo1, _txtEvo2, _txtEvo3;
    private Button _btnFeed, _btnPlay;
    private Label _txtFeedBadge;
    private Label _txtPetLevel, _txtPetStage, _txtXPProgress;
    private VisualElement _xpFillBar;
    private Label _txtTotalTreats;

    private int _lastRenderedLevel = -1;
    private bool _isDraggingPet = false;

    private void OnEnable()
    {
        var rootContainer = GetComponent<UIDocument>().rootVisualElement;
        _root = rootContainer.Q<VisualElement>("PetRoot");

        // UI Hookups
        _petRenderSurface = rootContainer.Q<VisualElement>("PetRenderSurface");
        _evoIcon1 = rootContainer.Q<VisualElement>("EvoIcon1");
        _evoIcon2 = rootContainer.Q<VisualElement>("EvoIcon2");
        _evoIcon3 = rootContainer.Q<VisualElement>("EvoIcon3");

        _btnFeed = rootContainer.Q<Button>("BtnFeed");
        _btnPlay = rootContainer.Q<Button>("BtnPlay");
        _txtFeedBadge = _btnFeed?.Q<Label>();

        _txtPetLevel = rootContainer.Q<Label>("TxtPetLevel");
        _txtPetStage = rootContainer.Q<Label>("TxtPetStage");
        _txtXPProgress = rootContainer.Q<Label>("TxtXPProgress");
        _xpFillBar = rootContainer.Q<VisualElement>("XpFillBar");
        _txtPetName = rootContainer.Q<Label>("TxtPetName");
        _txtTotalTreats = rootContainer.Q<Label>("TxtTotalTreats");
        _txtEvo1 = rootContainer.Q<Label>("TxtEvo1");
        _txtEvo2 = rootContainer.Q<Label>("TxtEvo2");
        _txtEvo3 = rootContainer.Q<Label>("TxtEvo3");

        if (_btnFeed != null) _btnFeed.clicked += OnFeedClicked;
        if (_btnPlay != null) _btnPlay.clicked += () => Debug.Log("Played!");


        // 3D Hookups
        if (_petRenderSurface != null && petTexture != null)
        {
            _petRenderSurface.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(petTexture));
            _petRenderSurface.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _petRenderSurface.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _petRenderSurface.RegisterCallback<PointerUpEvent>(OnPointerUp);
            _petRenderSurface.RegisterCallback<PointerOutEvent>(OnPointerOut);
        }
    }

    private void Start()
    {
        if (PetManager.Instance != null)
        {
            PetManager.Instance.OnPetDataChanged -= RefreshUI;
            PetManager.Instance.OnPetDataChanged += RefreshUI;
        }
        RefreshUI(); // Do an initial refresh to populate data
    }

    private void OnDisable()
    {
        if (PetManager.Instance != null) PetManager.Instance.OnPetDataChanged -= RefreshUI;
        if (_petRenderSurface != null)
        {
            _petRenderSurface.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            _petRenderSurface.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            _petRenderSurface.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            _petRenderSurface.UnregisterCallback<PointerOutEvent>(OnPointerOut);
        }
    }

    // --- SWIPE TO ROTATE LOGIC ---
    private void OnPointerDown(PointerDownEvent evt) { _isDraggingPet = true; _petRenderSurface.CapturePointer(evt.pointerId); }
    private void OnPointerUp(PointerUpEvent evt) { _isDraggingPet = false; _petRenderSurface.ReleasePointer(evt.pointerId); }
    private void OnPointerOut(PointerOutEvent evt) { _isDraggingPet = false; _petRenderSurface.ReleasePointer(evt.pointerId); }
    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (_isDraggingPet && petPivot != null)
        {
            float deltaX = evt.deltaPosition.x;
            petPivot.Rotate(Vector3.up, -deltaX * rotationSpeed, Space.World);
        }
    }

    public void ShowTab()
    {
        GetComponent<UIDocument>().rootVisualElement.style.display = DisplayStyle.Flex;
        RefreshUI();
    }

    public void HideTab()
    {
        GetComponent<UIDocument>().rootVisualElement.style.display = DisplayStyle.None;
    }

    private void OnFeedClicked()
    {
        if (PetManager.Instance != null) PetManager.Instance.FeedPet();
    }

    public void RefreshUI()
    {
        if (PetManager.Instance == null) return;

        int level = PetManager.Instance.PetLevel;
        int currentXP = PetManager.Instance.PetXP;
        int requiredXP = PetManager.Instance.GetXPRequiredForNextLevel();

        // Dynamic currency mapping: Safely extract TotalTreats from the overarching Inventory system.
        int treats = InventoryManager.Instance != null ? InventoryManager.Instance.TotalTreats : 0;

        // Add this new line to update the top wallet!
        if (_txtTotalTreats != null) _txtTotalTreats.text = treats.ToString("N0");

        if (_txtFeedBadge != null) _txtFeedBadge.text = $"Feed (x{treats:N0})";
        if (_btnFeed != null) _btnFeed.SetEnabled(treats > 0);
        if (_txtPetLevel != null) _txtPetLevel.text = $"LEVEL {level}";
        if (_txtXPProgress != null) _txtXPProgress.text = $"{currentXP:N0} / {requiredXP:N0}";

        if (_xpFillBar != null)
        {
            float percent = Mathf.Clamp01((float)currentXP / (float)requiredXP) * 100f;
            _xpFillBar.style.width = Length.Percent(percent);
            _xpFillBar.MarkDirtyRepaint();
        }

        if (level != _lastRenderedLevel)
        {
            UpdateEvolutionVisuals(level);
            _lastRenderedLevel = level;
        }
    }

    private void UpdateEvolutionVisuals(int level)
    {
        if (petDatabase == null || petDatabase.Count == 0) return;

        string activePetID = PlayerPrefs.GetString("ActivePetID", "Chimpon");
        PetFamily activeFamily = petDatabase.Find(p => p.petID == activePetID);
        if (activeFamily == null) activeFamily = petDatabase[0];

        if (_txtPetName != null) _txtPetName.text = activePetID;

        // 1. Turn OFF all 3D models first
        foreach (var family in petDatabase)
        {
            if (family.babyModel != null) family.babyModel.SetActive(false);
            if (family.teenModel != null) family.teenModel.SetActive(false);
            if (family.adultModel != null) family.adultModel.SetActive(false);
        }

        // 2. Determine Stage
        string stageName = "Junior Form";
        GameObject active3DModel = activeFamily.babyModel;

        Sprite evo1 = activeFamily.babySprite;
        Sprite evo2 = lockedEvoSprite;
        Sprite evo3 = lockedEvoSprite;
        string evo2Name = "???";
        string evo3Name = "???";

        if (level >= 25)
        {
            stageName = "Evolved Form";
            active3DModel = activeFamily.adultModel;
            evo2 = activeFamily.teenSprite;
            evo3 = activeFamily.adultSprite;
            evo2Name = "Growth";
            evo3Name = "Evolved";
        }
        else if (level >= 10)
        {
            stageName = "Growth Form";
            active3DModel = activeFamily.teenModel;
            evo2 = activeFamily.teenSprite;
            evo2Name = "Growth";
        }

        // 3. Turn ON correct 3D Model
        if (active3DModel != null) active3DModel.SetActive(true);

        // 4. Update UI Text & 2D Sprites
        if (_txtPetStage != null) _txtPetStage.text = stageName;
        if (_txtEvo1 != null) _txtEvo1.text = "Junior";
        if (_txtEvo2 != null) _txtEvo2.text = evo2Name;
        if (_txtEvo3 != null) _txtEvo3.text = evo3Name;

        if (_evoIcon1 != null && evo1 != null) _evoIcon1.style.backgroundImage = new StyleBackground(evo1);
        if (_evoIcon2 != null && evo2 != null) _evoIcon2.style.backgroundImage = new StyleBackground(evo2);
        if (_evoIcon3 != null && evo3 != null) _evoIcon3.style.backgroundImage = new StyleBackground(evo3);
    }
}