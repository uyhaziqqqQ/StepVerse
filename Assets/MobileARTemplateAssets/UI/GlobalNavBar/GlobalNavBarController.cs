using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class GlobalNavBarController : MonoBehaviour
{
    // Screen References we will connect in the Inspector
    [Header("Screen Layers")]
    [SerializeField] private HomeScreenController homeController;
    [SerializeField] private UserProfileController profileController;
    [SerializeField] private MissionsScreenController missionsController;
    [SerializeField] private PetScreenController petController;

    // Internal Menu State
    private VisualElement _globalNavContainer;
    private Button _btnNavHome;
    private Button _btnNavPet;
    private Button _btnNavMissions;
    private Button _btnNavProfile;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _globalNavContainer = root.Q<VisualElement>("GlobalNavContainer");

        // Nav Button Binding
        _btnNavHome = root.Q<Button>("BtnNavHome");
        _btnNavPet = root.Q<Button>("BtnNavPet");
        _btnNavMissions = root.Q<Button>("BtnNavMissions");
        _btnNavProfile = root.Q<Button>("BtnNavProfile");

        if (_btnNavHome != null) _btnNavHome.clicked += () => SwitchTab("Home");
        if (_btnNavPet != null) _btnNavPet.clicked += () => SwitchTab("Pet");
        if (_btnNavMissions != null) _btnNavMissions.clicked += () => SwitchTab("Missions");
        if (_btnNavProfile != null) _btnNavProfile.clicked += () => SwitchTab("Profile");
    }
    private void Start()
    {
        // Force the app to start on the Home tab so it isn't blank!
        SwitchTab("Home");
    }

    private void SwitchTab(string tabName)
    {
        Debug.Log($"Switching to Global Tab: {tabName}");

        // 1. Reset all tabs to inactive state
        _btnNavHome.RemoveFromClassList("nav-item-active");
        _btnNavPet.RemoveFromClassList("nav-item-active");
        _btnNavMissions.RemoveFromClassList("nav-item-active");
        _btnNavProfile.RemoveFromClassList("nav-item-active");

        // 2. Hide ALL actual screen content layers
        if (profileController != null) profileController.HideTab();
        if (homeController != null) homeController.HideHomeLayer();
        if (missionsController != null) missionsController.HideTab();
        if (petController != null) petController.HideTab();
        

        // 3. Activate the new selected state
        switch (tabName)
        {
            case "Home":
                _btnNavHome.AddToClassList("nav-item-active");
                if (homeController != null) homeController.ShowHomeLayer();
                break;
            case "Profile":
                _btnNavProfile.AddToClassList("nav-item-active");
                if (profileController != null) profileController.ShowTab();
                break;
            case "Missions":
                _btnNavMissions.AddToClassList("nav-item-active");
                if (missionsController != null) missionsController.ShowTab();
                break;
            case "Pet":
                _btnNavPet.AddToClassList("nav-item-active");
                if (petController != null) petController.ShowTab();
                break;
                // ... add others when we build them
        }
    }
}