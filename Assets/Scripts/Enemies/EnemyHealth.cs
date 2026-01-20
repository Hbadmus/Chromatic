using Chromatic.Combat;
using UnityEngine;

public class EnemyHealth : Health
{
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Bullet")) return;

        float damage = 5f;
        Projectile bullet = collision.GetComponent<Projectile>();
        if (bullet) damage = bullet.Damage;

        TakeDamage(damage);
    }
    

    protected override void Die()
    {
        base.Die();
        Destroy(gameObject);
    }
}
