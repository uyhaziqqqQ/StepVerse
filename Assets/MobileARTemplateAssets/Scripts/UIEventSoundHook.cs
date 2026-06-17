using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class UIEventSoundHook : MonoBehaviour
{
    private UIDocument _document;

    private void OnEnable()
    {
        _document = GetComponent<UIDocument>();
        if (_document != null && _document.rootVisualElement != null)
        {
            // Wait for the UI to be fully built before attaching events
            _document.rootVisualElement.schedule.Execute(AttachEvents).StartingIn(100);
        }
    }

    private void AttachEvents()
    {
        if (_document == null || _document.rootVisualElement == null) return;

        // Register for all button clicks
        _document.rootVisualElement.RegisterCallback<ClickEvent>(OnClickEvent);

        // Register for all toggle changes
        _document.rootVisualElement.RegisterCallback<ChangeEvent<bool>>(OnToggleChanged);
    }

    private void OnDisable()
    {
        if (_document != null && _document.rootVisualElement != null)
        {
            _document.rootVisualElement.UnregisterCallback<ClickEvent>(OnClickEvent);
            _document.rootVisualElement.UnregisterCallback<ChangeEvent<bool>>(OnToggleChanged);
        }
    }

    private void OnClickEvent(ClickEvent evt)
    {
        if (evt.target is Button)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("ui-touchscreen");
            }
        }
    }

    private void OnToggleChanged(ChangeEvent<bool> evt)
    {
        if (evt.target is Toggle)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("ui-touchscreen");
            }
        }
    }
}
