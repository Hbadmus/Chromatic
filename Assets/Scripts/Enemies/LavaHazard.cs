using UnityEngine;

public class LavaHazard : MonoBehaviour
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private float playerDamage = 0.5f;
    [SerializeField] private float lifetime = 3f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
        gameObject.tag = "Lava";
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<RedWardenBoss>() != null)
        {
            return;
        }

        if (collision.CompareTag("Player"))
        {
            PlayerHealth player = collision.GetComponent<PlayerHealth>();
            if (player != null)
            {
                player.TakeHazardDamage(playerDamage);
            }
            return;
        }

        Health health = collision.GetComponent<Health>();
        if (health != null)
        {
            health.TakeDamage(damage);
        }

        Vine vine = collision.GetComponent<Vine>();
        if (vine != null)
        {
            vine.OnHit(999f, Color.red);
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.GetComponent<RedWardenBoss>() != null)
        {
            return;
        }

        if (collision.CompareTag("Player"))
        {
            PlayerHealth player = collision.GetComponent<PlayerHealth>();
            if (player != null)
            {
                player.TakeHazardDamage(playerDamage);
            }
        }
    }
}