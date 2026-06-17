using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections.Generic;

/// <summary>
/// StepVerse — SignUpScreenController
/// Full version with NativeGallery avatar picker + Firebase Auth + Firestore
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class SignUpScreenController : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string splashSceneName = "MainMenu";
    [SerializeField] private string mainSceneName   = "MainMenu";
    [SerializeField] private string loginSceneName  = "MainMenu";

    [Header("Default Avatar")]
    [SerializeField] private Texture2D defaultAvatarTexture;

    // ── UI refs ───────────────────────────────────────────────────────────
    private UIDocument    _doc;
    private VisualElement _root;

    private Button        _btnBack;
    private Button        _btnAvatarEdit;
    private Button        _btnSubmit;
    private Button        _btnTogglePw;
    private Button        _btnLogin;

    private TextField     _fieldUsername;
    private TextField     _fieldEmail;
    private TextField     _fieldPassword;
    private TextField     _fieldConfirm;

    private VisualElement _avatarImage;
    private VisualElement _loadingOverlay;
    private Label         _errorLabel;
    private Label         _eyeLabel;

    // ── State ─────────────────────────────────────────────────────────────
    private bool      _passwordVisible = false;
    private Texture2D _chosenAvatar    = null;

    // ── Firebase ──────────────────────────────────────────────────────────
    private FirebaseAuth      _auth;
    private FirebaseFirestore _db;

    // ─────────────────────────────────────────────────────────────────────
    // Unity lifecycle
    // ─────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _doc  = GetComponent<UIDocument>();
        _root = _doc.rootVisualElement;

        BindElements();
        SetupDefaultAvatar();
        RegisterCallbacks();
        InitFirebase();
    }

    private void OnDestroy()
    {
        if (_btnBack       != null) _btnBack.clicked       -= OnBackClicked;
        if (_btnAvatarEdit != null) _btnAvatarEdit.clicked -= OnAvatarEditClicked;
        if (_btnSubmit     != null) _btnSubmit.clicked     -= OnSubmitClicked;
        if (_btnTogglePw   != null) _btnTogglePw.clicked   -= OnTogglePasswordClicked;
        if (_btnLogin      != null) _btnLogin.clicked      -= OnLoginRedirectClicked;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Setup
    // ─────────────────────────────────────────────────────────────────────

    private void BindElements()
    {
        _btnBack        = _root.Q<Button>("btn-back");
        _btnAvatarEdit  = _root.Q<Button>("btn-avatar-edit");
        _btnSubmit      = _root.Q<Button>("btn-submit");
        _btnTogglePw    = _root.Q<Button>("btn-toggle-pw");
        _btnLogin       = _root.Q<Button>("btn-login");

        _fieldUsername  = _root.Q<TextField>("field-username");
        _fieldEmail     = _root.Q<TextField>("field-email");
        _fieldPassword  = _root.Q<TextField>("field-password");
        _fieldConfirm   = _root.Q<TextField>("field-confirm");

        _avatarImage    = _root.Q<VisualElement>("avatar-image");
        _loadingOverlay = _root.Q<VisualElement>("loading-overlay");
        _errorLabel     = _root.Q<Label>("error-label");
        _eyeLabel       = _root.Q<Label>("eye-label");

        if (_btnBack       == null) Debug.LogWarning("[SignUp] btn-back not found");
        if (_btnAvatarEdit == null) Debug.LogWarning("[SignUp] btn-avatar-edit not found");
        if (_btnSubmit     == null) Debug.LogWarning("[SignUp] btn-submit not found");
        if (_fieldUsername == null) Debug.LogWarning("[SignUp] field-username not found");
        if (_fieldEmail    == null) Debug.LogWarning("[SignUp] field-email not found");
        if (_fieldPassword == null) Debug.LogWarning("[SignUp] field-password not found");
        if (_fieldConfirm  == null) Debug.LogWarning("[SignUp] field-confirm not found");
        if (_avatarImage   == null) Debug.LogWarning("[SignUp] avatar-image not found");
    }

    private void SetupDefaultAvatar()
    {
        if (defaultAvatarTexture != null && _avatarImage != null)
            _avatarImage.style.backgroundImage = new StyleBackground(defaultAvatarTexture);
    }

    private void RegisterCallbacks()
    {
        if (_btnBack       != null) _btnBack.clicked       += OnBackClicked;
        if (_btnAvatarEdit != null) _btnAvatarEdit.clicked += OnAvatarEditClicked;
        if (_btnSubmit     != null) _btnSubmit.clicked     += OnSubmitClicked;
        if (_btnTogglePw   != null) _btnTogglePw.clicked   += OnTogglePasswordClicked;
        if (_btnLogin      != null) _btnLogin.clicked      += OnLoginRedirectClicked;
    }

    private void InitFirebase()
    {
#if UNITY_EDITOR
        Debug.Log("[SignUp] Editor mode — Firebase skipped.");
        return;
#endif
        _auth = FirebaseAuth.DefaultInstance;
        _db   = FirebaseFirestore.DefaultInstance;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Avatar Picker — NativeGallery
    // ─────────────────────────────────────────────────────────────────────

    private void OnAvatarEditClicked()
    {
#if UNITY_EDITOR
        Debug.Log("[SignUp] Avatar picker not available in Editor. Build to device to test.");
        return;
#endif
        // Request permission then open gallery
        NativeGallery.GetImageFromGallery((path) =>
        {
            if (path == null)
            {
                Debug.Log("[SignUp] User cancelled avatar selection.");
                return;
            }

            // Load selected image — max 512px to keep memory light on mobile
            Texture2D selectedTexture = NativeGallery.LoadImageAtPath(path, 512);

            if (selectedTexture == null)
            {
                ShowError("Could not load the selected image. Try another.");
                return;
            }

            // Store and display chosen avatar
            _chosenAvatar = selectedTexture;
            _avatarImage.style.backgroundImage = new StyleBackground(_chosenAvatar);

            Debug.Log("[SignUp] Avatar selected successfully.");

        }, "Choose your avatar", "image/*");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Password toggle
    // ─────────────────────────────────────────────────────────────────────

    private void OnTogglePasswordClicked()
    {
        _passwordVisible = !_passwordVisible;

        if (_fieldPassword != null)
            _fieldPassword.isPasswordField = !_passwordVisible;

        if (_fieldConfirm != null)
            _fieldConfirm.isPasswordField = !_passwordVisible;

        if (_eyeLabel != null)
            _eyeLabel.text = _passwordVisible ? "🙈" : "👁";
    }

    // ─────────────────────────────────────────────────────────────────────
    // Navigation
    // ─────────────────────────────────────────────────────────────────────

    private void OnBackClicked()
    {
        SceneManager.LoadScene(splashSceneName);
    }

    private void OnLoginRedirectClicked()
    {
        SceneManager.LoadScene(loginSceneName);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Form submission + validation
    // ─────────────────────────────────────────────────────────────────────

    private void OnSubmitClicked()
    {
        ClearError();

        string username = _fieldUsername != null ? _fieldUsername.value.Trim() : "";
        string email    = _fieldEmail    != null ? _fieldEmail.value.Trim()    : "";
        string password = _fieldPassword != null ? _fieldPassword.value        : "";
        string confirm  = _fieldConfirm  != null ? _fieldConfirm.value         : "";

        if (string.IsNullOrEmpty(username))
        { ShowError("Please choose a codename."); return; }

        if (username.Length < 3)
        { ShowError("Codename must be at least 3 characters."); return; }

        if (string.IsNullOrEmpty(email) || !email.Contains("@"))
        { ShowError("Please enter a valid email address."); return; }

        if (password.Length < 6)
        { ShowError("Password must be at least 6 characters."); return; }

        if (password != confirm)
        { ShowError("Passwords do not match."); return; }

#if UNITY_EDITOR
        // Editor: skip Firebase, just navigate
        Debug.Log($"[SignUp] Editor test — would register: {username} / {email}");
        SceneManager.LoadScene(mainSceneName);
        return;
#endif

        StartCoroutine(RegisterUser(username, email, password));
    }

    // ─────────────────────────────────────────────────────────────────────
    // Firebase registration coroutine
    // ─────────────────────────────────────────────────────────────────────

    private IEnumerator RegisterUser(string username, string email, string password)
    {
        ShowLoading(true);

        bool         done     = false;
        string       errorMsg = null;
        FirebaseUser newUser  = null;

        // Step 1 — Create Firebase Auth account
        _auth.CreateUserWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                    errorMsg = ParseFirebaseError(task.Exception?.InnerException?.Message);
                else
                    newUser = task.Result.User;
                done = true;
            });

        yield return new WaitUntil(() => done);

        if (errorMsg != null)
        {
            ShowLoading(false);
            ShowError(errorMsg);
            yield break;
        }

        // Step 2 — Save profile to Firestore
        done     = false;
        errorMsg = null;

        var profileData = new Dictionary<string, object>
        {
            { "username",   username                   },
            { "email",      email                      },
            { "totalSteps", 0                          },
            { "petLevel",   1                          },
            { "createdAt",  FieldValue.ServerTimestamp },
        };

        _db.Collection("users").Document(newUser.UserId)
            .SetAsync(profileData)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                    Debug.LogWarning("[SignUp] Firestore write failed — continuing anyway.");
                done = true;
            });

        yield return new WaitUntil(() => done);

        ShowLoading(false);
        Debug.Log($"[SignUp] Registration complete: {username}");
        SceneManager.LoadScene(mainSceneName);
    }

    // ─────────────────────────────────────────────────────────────────────
    // UI helpers
    // ─────────────────────────────────────────────────────────────────────

    private void ShowLoading(bool show)
    {
        if (_loadingOverlay == null) return;
        if (show) _loadingOverlay.RemoveFromClassList("hidden");
        else      _loadingOverlay.AddToClassList("hidden");
        if (_btnSubmit != null) _btnSubmit.SetEnabled(!show);
    }

    private void ShowError(string message)
    {
        if (_errorLabel != null) _errorLabel.text = message;
        Debug.LogWarning($"[SignUp] Validation: {message}");
    }

    private void ClearError()
    {
        if (_errorLabel != null) _errorLabel.text = "";
    }

    private string ParseFirebaseError(string raw)
    {
        if (raw == null)                              return "Something went wrong. Please try again.";
        if (raw.Contains("email-already-in-use"))    return "This email is already registered.";
        if (raw.Contains("invalid-email"))           return "Please enter a valid email address.";
        if (raw.Contains("weak-password"))           return "Password too weak. Use at least 6 characters.";
        if (raw.Contains("network-request-failed"))  return "No internet connection. Check your network.";
        return "Registration failed. Please try again.";
    }
}
