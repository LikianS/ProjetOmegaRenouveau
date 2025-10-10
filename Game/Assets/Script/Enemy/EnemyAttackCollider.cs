using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttackCollider : MonoBehaviour
{
    public EnemyController enemyController;
    public MeleeEnemyController meleeEnemyController;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                if (enemyController != null)
                {
                    player.TakeDamage(enemyController.damage);

                }
                if (meleeEnemyController != null)
                {
                    player.TakeDamage(meleeEnemyController.damage);
                }
            }
        }
    }
}