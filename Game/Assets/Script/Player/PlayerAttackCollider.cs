using UnityEngine;

public class PlayerAttackCollider : MonoBehaviour
{
    public PlayerController playerController;

    private void Awake()
    {
        playerController = GetComponentInParent<PlayerController>();
    }

    private void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag("Enemy"))
        {
            EnemyController enemy = other.GetComponent<EnemyController>();
            MeleeEnemyController meleeEnemy = other.GetComponent<MeleeEnemyController>();

            if (playerController != null)
            {
                int damage = playerController.playerStats.equippedWeapon.damage;

                if (enemy != null)
                {
                    enemy.TakeDamage(damage, (enemy.transform.position - playerController.transform.position).normalized, 2f);

                }
                else if (meleeEnemy != null)
                {
                    meleeEnemy.TakeDamage(damage, (meleeEnemy.transform.position - playerController.transform.position).normalized, 2f);

                }
            }
        }
    }
}
