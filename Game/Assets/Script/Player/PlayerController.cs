using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;
using System;
using UnityEngine.SceneManagement;
using System.Transactions;
using UnityEngine.XR;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public CharacterController characterController;
    public Transform cameraTarget;
    public CinemachineFreeLook freeLookCamera;

    [Header("Movement Settings")]
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    public float rotationSpeed = 10f;
    public float dashDistance = 5f;
    public float dashDuration = 0.3f;
    public float dashCooldown = 1f;
    public float gravity = 9.8f;
    public float slopeLimit = 45f;

    [Header("Attack Settings")]
    public float attackDuration = 0.5f;
    public float attackCooldown = 0.5f;
    public float vrAttackVelocityThreshold = 2.0f; // Seuil de vitesse pour l'attaque VR

    [Header("Enemy Lock")]
    public float lockRange = 10f;
    public LayerMask enemyLayer;
    public Transform currentTarget;

    public PlayerStats playerStats;
    private Vector2 moveInput;
    private bool isRunning;
    private bool isSprinting;
    private bool isDashing;
    private bool canDash = true;
    private bool canAttack = true;
    private bool isLockedOn;

    private Vector3 moveDirection;
    private float dashCooldownTimer;
    private float attackCooldownTimer;
    private float verticalVelocity;
    private Transform mainCameraTransform;
    public bool inDialogueMode = false;
    private bool isParrying = false;
    private float parryWindow = 0.2f;
    private float parryStartTime;
    private bool parryBlockedDamage = false;

    private bool isAttacking = false;
    private AudioSource audioSource;
    public AudioClip attackSound;
    private Collectible interactableItem;
    private bool isInteracting = false;

    private float footstepTimer = 0f;
    private float footstepInterval = 0.4f;




    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        // Fallback if no main camera is tagged in the scene
        mainCameraTransform = (Camera.main != null) ? Camera.main.transform : transform;
        audioSource = GetComponent<AudioSource>();

    }

    private void Update()
    {
        if (playerStats.CurrentHealth <= 0)
        {
            Die();
            return;
        }
        // Safe check: handle cases with no gamepad connected
        if (Gamepad.current == null || !Gamepad.current.leftStickButton.isPressed)
        {
            isRunning = false;
        }
        if (inDialogueMode || isInteracting)
        {
            moveInput = Vector2.zero;
            animator.SetBool("IsRunning", false);
            animator.SetBool("IsSprinting", false);
            return;
        }
        if ((Gamepad.current == null || !Gamepad.current.leftStickButton.isPressed) && isSprinting)
        {
            isSprinting = false;
            animator.SetBool("IsSprinting", isSprinting);
        }
        if ((Gamepad.current == null || !Gamepad.current.buttonEast.isPressed) && isParrying)
        {
            isParrying = false;
            animator.SetBool("IsBlocking", isParrying);
        }

        bool isMoving = moveInput.magnitude > 0.1f;

        if (isMoving)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                if (isSprinting)
                {
                    SoundManager.Instance.PlayPlayerRun();
                    footstepInterval = 0.25f;
                }
                else
                {
                    SoundManager.Instance.PlayPlayerWalk();
                    footstepInterval = 0.5f;
                }
                footstepTimer = footstepInterval;
            }
        }
        else
        {
            footstepTimer = 0f;
        }

        HandleCooldowns();
        HandleMovement();
    }
    public void PlayAttackSound()
    {
        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound);
        }
    }
    public void ResetAttack()
    {
        isAttacking = false;
    }

    public void SetDialogueMode(bool isInDialogue)
    {
        inDialogueMode = isInDialogue;
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

    private void HandleCooldowns()
    {
        if (!canDash)
        {
            dashCooldownTimer -= Time.deltaTime;
            if (dashCooldownTimer <= 0)
            {
                canDash = true;
            }
        }

        if (!canAttack)
        {
            attackCooldownTimer -= Time.deltaTime;
            if (attackCooldownTimer <= 0)
            {
                canAttack = true;
            }
        }
    }

    private void HandleMovement()
    {
        if (isDashing)
            return;

        if (characterController.isGrounded)
        {
            verticalVelocity = -0.5f;
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        Vector3 forward = mainCameraTransform.forward;
        Vector3 right = mainCameraTransform.right;

        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        moveDirection = forward * moveInput.y + right * moveInput.x;

        if (moveDirection.magnitude > 0.1f)
        {
            moveDirection.Normalize();

            if (isLockedOn && currentTarget != null)
            {
                Vector3 lookDirection = currentTarget.position - transform.position;
                lookDirection.y = 0;

                if (lookDirection.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
            }
            else
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        float currentSpeed = isRunning ? runSpeed * playerStats.SpeedMultiplier : walkSpeed * playerStats.SpeedMultiplier;

        Vector3 horizontalMovement = moveDirection * currentSpeed;

        Vector3 verticalMovement = new Vector3(0, verticalVelocity, 0);

        characterController.Move((horizontalMovement + verticalMovement) * Time.deltaTime);

        if (isRunning && moveDirection.magnitude > 0.1f)
        {
            playerStats.UseStamina(playerStats.runningStaminaCost * Time.deltaTime);
            if (playerStats.CurrentStamina <= 0)
            {
                isRunning = false;
                isSprinting = false;
                animator.SetBool("IsRunning", isRunning);
                animator.SetBool("IsSprinting", isSprinting);
            }
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (ShopManager.Instance.shopPanel.activeSelf || inDialogueMode || context.ReadValue<Vector2>().magnitude == 0)
        {
            moveInput = Vector2.zero;
            isRunning = false;
            animator.SetBool("IsRunning", isRunning);
            return;
        }

        moveInput = context.ReadValue<Vector2>();
        isRunning = true;
        animator.SetBool("IsRunning", isRunning);
    }

    public void OnRun(InputAction.CallbackContext context)
    {
        if (ShopManager.Instance.shopPanel.activeSelf || inDialogueMode)
        {
            isSprinting = false;
            animator.SetBool("IsSprinting", isSprinting);
            return;
        }

        isSprinting = context.performed && playerStats.CurrentStamina > 0;
        animator.SetBool("IsSprinting", isSprinting);
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.performed && canDash && playerStats.CurrentStamina >= playerStats.dashStaminaCost)
        {
            SoundManager.Instance.PlayPlayerDash();
            StartCoroutine(DashCoroutine());
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (inDialogueMode || isInteracting)
        {
            moveInput = Vector2.zero;
            animator.SetBool("IsRunning", false);
            animator.SetBool("IsSprinting", false);
            return;
        }

        if (context.performed && canAttack && playerStats.CurrentStamina >= playerStats.attackStaminaCost)
        {
            HandleAttack();
        }
    }

    private void HandleAttack()
    {
        if (isInteracting || inDialogueMode)
            return;
        if (isAttacking)
            return;

        isAttacking = true;
        canAttack = false;

        playerStats.UseStamina(playerStats.attackStaminaCost);

        int randomAttackIndex = UnityEngine.Random.Range(1, 4);
        SoundManager.Instance.PlayPlayerAttack();

        animator.SetInteger("AttackIndex", randomAttackIndex);
        animator.SetTrigger("Attack");

        attackCooldownTimer = attackCooldown / playerStats.AttackSpeedMultiplier;
    }

    public void OnLock(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            ToggleLock();
        }
    }

    private IEnumerator DashCoroutine()
    {
        isDashing = true;
        canDash = false;
        dashCooldownTimer = dashCooldown / playerStats.SpeedMultiplier;

        playerStats.UseStamina(playerStats.dashStaminaCost);

        Vector3 dashDirection = moveDirection.magnitude > 0.1f ? moveDirection : transform.forward;

        float dashDistanceWithStats = dashDistance * playerStats.SpeedMultiplier;

        float startTime = Time.time;

        while (Time.time < startTime + dashDuration)
        {
            characterController.Move(dashDirection * dashDistanceWithStats * Time.deltaTime / dashDuration);
            yield return null;
        }

        isDashing = false;
    }

    private void ToggleLock()
    {
        if (!isLockedOn)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, lockRange, enemyLayer);

            float closestDistanceToCenter = Mathf.Infinity;
            Transform closestEnemy = null;

            foreach (var hitCollider in hitColliders)
            {
                Vector3 viewportPoint = Camera.main.WorldToViewportPoint(hitCollider.transform.position);
                float distanceToCenter = Vector2.Distance(new Vector2(0.5f, 0.5f), new Vector2(viewportPoint.x, viewportPoint.y));

                if (distanceToCenter < closestDistanceToCenter && viewportPoint.z > 0)
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

    public void TakeDamage(int damage)
    {
        if (parryBlockedDamage)
        {
            parryBlockedDamage = false;
            return;
        }

        playerStats.TakeDamage(damage);

        if (playerStats.CurrentHealth > 0)
        {
            SoundManager.Instance.PlayPlayerHurt();
            animator.SetTrigger("GetHit");
        }

        GetComponent<HitFeedback>().PlayHitFeedback();
    }

    private void Die()
    {
        if (animator != null)
        {
            animator.SetTrigger("IsDead");
        }
        SoundManager.Instance.PlayPlayerDeath();
        SaveData saveData = SaveManager.LoadGame();
        playerStats.SavePlayerStats(saveData);
        SaveManager.SaveGame(saveData);
        ScreenFader.FadeAndLoadScene("House Interior", 1f);
        this.enabled = false;
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (ShopManager.Instance.shopPanel.activeSelf)
        {
            if (context.performed)
            {
                ShopManager.Instance.CloseShop();

                GameManager.Instance.SaveGame();
            }
            return;
        }

        if (inDialogueMode)
        {
            return;
        }

        if (context.performed)
        {
            isInteracting = true;
            if (interactableItem != null)
            {
                interactableItem.Collect(playerStats);
                interactableItem = null;
            }
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, 2f);
            foreach (var hitCollider in hitColliders)
            {
                WeaponPickup weaponPickup = hitCollider.GetComponent<WeaponPickup>();
                if (weaponPickup != null)
                {
                    playerStats.EquipWeapon(weaponPickup.weaponStats);
                    Destroy(weaponPickup.gameObject);
                    break;
                }
            }
            isInteracting = false;
        }
    }

    public void OnParry(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            isParrying = true;
            parryStartTime = Time.time;
            animator.SetBool("IsBlocking", true);
        }
        else
        {
            animator.SetBool("IsBlocking", false);
            SoundManager.Instance.PlayPlayerParry();

            isParrying = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isParrying && other.CompareTag("EnemyWeapon"))
        {
            float parryTiming = Time.time - parryStartTime;

            if (parryTiming <= parryWindow)
            {
                animator.SetTrigger("Parry");
                StunEnemy(other.GetComponentInParent<EnemyController>());
                StunEnemy(other.GetComponentInParent<MeleeEnemyController>());
            }
            parryBlockedDamage = true;
        }
    }
    public void OnPause(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (!SceneManager.GetSceneByName("PauseMenu").isLoaded)
            {
                SceneManager.LoadScene("PauseMenu", LoadSceneMode.Additive);
            }
        }
    }

    private void StunEnemy(EnemyController enemy)
    {
        if (enemy != null)
        {
            enemy.Stun(2f);
        }
    }

    private void StunEnemy(MeleeEnemyController enemy)
    {
        if (enemy != null)
        {
            enemy.Stun(2f);
        }
    }
    public void SetInteractableItem(Collectible item)
    {
        interactableItem = item;
    }

    public void ClearInteractableItem(Collectible item)
    {
        if (interactableItem == item)
        {
            interactableItem = null;
        }
    }
}