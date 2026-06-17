using UnityEngine;
using UnityEngine.UIElements;
using Firebase.Auth;
using Firebase.Extensions;

[RequireComponent(typeof(UIDocument))]
public class ProfileScreenController : MonoBehaviour
{
    [Header("Navigation Links")]
    [SerializeField] private HomeScreenController homeScreenController;
    [SerializeField] private GameObject splashScreenUI;
    [SerializeField] private GameObject globalUIGameObject;

    [Header("Starter Pets Setup")]
    [SerializeField] private GameObject[] starterPetModels;
    [SerializeField] private RenderTexture petRenderTexture;// Drag your 4 3D models here in the Inspector!
    [SerializeField] private float rotationSpeed = 0.5f;
    private string[] petNames = { "Chimpon", "Dracomon", "Koarkat", "Baemon" }; // Update these names!
    private int _currentPetIndex = 0;
    private bool _isDraggingPet = false;

    // UI Elements
    private VisualElement _rootContainer;
    private VisualElement _avatarCircle;
    private Label _avatarPlaceholder;
    private Label _feedbackMessage;

    private Button _btnBack;
    private Button _btnStartAdventure;
    private Button _btnEditAvatar;

    private TextField _inputUsername;
    private TextField _inputEmail;
    private TextField _inputPassword;
    private TextField _inputConfirmPassword;

    // New Carousel UI Elements
    private VisualElement _petRenderSurface;
    private Button _btnPrevPet;
    private Button _btnNextPet;
    private Label _txtSelectedPetName;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        _rootContainer = root.Q<VisualElement>("ProfileScreenRoot");
        _avatarCircle = root.Q<VisualElement>("AvatarCircle");
        _avatarPlaceholder = root.Q<Label>("AvatarPlaceholder");
        _feedbackMessage = root.Q<Label>("FeedbackMessage");

        _btnBack = root.Q<Button>("BtnBack");
        _btnStartAdventure = root.Q<Button>("BtnStartAdventure");
        _btnEditAvatar = root.Q<Button>("BtnEditAvatar");

        _inputUsername = root.Q<TextField>("InputUsername");
        _inputEmail = root.Q<TextField>("InputEmail");
        _inputPassword = root.Q<TextField>("InputPassword");
        _inputConfirmPassword = root.Q<TextField>("InputConfirmPassword");

        // --- BIND CAROUSEL ELEMENTS ---
        _petRenderSurface = root.Q<VisualElement>("SignupPetRenderSurface");

