using UnityEngine;

public class AchievementPickup : MonoBehaviour
{
    [Tooltip("Cout en points d'achievement pour ramasser cet objet")]
    public int achievementCost = 1;

    public static int ActiveCount { get; private set; }

    private bool isCounted;

    private void OnEnable()
    {
        if (isCounted) return;
        ActiveCount++;
        isCounted = true;
    }

    private void OnDisable()
    {
        if (!isCounted) return;
        ActiveCount = Mathf.Max(0, ActiveCount - 1);
        isCounted = false;
    }
}
