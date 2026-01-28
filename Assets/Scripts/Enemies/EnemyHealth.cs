using Chromatic.Combat;
using UnityEngine;

public class EnemyHealth : Health
{

    private void OnCollisionEnter2D(Collision2D collision) // Changed from OnTriggerEnter2D
    {
        if (!collision.gameObject.CompareTag("Bullet")) return;

        float damage = 5f;
        Projectile bullet = collision.gameObject.GetComponent<Projectile>();
        if (bullet) damage = bullet.Damage;

        TakeDamage(damage);
    }


    protected override void Die()
    {
        base.Die();
        Destroy(gameObject);
    }
}
