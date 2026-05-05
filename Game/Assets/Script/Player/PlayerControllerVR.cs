using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

// Lightweight VR-adapted player controller
// - Uses CharacterController for collisions
// - Movement with one joystick (OnMove)
// - Rotate yaw with the other joystick (OnRotate)
// - Dash with button (OnDash)
// - Interact with button (OnInteract)
// - Attack / Parry intentionally omitted

public class PlayerControllerVR : MonoBehaviour
{
    [Header("References")]
    public PlayerStats playerStats;
    public Animator animator;

    [Tooltip("Assign the XR camera (head) so movement is relative to headset")] 
    public Transform xrHead;

    [Header("Components")]
    public CharacterController characterController;

    [Header("Input")]
    [Tooltip("Optional: assign the InputActionAsset used by your other PlayerController (Action Map 'Player')")]
    public InputActionAsset inputActions;

    [Header("Movement")]
    public float walkSpeed = 2f;
    public float runSpeed = 4f;
    public float rotateSensitivity = 60f; // degrees per second per unit input
    public bool invertY = false;
    public float gravity = 9.81f;

    [Header("Head Alignment")]
    [Tooltip("If true, the player body will smoothly rotate to face where the HMD is looking (yaw only)")]
    public bool autoAlignYawToHead = true;
    [Tooltip("How fast the body aligns to head yaw (higher = faster)")]
    public float alignYawSpeed = 5f;

    [Header("Dash")]
    public float dashDistance = 4f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    [Header("Interaction")]
    public float interactRange = 2f;

    [Header("Character Height Auto-Adjust")]
    public bool autoAdjustHeight = true;
    public bool followHeadHorizontally = false; // if true, capsule X/Z follows head (can cause camera shift)
    public float minHeight = 1.0f;
    public float characterSkinWidth = 0.08f;
    public float maxHeight = 2.5f;

    [Header("Debug")]
    public bool debugLogs = false;

    private Vector2 moveInput = Vector2.zero;
    private Vector2 rotateInput = Vector2.zero;
    private bool isRunning = false;
    private bool isDashing = false;
    private bool canDash = true;
    private float dashCooldownTimer = 0f;

    private float verticalVelocity = 0f;
    private float previousHeadHeight = 1.7f;

    private float footstepTimer = 0f;
    private float footstepInterval = 0.4f;
    private bool isMoving = false;

    // InputAction references
    private InputAction m_MoveAction;
    private InputAction m_RotateAction;
    private InputAction m_RunAction;
    private InputAction m_DashAction;
    private InputAction m_InteractAction;

    // stored delegates so we can unsubscribe
    private System.Action<InputAction.CallbackContext> runPerformedDelegate;
    private System.Action<InputAction.CallbackContext> runCanceledDelegate;
    private System.Action<InputAction.CallbackContext> dashPerformedDelegate;
    private System.Action<InputAction.CallbackContext> interactPerformedDelegate;

    [Header("XR Rig")]
    [Tooltip("XR Rig parent (ex: XR Origin ou Camera Offset). Si null, la caméra xrHead sera déplacée.")]
    public Transform xrRig;

    private void Awake()
    {
        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();

        if (xrHead == null && Camera.main != null)
            xrHead = Camera.main.transform;

        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (characterController == null)
        {
            // Add one if missing
            characterController = gameObject.AddComponent<CharacterController>();
            characterController.skinWidth = characterSkinWidth;
        }

        // sensible defaults
        characterController.slopeLimit = 45f;
        characterController.stepOffset = 0.3f;
    }

