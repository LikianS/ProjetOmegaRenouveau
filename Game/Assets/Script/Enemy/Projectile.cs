using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f; 
    public int damage = 10; 
    public float gravity = -9.8f; 

    private Vector3 velocity; 
    private bool isInitialized = false;

    public void Initialize(Vector3 target, float launchSpeed)
    {
        Vector3 direction = target - transform.position;
        float horizontalDistance = new Vector3(direction.x, 0, direction.z).magnitude;
        float verticalDistance = direction.y;

        float timeToTarget = horizontalDistance / launchSpeed;
        float verticalVelocity = (verticalDistance - 0.5f * gravity * timeToTarget * timeToTarget) / timeToTarget;

        Vector3 horizontalVelocity = new Vector3(direction.x, 0, direction.z).normalized * launchSpeed;

        velocity = horizontalVelocity + Vector3.up * verticalVelocity;
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized) return;

        velocity += Vector3.up * gravity * Time.deltaTime;

        transform.position += velocity * Time.deltaTime;

        transform.rotation = Quaternion.LookRotation(velocity);

        if (transform.position.y < 0)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(damage);
            }

            Destroy(gameObject);
        }
    }
}
