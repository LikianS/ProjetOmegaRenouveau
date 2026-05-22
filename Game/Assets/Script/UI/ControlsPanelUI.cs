using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class ControlsPanelUI : MonoBehaviour
{
    public GameObject image1;
    public GameObject image2;
    [HideInInspector] public GameObject menuPanel;

    private int currentIndex = 0;
    private PlayerInput playerInput;
    private InputAction navigateAction;
    private InputAction cancelAction;

    private void OnEnable()
    {
        ShowImage(0);
        SetMenuPanelActive(false);
        CacheUIActions();
    }

    private void Update()
    {
        if (!gameObject.activeSelf) return;

        if (navigateAction != null)
        {
            float navX = navigateAction.ReadValue<Vector2>().x;
            if (navX < -0.5f)
                ShowImage(0);
            else if (navX > 0.5f)
                ShowImage(1);
        }

        if (cancelAction != null && cancelAction.WasPressedThisFrame())
            ReturnToMenu();
    }

    private void ShowImage(int index)
    {
        currentIndex = index;
        if (image1 != null) image1.SetActive(index == 0);
        if (image2 != null) image2.SetActive(index == 1);
    }

    private void SetMenuPanelActive(bool isActive)
    {
        if (menuPanel != null)
            menuPanel.SetActive(isActive);
    }

    private void ReturnToMenu()
    {
        gameObject.SetActive(false);
        SetMenuPanelActive(true);
    }

    private void CacheUIActions()
    {
        playerInput = FindAnyObjectByType<PlayerInput>();
        if (playerInput == null || playerInput.actions == null) return;

        var uiMap = playerInput.actions.FindActionMap("UI", false);
        if (uiMap == null) return;

        navigateAction = uiMap.FindAction("Navigate", false);
        cancelAction = uiMap.FindAction("Cancel", false);
    }
}