    private void OnEnable()
    {
        // If inputActions not assigned, try to get from PlayerInput component on this object
        if (inputActions == null)
        {
            var pi = GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (pi != null)
            {
                inputActions = pi.actions;
                if (debugLogs) Debug.Log("PlayerControllerVR: grabbed InputActionAsset from PlayerInput component.");
            }
        }

        // Bind actions from InputActionAsset if provided
        if (inputActions != null)
        {
            var map = inputActions.FindActionMap("Player", true);
            if (map != null)
            {
                m_MoveAction = map.FindAction("Move", true);

                // Try common names first
                m_RotateAction = map.FindAction("CameraRotate", false) ?? map.FindAction("Rotate", false) ?? map.FindAction("Look", false) ?? map.FindAction("LookDelta", false);

                // If not found, try to auto-select a Vector2 action bound to a right-hand stick/thumb
                if (m_RotateAction == null)
                {
                    foreach (var a in map.actions)
                    {
                        if (a == null) continue;
                        if (a.expectedControlType != "Vector2") continue;

                        foreach (var b in a.bindings)
                        {
                            if (b.isComposite) continue;
                            var path = (b.path ?? string.Empty).ToLower();
                            if (path.Contains("right") || path.Contains("thumbstick") || path.Contains("primary2d") || path.Contains("stick") || path.Contains("secondary") || path.Contains("camera"))
                            {
                                m_RotateAction = a;
                                if (debugLogs) Debug.Log($"PlayerControllerVR: auto-selected rotate action '{a.name}' (binding {b.path}).");
                                break;
                            }
                        }

                        if (m_RotateAction != null) break;
                    }
                }

                // Fallback: any Vector2 action
                if (m_RotateAction == null)
                {
                    foreach (var a in map.actions)
                    {
                        if (a == null) continue;
                        if (a.expectedControlType == "Vector2")
                        {
                            m_RotateAction = a;
                            if (debugLogs) Debug.Log($"PlayerControllerVR: fallback selected rotate action '{a.name}'.");
                            break;
                        }
                    }
                }

                m_RunAction = map.FindAction("Run", false);
                m_DashAction = map.FindAction("Dash", false);
                m_InteractAction = map.FindAction("Interact", false);

                if (m_MoveAction != null)
                {
                    m_MoveAction.performed += OnMovePerformed;
                    m_MoveAction.canceled += OnMoveCanceled;
                    m_MoveAction.Enable();
                }
                if (m_RotateAction != null && !autoAlignYawToHead)
                {
                    m_RotateAction.performed += OnRotatePerformed;
                    m_RotateAction.canceled += OnRotateCanceled;
                    m_RotateAction.Enable();
                }
                else if (debugLogs && m_RotateAction == null)
                {
                    Debug.LogWarning("PlayerControllerVR: No rotate action found in 'Player' map. Ensure an action exists for camera rotation (Vector2).");
                }
                if (m_RunAction != null)
                {
                    runPerformedDelegate = ctx => isRunning = true;
                    runCanceledDelegate = ctx => isRunning = false;
                    m_RunAction.performed += runPerformedDelegate;
                    m_RunAction.canceled += runCanceledDelegate;
                    m_RunAction.Enable();
                }
                if (m_DashAction != null)
                {
                    dashPerformedDelegate = ctx => OnDash(ctx);
                    m_DashAction.performed += dashPerformedDelegate;
                    m_DashAction.Enable();
                }
                if (m_InteractAction != null)
                {
                    interactPerformedDelegate = ctx => OnInteract(ctx);
                    m_InteractAction.performed += interactPerformedDelegate;
                    m_InteractAction.Enable();
                }
            }
            else
            {
                if (debugLogs) Debug.LogWarning("PlayerControllerVR: InputActionAsset has no 'Player' action map.");
            }
        }
    }

