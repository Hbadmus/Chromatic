using UnityEngine;

public class PlayerHealth : Health
{
    [SerializeField] private float damage = 10f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            TakeDamage(damage);
        }
    }

    protected override void Die()
    {
        base.Die();
        Debug.Log("Player died");
        Destroy(gameObject);
    }
}
