using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public class DualAnimPlayerController : MonoBehaviour
{
    [Header("R�f�rences")]
    public Animator animator;
    public PlayerStats playerStats;
    public float moveSpeed = 3f;
    public string idleAnimNormal = "Idle";
    public string walkAnimNormal = "Walk";
    public string idleAnimAlt = "Idle_Alt";
    public string walkAnimAlt = "Walk_Alt";

    private CharacterController characterController;
    private Vector2 moveInput;
    private bool useAltAnim = false;
    private bool isWalking = false;

    private Collectible interactableCollectible;
    private AchievementPickup interactablePickup;
    private Teleporter interactableTeleporter;

    [Header("D�tection d'interaction")]
    public float interactDistance = 2f;
    public float interactSphereRadius = 0.5f;

    [Header("Changement d'animation par collectibles")]
    public int collectibleAnimSwitchThreshold = 5;

    public int collectiblesRestants;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        float moveX = moveInput.x;
        float moveZ = moveInput.y;
        float moveSqrMagnitude = moveX * moveX + moveZ * moveZ;

        if (moveSqrMagnitude > 0.01f)
        {
            isWalking = true;
            Vector3 moveDirection = new Vector3(moveX, 0f, moveZ).normalized;
            characterController.Move(moveDirection * moveSpeed * Time.deltaTime);
            transform.forward = moveDirection;
        }
        else
        {
            isWalking = false;
        }

        int remainingCollectibles = AchievementPickup.ActiveCount;

        bool shouldUseAlt = remainingCollectibles <= collectibleAnimSwitchThreshold;
        if (shouldUseAlt != useAltAnim)
        {
            useAltAnim = shouldUseAlt;
        }

        if (animator != null)
        {
            string state = useAltAnim
                ? (isWalking ? walkAnimAlt : idleAnimAlt)
                : (isWalking ? walkAnimNormal : idleAnimNormal);

            if (!animator.GetCurrentAnimatorStateInfo(0).IsName(state))
                animator.Play(state);
        }
        collectiblesRestants = remainingCollectibles;
        DetectInteractableInFront();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (interactableCollectible != null)
            {
                interactableCollectible.Collect(playerStats);
                interactableCollectible = null;
                InteractionManager.Instance.HideInteraction();
                return;
            }
            if (interactablePickup != null)
            {
                int cost = interactablePickup.achievementCost;
                if (playerStats.baseStats.achievement >= cost)
                {
                    playerStats.baseStats.achievement -= cost;
                    Destroy(interactablePickup.gameObject);
                    interactablePickup = null;
                    InteractionManager.Instance.HideInteraction();
                }
                else
                {
                    InteractionManager.Instance.ShowInteraction("Pas assez de points d'achievement !");
                }
                return;
            }
            if (interactableTeleporter != null)
            {
                if (!string.IsNullOrEmpty(interactableTeleporter.sceneName))
                {
                    ScreenFader.FadeAndLoadScene(interactableTeleporter.sceneName, 1f);
                }
                else
                {
                    InteractionManager.Instance.ShowInteraction("Aucune sc�ne cible d�finie !");
                }
                interactableTeleporter = null;
                return;
            }
        }
    }

    private void DetectInteractableInFront()
    {
        interactableCollectible = null;
        interactablePickup = null;
        interactableTeleporter = null;

        Ray ray = new Ray(transform.position + Vector3.up * 0.5f, transform.forward);
        RaycastHit hit;
        if (Physics.SphereCast(ray, interactSphereRadius, out hit, interactDistance))
        {
            var collectible = hit.collider.GetComponent<Collectible>();
            if (collectible != null)
            {
                interactableCollectible = collectible;
                InteractionManager.Instance.ShowInteraction($"Appuyez sur A pour ramasser {collectible.itemName}");
                InteractionManager.Instance.PositionInteractionUI(collectible.transform.position + Vector3.up * 1.5f);
                return;
            }
            var pickup = hit.collider.GetComponent<AchievementPickup>();
            if (pickup != null)
            {
                interactablePickup = pickup;
                if (playerStats.baseStats.achievement >= pickup.achievementCost)
                {
                    InteractionManager.Instance.ShowInteraction(
                        $"Appuyez sur A pour ramasser (co�t : {pickup.achievementCost} achievement)");
                }
                else
                {
                    InteractionManager.Instance.ShowInteraction(
                        $"Pas assez de points d'achievement ({pickup.achievementCost} requis)");
                }
                InteractionManager.Instance.PositionInteractionUI(pickup.transform.position + Vector3.up * 1.5f);
                return;
            }
            var teleporter = hit.collider.GetComponent<Teleporter>();
            if (teleporter != null)
            {
                interactableTeleporter = teleporter;
                InteractionManager.Instance.ShowInteraction(
                    $"Appuyez sur A pour vous t�l�porter vers {teleporter.sceneName}");
                InteractionManager.Instance.PositionInteractionUI(teleporter.transform.position + Vector3.up * 1.5f);
                return;
            }
        }
        InteractionManager.Instance.HideInteraction();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 direction = transform.forward;

        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(origin, interactSphereRadius);

        Gizmos.DrawWireSphere(origin + direction * interactDistance, interactSphereRadius);

        int segments = 16;
        for (int i = 0; i < segments; i++)
        {
            float angle1 = (i / (float)segments) * Mathf.PI * 2f;
            float angle2 = ((i + 1) / (float)segments) * Mathf.PI * 2f;
            Vector3 offset1 = new Vector3(Mathf.Cos(angle1), 0, Mathf.Sin(angle1)) * interactSphereRadius;
            Vector3 offset2 = new Vector3(Mathf.Cos(angle2), 0, Mathf.Sin(angle2)) * interactSphereRadius;

            Vector3 start1 = origin + offset1;
            Vector3 end1 = origin + direction * interactDistance + offset1;
            Vector3 start2 = origin + offset2;
            Vector3 end2 = origin + direction * interactDistance + offset2;

            Gizmos.DrawLine(start1, end1);
            Gizmos.DrawLine(start1, start2);
            Gizmos.DrawLine(end1, end2);
        }
    }
#endif

    public void SetDialogueMode(bool isInDialogue)
    {
        if (isInDialogue)
        {
            moveInput = Vector2.zero;
            characterController.enabled = false;
        }
        else
        {
            characterController.enabled = true;
        }
    }
}
