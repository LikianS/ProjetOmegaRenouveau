using TMPro;
using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance;

    [Header("Interaction Prompt UI")]
    public GameObject interactionPrompt;
    public TextMeshProUGUI interactionText;

    private int promptRequestCount = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public void ShowInteraction(string prompt)
    {
        promptRequestCount++;
        if (interactionPrompt != null && interactionText != null)
        {
            interactionText.text = prompt;
            interactionPrompt.SetActive(true);
        }
    }

    public void HideInteraction()
    {
        promptRequestCount = Mathf.Max(0, promptRequestCount - 1);
        if (promptRequestCount == 0 && interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }

    public void ForceHideInteraction()
    {
        promptRequestCount = 0;
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }

    public bool IsInteractionPromptActive()
    {
        return interactionPrompt != null && interactionPrompt.activeSelf;
    }

    public void PositionInteractionUI(Vector3 worldPosition)
    {
        Camera mainCamera = Camera.main;
        Vector2 viewportPosition = mainCamera.WorldToViewportPoint(worldPosition);
        RectTransform canvasRect = interactionPrompt.transform.parent.GetComponent<RectTransform>();

        if (canvasRect != null)
        {
            Vector2 screenPosition = new Vector2(
                (viewportPosition.x * canvasRect.sizeDelta.x) - (canvasRect.sizeDelta.x * 0.5f),
                (viewportPosition.y * canvasRect.sizeDelta.y) - (canvasRect.sizeDelta.y * 0.5f)
            );

            interactionPrompt.GetComponent<RectTransform>().anchoredPosition = screenPosition;
        }
    }
}
