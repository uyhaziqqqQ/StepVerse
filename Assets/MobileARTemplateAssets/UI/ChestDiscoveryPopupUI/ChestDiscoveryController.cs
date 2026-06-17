using UnityEngine;
using UnityEngine.UIElements;
using System;

[RequireComponent(typeof(UIDocument))]
public class ChestDiscoveryController : MonoBehaviour
{
    public static ChestDiscoveryController Instance;

    private VisualElement _overlay;
    private Button _btnAccept;
    private Button _btnDecline;

    private Action _onAcceptCallback;
    private Action _onDeclineCallback;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _overlay = root.Q<VisualElement>("DiscoveryOverlay");
        _btnAccept = root.Q<Button>("BtnAccept");
        _btnDecline = root.Q<Button>("BtnDecline");

        if (_btnAccept != null) _btnAccept.clicked += OnAccept;
        if (_btnDecline != null) _btnDecline.clicked += OnDecline;
    }

    // Call this to display the popup and pass the Yes/No logic
    public void ShowPopup(Action onAccept, Action onDecline)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("popup-chest-sound");
        _onAcceptCallback = onAccept;
        _onDeclineCallback = onDecline;
        _overlay.RemoveFromClassList("hidden");
    }

    private void OnAccept()
    {
        _overlay.AddToClassList("hidden");
        _onAcceptCallback?.Invoke();
    }

    private void OnDecline()
    {
        _overlay.AddToClassList("hidden");
        _onDeclineCallback?.Invoke();
    }
}