        if (_petRenderSurface != null && petRenderTexture != null)
        {
            _petRenderSurface.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(petRenderTexture));
        }
        _btnPrevPet = root.Q<Button>("BtnPrevPet");
        _btnNextPet = root.Q<Button>("BtnNextPet");
        _txtSelectedPetName = root.Q<Label>("TxtSelectedPetName");

        if (_btnBack != null) _btnBack.clicked += HandleBackClicked;
        if (_btnStartAdventure != null) _btnStartAdventure.clicked += HandleStartAdventure;
        if (_btnEditAvatar != null) _btnEditAvatar.clicked += HandleAvatarSelection;

        // --- CAROUSEL BUTTON LOGIC ---
        if (_btnPrevPet != null) _btnPrevPet.clicked += () => ChangePet(-1);
        if (_btnNextPet != null) _btnNextPet.clicked += () => ChangePet(1);

        // --- 3D ROTATION LOGIC ---
        if (_petRenderSurface != null)
        {
            _petRenderSurface.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _petRenderSurface.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _petRenderSurface.RegisterCallback<PointerUpEvent>(OnPointerUp);
            _petRenderSurface.RegisterCallback<PointerOutEvent>(OnPointerOut);
        }

        // Initialize the first pet
        UpdatePetDisplay();
    }

    private void OnDisable()
    {
        if (_btnBack != null) _btnBack.clicked -= HandleBackClicked;
        if (_btnStartAdventure != null) _btnStartAdventure.clicked -= HandleStartAdventure;
        if (_btnEditAvatar != null) _btnEditAvatar.clicked -= HandleAvatarSelection;
    }

    public void ShowScreen() => _rootContainer.RemoveFromClassList("screen-hidden-right");

    public void HideScreen()
    {
        _rootContainer.AddToClassList("screen-hidden-right");
        _feedbackMessage.style.display = DisplayStyle.None;
    }

    // --- CAROUSEL METHODS ---

    private void ChangePet(int direction)
    {
        // Hide current model
        if (starterPetModels.Length > 0 && starterPetModels[_currentPetIndex] != null)
            starterPetModels[_currentPetIndex].SetActive(false);

        // Calculate new index (looping around)
        _currentPetIndex += direction;
        if (_currentPetIndex < 0) _currentPetIndex = starterPetModels.Length - 1;
        if (_currentPetIndex >= starterPetModels.Length) _currentPetIndex = 0;

        UpdatePetDisplay();
    }

    private void UpdatePetDisplay()
    {
        // 1. Turn off ALL pets first (cleans up any mutants!)
        for (int i = 0; i < starterPetModels.Length; i++)
        {
            if (starterPetModels[i] != null)
            {
                starterPetModels[i].SetActive(false);
            }
        }

        // 2. Turn on ONLY the current pet
        if (starterPetModels.Length > 0 && starterPetModels[_currentPetIndex] != null)
        {
            starterPetModels[_currentPetIndex].SetActive(true);

            // Reset its rotation so it faces forward
            starterPetModels[_currentPetIndex].transform.rotation = Quaternion.identity;
        }

        // 3. Update Text
        if (_txtSelectedPetName != null && _currentPetIndex < petNames.Length)
        {
            _txtSelectedPetName.text = petNames[_currentPetIndex];
        }
    }

    // --- 3D SWIPE LOGIC ---
    private void OnPointerDown(PointerDownEvent evt)
    {
        _isDraggingPet = true;
        _petRenderSurface.CapturePointer(evt.pointerId);
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (_isDraggingPet && starterPetModels.Length > 0)
        {
            GameObject activePet = starterPetModels[_currentPetIndex];
            if (activePet != null)
            {
                float deltaX = evt.deltaPosition.x;
                activePet.transform.Rotate(Vector3.up, -deltaX * rotationSpeed, Space.World);
            }
        }
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        _isDraggingPet = false;
        _petRenderSurface.ReleasePointer(evt.pointerId);
    }

    private void OnPointerOut(PointerOutEvent evt)
    {
        _isDraggingPet = false;
        _petRenderSurface.ReleasePointer(evt.pointerId);
    }

    private void HandleBackClicked()
    {
        HideScreen();
    }

    private void HandleAvatarSelection()
    {
        NativeGallery.GetImageFromGallery((path) =>
        {
            if (!string.IsNullOrEmpty(path))
            {
                Texture2D texture = NativeGallery.LoadImageAtPath(path);
                if (texture != null)
                {
                    _avatarCircle.style.backgroundImage = new StyleBackground(texture);
                    _avatarPlaceholder.style.display = DisplayStyle.None;
                }
            }
        }, "Select Profile Picture", "image/*");
    }

    // Helper method to push messages to the UI
    private void ShowFeedback(string message, bool isSuccess = false)
    {
        _feedbackMessage.text = message;
        _feedbackMessage.style.display = DisplayStyle.Flex;

        if (isSuccess)
        {
            _feedbackMessage.AddToClassList("feedback-success");
        }
        else
        {
            _feedbackMessage.RemoveFromClassList("feedback-success");
        }
    }

    private void HandleStartAdventure()
    {
        string username = _inputUsername.value;
        string email = _inputEmail.value;
        string password = _inputPassword.value;
        string confirmPassword = _inputConfirmPassword.value;

        // 1. Local Validation Constraints
        if (string.IsNullOrWhiteSpace(username))
        {
            ShowFeedback("Username cannot be empty.");
            return;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            ShowFeedback("Email address cannot be empty.");
            return;
        }

        if (password.Length < 8)
        {
            ShowFeedback("Password must be at least 8 characters.");
            return;
        }

        if (password != confirmPassword)
        {
            ShowFeedback("Password mismatch. Please verify.");
            return;
        }

        ShowFeedback("Communicating with server...", true);


        // 2. Firebase Backend Execution
        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                ShowFeedback("Signup was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                ShowFeedback($"Error: {task.Exception.InnerExceptions[0].Message}");
                return;
            }

            

            // Authentication confirmed
            Firebase.Auth.AuthResult result = task.Result;

            // 3. Establish persistent local state
            PlayerPrefs.SetInt("IsRegistered", 1);
            PlayerPrefs.SetString("Username", username);

            if (!PlayerPrefs.HasKey("JoinDate"))
            {
                string currentDate = System.DateTime.Now.ToString("MMM yyyy");
                PlayerPrefs.SetString("JoinDate", currentDate);
            }

            // Grab the actual name of the pet they spun to (e.g., "Chimpon", "Dracomon")
            string chosenPetName = petNames[_currentPetIndex];

            // Save it as a string so the Pet Menu can search the database for it!
            PlayerPrefs.SetString("ActivePetID", chosenPetName);
            PlayerPrefs.Save();

            ShowFeedback($"Signup successful! Welcome {username}.", true);

            // 4. Trigger progression to the core application
            Debug.Log("Registration sequence complete. Transitioning to Home Dashboard...");

            // --- NEW TRANSITION LOGIC ---
            HideScreen(); // Slide the profile screen away
            if (homeScreenController != null) homeScreenController.ShowHomeLayer(); // Slide the home screen in
            if (globalUIGameObject != null) globalUIGameObject.SetActive(true); // <-- ADD THIS
            if (splashScreenUI != null) splashScreenUI.SetActive(false); // Turn off the splash screen in the background
        });
    }
    
}