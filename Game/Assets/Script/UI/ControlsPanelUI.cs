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

    private void OnEnable()
    {
        ShowImage(0);
        SetMenuPanelActive(false);
    }

    private void Update()
    {
        if (!gameObject.activeSelf) return;

        if (Gamepad.current != null)
        {
            if (Gamepad.current.leftTrigger.wasPressedThisFrame)
                ShowImage(0);
            if (Gamepad.current.rightTrigger.wasPressedThisFrame)
                ShowImage(1);
            if (Gamepad.current.buttonEast.wasPressedThisFrame)
            {
                ReturnToMenu();
            }
        }
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
}
