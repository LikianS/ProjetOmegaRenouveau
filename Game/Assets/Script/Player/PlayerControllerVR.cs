using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

// VR Player Controller
// - CharacterController pour les collisions
// - Déplacement via joystick gauche (OnMove)
// - Rotation du yaw du player via suivi de la tête HMD (avec seuil pour éviter la boucle)
// - Le mesh s'aligne instantanément vers la direction de la caméra
// - Dash via bouton (OnDash)
// - Interaction via bouton (OnInteract)
// - Attaque via swing rapide de la manette VR

public class PlayerControllerVR : MonoBehaviour
{
    [Header("Références")]
    public PlayerStats playerStats;
    public Animator animator;

    [Tooltip("Caméra XR (tête / HMD). Si null, Camera.main est utilisée.")]
    public Transform xrHead;

    [Tooltip("XR Rig parent (ex: XR Origin). Doit être ENFANT du Player GameObject.")]
    public Transform xrRig;

    [Tooltip("Transform du mesh du personnage (enfant du Player). " +
             "Il tourne pour faire face à la direction de la caméra. " +
             "Si null, c'est le Player entier qui tourne (non recommandé si la cam est enfant).")]
    public Transform characterMesh;

    [Header("Composants")]
    public CharacterController characterController;

    [Header("Input")]
    [Tooltip("InputActionAsset avec la map 'Player'")]
    public InputActionAsset inputActions;

    [Header("Mouvement")]
    public float walkSpeed = 2f;
    public float runSpeed  = 4f;
    public float gravity   = 9.81f;

    [Header("Alignement mesh → caméra")]
    [Tooltip("Vitesse de rotation du mesh vers la direction de la caméra (degrés/seconde). " +
             "Mettre une grande valeur (ex: 720) pour un alignement quasi-instantané.")]
    public float meshRotateSpeed = 720f;

    [Tooltip("Vitesse minimale (m/s) en dessous de laquelle le mesh ne tourne pas " +
             "(évite qu'il tourne sur place à l'arrêt)")]
    public float meshRotateMinSpeed = 0.1f;

    [Tooltip("Offset en degrés appliqué à la rotation du mesh quand il suit la caméra. Valeur positive = tourne légèrement vers la droite.")]
    public float meshYawOffset = 0f;

    [Header("Alignement player root → tête (yaw broad)")]
    [Tooltip("Le player root tourne lentement pour suivre le HMD. " +
             "Désactiver si tu veux que SEUL le mesh s'aligne.")]
    public bool autoAlignYawToHead = true;

    [Tooltip("Vitesse de rotation du player root vers la tête (degrés/seconde)")]
    public float alignYawSpeed = 90f;

    [Tooltip("Écart angulaire minimal (degrés) avant que le player root tourne. " +
             "Évite l'oscillation quand la cam est enfant du player.")]
    public float alignYawDeadzone = 30f;

    [Tooltip("Input forward minimal pour activer l'alignement du root")]
    public float bodyAlignForwardThreshold = 0.1f;

    [Header("Dash")]
    public float dashDistance = 4f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    [Header("Interaction")]
    public float interactRange = 2f;

    [Header("Hauteur automatique (CharacterController)")]
    public bool autoAdjustHeight = true;
    public float minHeight = 1.0f;
    public float maxHeight = 2.5f;
    [Tooltip("Offset ajouté à la hauteur du CharacterController (ne change PAS la hauteur perçue en VR)")]
    public float headHeightOffset = 0f;

    [Header("Offset de départ de la caméra")]
    [Tooltip("Offset vertical LOCAL appliqué au XR Rig. Valeur positive = monte, négative = descend.")]
    public float startingRigHeightOffset = 0f;

    [Header("Attaque VR (détection de swing)")]
    public float swingVelocityThreshold = 2.5f;
    public float vrAttackRange          = 1.2f;
    public LayerMask enemyLayer;
    public float attackDuration = 0.5f;
    public float attackCooldown = 0.5f;

    [Header("Lock ennemi")]
    public float lockRange = 10f;
    public Transform currentTarget;

    [Header("Parry")]
    public float parryWindow = 0.2f;

    [Header("Debug")]
    public bool debugLogs = false;

    // -------------------------------------------------------------------------
    // État privé
    // -------------------------------------------------------------------------

