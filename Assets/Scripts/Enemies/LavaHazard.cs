using UnityEngine;

public class LavaHazard : MonoBehaviour
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private float playerDamage = 1f;
    [SerializeField] private float lifetime = 3f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
        gameObject.tag = "Lava";
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        ApplyDamage(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        ApplyDamage(collision);
    }

    private void ApplyDamage(Collider2D collision)
    {
        if (collision.GetComponentInParent<RedWardenBoss>() != null)
        {
            return;
        }

        PlayerHealth player = collision.GetComponentInParent<PlayerHealth>();
        if (player != null)
        {
            player.TakeHazardDamage(playerDamage);
            return;
        }

        Health health = collision.GetComponentInParent<Health>();
        if (health != null)
        {
            health.TakeDamage(damage);
        }

        Vine vine = collision.GetComponentInParent<Vine>();
        if (vine != null)
        {
            vine.OnHit(999f, Color.red);
        }
    }
}