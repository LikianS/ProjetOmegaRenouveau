using UnityEngine;

public class PlayerAttackCollider : MonoBehaviour
{
    public PlayerController playerController;
    public PlayerControllerVR playerControllerVR;

    private void Awake()
    {
        playerController = GetComponentInParent<PlayerController>();
        playerControllerVR = GetComponentInParent<PlayerControllerVR>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            EnemyController enemy = other.GetComponent<EnemyController>();
            MeleeEnemyController meleeEnemy = other.GetComponent<MeleeEnemyController>();

            PlayerStats stats = null;
            Transform attackerTransform = null;

            if (playerController != null)
            {
                stats = playerController.playerStats;
                attackerTransform = playerController.transform;
            }
            else if (playerControllerVR != null)
            {
                stats = playerControllerVR.playerStats;
                attackerTransform = playerControllerVR.transform;
            }

            if (stats != null && stats.equippedWeapon != null)
            {
                int damage = stats.equippedWeapon.damage;
                Vector3 hitDir = attackerTransform != null
                    ? (other.transform.position - attackerTransform.position).normalized
                    : Vector3.zero;

                if (enemy != null)
                {
                    enemy.TakeDamage(damage, hitDir, 2f);
                }
                else if (meleeEnemy != null)
                {
                    meleeEnemy.TakeDamage(damage, hitDir, 2f);
                }
            }
        }
    }
}