    private Vector2 moveInput;
    private bool    isWalking;   // true dès qu'on a un input de déplacement
    private bool    isSprinting; // true quand le bouton Sprint est maintenu
    private bool    isDashing;
    private bool    canDash   = true;
    private float   dashCooldownTimer;

    private bool    isAttacking;
    private bool    canAttack = true;
    private float   attackCooldownTimer;
    private bool    isLockedOn;
    private bool    isParrying;
    private bool    parryBlockedDamage;
    private float   parryStartTime;
    public bool inDialogueMode;
    private bool    isInteracting;

    private float   verticalVelocity;
    private float   previousHeadHeight = 1.7f;
    private float   footstepTimer;

    // Position locale initiale du rig (calculée une fois en Start)
    private Vector3 rigInitialLocalPos;
    private bool    rigInitialPosCached = false;

    // Swing VR
    private Vector3 prevRightHandPos;
    private bool    prevHandPositionsValid;

    // Actions input
    private InputAction m_MoveAction;
    private InputAction m_SprintAction;
    private InputAction m_DashAction;
    private InputAction m_InteractAction;
    private InputAction m_AttackAction;
    private InputAction m_LockAction;
    private InputAction m_ParryAction;
    private InputAction m_PauseAction;
    private InputAction m_RightHandPositionAction;

    // Cache pour limiter les allocations/runtime lookups
    private UnityEngine.XR.InputDevice m_RightHandDevice;
    private bool m_RightHandDeviceInitialized;
    private readonly Collider[] interactHitsBuffer = new Collider[64];
    private readonly Collider[] attackHitsBuffer = new Collider[64];

