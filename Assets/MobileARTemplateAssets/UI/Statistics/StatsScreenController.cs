using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class StatsScreenController : MonoBehaviour
{
    private VisualElement _rootContainer;
    private Button _btnBack;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _rootContainer = root.Q<VisualElement>("StatsScreenRoot");
        _btnBack = root.Q<Button>("BtnBack");

        if (_btnBack != null) _btnBack.clicked += HandleBackClicked;
    }

    private void OnDisable()
    {
        if (_btnBack != null) _btnBack.clicked -= HandleBackClicked;
    }

    public void ShowScreen()
    {
        _rootContainer.RemoveFromClassList("screen-hidden-right");
        // TODO: In the future, we will pull PlayerPrefs arrays here to populate the lifetime stats!
    }

    public void HideScreen()
    {
        _rootContainer.AddToClassList("screen-hidden-right");
    }

    private void HandleBackClicked()
    {
        HideScreen();
    }
}