    private void OnDisable()
    {
        if (m_MoveAction != null)
        {
            m_MoveAction.performed -= OnMovePerformed;
            m_MoveAction.canceled -= OnMoveCanceled;
            m_MoveAction.Disable();
        }
        if (m_RotateAction != null)
        {
            m_RotateAction.performed -= OnRotatePerformed;
            m_RotateAction.canceled -= OnRotateCanceled;
            m_RotateAction.Disable();
        }
        if (m_RunAction != null)
        {
            if (runPerformedDelegate != null) m_RunAction.performed -= runPerformedDelegate;
            if (runCanceledDelegate != null) m_RunAction.canceled -= runCanceledDelegate;
            m_RunAction.Disable();
        }
        if (m_DashAction != null)
        {
            if (dashPerformedDelegate != null) m_DashAction.performed -= dashPerformedDelegate;
            m_DashAction.Disable();
        }
        if (m_InteractAction != null)
        {
            if (interactPerformedDelegate != null) m_InteractAction.performed -= interactPerformedDelegate;
            m_InteractAction.Disable();
        }
    }

    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
        if (debugLogs) Debug.Log($"[PlayerVR] Move performed -> {moveInput}");
    }
    private void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        moveInput = Vector2.zero;
        if (debugLogs) Debug.Log($"[PlayerVR] Move canceled");
    }

    private void OnRotatePerformed(InputAction.CallbackContext ctx)
    {
        rotateInput = ctx.ReadValue<Vector2>();
        if (debugLogs) Debug.Log($"[PlayerVR] Rotate performed -> {rotateInput}");
    }

    private void OnRotateCanceled(InputAction.CallbackContext ctx)
    {
        rotateInput = Vector2.zero;
        if (debugLogs) Debug.Log($"[PlayerVR] Rotate canceled");
    }

    private void Update()
    {
        HandleCooldowns();

        if (autoAdjustHeight)
            UpdateCharacterControllerHeight();

        if (!isDashing)
        {
            ApplyMovement();
        }

        // Always align body yaw with head so the player is always facing head direction
        if (xrHead != null)
            AlignYawWithHead();

        // Handle animations based on movement speed
        UpdateAnimations();

        // joystick rotate is ignored when body follows head
        // if you want joystick rotate, set logic here (currently disabled)

        if (debugLogs && characterController != null)
        {
            Debug.Log($"[PlayerVR] moveInput={moveInput} rotateInput={rotateInput} grounded={characterController.isGrounded} height={characterController.height} center={characterController.center}");
        }
    }

    private void UpdateAnimations()
    {
        if (animator == null || characterController == null) return;

        // Calculate current speed
        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        if (playerStats != null)
            currentSpeed *= playerStats.SpeedMultiplier;

        // Determine if grounded and moving
        bool isGrounded = characterController.isGrounded;
        bool moving = moveInput.magnitude > 0.1f && isGrounded;

        // Calculate normalized speed (0 to 1) for blend tree
        float maxSpeed = runSpeed * (playerStats != null ? playerStats.SpeedMultiplier : 1f);
        float normalizedSpeed = moving ? (currentSpeed / maxSpeed) : 0f;

        // Set animator parameters
        animator.SetFloat("Speed", normalizedSpeed, 0.1f, Time.deltaTime);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsRunning", isRunning && moving);

        // Handle footsteps sound
        if (moving && isGrounded)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                float interval = isRunning ? 0.25f : 0.5f;
                if (SoundManager.Instance != null)
                {
                    if (isRunning)
                        SoundManager.Instance.PlayPlayerRun();
                    else
                        SoundManager.Instance.PlayPlayerWalk();
                }
                footstepTimer = interval;
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }

    private void LateUpdate()
    {
        // Si XR Rig assigné, déplace-le pour suivre le player (XZ), sinon déplace la caméra
        if (xrRig != null)
        {
            // Positionner le XR Rig sur la même XZ que le player et utiliser
            // la hauteur du player pour éviter que le rig tombe sous le sol.
            Vector3 playerPos = transform.position;
            xrRig.position = new Vector3(playerPos.x, playerPos.y, playerPos.z);
        }
        else if (xrHead != null)
        {
            Vector3 camPos = xrHead.position;
            Vector3 playerPos = transform.position;
            xrHead.position = new Vector3(playerPos.x, camPos.y, playerPos.z);
        }
    }

    private void HandleCooldowns()
    {
        if (!canDash)
        {
            dashCooldownTimer -= Time.deltaTime;
            if (dashCooldownTimer <= 0f)
                canDash = true;
        }
    }

    private void AlignYawWithHead()
    {
        if (xrHead == null) return;

        // Project the HMD's forward vector onto the XZ plane (world space)
        Vector3 headForward = xrHead.forward;
        headForward.y = 0f;
        if (headForward.sqrMagnitude < 0.0001f)
            return;
        headForward.Normalize();

        // Compute the world yaw angle that would make the player face the same direction as the HMD
        float targetYaw = Mathf.Atan2(headForward.x, headForward.z) * Mathf.Rad2Deg;
        Vector3 currentEuler = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, targetYaw, 0f);

        if (debugLogs)
            Debug.Log($"[PlayerVR] AlignYawWithHead: targetYaw={targetYaw} headForward={headForward}");
    }

    private void UpdateCharacterControllerHeight()
    {
        if (xrHead == null || characterController == null) return;

        // Choose method based on whether xrHead is child of this transform
        float headLocalY;
        Vector3 headLocal;
        if (xrHead.IsChildOf(transform))
        {
            headLocal = xrHead.localPosition;
            headLocalY = headLocal.y;
        }
        else
        {
            headLocal = transform.InverseTransformPoint(xrHead.position);
            headLocalY = headLocal.y;
        }

        if (headLocalY <= 0f)
        {
            headLocalY = previousHeadHeight;
        }

        headLocalY = Mathf.Clamp(headLocalY, minHeight, maxHeight);
        previousHeadHeight = headLocalY;

        // Set controller height slightly below the head height so camera doesn't intersect the capsule
        float newHeight = headLocalY;
        characterController.height = newHeight;

        Vector3 newCenter = Vector3.zero;
        // Keep capsule horizontally under the player root by default to avoid camera sink when leaning
        if (followHeadHorizontally)
        {
            newCenter.x = headLocal.x;
            newCenter.z = headLocal.z;
        }
        else
        {
            newCenter.x = 0f;
            newCenter.z = 0f;
        }
        // center.y should be half of the height plus a small offset, ensure positive
        newCenter.y = Mathf.Max(characterController.height * 0.5f + characterSkinWidth, 0.1f);

        characterController.center = newCenter;
    }

    private void ApplyMovement()
    {
        if (characterController == null) return;

        // Gravity
        if (characterController.isGrounded)
        {
            verticalVelocity = -0.5f; // small downward force to keep grounded
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        Vector3 forward = xrHead != null ? xrHead.forward : transform.forward;
        Vector3 right = xrHead != null ? xrHead.right : transform.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 dir = forward * moveInput.y + right * moveInput.x;
        if (dir.sqrMagnitude > 0.0001f)
            dir.Normalize();

        float speed = isRunning ? runSpeed * (playerStats != null ? playerStats.SpeedMultiplier : 1f) : walkSpeed * (playerStats != null ? playerStats.SpeedMultiplier : 1f);

        Vector3 motion = dir * speed + Vector3.up * verticalVelocity;

        // Use CharacterController to move (handles collisions)
        characterController.Move(motion * Time.deltaTime);

        // consume stamina when running
        if (isRunning && dir.magnitude > 0.1f && playerStats != null)
        {
            playerStats.UseStamina(playerStats.runningStaminaCost * Time.deltaTime);
            if (playerStats.CurrentStamina <= 0f)
                isRunning = false;
        }
    }

    private void ApplyRotate()
    {
        if (rotateInput.sqrMagnitude < 0.000001f)
            return;

        float deltaYaw = rotateInput.x * rotateSensitivity * Time.deltaTime;
        // We don't apply pitch to the rig; headset controls pitch. Only yaw rotation of the player parent.
        transform.Rotate(Vector3.up, deltaYaw, Space.World);

        // Optionally, small pitch applied to body for visual purposes is omitted since headset handles view.
    }

    // These methods remain for PlayerInput Send Messages fallback
    public void OnMove(InputAction.CallbackContext context)
    {
        if (context.canceled)
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput = context.ReadValue<Vector2>();

        if (debugLogs)
            Debug.Log($"[PlayerVR] OnMove -> {moveInput}");
    }

    public void OnRotate(InputAction.CallbackContext context)
    {
        if (context.canceled)
            {
            rotateInput = Vector2.zero;
            return;
        }

        rotateInput = context.ReadValue<Vector2>();

        if (debugLogs)
            Debug.Log($"[PlayerVR] OnRotate -> {rotateInput}");
    }

    public void OnRun(InputAction.CallbackContext context)
    {
        if (context.performed)
            isRunning = true;
        else if (context.canceled)
            isRunning = false;
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (!canDash || isDashing)
            return;

        // Deduct stamina if available
        if (playerStats != null && playerStats.CurrentStamina < playerStats.dashStaminaCost)
            return;

        if (playerStats != null)
            playerStats.UseStamina(playerStats.dashStaminaCost);

        StartCoroutine(DashCoroutine());
    }

    private IEnumerator DashCoroutine()
    {
        if (characterController == null) yield break;

        isDashing = true;
        canDash = false;
        dashCooldownTimer = dashCooldown;

        Vector3 forward = xrHead != null ? xrHead.forward : transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 dashDir = (moveInput.sqrMagnitude > 0.01f) ? (xrHead.forward * moveInput.y + xrHead.right * moveInput.x) : forward;
        dashDir.y = 0f;
        if (dashDir.sqrMagnitude > 0.01f) dashDir.Normalize();
        else dashDir = forward;

        float elapsed = 0f;
        float dashSpeed = dashDistance / Mathf.Max(0.0001f, dashDuration) * (playerStats != null ? playerStats.SpeedMultiplier : 1f);

        while (elapsed < dashDuration)
        {
            float dt = Time.deltaTime;
            characterController.Move(dashDir * dashSpeed * dt);
            elapsed += dt;
            yield return null;
        }

        isDashing = false;
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        // Simple overlap sphere to find interactables
        Collider[] hits = Physics.OverlapSphere(transform.position, interactRange);
        foreach (var hit in hits)
        {
            Collectible col = hit.GetComponent<Collectible>();
            if (col != null)
            {
                col.Collect(playerStats);
                return;
            }

            WeaponPickup wp = hit.GetComponent<WeaponPickup>();
            if (wp != null)
            {
                if (playerStats != null)
                    playerStats.EquipWeapon(wp.weaponStats);
                Destroy(wp.gameObject);
                return;
            }
        }
    }

    // debug draw
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }

    public void TakeDamage(int damage)
    {
        if (playerStats == null) return;

        playerStats.TakeDamage(damage);

        if (playerStats.CurrentHealth > 0)
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlayPlayerHurt();

            if (animator != null)
                animator.SetTrigger("GetHit");

            if (GetComponent<HitFeedback>() != null)
                GetComponent<HitFeedback>().PlayHitFeedback();
        }
        else
        {
            Die();
        }
    }

    private void Die()
    {
        if (animator != null)
        {
            animator.SetTrigger("IsDead");
        }

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayPlayerDeath();

        // Save game state before dying
        SaveData saveData = SaveManager.LoadGame();
        if (playerStats != null)
            playerStats.SavePlayerStats(saveData);
        SaveManager.SaveGame(saveData);

        // Fade and load scene (optional - commented out for VR as SceneManager can be tricky in VR)
        // ScreenFader.FadeAndLoadScene("House Interior", 1f);

        this.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Handle damage from enemies
        if (other.CompareTag("EnemyWeapon"))
        {
            EnemyController enemy = other.GetComponentInParent<EnemyController>();
            MeleeEnemyController meleeEnemy = other.GetComponentInParent<MeleeEnemyController>();

            int damage = 10; // Default damage
            if (enemy != null)
                damage = enemy.damage;
            else if (meleeEnemy != null)
                damage = meleeEnemy.damage;

            TakeDamage(damage);
        }
    }
}
