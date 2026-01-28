using UnityEngine;

namespace Chromatic.Combat
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float speed = 10f;
        [SerializeField] private float lifeTime = 3f; 
        public float Damage { get; private set; } = 10f;

        private Rigidbody2D rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            rb.linearVelocity = transform.right * speed;
            rb.gravityScale = 0f; 

            Destroy(gameObject, lifeTime);
        }

        private void OnTriggerEnter2D(Collider2D hitInfo)
        {
            IInteractiveTarget target = hitInfo.GetComponent<IInteractiveTarget>();
            if (target != null)
            {
                target.OnHit(Damage);
            }

            EnemyHealth enemy = hitInfo.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(Damage); 
            }

            Destroy(gameObject);
        }
    }
}