    private System.Action<InputAction.CallbackContext> sprintPerformedDelegate;
    private System.Action<InputAction.CallbackContext> sprintCanceledDelegate;
    private System.Action<InputAction.CallbackContext> dashPerformedDelegate;
    private System.Action<InputAction.CallbackContext> interactPerformedDelegate;
    private System.Action<InputAction.CallbackContext> attackPerformedDelegate;
    private System.Action<InputAction.CallbackContext> lockPerformedDelegate;
    private System.Action<InputAction.CallbackContext> parryPerformedDelegate;
    private System.Action<InputAction.CallbackContext> parryCanceledDelegate;
    private System.Action<InputAction.CallbackContext> pausePerformedDelegate;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

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
            characterController = gameObject.AddComponent<CharacterController>();
            characterController.skinWidth = 0.08f;
        }

        characterController.slopeLimit = 45f;
        characterController.stepOffset = 0.3f;
    }

    private void Start()
    {
        CacheRigInitialLocalPos();
    }

    private void CacheRigInitialLocalPos()
    {
        if (xrRig == null || rigInitialPosCached) return;

        rigInitialLocalPos = new Vector3(
            xrRig.localPosition.x,
            startingRigHeightOffset,
            xrRig.localPosition.z
        );

        xrRig.localPosition = rigInitialLocalPos;
        rigInitialPosCached = true;

        if (debugLogs) Debug.Log($"[PlayerVR] Rig local pos initialisée : {rigInitialLocalPos}");
    }

    private void OnEnable()
    {
        if (inputActions == null)
        {
            var pi = GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (pi != null) inputActions = pi.actions;
        }

        if (inputActions == null) return;

        var map = inputActions.FindActionMap("Player", true);
        if (map == null)
        {
            if (debugLogs) Debug.LogWarning("PlayerControllerVR: pas de map 'Player' dans l'InputActionAsset.");
            return;
        }

        m_MoveAction = map.FindAction("Move", true);
        if (m_MoveAction != null)
        {
            m_MoveAction.performed += OnMovePerformed;
            m_MoveAction.canceled  += OnMoveCanceled;
            m_MoveAction.Enable();
        }

        m_SprintAction = map.FindAction("Sprint", false) ?? map.FindAction("Run", false);
        if (m_SprintAction != null)
        {
            sprintPerformedDelegate = _ => isSprinting = true;
            sprintCanceledDelegate  = _ => isSprinting = false;
            m_SprintAction.performed += sprintPerformedDelegate;
            m_SprintAction.canceled  += sprintCanceledDelegate;
            m_SprintAction.Enable();
        }

        m_DashAction = map.FindAction("Dash", false);
        if (m_DashAction != null)
        {
            dashPerformedDelegate = ctx => OnDash(ctx);
            m_DashAction.performed += dashPerformedDelegate;
            m_DashAction.Enable();
        }

        m_InteractAction = map.FindAction("Interact", false);
        if (m_InteractAction != null)
        {
            interactPerformedDelegate = ctx => OnInteract(ctx);
            m_InteractAction.performed += interactPerformedDelegate;
            m_InteractAction.Enable();
        }

        m_AttackAction = map.FindAction("Attack", false);
        if (m_AttackAction != null)
        {
            attackPerformedDelegate = ctx => OnAttack(ctx);
            m_AttackAction.performed += attackPerformedDelegate;
            m_AttackAction.Enable();
        }

        m_LockAction = map.FindAction("Lock", false);
        if (m_LockAction != null)
        {
            lockPerformedDelegate = ctx => OnLock(ctx);
            m_LockAction.performed += lockPerformedDelegate;
            m_LockAction.Enable();
        }

        m_ParryAction = map.FindAction("Parry", false) ?? map.FindAction("Block", false);
        if (m_ParryAction != null)
        {
            parryPerformedDelegate = ctx => OnParry(ctx);
            parryCanceledDelegate = ctx => OnParry(ctx);
            m_ParryAction.performed += parryPerformedDelegate;
            m_ParryAction.canceled += parryCanceledDelegate;
            m_ParryAction.Enable();
        }

        m_PauseAction = map.FindAction("Pause", false);
        if (m_PauseAction != null)
        {
            pausePerformedDelegate = ctx => OnPause(ctx);
            m_PauseAction.performed += pausePerformedDelegate;
            m_PauseAction.Enable();
        }

        m_RightHandPositionAction = map.FindAction("RightHandPosition", false);
        if (m_RightHandPositionAction != null)
            m_RightHandPositionAction.Enable();

        m_RightHandDeviceInitialized = false;
    }

    private void OnDisable()
    {
        if (m_MoveAction     != null) { m_MoveAction.performed -= OnMovePerformed; m_MoveAction.canceled -= OnMoveCanceled; m_MoveAction.Disable(); }
        if (m_SprintAction   != null) { if (sprintPerformedDelegate != null) m_SprintAction.performed -= sprintPerformedDelegate; if (sprintCanceledDelegate != null) m_SprintAction.canceled -= sprintCanceledDelegate; m_SprintAction.Disable(); }
        if (m_DashAction     != null) { if (dashPerformedDelegate != null) m_DashAction.performed -= dashPerformedDelegate; m_DashAction.Disable(); }
        if (m_InteractAction != null) { if (interactPerformedDelegate != null) m_InteractAction.performed -= interactPerformedDelegate; m_InteractAction.Disable(); }
        if (m_AttackAction   != null) { if (attackPerformedDelegate != null) m_AttackAction.performed -= attackPerformedDelegate; m_AttackAction.Disable(); }
        if (m_LockAction     != null) { if (lockPerformedDelegate != null) m_LockAction.performed -= lockPerformedDelegate; m_LockAction.Disable(); }
        if (m_ParryAction    != null) { if (parryPerformedDelegate != null) m_ParryAction.performed -= parryPerformedDelegate; if (parryCanceledDelegate != null) m_ParryAction.canceled -= parryCanceledDelegate; m_ParryAction.Disable(); }
        if (m_PauseAction    != null) { if (pausePerformedDelegate != null) m_PauseAction.performed -= pausePerformedDelegate; m_PauseAction.Disable(); }
        if (m_RightHandPositionAction != null) m_RightHandPositionAction.Disable();
    }

    // -------------------------------------------------------------------------
    // Update
    // -------------------------------------------------------------------------

    private void Update()
    {
        if (playerStats != null && playerStats.CurrentHealth <= 0)
        {
            Die();
            return;
        }

        if (inDialogueMode || isInteracting)
        {
            moveInput = Vector2.zero;
            animator?.SetBool("IsRunning", false);
            animator?.SetBool("IsSprinting", false);
            return;
        }

        HandleCooldowns();

        if (autoAdjustHeight)
            UpdateCharacterControllerHeight();

        if (!isDashing)
            ApplyMovement();

        // Rotation large du player root (lente, avec deadzone)
        if (autoAlignYawToHead && xrHead != null && moveInput.y > bodyAlignForwardThreshold)
            AlignRootYawWithHead();

        UpdateAnimations();
        DetectVRSwingAttack();
    }

    private void LateUpdate()
    {
        // Maintient la position locale du rig résistante au repositionnement du XR tracking
        if (xrRig != null && rigInitialPosCached)
            xrRig.localPosition = rigInitialLocalPos;

        // Aligne le mesh vers la direction de la caméra (fait face à où on regarde)
        AlignMeshToCamera();
    }

    // -------------------------------------------------------------------------
    // Alignement mesh → caméra
    // -------------------------------------------------------------------------

    /// <summary>
    /// Fait tourner le mesh (ou le player root si characterMesh == null) pour qu'il
    /// fasse face à la direction de la caméra, mais seulement quand on se déplace.
    /// Utilise RotateTowards pour une rotation fluide sans boucle.
    /// </summary>
    private void AlignMeshToCamera()
    {
        if (xrHead == null) return;

        // Ne tourne que si on bouge réellement
        Vector3 horizontalVel = characterController != null ? characterController.velocity : Vector3.zero;
        horizontalVel.y = 0f;
        if (horizontalVel.magnitude < meshRotateMinSpeed) return;

        // Direction cible = yaw de la caméra (plan XZ uniquement)
        Vector3 camForward = xrHead.forward;
        camForward.y = 0f;
        if (camForward.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(camForward, Vector3.up) * Quaternion.Euler(0f, meshYawOffset, 0f);

        Transform target = characterMesh != null ? characterMesh : transform;
        target.rotation = Quaternion.RotateTowards(target.rotation, targetRot, meshRotateSpeed * Time.deltaTime);
    }

    // -------------------------------------------------------------------------
    // Alignement root yaw → tête (broad, lent)
    // -------------------------------------------------------------------------

    private void AlignRootYawWithHead()
    {
        Vector3 headForward = xrHead.forward;
        headForward.y = 0f;
        if (headForward.sqrMagnitude < 0.0001f) return;

        float targetYaw  = Mathf.Atan2(headForward.x, headForward.z) * Mathf.Rad2Deg;
        float currentYaw = transform.eulerAngles.y;
        float delta      = Mathf.DeltaAngle(currentYaw, targetYaw);

        if (Mathf.Abs(delta) < alignYawDeadzone) return;

        float step   = alignYawSpeed * Time.deltaTime;
        float newYaw = currentYaw + Mathf.Clamp(delta, -step, step);
        transform.rotation = Quaternion.Euler(0f, newYaw, 0f);

        if (debugLogs) Debug.Log($"[PlayerVR] AlignRoot: target={targetYaw:F1} current={currentYaw:F1} delta={delta:F1}");
    }

    // -------------------------------------------------------------------------
    // Callbacks input
    // -------------------------------------------------------------------------

    private void OnMovePerformed(InputAction.CallbackContext ctx) => moveInput = ctx.ReadValue<Vector2>();
    private void OnMoveCanceled (InputAction.CallbackContext ctx) => moveInput = Vector2.zero;

    public void OnMove(InputAction.CallbackContext context)
    {
        if (inDialogueMode || isInteracting)
        {
            moveInput = Vector2.zero;
            isSprinting = false;
            animator?.SetBool("IsRunning", false);
            animator?.SetBool("IsSprinting", false);
            return;
        }

        moveInput = context.canceled ? Vector2.zero : context.ReadValue<Vector2>();
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (inDialogueMode || isInteracting || playerStats == null)
        {
            isSprinting = false;
            animator?.SetBool("IsSprinting", false);
            return;
        }

        if (context.performed)     isSprinting = playerStats.CurrentStamina > 0f;
        else if (context.canceled) isSprinting = false;
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (!context.performed || !canDash || isDashing) return;
        if (playerStats != null && playerStats.CurrentStamina < playerStats.dashStaminaCost) return;
        playerStats?.UseStamina(playerStats.dashStaminaCost);
        StartCoroutine(DashCoroutine());
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!context.performed || inDialogueMode) return;

        isInteracting = true;

        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, interactRange, interactHitsBuffer);
        for (int i = 0; i < hitCount; i++)
        {
            var hit = interactHitsBuffer[i];
            if (hit == null) continue;

            Collectible col = hit.GetComponent<Collectible>();
            if (col != null) { col.Collect(playerStats); isInteracting = false; return; }

            WeaponPickup wp = hit.GetComponent<WeaponPickup>();
            if (wp != null) { playerStats?.EquipWeapon(wp.weaponStats); Destroy(wp.gameObject); isInteracting = false; return; }
        }

        isInteracting = false;
    }

    // -------------------------------------------------------------------------
    // Mouvement & physique
    // -------------------------------------------------------------------------

    private void ApplyMovement()
    {
        if (characterController == null) return;

        verticalVelocity = characterController.isGrounded
            ? -0.5f
            : verticalVelocity - gravity * Time.deltaTime;

        Vector3 forward = xrHead != null ? xrHead.forward : transform.forward;
        Vector3 right   = xrHead != null ? xrHead.right   : transform.right;
        forward.y = 0f; forward.Normalize();
        right.y   = 0f; right.Normalize();

        Vector3 dir = forward * moveInput.y + right * moveInput.x;
        if (dir.sqrMagnitude > 0.0001f) dir.Normalize();

        float speedMult = playerStats != null ? playerStats.SpeedMultiplier : 1f;
        float speed     = (isSprinting ? runSpeed : walkSpeed) * speedMult;

        characterController.Move((dir * speed + Vector3.up * verticalVelocity) * Time.deltaTime);

        if (isSprinting && dir.magnitude > 0.1f && playerStats != null)
        {
            playerStats.UseStamina(playerStats.runningStaminaCost * Time.deltaTime);
            if (playerStats.CurrentStamina <= 0f) isSprinting = false;
        }
    }

    private void UpdateCharacterControllerHeight()
    {
        if (xrHead == null || characterController == null) return;

        Vector3 headLocal = xrHead.IsChildOf(transform)
            ? xrHead.localPosition
            : transform.InverseTransformPoint(xrHead.position);

        float headY = headLocal.y > 0f ? headLocal.y : previousHeadHeight;
        headY = Mathf.Clamp(headY, minHeight, maxHeight);
        previousHeadHeight = headY;

        float newHeight = Mathf.Clamp(headY + headHeightOffset, minHeight, maxHeight);
        characterController.height = newHeight;
        characterController.center = new Vector3(0f, newHeight * 0.5f, 0f);
    }

    private IEnumerator DashCoroutine()
    {
        isDashing = true;
        canDash   = false;
        dashCooldownTimer = dashCooldown;

        Vector3 forward = xrHead != null ? xrHead.forward : transform.forward;
        forward.y = 0f; forward.Normalize();

        Vector3 dashDir = moveInput.sqrMagnitude > 0.01f
            ? (xrHead.forward * moveInput.y + xrHead.right * moveInput.x)
            : forward;
        dashDir.y = 0f;
        if (dashDir.sqrMagnitude > 0.01f) dashDir.Normalize();
        else dashDir = forward;

        float speedMult = playerStats != null ? playerStats.SpeedMultiplier : 1f;
        float dashSpeed = dashDistance / Mathf.Max(0.0001f, dashDuration) * speedMult;
        float elapsed   = 0f;

        while (elapsed < dashDuration)
        {
            characterController.Move(dashDir * dashSpeed * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        isDashing = false;
    }

    // -------------------------------------------------------------------------
    // Cooldowns
    // -------------------------------------------------------------------------

    private void HandleCooldowns()
    {
        if (!canDash)   { dashCooldownTimer   -= Time.deltaTime; if (dashCooldownTimer   <= 0f) canDash   = true; }
        if (!canAttack) { attackCooldownTimer -= Time.deltaTime; if (attackCooldownTimer <= 0f) canAttack = true; }
    }

    // -------------------------------------------------------------------------
    // Animations & sons
    // -------------------------------------------------------------------------

    private void UpdateAnimations()
    {
        if (animator == null || characterController == null) return;

        bool grounded = characterController.isGrounded;

        // Vitesse horizontale réelle du CharacterController
        Vector3 horizontalVel = characterController.velocity;
        horizontalVel.y = 0f;
        float actualSpeed = horizontalVel.magnitude;

        // On est en train de marcher si on a un input joystick ET une vitesse réelle
        // Le seuil moveInput est abaissé à 0.01 pour capturer les petits inputs VR
        bool hasInput = moveInput.magnitude > 0.01f;
        // Note : on retire grounded de la condition "moving" pour les bools Animator —
        // isGrounded peut être instable avec la hauteur dynamique et bloquerait IsRunning/IsSprinting.
        bool moving   = actualSpeed > 0.05f && hasInput;
        isWalking = moving && !isSprinting;

        float speedMult = playerStats != null ? playerStats.SpeedMultiplier : 1f;
        float maxSpeed  = runSpeed * speedMult;

        // normalizedSpeed : 0 = immobile, ~0.5 = marche, 1 = sprint
        float normalizedSpeed = Mathf.Clamp01(actualSpeed / maxSpeed);

        animator.SetFloat("Speed", normalizedSpeed, 0.05f, Time.deltaTime);
        animator.SetBool("IsGrounded",  grounded);
        animator.SetBool("IsRunning",   isWalking);
        animator.SetBool("IsSprinting", isSprinting && moving);

        if (debugLogs)
            Debug.Log($"[PlayerVR] speed={actualSpeed:F2} norm={normalizedSpeed:F2} input={moveInput.magnitude:F2} walking={isWalking} sprinting={isSprinting}");

        if (moving && grounded)        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                footstepTimer = isSprinting ? 0.25f : 0.5f;
                if (SoundManager.Instance != null)
                {
                    if (isSprinting) SoundManager.Instance.PlayPlayerRun();
                    else             SoundManager.Instance.PlayPlayerWalk();
                }
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }

    // -------------------------------------------------------------------------
    // Attaque VR (swing)
    // -------------------------------------------------------------------------

    private void DetectVRSwingAttack()
    {
        Vector3 rightHandPos = Vector3.zero;
        bool rightValid = false;

        if (!m_RightHandDeviceInitialized || !m_RightHandDevice.isValid)
        {
            m_RightHandDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            m_RightHandDeviceInitialized = true;
        }

        if (m_RightHandDevice.isValid)
            rightValid = m_RightHandDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out rightHandPos);

        if (!rightValid && m_RightHandPositionAction != null)
        {
            rightHandPos = m_RightHandPositionAction.ReadValue<Vector3>();
            rightValid = true;
        }

        if (!prevHandPositionsValid)
        {
            if (rightValid) prevRightHandPos = rightHandPos;
            prevHandPositionsValid = rightValid;
            return;
        }

        if (rightValid)
        {
            float rightSpeed = (rightHandPos - prevRightHandPos).magnitude / Time.deltaTime;

            if (rightSpeed >= swingVelocityThreshold && canAttack && !isAttacking
                && playerStats != null && playerStats.CurrentStamina >= playerStats.attackStaminaCost)
            {
                HandleAttack(transform.TransformPoint(rightHandPos));
            }

            prevRightHandPos = rightHandPos;
        }

        prevHandPositionsValid = rightValid;
    }

    private void HandleAttack(Vector3 swingWorldPos)
    {
        if (isAttacking || !canAttack) return;

        isAttacking = true;
        canAttack   = false;

        playerStats?.UseStamina(playerStats.attackStaminaCost);

        if (animator != null)
        {
            animator.SetInteger("AttackIndex", Random.Range(1, 4));
            animator.SetTrigger("Attack");
        }

        SoundManager.Instance?.PlayPlayerAttack();

        float speedMult = playerStats != null ? playerStats.AttackSpeedMultiplier : 1f;
        attackCooldownTimer = attackCooldown / Mathf.Max(0.001f, speedMult);

        int damage = (playerStats != null && playerStats.equippedWeapon != null)
            ? playerStats.equippedWeapon.damage : 10;

        int hitCount = Physics.OverlapSphereNonAlloc(swingWorldPos, vrAttackRange, attackHitsBuffer, enemyLayer);
        for (int i = 0; i < hitCount; i++)
        {
            var hitCollider = attackHitsBuffer[i];
            if (hitCollider == null || !hitCollider.CompareTag("Enemy")) continue;

            Vector3 hitDir = (hitCollider.transform.position - swingWorldPos).normalized;

            EnemyController enemy = hitCollider.GetComponent<EnemyController>();
            if (enemy != null) { enemy.TakeDamage(damage, hitDir, 2f); continue; }

            MeleeEnemyController meleeEnemy = hitCollider.GetComponent<MeleeEnemyController>();
            meleeEnemy?.TakeDamage(damage, hitDir, 2f);
        }

        StartCoroutine(ResetAttackAfterCooldown());
    }

    private IEnumerator ResetAttackAfterCooldown()
    {
        yield return new WaitForSeconds(attackDuration);
        isAttacking = false;
    }

    public void ResetAttack() => isAttacking = false;

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (inDialogueMode || isInteracting || !context.performed || !canAttack) return;
        if (playerStats == null || playerStats.CurrentStamina < playerStats.attackStaminaCost) return;

        Vector3 attackOrigin = xrHead != null ? xrHead.position : transform.position;
        HandleAttack(attackOrigin);
    }

    public void OnLock(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        ToggleLock();
    }

    public void OnParry(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            isParrying = true;
            parryStartTime = Time.time;
            animator?.SetBool("IsBlocking", true);
        }
        else if (context.canceled)
        {
            isParrying = false;
            animator?.SetBool("IsBlocking", false);
            SoundManager.Instance?.PlayPlayerParry();
        }
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (!SceneManager.GetSceneByName("PauseMenu").isLoaded)
            SceneManager.LoadScene("PauseMenu", LoadSceneMode.Additive);
    }

    public void SetDialogueMode(bool isInDialogue)
    {
        inDialogueMode = isInDialogue;
        if (inDialogueMode)
        {
            moveInput = Vector2.zero;
            isSprinting = false;
            animator?.SetBool("IsRunning", false);
            animator?.SetBool("IsSprinting", false);
        }
    }

    private void ToggleLock()
    {
        if (!isLockedOn)
        {
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, lockRange, attackHitsBuffer, enemyLayer);
            float closestDistanceToCenter = Mathf.Infinity;
            Transform closestEnemy = null;

            Camera cam = Camera.main;
            if (cam == null) return;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = attackHitsBuffer[i];
                if (hitCollider == null) continue;

                Vector3 viewportPoint = cam.WorldToViewportPoint(hitCollider.transform.position);
                float distanceToCenter = Vector2.Distance(new Vector2(0.5f, 0.5f), new Vector2(viewportPoint.x, viewportPoint.y));

                if (distanceToCenter < closestDistanceToCenter && viewportPoint.z > 0f)
                {
                    closestDistanceToCenter = distanceToCenter;
                    closestEnemy = hitCollider.transform;
                }
            }

            if (closestEnemy != null)
            {
                currentTarget = closestEnemy;
                isLockedOn = true;
            }
        }
        else
        {
            isLockedOn = false;
            currentTarget = null;
        }
    }

    // -------------------------------------------------------------------------
    // Dégâts & mort
    // -------------------------------------------------------------------------

    public void TakeDamage(int damage)
    {
        if (parryBlockedDamage)
        {
            parryBlockedDamage = false;
            return;
        }

        if (playerStats == null) return;
        playerStats.TakeDamage(damage);

        if (playerStats.CurrentHealth > 0)
        {
            SoundManager.Instance?.PlayPlayerHurt();
            animator?.SetTrigger("GetHit");
            GetComponent<HitFeedback>()?.PlayHitFeedback();
        }
        else
        {
            Die();
        }
    }

    private void Die()
    {
        animator?.SetTrigger("IsDead");
        SoundManager.Instance?.PlayPlayerDeath();

        SaveData saveData = SaveManager.LoadGame();
        playerStats?.SavePlayerStats(saveData);
        SaveManager.SaveGame(saveData);

        this.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isParrying && other.CompareTag("EnemyWeapon"))
        {
            float parryTiming = Time.time - parryStartTime;
            if (parryTiming <= parryWindow)
            {
                animator?.SetTrigger("Parry");
                StunEnemy(other.GetComponentInParent<EnemyController>());
                StunEnemy(other.GetComponentInParent<MeleeEnemyController>());
            }

            parryBlockedDamage = true;
        }

        if (!other.CompareTag("EnemyWeapon")) return;

        int damage = 10;
        EnemyController enemy    = other.GetComponentInParent<EnemyController>();
        MeleeEnemyController mel = other.GetComponentInParent<MeleeEnemyController>();
        if (enemy != null) damage = enemy.damage;
        else if (mel != null) damage = mel.damage;

        TakeDamage(damage);
    }

    private void StunEnemy(EnemyController enemy)
    {
        if (enemy != null) enemy.Stun(2f);
    }

    private void StunEnemy(MeleeEnemyController enemy)
    {
        if (enemy != null) enemy.Stun(2f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}